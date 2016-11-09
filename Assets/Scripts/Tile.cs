using UnityEngine;
using System.Collections;

public class Tile : InteractableObject
{
	public enum SoilState
    {
        
    }

    private SoilState _state;
    private int _xIndex;
    private int _yIndex;

    public int X { get { return _xIndex; } }
    public int Y { get { return _yIndex; } }

    public void SetIndices(int x, int y)
    {
        _xIndex = x;
        _yIndex = y;
    }

    public void SetTileInteractable(bool interact)
    {
        EnableInteraction(interact);
    }

    protected override void ClickAction()
    {
        Debug.Log(this.gameObject.name);
    }
}
