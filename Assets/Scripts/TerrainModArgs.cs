using UnityEngine;
using System.Collections;
using System;

public class TerrainModArgs : EventArgs
{
    public int UndoIndex;
    public bool WasUndo;
    public Vector3 WorldPos;
}
