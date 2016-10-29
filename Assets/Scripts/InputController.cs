using UnityEngine;
using System.Collections;
using System;
using UnityEngine.EventSystems;

public enum MouseAction
{
	LeftClick,
	RightClick
}

public class InputController : MonoBehaviour
{
	private IControllable _control = null;

	private float _h;
	private float _v;
	private float _scroll;
	private Vector3 _mousePos;

    void Awake()
    {
        GameManager.Instance.InputController = this;
    }

	void Update()
	{
		if (_control != null)
		{
			GetAxisInput();
			GetMouseInput();
		}
	}

	public void SetControllable (IControllable c)
	{
		_control = c;
	}

	private void GetAxisInput()
	{
		_h = Input.GetAxis("Horizontal");
		_v = Input.GetAxis("Vertical");

		_control.AcceptAxisInput(_h, _v);
	}

	private void GetMouseInput()
	{
        if (!EventSystem.current.IsPointerOverGameObject(-1))
        {
            _mousePos = Input.mousePosition;
            if (Input.GetMouseButtonDown((int)MouseAction.LeftClick))
            {
                _control.AcceptMouseAction(MouseAction.LeftClick, _mousePos);
            }
            else if (Input.GetMouseButton((int)MouseAction.RightClick))
            {
                _control.AcceptMouseAction(MouseAction.RightClick, _mousePos);
            }

            _scroll = Input.GetAxis("MouseScrollWheel");
            if (_scroll != 0f)
            {
                _control.AcceptScrollInput(_scroll);
            }
        }
    }
}
