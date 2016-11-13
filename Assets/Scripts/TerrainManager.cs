using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour, IControllable
{
    [System.Serializable]
    public class SeasonalData
    {
        public Color GrassHue;
        public Color[] TreeHues;
    }

    public class TreeData
    {
        public GameObject Obj;
        public Material[,] LeafMaterials;
        public int TurnStartDay;

        public int DaysToTurn;
        public Color StartColor;
        public Color ColorToReach;
        public float StartAlpha;
        public float AlphaToReach;

        public TreeData()
        {
            // three LODs, 2 leaf materials per LOD
            //LeafMaterials = new Material[3, 2];
            LeafMaterials = new Material[1, 2]; // just do LOD 0 for now
        }
    }

    public Action OnWorldCreated;
    public Action<bool> OnInteractModeChanged;
    public Action<int> OnTerrainModified;

    [SerializeField] private GameObject _terrainPrefab;
    [SerializeField] private GameObject _treePrefab;
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private GameObject _waterPrefab;
    [SerializeField] private GameObject _heightMarkerPrefab;
    [SerializeField] private GameObject _heightDraggerPrefab;
    [SerializeField] private Color _draggedValidColor;
    [SerializeField] private Color _draggedInvalidColor;
    [SerializeField] private SeasonalData[] _seasonalData;

    private ModificationManager _modifications;
    private Terrain _terrain;
    private List<TreeData> _treeData;
    private int _worldSeed;
    private float _minDetailHeight = 1f;
    private float _maxDetailHeight = 8f;
    private bool _interactMode = false;
    private float _customHeight;
    private bool _customHeightSet = false;
    private GameObject _customHeightMarker = null;
    private Vector3 _firstPoint;
    private bool _firstPointSet = false;
    private GameObject _heightDragger = null;
    private MeshRenderer _draggerRenderer;
    private int _currentActionIndex = 0;
    private int _modifingCost = 0;

    public int WorldSeed { get { return _worldSeed; } }
    public bool InteractMode { get { return _interactMode; } }
    public int MapSize { get { return _terrain.terrainData.alphamapWidth; } }

    void Awake()
    {
        GameManager.Instance.TerrainManager = this;
    }

    void Start()
    {
        OnTerrainModified += UIController.Instance.OnTerrainModified;
        GameManager.Instance.TimeManager.OnTimeChanged += OnTimeChanged;

        UIController.Instance.OnUndo += UndoLastModify;
        UIController.Instance.OnTerrainModifyModeChanged += ToggleInteractMode;

        _modifications = ModificationManager.Instance;
    }

    private void ToggleInteractMode(bool modifying)
    {
        _interactMode = modifying;

        if (_interactMode)
        {
            GameManager.Instance.CameraController.OnPositionClick += OnTerrainClick;
            GameManager.Instance.CameraController.OnCancelClick += OnTerrainCancel;

            if (_customHeightMarker == null)
            {
                _customHeightMarker = (GameObject)Instantiate(_heightMarkerPrefab);
                _customHeightMarker.SetActive(false);
            }

            if (_heightDragger == null)
            {
                _heightDragger = (GameObject)Instantiate(_heightDraggerPrefab);
                _draggerRenderer = _heightDragger.transform.GetChild(0).GetComponent<MeshRenderer>();
                _heightDragger.SetActive(false);
            }

            GameManager.Instance.InputController.SetControllable(this, ControllableType.Key);
        }
        else
        {
            GameManager.Instance.CameraController.OnPositionClick -= OnTerrainClick;
            GameManager.Instance.CameraController.OnCancelClick -= OnTerrainCancel;

            GameManager.Instance.InputController.SetControllable(null, ControllableType.Key);
            _customHeightMarker.SetActive(false);
        }

        if (OnInteractModeChanged != null)
        {
            OnInteractModeChanged(_interactMode);
        }
    }

    private void UndoLastModify()
    {
        int count = _modifications.RemainingActions;
        if (count > 0)
        {
            ModificationAction lastAction = _modifications.RetreiveAction(_currentActionIndex - 1);
            ModifyHeightsFromAction(lastAction);
        }
    }

    // initalize
    public void LoadMap(int seed)
	{
        GameObject terrainObj = (GameObject)Instantiate(_terrainPrefab, Vector3.zero, Quaternion.identity);
        _terrain = terrainObj.GetComponent<Terrain>();

		TerrainData data = _terrain.terrainData;

        _worldSeed = seed;
        data = CreateMultiLevelTerrain(_worldSeed, ref data);

        UpdateSeasonalColors(GameManager.Instance.TimeManager.CurrentDate, true);

        Vector3 newPos = new Vector3(-data.heightmapWidth / 2f, 0f, -data.heightmapWidth / 2f);
        terrainObj.transform.position = newPos;

        GameObject.Instantiate(_waterPrefab, newPos, Quaternion.identity);

        if (OnWorldCreated != null)
        {
            OnWorldCreated();
        }
	}

    #region Terrain Generation
    // create and merge some noise to create interesting terrain height map
    private TerrainData CreateMultiLevelTerrain(int seed, ref TerrainData data)
    {
        System.Random noiseSeedGen = new System.Random(seed);
        PerlinGenerator generator = new PerlinGenerator();

        int terrainSize = data.heightmapWidth;
        int baseOctaves = 5; // regular plains
        int highOctaves = 8; // mountains
        int lowOctaves = 8; // lake
        float[][] baseHeights = generator.Generate(noiseSeedGen.Next(), terrainSize, baseOctaves);
        float[][] highHeights = generator.Generate(noiseSeedGen.Next(), terrainSize, highOctaves);
        float[][] lowHeights = generator.Generate(noiseSeedGen.Next(), terrainSize, lowOctaves);

        int detailOctaves = 1; // grass
        int treeOctaves = 4; // trees
        float[][] detailNoise = generator.Generate(noiseSeedGen.Next(), terrainSize, detailOctaves);
        float[][] treeNoise = generator.Generate(noiseSeedGen.Next(), terrainSize, treeOctaves);

        baseHeights = BlendHeights(baseHeights, highHeights, lowHeights);
        AssignTextures(ref data, baseHeights);
        PlaceDetails(ref data, detailNoise);
        PlaceTrees(ref data, noiseSeedGen.Next(), terrainSize, treeNoise);
        data.SetHeights(0, 0, ConvertTo2D(baseHeights));

        return data;
    }

    private void PlaceTrees(ref TerrainData data, int seed, int size, float[][] treeNoise)
    {
        _treeData = new List<TreeData>();
        GameObject currentTree = null;
        System.Random treeGen = new System.Random(seed);
        float groundHeight;
        float treeOffsetMin = 0.75f;
        float treeOffsetMax = 1.25f;
        float treeThreshold = 0.925f;
        float terrainOffset = data.alphamapWidth / 2f;
        Vector3 rotation = Vector3.zero;
        Vector3 scaleY = Vector3.one;
        Vector3 scaleXZ = Vector3.one;

        for (int i = 0; i < size; ++i)
        {
            for (int j = 0; j < size; ++j)
            {
                if (treeNoise[i][j] >= treeThreshold)
                {
                    groundHeight = data.GetHeight(i, j);
                    if (groundHeight > _minDetailHeight && groundHeight < _maxDetailHeight)
                    {
                        scaleY.y = (float)(treeGen.NextDouble() * (treeOffsetMax - treeOffsetMin)) + treeOffsetMin;
                        scaleXZ.x = scaleXZ.z = (float)(treeGen.NextDouble() * (treeOffsetMax - treeOffsetMin)) + treeOffsetMin;
                        rotation.y = treeGen.Next(360);
                        currentTree = (GameObject)Instantiate(_treePrefab, new Vector3(i - terrainOffset, groundHeight, j - terrainOffset), Quaternion.Euler(rotation));

                        _treeData.Add(GenerateTreeData(currentTree));
                    }
                }
            }
        }
    }

    private TreeData GenerateTreeData(GameObject treeObject)
    {
        TreeData d = new TreeData();
        d.Obj = treeObject;
        MeshRenderer lod0 = treeObject.transform.FindChild("Broadleaf_Desktop_LOD0").GetComponent<MeshRenderer>();

        // copy and assign new materials so that they can be colored individually
        Material leavesA = new Material(lod0.materials[2]);
        Material leavesB = new Material(lod0.materials[4]);
        lod0.materials[2] = leavesA;
        lod0.materials[4] = leavesB;

        d.LeafMaterials[0, 0] = lod0.materials[2];
        d.LeafMaterials[0, 1] = lod0.materials[4];

        UpdateTreeData(d);
        return d;
    }

    private void UpdateTreeData(TreeData d)
    {
        // need to start the turn of a tree around 10 days +- the start of the next season
        int plusMinus = UnityEngine.Random.Range(-10, 10);
        int turnTime = UnityEngine.Random.Range(5, 10);
        if (plusMinus < 0)
        {
            d.TurnStartDay = TimeConstants.DAYS_PER_MONTH + plusMinus;
        }
        else
        {
            d.TurnStartDay = plusMinus;
        }
        d.DaysToTurn = turnTime;

        // colors
        int currentSeason = (int)GameManager.Instance.TimeManager.CurrentDate.GetSeason();
        SeasonalData current = _seasonalData[currentSeason];
        int rand = UnityEngine.Random.Range(0, current.TreeHues.Length);
        d.StartColor = current.TreeHues[rand];

        int nextSeason = currentSeason + 1;
        SeasonalData nextData = _seasonalData[nextSeason];
        rand = UnityEngine.Random.Range(0, nextData.TreeHues.Length);
        d.ColorToReach = nextData.TreeHues[rand];

        // alpha to reach should always be 0 unless the next season is spring
        d.StartAlpha = 1;
        d.AlphaToReach = 0f;
        if (nextSeason == (int)Season.Spring)
        {
            d.StartAlpha = 0f;
            d.AlphaToReach = 1f;
        }
    }

    private void PlaceDetails(ref TerrainData data, float[][] detailNoise)
    {
        float detailThreshold = 0.5f;
        int numberDetails = data.detailPrototypes.Length;
        int detailSize = data.detailWidth; // should be the same as the heights size
        int[,] detailData;
        float height;

        for (int k = 0; k < numberDetails; ++k)
        {
            detailData = data.GetDetailLayer(0, 0, detailSize, detailSize, k);
            for (int i = 0; i < detailSize; ++i)
            {
                for (int j = 0; j < detailSize; ++j)
                {
                    height = data.GetHeight(i, j);
                    float detailValue = detailNoise[i][j];

                    if (height > _minDetailHeight && height < _maxDetailHeight)
                    {
                        detailData[i, j] = 0;
                        if (detailValue >= detailThreshold)
                        {
                            detailData[i, j] = 1;
                        }
                    }
                }
            }

            data.SetDetailLayer(0, 0, k, detailData);
        }
    }

    // take the relevent portions of the high and low and add them into the base heights
    private float[][] BlendHeights(float[][] baseH, float[][] highH, float[][] lowH)
    {
        float minHeight = 0.3f;
        float maxHeight = 0.7f;
        for (int i = 0; i < baseH.Length; ++i)
        {
            for (int j = 0; j < baseH.Length; ++j)
            {
                baseH[i][j] += 0.1f;
                if (highH[i][j] >= maxHeight)
                {
                    baseH[i][j] *= 1.25f;
                }
                else if (lowH[i][j] <= minHeight)
                {
                    baseH[i][j] *= 0.25f;
                }
            }
        }

        return baseH;
    }

	// assign textures based on height
	private void AssignTextures(ref TerrainData data, float[][] heightMap)
	{
		// get current texture painting
		float[,,] splatmapData = new float[data.alphamapWidth, data.alphamapHeight, data.alphamapLayers];

		// validate size
		if (heightMap.Length - 1 != data.alphamapWidth)
		{
			Debug.LogWarning("terrain data and heights arrays don't have the same size");
			Debug.LogWarning("heights: " + heightMap.Length + ", data: " + data.alphamapWidth);
		}

		for (int i = 0; i < heightMap.Length - 1; i++)
		{
			for (int j = 0; j < heightMap[0].Length - 1; j++)
			{
                // read the height at this location
                float height = heightMap[j][i];//data.GetHeight(i, j);

                // the last index for splatmapData is the number of the texture on the terrain prefab
                // the value that that index is being set to seems to be regular alpha
                // set it based on terrain height
                if (height <= 0.4f)
                    splatmapData[j, i, 3] = 1; // mud
                else if (height <= 0.6f)
                    splatmapData[j, i, 0] = 1; // dirt with grass bits
                else if (height <= 0.8f)
                    splatmapData[j, i, 1] = 1; // grass
                else
                    splatmapData[j, i, 2] = 1; // rock

            }
		}

		// apply the new alpha
		data.SetAlphamaps(0, 0, splatmapData);
	}
    #endregion

    private void OnTerrainClick(int layer, Vector3 terrainPos, GameObject obj)
    {
        if (layer == LayerMask.NameToLayer("Terrain"))
        {
            Vector3 objPos = SnapToGrid(terrainPos);
            if (_interactMode)
            {
                if (_customHeightSet)
                {
                    _customHeightMarker.transform.position = objPos;
                    _customHeightMarker.SetActive(true);

                    Vector3 terrainLocal = GetTerrainRelativePosition(objPos);
                    _customHeight = _terrain.terrainData.GetHeight((int)terrainLocal.x, (int)terrainLocal.z) / 10f; // height of terrain
                }
                else if (_customHeightMarker.activeInHierarchy)
                {
                    if (!_firstPointSet)
                    {
                        _firstPointSet = true;
                        _firstPoint = objPos;
                        _heightDragger.transform.position = _firstPoint;
                        _draggerRenderer.material.color = _draggedValidColor;
                        _heightDragger.SetActive(true);

                        GameManager.Instance.InputController.SetControllable(this, ControllableType.Position);
                    }
                    else
                    {
                       
                        GameManager.Instance.InputController.SetControllable(null, ControllableType.Position);
                        int sizeX = Mathf.RoundToInt(_heightDragger.transform.localScale.x);
                        int sizeZ = Mathf.RoundToInt(_heightDragger.transform.localScale.z);

                        if (GameManager.Instance.Game.Player.CanAffordAction(_modifingCost))
                        {
                            // for indexing the terrain correctly with negative scales, just move the point back by the scale amount
                            // and make the scale positive
                            Vector3 modifiedFirstPoint = _firstPoint;
                            if (sizeZ > 0)
                            {
                                modifiedFirstPoint.z -= sizeZ;
                            }

                            if (sizeX < 0)
                            {
                                modifiedFirstPoint.x += sizeX;
                            }

                            ModifyHeightsAtPos(modifiedFirstPoint, Mathf.Abs(sizeX), Mathf.Abs(sizeZ), _customHeight);

                            GameManager.Instance.Game.Player.ModifyStamina(-5);
                            GameManager.Instance.Game.Player.ModifyTotalXP(2);
                            GameManager.Instance.Game.Player.ModifyWallet(-1 * _modifingCost); // works out to be $1 a tile
                        }

                        _firstPointSet = false;
                        _heightDragger.transform.localScale = Vector3.one;
                        _heightDragger.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnTerrainCancel()
    {
        if (_firstPointSet)
        {
            _firstPointSet = false;
            _heightDragger.transform.localScale = Vector3.one;
            _heightDragger.SetActive(false);
        }
    }

    private void OnTimeChanged(object sender, System.EventArgs e)
    {
        TimeChangedArgs args = (TimeChangedArgs)e;
        if (args.DayChanged)
        {
            UpdateSeasonalColors(args.dateTime, false);
        }

        if (args.SeasonChanged)
        {
            for (int i = 0; i < _treeData.Count; ++i)
            {
                UpdateTreeData(_treeData[i]);
            }
        }
    }

    private void UpdateSeasonalColors(CustomDateTime date, bool onBoot)
    {
        Season season = date.GetSeason();
        int day = date.GetDay();
        int month = date.GetMonth();
        int numOfLODS = 1; // just lod0 for now

        // last month of the season
        if (month % 3 == 0 || onBoot)
        {
            SeasonalData data = _seasonalData[(int)season];
            _terrain.terrainData.wavingGrassTint = data.GrassHue;

            for (int i = 0; i < _treeData.Count; ++i)
            {
                // loops through tree data and change materials to reflect new colors
                TreeData d = _treeData[i];
                if (d.TurnStartDay >= day)
                {
                    Debug.DrawLine(d.Obj.transform.position, d.Obj.transform.position + Vector3.up * 10f, Color.red, 5f);

                    for (int j = 0; j < numOfLODS; ++j)
                    {
                        if (season != Season.Winter)
                        {
                            Color current = d.LeafMaterials[j, 0].color;
                            d.LeafMaterials[j, 0].color = (onBoot ? d.StartColor : Color.Lerp(current, d.ColorToReach, 1 / (float)d.DaysToTurn));

                            current = d.LeafMaterials[j, 1].color;
                            d.LeafMaterials[j, 1].color = (onBoot ? d.StartColor : Color.Lerp(current, d.ColorToReach, 1 / (float)d.DaysToTurn));
                        }
                        else
                        {
                            Color current = d.LeafMaterials[j, 0].color;
                            current.a = (onBoot ? d.StartAlpha : Mathf.Lerp(current.a, d.AlphaToReach, 1 / (float)d.DaysToTurn));
                            d.LeafMaterials[j, 0].color = current;

                            current = d.LeafMaterials[j, 1].color;
                            current.a = (onBoot ? d.StartAlpha : Mathf.Lerp(current.a, d.AlphaToReach, 1 / (float)d.DaysToTurn));
                            d.LeafMaterials[j, 1].color = current;
                        }
                    }
                }
            }
        }
    }

    private Vector3 SnapToGrid(Vector3 pos, float factor = 1f)
    {
        if (factor <= 0f)
        {
            Debug.Log("Factor must be greater than 0");
            return Vector3.zero;
        }

        float x = Mathf.Round(pos.x / factor) * factor;
        float z = Mathf.Round(pos.z / factor) * factor;
        Vector3 testPoint = new Vector3(x, 0f, z);

        return new Vector3(x, _terrain.SampleHeight(testPoint), z);
    }

    private Vector3 GetTerrainRelativePosition(Vector3 worldPos)
    {
        float terrainSize = (float)_terrain.terrainData.alphamapWidth;
        int heightMapRes = _terrain.terrainData.heightmapResolution;

        Vector3 terrainLocalPos = worldPos - _terrain.transform.position;
        terrainLocalPos.x = Mathf.RoundToInt((terrainLocalPos.x / terrainSize) * heightMapRes);
        terrainLocalPos.z = Mathf.RoundToInt((terrainLocalPos.z / terrainSize) * heightMapRes);

        return terrainLocalPos;
    }

    private void ModifyHeightsFromAction(ModificationAction action)
    {
        int x = Mathf.RoundToInt(action.StartIndex.x);
        int z = Mathf.RoundToInt(action.StartIndex.y);
        float[,] existingHeights = _terrain.terrainData.GetHeights(x, z, action.Width, action.Depth);

        for (int i = 0; i < action.Depth; ++i)
        {
            for (int j = 0; j < action.Width; ++j)
            {
                existingHeights[i, j] = action.OriginalTerrainHeights[i, j];
            }
        }

        _terrain.terrainData.SetHeights(x, z, existingHeights);
        _currentActionIndex -= 1;
        
        HandleChangeForTiles(action, Vector3.zero, true);
    }

    private void ModifyHeightsAtPos(Vector3 worldPos, int sizeX, int sizeZ, float height)
    {
        Vector3 localPos = GetTerrainRelativePosition(worldPos);
        int x = Mathf.RoundToInt(localPos.x);
        int z = Mathf.RoundToInt(localPos.z);
        ModificationAction action = new ModificationAction(x, z, sizeX, sizeZ);
        float[,] heights = _terrain.terrainData.GetHeights(x, z, sizeX, sizeZ);
        for (int i = 0; i < sizeZ; ++i)
        {
            for (int j = 0; j < sizeX; ++j)
            {
                action.OriginalTerrainHeights[i, j] = heights[i, j];
                heights[i, j] = height;
            }
        }

        _modifications.RecordAction(action);
        _currentActionIndex += 1;
        _terrain.terrainData.SetHeights(x, z, heights);

        HandleChangeForTiles(action, worldPos, false);
    }

    private void HandleChangeForTiles(ModificationAction action, Vector3 worldPos, bool wasUndo)
    {
        TileManager tiles = GameManager.Instance.TileManager;
        if (wasUndo)
        {
            TileUndoCheck(action, tiles);
        }
        else
        {
            TileCreationCheck(action, tiles, worldPos);
        }
    }

    private void TileCreationCheck(ModificationAction action, TileManager tiles, Vector3 worldPos)
    {
        int startX = (int)action.StartIndex.x;
        int startZ = (int)action.StartIndex.y;

        Tile t = null;
        for (int i = startX + 1; i < startX + action.Width; ++i)
        {
            for (int j = startZ + 1; j < startZ + action.Depth; ++j)
            {
                t = tiles.GetTileAt(i, j);
                if (t == null)
                {
                    // no tile exists -> add tile
                    GameObject tileObj = (GameObject)Instantiate(_tilePrefab);
                    t = tileObj.GetComponent<Tile>();
                    tiles.OnEnableInteraction += t.SetTileInteractable;
                    t.SetIndices(i, j);

                    Vector3 posInWorld = worldPos;
                    posInWorld.x += (i - startX);
                    posInWorld.z += (j - startZ);

                    posInWorld.y = GameManager.Instance.TerrainManager.GetTerrainHeightAt(posInWorld);
                    tileObj.name = string.Format("Tile [{0},{1}]", posInWorld.x, posInWorld.z);
                    tileObj.transform.position = posInWorld;
                    tiles.SetTileAt(i, j, t);

                    action.CurrentTileHeights[i - startX, j - startZ] = worldPos.y;
                }
                else
                {
                    // tile exists -> update the new height
                    Vector3 pos = t.gameObject.transform.position;

                    action.PreviousTileHeights[i - startX, j - startZ] = pos.y;

                    pos.y = GameManager.Instance.TerrainManager.GetTerrainHeightAt(pos);
                    t.gameObject.transform.position = pos;

                    action.CurrentTileHeights[i - startX, j - startZ] = pos.y;
                }
            }
        }

        if (OnTerrainModified != null)
        {
            OnTerrainModified(_currentActionIndex);
        }
    }

    private void TileUndoCheck(ModificationAction previousAction, TileManager tiles)
    {
        int startX = (int)previousAction.StartIndex.x;
        int startZ = (int)previousAction.StartIndex.y;

        Tile t = null;
        for (int i = startX + 1; i < startX + previousAction.Width; ++i)
        {
            for (int j = startZ + 1; j < startZ + previousAction.Depth; ++j)
            {
                t = tiles.GetTileAt(i, j);
                if (t != null)
                {
                    if (previousAction.PreviousTileHeights[i - startX, j - startZ] >= 0f)
                    {
                        // tile existed before and was moved, move it back
                        Vector3 oldPos = t.transform.position;
                        oldPos.y = previousAction.PreviousTileHeights[i - startX, j - startZ];
                        t.transform.position = oldPos;
                    }
                    else
                    {
                        // tile didn't exist before, remove it
                        GameObject tileObj = t.gameObject;
                        tiles.SetTileAt(i, j, null);
                        Destroy(tileObj);
                    }
                }
            }
        }

        if (OnTerrainModified != null)
        {
            OnTerrainModified(_currentActionIndex);
        }
    }

    public float GetTerrainHeightAt(Vector3 pos)
    {
        return _terrain.SampleHeight(pos);
    }

    public void AcceptAxisInput(float h, float v)
    {
        throw new NotImplementedException();
    }

    public void AcceptMouseAction(MouseAction a, Vector3 pos)
    {
        throw new NotImplementedException();
    }

    public void AcceptScrollInput(float f)
    {
        throw new NotImplementedException();
    }

    public void AcceptKeyInput(KeyCode k, bool value)
    {
        if (k == KeyCode.LeftShift)
        {
            _customHeightSet = value;
        }
    }

    public void AcceptMousePosition(Vector3 pos)
    {
        int unusedLayer;
        GameObject obj;
        Vector3 worldPos = GameManager.Instance.CameraController.GetWorldPosFromScreen(pos, out unusedLayer, out obj);
        worldPos.y = _firstPoint.y;

        Vector3 snappedPos = SnapToGrid(worldPos);
        Vector3 firstToMouse = snappedPos - _firstPoint;
        Vector3 newScale = _heightDragger.transform.localScale;

        newScale.x = firstToMouse.x;
        newScale.z = firstToMouse.z * -1;

        _modifingCost = (int)((newScale.x - 1) * (newScale.z - 1));
        if (GameManager.Instance.Game.Player.CanAffordAction(_modifingCost))
        {
            _draggerRenderer.material.color = _draggedValidColor;
            _heightDragger.transform.localScale = newScale;
        }
        else
        {
            _draggerRenderer.material.color = _draggedInvalidColor;
        }
    }

    #region helper functions
    // convert jagged array to 2d array
    private float[,] ConvertTo2D(float[][] jagged)
	{
		float[,] _2d = new float[jagged.Length, jagged[0].Length];

		for (int i = 0; i < jagged.Length; ++i)
		{
			for (int j = 0; j < jagged[0].Length; ++j)
			{
				_2d[i,j] = jagged[i][j];
			}
		}

		return _2d;
	}

    #endregion
}
