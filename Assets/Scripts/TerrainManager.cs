using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    public Action OnWorldCreated;

    [SerializeField] private GameObject _terrainPrefab;
    [SerializeField] private GameObject _waterPrefab;

    private Terrain _terrain;
    private int _worldSeed;
    private float _minDetailHeight = 1f;
    private float _maxDetailHeight = 8f;
    private bool _interactMode = false;
    private float _customHeight;
    private bool _customHeightSet = false;
    private GameObject _customHeightMarker = null;

    public int WorldSeed { get { return _worldSeed; } }
    public bool InteractMode { get { return _interactMode; } }

    void Awake()
    {
        GameManager.Instance.TerrainManager = this;
    }

    void Start()
    {
        GameManager.Instance.CameraController.OnPositionClick += OnTerrainClick;
    }

    // called from UIController on button click
    public bool ToggleInteractMode()
    {
        _interactMode = !_interactMode;

        if (_customHeightMarker != null)
        {
            _customHeightMarker.SetActive(_interactMode);
        }

        return _interactMode;
    }

    // initalize
    // TODO pass in a save file? or saved terrain height data
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

    private void OnTerrainClick(int layer, Vector3 terrainPos)
    {
        if (layer == LayerMask.NameToLayer("Terrain"))
        {
            Vector3 objPos = SnapToGrid(terrainPos);
            if (_interactMode)
            {
                Vector3 terrainLocal = GetTerrainRelativePosition(objPos);
                if (_customHeightSet)
                {
                    if (_customHeightMarker == null)
                    {
                        _customHeightMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        _customHeightMarker.layer = LayerMask.NameToLayer("TerrainObject");
                    }
                    _customHeightMarker.transform.position = objPos;

                    // this doesn't seem right...
                    _customHeight = _terrain.terrainData.GetHeight((int)terrainLocal.x, (int)terrainLocal.z) / 10f;
                    Debug.Log(_customHeight);
                }
                else
                {
                    ModifyHeightsAtPos(terrainLocal, _customHeight);
                }
            }
            else
            {
                GameObject debug = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debug.transform.position = objPos;
                debug.layer = LayerMask.NameToLayer("TerrainObject");
            }
        }
    }

    void Update()
    {
        if (_interactMode)
        {
            _customHeightSet = Input.GetKey(KeyCode.LeftShift);
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

        return new Vector3(x, pos.y, z);
    }

    private Vector3 GetTerrainRelativePosition(Vector3 worldPos)
    {
        float terrainSize = (float)_terrain.terrainData.alphamapWidth;
        int heightMapRes = _terrain.terrainData.heightmapResolution;

        Vector3 terrainLocalPos = worldPos - _terrain.transform.position;
        terrainLocalPos.x = Mathf.Round((terrainLocalPos.x / terrainSize) * heightMapRes);
        terrainLocalPos.z = Mathf.Round((terrainLocalPos.z / terrainSize) * heightMapRes);

        return terrainLocalPos;
    }

    private void ModifyHeightsAtPos(Vector3 localPos, float height)
    {
        int size = 2;
        int offset = size / 2;
        int localX = (int)localPos.x - offset;
        int localZ = (int)localPos.z - offset;

        float[,] heights = _terrain.terrainData.GetHeights(localX, localZ, size, size);
        for (int i = 0; i < size; ++i)
        {
            for (int j = 0; j < size; ++j)
            {
                Debug.Log(heights[i, j]);

                heights[i, j] = height;
            }
        }

        _terrain.terrainData.SetHeights(localX, localZ, heights);
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
