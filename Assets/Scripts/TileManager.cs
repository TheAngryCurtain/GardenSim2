using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TileManager : MonoBehaviour
{
    public Action<bool> OnEnableInteraction;

    [SerializeField] private GameObject[] _dirtMoundPrefabs;
    public GameObject[] SoilPrefabs { get { return _dirtMoundPrefabs; } }

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
                GameObject soilObj = (GameObject)Instantiate(_dirtMoundPrefabs[0], t.transform.position, Quaternion.identity);
                t.SetSoilObject(soilObj);
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

        NotifyNeighboursOfChange(_tiles[tileX, tileZ], neighbours);
    }

    // TODO this will need some parameters for the type of change
    private void NotifyNeighboursOfChange(Tile changed, List<Tile> validNeighbours)
    {
        Tile t = null;
        for (int i = 0; i < validNeighbours.Count; ++i)
        {
            t = validNeighbours[i];
            if (t != null)
            {
                //t.OnNeighbourChanged(changed);
                HandleNeighbourChanged(changed, t);
            }
        }
    }

    private void HandleNeighbourChanged(Tile changed, Tile neighbour)
    {
        if (changed.State == Tile.eSoilState.Dug)
        {
            if (neighbour.State == Tile.eSoilState.Dug)
            {
                // update the neighbours with soil
                int diffX = changed.X - neighbour.X;
                int diffZ = changed.Y - neighbour.Y;
                bool changeMade = false;

                if (diffX < 0)
                {
                    // neighbour is to the right
                    changed.SetNeighbourWithSoil(Tile.eNeighbourDirection.East, true);
                    neighbour.SetNeighbourWithSoil(Tile.eNeighbourDirection.West, true);
                }
                else if (diffX > 0)
                {
                    // neighbour is to the left
                    changed.SetNeighbourWithSoil(Tile.eNeighbourDirection.West, true);
                    neighbour.SetNeighbourWithSoil(Tile.eNeighbourDirection.East, true);
                }

                // since soil changes only matter in the 4 cardinal directions, if the x was changed, the z can't also
                if (!changeMade)
                {
                    if (diffZ < 0)
                    {
                        // neighbour is above
                        changed.SetNeighbourWithSoil(Tile.eNeighbourDirection.North, true);
                        neighbour.SetNeighbourWithSoil(Tile.eNeighbourDirection.South, true);
                    }
                    else if (diffZ > 0)
                    {
                        // neighbour is below
                        changed.SetNeighbourWithSoil(Tile.eNeighbourDirection.South, true);
                        neighbour.SetNeighbourWithSoil(Tile.eNeighbourDirection.North, true);
                    }
                }

                UpdateSoilPrefab(changed);
                UpdateSoilPrefab(neighbour);
            }
        }
    }

    private void UpdateSoilPrefab(Tile t)
    {
        bool[] directionsWithSoil = t.NeighboursWithSoil;
        GameObject requiredPrefab = null;
        Transform facingDirection = null;

        for (int i = 0; i < directionsWithSoil.Length; i++)
        {
            if (directionsWithSoil[i] == false) continue;
            else
            {
                // found one with soil, start searching from the next one
                for (int j = i + 1; j < directionsWithSoil.Length; j++)
                {
                    if (directionsWithSoil[j] == false) continue;
                    else
                    {
                        switch ((Tile.eNeighbourDirection)j)
                        {
                            // can't be north, as it would at least be the one found above

                            case Tile.eNeighbourDirection.East:
                                //
                                break;

                            case Tile.eNeighbourDirection.South:
                                // N and 
                                break;

                            case Tile.eNeighbourDirection.West:
                                break;
                        }
                    }
                }

                break;
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
