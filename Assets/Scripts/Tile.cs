using UnityEngine;
using System.Collections;

public class Tile : InteractableObject
{
    public System.Action<int, int> StateChanged;

	public enum SoilState
    {
        Blocked,
        Untouched,
        Dug,
        Seeded,
        Harvested
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

    public void OnNeighbourChanged()
    {
        Debug.LogFormat("{0} heard a change from neighbour", this.gameObject.name);
    }

    protected override void ClickAction()
    {
        Debug.Log(this.gameObject.name);

        // test
        //StateChanged(X, Y);
    }

    // TODO change state function
    // call StateChanged(_xIndex, _yIndex) or X,Y
}
