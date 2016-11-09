using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TileManager : MonoBehaviour
{
    public Action<bool> OnEnableInteraction;

    private Tile[,] _tiles;

    void Awake()
    {
        GameManager.Instance.TileManager = this;

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

    public Tile GetTileAt(int x, int y)
    {
        return _tiles[x, y];
    }

    public void SetTileAt(int x, int y, Tile t)
    {
        _tiles[x, y] = t;
    }

    private void OninteractModeChanged(bool interactMode)
    {
        if (OnEnableInteraction != null)
        {
            OnEnableInteraction(!interactMode);
        }
    }
}
