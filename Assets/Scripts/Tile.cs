using UnityEngine;
using System.Collections;

public class Tile : InteractableObject
{
    public System.Action<int, int> StateChanged;

	public enum eSoilState
    {
        Blocked,
        Untouched,
        Dug,
        Seeded,
        Harvested
    }

    private eSoilState _state = eSoilState.Untouched;
    private int _xIndex;
    private int _yIndex;

    public eSoilState State { get { return _state; } }
    public int X { get { return _xIndex; } }
    public int Y { get { return _yIndex; } }

    private GameObject _soilObj;
    public GameObject SoilObject { get { return _soilObj; }  set { _soilObj = value; } }

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

        eSoilState previous = _state;
        ToolData tool = GameManager.Instance.Game.Player.GetCurrentTool();
        if (tool != null)
        {
            Debug.Log(tool.Description);
            switch (tool.ToolType)
            {
                case ToolData.eToolType.Shovel:
                    if (_state == eSoilState.Untouched)
                    {
                        _state = eSoilState.Dug;
                    }
                    break;
            }

            if (previous != _state)
            {
                StateChanged(X, Y);
                previous = _state;
            }
        }
    }

    protected override void OnObjectHoverEnter()
    {
        base.OnObjectHoverEnter();
    }

    protected override void OnObjectHoverExit()
    {
        base.OnObjectHoverExit();
    }
}
