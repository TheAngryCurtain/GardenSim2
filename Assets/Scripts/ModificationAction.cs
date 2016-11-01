using UnityEngine;
using System.Collections;

public class ModificationAction
{
    public Vector2 StartIndex;
    public int Width;
    public int Depth;

    public float[,] OriginalTerrainHeights;
    public float[,] PreviousTileHeights;
    public float[,] CurrentTileHeights;

    public ModificationAction(int x, int y, int w, int d)
    {
        StartIndex = new Vector2(x, y);
        Width = w;
        Depth = d;

        OriginalTerrainHeights = new float[d, w]; // intentional swap for terrain iteration
        PreviousTileHeights = new float[w, d];
        CurrentTileHeights = new float[w, d];
    }
}