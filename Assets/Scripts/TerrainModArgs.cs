using UnityEngine;
using System.Collections;
using System;

public class TerrainModArgs : EventArgs
{
    public int UndoIndex;

    public bool WasUndo;
    public Vector2 StartIndices;
    public Vector3 WorldPos;
    public int Width;
    public int Depth;
}
