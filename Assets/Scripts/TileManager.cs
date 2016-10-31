using UnityEngine;
using System.Collections;
using System;

public class TileManager : MonoBehaviour
{
    private class TileDatum
    {
        public GameObject PhysicalTile;
    };

    [SerializeField] private GameObject _tilePrefab;

    private TileDatum[,] _tileData;

    void Awake()
    {
        GameManager.Instance.TileManager = this;

        GameManager.Instance.CameraController.OnPositionClick += OnTileClick;
        GameManager.Instance.TerrainManager.OnInteractModeChanged += OninteractModeChanged;
    }

    public void InitializeTiles(int mapSize)
    {
        _tileData = new TileDatum[mapSize, mapSize];
        for (int i = 0; i < mapSize; ++i)
        {
            for (int j = 0; j < mapSize; ++j)
            {
                _tileData[i, j] = null;
            }
        }
    }

    public void OnTerrainModified(object sender, System.EventArgs e)
    {
        TerrainModArgs args = (TerrainModArgs)e;
        int startX = (int)args.StartIndices[0];
        int startZ = (int)args.StartIndices[1];

        for (int i = startX + 1; i < startX + args.Width; ++i)
        {
            for (int j = startZ + 1; j < startZ + args.Depth; ++j)
            {
                if (_tileData[i,j] == null)
                {
                    if (!args.WasUndo)
                    {
                        // no tile exists, not an undo -> add tile
                        TileDatum data = new TileDatum();
                        GameObject tileObj = (GameObject)Instantiate(_tilePrefab);
                        Vector3 worldPos = args.WorldPos;
                        worldPos.x += (i - startX);
                        worldPos.z += (j - startZ);
                        worldPos.y = GameManager.Instance.TerrainManager.GetTerrainHeightAt(worldPos);
                        tileObj.name = string.Format("Tile [{0},{1}]", worldPos.x, worldPos.z);
                        tileObj.transform.position = worldPos;
                        data.PhysicalTile = tileObj;
                        _tileData[i, j] = data;
                    }
                }
                else
                {
                    if (args.WasUndo)
                    {
                        // tile exists, there was an undo...

                    }
                    else
                    {
                        // tile exists, no undo -> update the new height
                        TileDatum data = _tileData[i, j];
                        Vector3 pos = data.PhysicalTile.transform.position;
                        pos.y = GameManager.Instance.TerrainManager.GetTerrainHeightAt(pos);
                        data.PhysicalTile.transform.position = pos;
                    }
                }
            }
        }
    }

    private void OnTileClick(int layer, Vector3 terrainPos, GameObject obj)
    {
        if (layer == LayerMask.NameToLayer("Tile"))
        {
            Debug.Log(obj.transform.root.name);
        }
    }

    private void OninteractModeChanged(bool interactMode)
    {
        if (interactMode)
        {
            GameManager.Instance.CameraController.OnPositionClick -= OnTileClick;
        }
        else
        {
            GameManager.Instance.CameraController.OnPositionClick += OnTileClick;
        }
    }
}
