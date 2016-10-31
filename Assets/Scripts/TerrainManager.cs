using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour, IControllable
{
    public Action OnWorldCreated;
    public event EventHandler OnTerrainModified;

    private struct TerrainModificationAction
    {
        public float[,] OriginalHeights;
        public Vector3 referencePoint;
        public int scaleX;
        public int scaleZ;

        public TerrainModificationAction(Vector3 refPoint, int sizeX, int sizeZ)
        {
            OriginalHeights = new float[sizeZ, sizeX];
            referencePoint = refPoint;
            scaleX = sizeX;
            scaleZ = sizeZ;
        }
    }

    [SerializeField] private GameObject _terrainPrefab;
    [SerializeField] private GameObject _waterPrefab;
    [SerializeField] private GameObject _heightMarkerPrefab;
    [SerializeField] private GameObject _heightDraggerPrefab;

    private List<TerrainModificationAction> _terrainActions;

    private Terrain _terrain;
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
    private int _undoIndex = 0;

    public int WorldSeed { get { return _worldSeed; } }
    public bool InteractMode { get { return _interactMode; } }
    public int MapSize { get { return _terrain.terrainData.alphamapWidth; } }

    void Awake()
    {
        GameManager.Instance.TerrainManager = this;
    }

    void Start()
    {
        GameManager.Instance.CameraController.OnPositionClick += OnTerrainClick;
        OnTerrainModified += UIController.Instance.OnTerrainModified;
        OnTerrainModified += GameManager.Instance.TileManager.OnTerrainModified;

        _terrainActions = new List<TerrainModificationAction>();
    }

    #region UIController button click calls
    public bool ToggleInteractMode()
    {
        _interactMode = !_interactMode;

        if (_interactMode)
        {
            if (_customHeightMarker == null)
            {
                _customHeightMarker = (GameObject)Instantiate(_heightMarkerPrefab);
                _customHeightMarker.SetActive(false);
            }

            if (_heightDragger == null)
            {
                _heightDragger = (GameObject)Instantiate(_heightDraggerPrefab);
                _heightDragger.SetActive(false);
            }

            GameManager.Instance.InputController.SetControllable(this, ControllableType.Key);
        }
        else
        {
            GameManager.Instance.InputController.SetControllable(null, ControllableType.Key);
            _customHeightMarker.SetActive(false);
        }

        return _interactMode;
    }

    public void UndoLastModify()
    {
        int count = _terrainActions.Count;
        if (count > 0)
        {
            TerrainModificationAction lastAction = _terrainActions[_undoIndex - 1];
            _undoIndex -= 1;
            ModifyHeightsFromAction(lastAction);
            _terrainActions.Remove(lastAction);
        }
    }
    #endregion

    // initalize
    public void LoadMap(int seed)
	{
        GameObject terrainObj = (GameObject)Instantiate(_terrainPrefab, Vector3.zero, Quaternion.identity);
        _terrain = terrainObj.GetComponent<Terrain>();

		TerrainData data = _terrain.terrainData;

        _worldSeed = seed;
        data = CreateMultiLevelTerrain(_worldSeed, ref data);

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
        List<TreeInstance> instances = new List<TreeInstance>();
        System.Random treeGen = new System.Random(seed);
        float groundHeight;
        float treeOffsetMin = 0.75f;
        float treeOffsetMax = 1.25f;
        float treeThreshold = 0.925f;

        for (int i = 0; i < size; ++i)
        {
            for (int j = 0; j < size; ++j)
            {
                if (treeNoise[i][j] >= treeThreshold)
                {
                    groundHeight = data.GetHeight(i, j);
                    if (groundHeight > _minDetailHeight && groundHeight < _maxDetailHeight)
                    {
                        TreeInstance tree = new TreeInstance();
                        tree.prototypeIndex = 0; // first tree prototype on the terrain
                        tree.position = new Vector3(i / (float)size, groundHeight, j / (float)size);
                        tree.rotation = Mathf.Deg2Rad * treeGen.Next(360);
                        tree.heightScale = (float)(treeGen.NextDouble() * (treeOffsetMax - treeOffsetMin)) + treeOffsetMin;
                        tree.widthScale = (float)(treeGen.NextDouble() * (treeOffsetMax - treeOffsetMin)) + treeOffsetMin;
                        tree.color = Color.white; // use seasonal theme for this later
                        tree.lightmapColor = Color.white;

                        instances.Add(tree);
                    }
                }
            }
        }

        data.treeInstances = instances.ToArray();
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

    private void OnTerrainClick(int layer, Vector3 terrainPos)
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
                        _heightDragger.SetActive(true);

                        GameManager.Instance.InputController.SetControllable(this, ControllableType.Position);
                    }
                    else
                    {
                        GameManager.Instance.InputController.SetControllable(null, ControllableType.Position);
                        int sizeX = Mathf.RoundToInt(_heightDragger.transform.localScale.x);
                        int sizeZ = Mathf.RoundToInt(_heightDragger.transform.localScale.z);

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

                        _firstPointSet = false;
                        _heightDragger.transform.localScale = Vector3.one;
                        _heightDragger.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.Log("> Terrain Click");
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

    private void ModifyHeightsFromAction(TerrainModificationAction action)
    {
        int x = Mathf.RoundToInt(action.referencePoint.x);
        int z = Mathf.RoundToInt(action.referencePoint.z);
        float[,] existingHeights = _terrain.terrainData.GetHeights(x, z, action.scaleX, action.scaleZ);

        for (int i = 0; i < action.scaleZ; ++i)
        {
            for (int j = 0; j < action.scaleX; ++j)
            {
                existingHeights[i, j] = action.OriginalHeights[i, j];
            }
        }

        _terrain.terrainData.SetHeights(x, z, existingHeights);

        if (OnTerrainModified != null)
        {
            TerrainModArgs args = new TerrainModArgs();
            args.UndoIndex = _undoIndex;
            args.WasUndo = true;
            args.WorldPos = Vector3.zero;
            args.StartIndices = new Vector2(x, z);
            args.Width = action.scaleX;
            args.Depth = action.scaleZ;
            OnTerrainModified(this, args);
        }
    }

    private void ModifyHeightsAtPos(Vector3 worldPos, int sizeX, int sizeZ, float height)
    {
        Vector3 localPos = GetTerrainRelativePosition(worldPos);
        int x = Mathf.RoundToInt(localPos.x);
        int z = Mathf.RoundToInt(localPos.z);
        TerrainModificationAction action = new TerrainModificationAction(localPos, sizeX, sizeZ);
        float[,] heights = _terrain.terrainData.GetHeights(x, z, sizeX, sizeZ);
        for (int i = 0; i < sizeZ; ++i)
        {
            for (int j = 0; j < sizeX; ++j)
            {
                action.OriginalHeights[i, j] = heights[i, j];
                heights[i, j] = height;
            }
        }

        _terrainActions.Add(action);
        _undoIndex += 1;
        _terrain.terrainData.SetHeights(x, z, heights);

        if (OnTerrainModified != null)
        {
            TerrainModArgs args = new TerrainModArgs();
            args.UndoIndex = _undoIndex;
            args.WasUndo = false;
            args.WorldPos = worldPos;
            args.StartIndices = new Vector2(x, z);
            args.Width = sizeX;
            args.Depth = sizeZ;
            OnTerrainModified(this, args);
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
        Vector3 worldPos = GameManager.Instance.CameraController.GetWorldPosFromScreen(pos, out unusedLayer);
        worldPos.y = _firstPoint.y;

        Vector3 snappedPos = SnapToGrid(worldPos);
        Vector3 firstToMouse = snappedPos - _firstPoint;
        Vector3 newScale = _heightDragger.transform.localScale;

        newScale.x = firstToMouse.x;
        newScale.z = firstToMouse.z * -1;
        _heightDragger.transform.localScale = newScale;
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
