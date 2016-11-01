using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TileManager : MonoBehaviour
{
    private struct TileModificationAction
    {
        public Vector2 StartIndex;
        public int Width;
        public int Depth;
        public float[,] CurrentHeights;
        public float[,] PreviousHeights;

        public TileModificationAction(int x, int y, int width, int depth)
        {
            StartIndex = new Vector2(x, y);
            Width = width;
            Depth = depth;
            CurrentHeights = new float[width, depth];
            PreviousHeights = new float[width, depth];
        }
    };

    [SerializeField] private GameObject _tilePrefab;

    private Tile[,] _tiles;
    private List<TileModificationAction> _tileActions;

    void Awake()
    {
        GameManager.Instance.TileManager = this;

        _tileActions = new List<TileModificationAction>();

        GameManager.Instance.CameraController.OnPositionClick += OnTileClick;
        GameManager.Instance.TerrainManager.OnInteractModeChanged += OninteractModeChanged;
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
        int startX = (int)args.StartIndices[0];
        int startZ = (int)args.StartIndices[1];

        if (args.WasUndo)
        {
            TileUndoCheck(startX, startZ, args);
        }
        else
        {
            TileCreationCheck(startX, startZ, args);
        }
    }

    private void TileCreationCheck(int startX, int startZ, TerrainModArgs args)
    {
        TileModificationAction action = new TileModificationAction(startX, startZ, args.Width, args.Depth);
        for (int i = startX + 1; i < startX + args.Width; ++i)
        {
            for (int j = startZ + 1; j < startZ + args.Depth; ++j)
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

                    action.CurrentHeights[i - startX, j - startZ] = worldPos.y;
                }
                else
                {
                    // tile exists -> update the new height
                    Tile t = _tiles[i, j];
                    Vector3 pos = t.gameObject.transform.position;

                    action.PreviousHeights[i - startX,j - startZ] = pos.y;

                    pos.y = GameManager.Instance.TerrainManager.GetTerrainHeightAt(pos);
                    t.gameObject.transform.position = pos;

                    action.CurrentHeights[i - startX, j - startZ] = pos.y;
                }
            }
        }

        _tileActions.Add(action);
    }

    private void TileUndoCheck(int startX, int startZ, TerrainModArgs args)
    {
        TileModificationAction previousAction = _tileActions[args.UndoIndex];

        for (int i = startX + 1; i < startX + args.Width; ++i)
        {
            for (int j = startZ + 1; j < startZ + args.Depth; ++j)
            {
                if (_tiles[i, j] != null)
                {
                    if (previousAction.PreviousHeights[i - startX, j - startZ] >= 0f)
                    {
                        // tile existed before and was moved, move it back
                        Vector3 oldPos = _tiles[i, j].transform.position;
                        oldPos.y = previousAction.PreviousHeights[i - startX, j - startZ];
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

        _tileActions.Remove(previousAction);
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
