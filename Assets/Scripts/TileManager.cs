using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TileManager : MonoBehaviour
{
    [SerializeField] private GameObject _tilePrefab;

    private Tile[,] _tiles;
    private ModificationManager _modifications;

    void Awake()
    {
        GameManager.Instance.TileManager = this;

        GameManager.Instance.CameraController.OnPositionClick += OnTileClick;
        GameManager.Instance.TerrainManager.OnInteractModeChanged += OninteractModeChanged;
    }

    void Start()
    {
        _modifications = ModificationManager.Instance;
    }

    public void InitializeTiles(int mapSize)
    {
        _tiles = new Tile[mapSize, mapSize];
        for (int i = 0; i < mapSize; ++i)
        {
            for (int j = 0; j < mapSize; ++j)
            {
                _tiles[i, j] = null;
            }
        }
    }

    public void OnTerrainModified(object sender, System.EventArgs e)
    {
        TerrainModArgs args = (TerrainModArgs)e;

        // get the action just created in the terrain manager
        ModificationAction actionInProgress = _modifications.RetreiveAction(args.UndoIndex - 1);

        if (args.WasUndo)
        {
            TileUndoCheck(actionInProgress, args);
        }
        else
        {
            TileCreationCheck(actionInProgress, args);
        }
    }

    private void TileCreationCheck(ModificationAction action, TerrainModArgs args)
    {
        int startX = (int)action.StartIndex.x;
        int startZ = (int)action.StartIndex.y;

        for (int i = startX + 1; i < startX + action.Width; ++i)
        {
            for (int j = startZ + 1; j < startZ + action.Depth; ++j)
            {
                if (_tiles[i, j] == null)
                {
                    // no tile exists -> add tile
                    GameObject tileObj = (GameObject)Instantiate(_tilePrefab);
                    Tile t = tileObj.GetComponent<Tile>();
                    Vector3 worldPos = args.WorldPos;
                    worldPos.x += (i - startX);
                    worldPos.z += (j - startZ);
                    worldPos.y = GameManager.Instance.TerrainManager.GetTerrainHeightAt(worldPos);
                    tileObj.name = string.Format("Tile [{0},{1}]", worldPos.x, worldPos.z);
                    tileObj.transform.position = worldPos;
                    _tiles[i, j] = t;

                    action.CurrentTileHeights[i - startX, j - startZ] = worldPos.y;
                }
                else
                {
                    // tile exists -> update the new height
                    Tile t = _tiles[i, j];
                    Vector3 pos = t.gameObject.transform.position;

                    action.PreviousTileHeights[i - startX, j - startZ] = pos.y;

                    pos.y = GameManager.Instance.TerrainManager.GetTerrainHeightAt(pos);
                    t.gameObject.transform.position = pos;

                    action.CurrentTileHeights[i - startX, j - startZ] = pos.y;
                }
            }
        }
    }

    private void TileUndoCheck(ModificationAction previousAction, TerrainModArgs args)
    {
        int startX = (int)previousAction.StartIndex.x;
        int startZ = (int)previousAction.StartIndex.y;

        for (int i = startX + 1; i < startX + args.Width; ++i)
        {
            for (int j = startZ + 1; j < startZ + args.Depth; ++j)
            {
                if (_tiles[i, j] != null)
                {
                    if (previousAction.PreviousTileHeights[i - startX, j - startZ] >= 0f)
                    {
                        // tile existed before and was moved, move it back
                        Vector3 oldPos = _tiles[i, j].transform.position;
                        oldPos.y = previousAction.PreviousTileHeights[i - startX, j - startZ];
                        _tiles[i, j].transform.position = oldPos;
                    }
                    else
                    {
                        // tile didn't exist before, remove it
                        GameObject tileObj = _tiles[i, j].gameObject;
                        _tiles[i, j] = null;
                        Destroy(tileObj);
                    }
                }
            }
        }

        _modifications.RemoveAction(previousAction);
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
