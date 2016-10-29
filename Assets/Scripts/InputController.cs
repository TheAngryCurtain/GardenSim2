using UnityEngine;
using System.Collections;
using System;
using UnityEngine.EventSystems;

public enum MouseAction
{
	LeftClick,
	RightClick
}

public enum ControllableType
{
	Axis,
	Scroll,
	Key,
	Click
};

public class InputController : MonoBehaviour
{
	private IControllable _control = null;

	private float _h;
	private float _v;
	private float _scroll;
	private Vector3 _mousePos;

	private IControllable[] _previousControllables;
	private IControllable[] _currentControllables;

    void Awake()
    {
        GameManager.Instance.InputController = this;

		_previousControllables = new IControllable[4]; // length of enum
		_currentControllables = new IControllable[4];
    }

	void Update()
	{
		GetAxisInput();
		GetMouseInput();
		GetKeyInput();
	}

	public void SetControllable (IControllable c, ControllableType type)
	{
		int typeIndex = (int)type;
		_previousControllables[typeIndex] = _currentControllables[typeIndex];
		_currentControllables[typeIndex] = c;
	}

	private void GetAxisInput()
	{
		_h = Input.GetAxis("Horizontal");
		_v = Input.GetAxis("Vertical");

		if (_currentControllables[(int)ControllableType.Axis] != null)
		{
			_currentControllables[(int)ControllableType.Axis].AcceptAxisInput(_h, _v);
		}
	}

	private void GetKeyInput()
	{
		if (_currentControllables[(int)ControllableType.Key] != null)
		{
			_currentControllables[(int)ControllableType.Key].AcceptKeyInput(KeyCode.LeftShift, Input.GetKeyDown(KeyCode.LeftShift));
		}
	}

	private void GetMouseInput()
	{
        if (!EventSystem.current.IsPointerOverGameObject(-1))
        {
			// click
			if (_currentControllables[(int)ControllableType.Click] != null)
			{
	            _mousePos = Input.mousePosition;
	            if (Input.GetMouseButtonDown((int)MouseAction.LeftClick))
	            {
					_currentControllables[(int)ControllableType.Click].AcceptMouseAction(MouseAction.LeftClick, _mousePos);
	            }
	            else if (Input.GetMouseButton((int)MouseAction.RightClick))
	            {
					_currentControllables[(int)ControllableType.Click].AcceptMouseAction(MouseAction.RightClick, _mousePos);
	            }
			}

			// scroll
			if (_currentControllables[(int)ControllableType.Scroll] != null)
			{
	            _scroll = Input.GetAxis("MouseScrollWheel");
	            if (_scroll != 0f)
	            {
					_currentControllables[(int)ControllableType.Scroll].AcceptScrollInput(_scroll);
	            }
			}
        }
    }
}
