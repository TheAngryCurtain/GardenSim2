using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TileManager : MonoBehaviour
{
    public Action<bool> OnEnableInteraction;

    [SerializeField] private GameObject _dirtMoundPrefab;

    public static int[] ToolScrollXSequence = new int[] { -1, 0, 1, 0 };
    public static int[] ToolScrollYSequence = new int[] { 0, 1, 0, -1 };
    public static Vector2 ToolScrollDirection = new Vector2(-1, 0);
    public static int ToolScrollSequenceIndex = 0;

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
        // if there is a tile here, and it's about to be set to null, remove listener
        Tile existing = _tiles[x, y];
        if (existing != null)
        {
            existing.StateChanged -= HandleTileStateChange;
        }

        if (t != null)
        {
            t.StateChanged += HandleTileStateChange;
        }

        _tiles[x, y] = t;
    }

    private void CheckForSoilChange(int x, int z)
    {
        Tile t = _tiles[x, z];
        switch (t.State)
        {
            case Tile.eSoilState.Dug:
                GameObject soilObj = (GameObject)Instantiate(_dirtMoundPrefab, t.transform.position, Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(0, 360), 0f)));
                t.SoilObject = soilObj;
                break;
        }
    }

    private void HandleTileStateChange(int tileX, int tileZ)
    {
        CheckForSoilChange(tileX, tileZ);

        List<Tile> neighbours = new List<Tile>();
        int mapSize = 256; // pretty sure this is the size.. don't remember where to find it. Terrain data maybe?

        // check the 8 surrounding neighbours
        int above = tileZ + 1;
        int below = tileZ - 1;
        int left = tileX - 1;
        int right = tileX + 1;

        // above row
        if (above < mapSize)
        {
            neighbours.Add(_tiles[tileX, above]);
            if (left > 0)
            {
                neighbours.Add(_tiles[left, above]);
            }

            if (right < mapSize)
            {
                neighbours.Add(_tiles[right, above]);
            }
        }

        // below row
        if (below > 0)
        {
            neighbours.Add(_tiles[tileX, below]);
            if (left > 0)
            {
                neighbours.Add(_tiles[left, below]);
            }

            if (right < mapSize)
            {
                neighbours.Add(_tiles[right, below]);
            }
        }

        // left and right
        if (left > 0)
        {
            neighbours.Add(_tiles[left, tileZ]);
        }

        if (right < mapSize)
        {
            neighbours.Add(_tiles[right, tileZ]);
        }

        NotifyNeighboursOfChange(neighbours);
    }

    // TODO this will need some parameters for the type of change
    private void NotifyNeighboursOfChange(List<Tile> validNeighbours)
    {
        Tile t = null;
        for (int i = 0; i < validNeighbours.Count; ++i)
        {
            t = validNeighbours[i];
            if (t != null)
            {
                t.OnNeighbourChanged();
            }
        }
    }

    private void OninteractModeChanged(bool interactMode)
    {
        if (OnEnableInteraction != null)
        {
            OnEnableInteraction(!interactMode);
        }
    }

    public List<Tile> RequestHighlightRow(int currentX, int currentY, int toolLevel, Vector2 direction)
    {
        int numTiles = ToolData.BaseLevelModifier + (ToolData.LevelModifierIncrement * (toolLevel - 1));
        List<Tile> tiles = new List<Tile>(numTiles);
        Tile t = null;
        int x, y;

        for (int i = 1; i < numTiles; ++i)
        {
            x = currentX + ((int)direction.x * i);
            y = currentY + ((int)direction.y * i);

            if (x < 0 || x > _tiles.Length) continue;
            if (y < 0 || y > _tiles.Length) continue;

            t = _tiles[x, y];
            if (t != null)
            {
                tiles.Add(t);
            }
        }

        return tiles;
    }
}
