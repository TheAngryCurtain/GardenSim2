using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Tile : InteractableObject, IControllable
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
    private List<Tile> _highlightedGroup;

    public eSoilState State { get { return _state; } }
    public int X { get { return _xIndex; } }
    public int Y { get { return _yIndex; } }

    private GameObject _soilObj;
    public GameObject SoilObject { get { return _soilObj; }  set { _soilObj = value; } }

    protected override void Start()
    {
        base.Start();

        _highlightedGroup = new List<Tile>();
    }

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
            ApplyTool(previous, tool.ToolType);
            for (int i = 0; i < _highlightedGroup.Count; ++i)
            {
                _highlightedGroup[i].ApplyTool(previous, tool.ToolType);
            }
        }
    }

    public void ApplyTool(eSoilState prev, ToolData.eToolType type)
    {
        switch (type)
        {
            case ToolData.eToolType.Shovel:
                if (_state == eSoilState.Untouched)
                {
                    _state = eSoilState.Dug;
                }
                break;
        }

        if (prev != _state)
        {
            StateChanged(X, Y);
            prev = _state;
        }
    }

    public void Highlight(bool highlight)
    {
        if (highlight)
        {
            base.OnObjectHoverEnter();
        }
        else
        {
            base.OnObjectHoverExit();
        }
    }

    protected override void OnObjectHoverEnter()
    {
        base.OnObjectHoverEnter();

        CheckToolHighlight();
    }

    protected override void OnObjectHoverExit()
    {
        base.OnObjectHoverExit();

        if (_highlightedGroup.Count > 0)
        {
            HighlightGroup(false);
            _highlightedGroup.Clear();

            GameManager.Instance.InputController.SetControllable(GameManager.Instance.CameraController, ControllableType.Scroll);
        }
    }

    private void CheckToolHighlight()
    {
        ToolData tool = GameManager.Instance.Game.Player.GetCurrentTool();
        if (tool != null)
        {
            int toolLevel = tool.ToolLevel;
            if (toolLevel > 0)
            {
                RequestHighlightGroup(toolLevel, TileManager.ToolScrollDirection);
                GameManager.Instance.InputController.SetControllable(this, ControllableType.Scroll);
            }
        }
    }

    private void RequestHighlightGroup(int toolLevel, Vector2 direction)
    {
        HighlightGroup(false);
        _highlightedGroup.Clear();

        _highlightedGroup = GameManager.Instance.TileManager.RequestHighlightRow(X, Y, toolLevel, direction);
        HighlightGroup(true);
    }

    private void HighlightGroup(bool highlight)
    {
        for (int i = 0; i < _highlightedGroup.Count; ++i)
        {
            _highlightedGroup[i].Highlight(highlight);
        }
    }

    public void AcceptAxisInput(float h, float v) { }
    public void AcceptMouseAction(MouseAction a, Vector3 pos) { }
    public void AcceptKeyInput(KeyCode k, bool value) { }
    public void AcceptMousePosition(Vector3 position) { }

    public void AcceptScrollInput(float f)
    {
        if (f != 0f)
        {
            int index = TileManager.ToolScrollSequenceIndex;
            if (f > 0f)
            {
                index += 1;
                if (index >= TileManager.ToolScrollXSequence.Length)
                {
                    index = 0;
                }
            }
            else
            {
                index -= 1;
                if (index < 0)
                {
                    index = TileManager.ToolScrollXSequence.Length - 1;
                }
            }

            TileManager.ToolScrollDirection.x = TileManager.ToolScrollXSequence[index];
            TileManager.ToolScrollDirection.y = TileManager.ToolScrollYSequence[index];

            TileManager.ToolScrollSequenceIndex = index;

            CheckToolHighlight();
        }
    }
}
