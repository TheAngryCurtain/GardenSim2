﻿using UnityEngine;
using System.Collections;
using System;

public class CameraController : MonoBehaviour, IControllable
{
    public Action<int, Vector3, GameObject> OnPositionClick;
    public Action OnCancelClick;

	enum ZoomDirection { IN = 0, OUT = 1 };

    [SerializeField] private Camera _camera;
    [SerializeField] LayerMask _clickLayers;

    private float _startHeight = 15f;
    private float _angle = 45f;
	private int _maxZoomLevel = 1;
	private int _minZoomLevel = 5;
    private float _zoomAmount = 5f;
	private	float _scrollSpeed = 15f;
    private float _raycastDist = 100f;

    private float _screenMarginX = 0.05f;
    private float _screenMarginY = 0.075f;

    private Vector3 _activeZoomPosition;
	private float _currentZoomLevel;
	private Transform _leftEdge;
	private Transform _rightEdge;
	private Transform _topEdge;
	private Transform _bottomEdge;

    void Awake()
    {
        GameManager.Instance.CameraController = this;

        Transform CameraBoundaries = GameObject.Find("CameraBoundaries").transform;
        _leftEdge = CameraBoundaries.GetChild(0);
        _rightEdge = CameraBoundaries.GetChild(1);
        _topEdge = CameraBoundaries.GetChild(2);
        _bottomEdge = CameraBoundaries.GetChild(3);

        transform.position += new Vector3(0f, _startHeight, 0f);
        transform.Rotate(new Vector3(_angle, 0f, 0f));

		_currentZoomLevel = _maxZoomLevel;

		// take required controls
		GameManager.Instance.InputController.SetControllable(this, ControllableType.Axis);
        GameManager.Instance.InputController.SetControllable(this, ControllableType.Scroll);
		GameManager.Instance.InputController.SetControllable(this, ControllableType.Click);
        GameManager.Instance.InputController.SetControllable(this, ControllableType.Position);
    }

    public void AcceptMouseAction(MouseAction a, Vector3 mousePosition)
    {
		//Debug.LogFormat("action: {0}, pos: {1}", a, mousePosition);
        if (a == MouseAction.LeftClick)
        {
            InteractWithWorld(mousePosition);
        }
        else if (a == MouseAction.RightClick)
        {
            CancelInteraction();
        }
    }

	public void AcceptScrollInput(float scroll)
    {
        if (scroll > 0f)
        {
            Zoom(ZoomDirection.IN);
        }
        else if (scroll < 0f)
        {
            Zoom(ZoomDirection.OUT);
        }
    }

	public void AcceptAxisInput(float h, float v)
	{
		moveCamera(h, v);
	}

	public void AcceptKeyInput(KeyCode k, bool value)
	{

	}

    public void AcceptMousePosition(Vector3 pos)
    {
        float horizontalEdgePercent = Screen.width * _screenMarginX;
        float verticalEdgePercent = Screen.height * _screenMarginY;

        float x = 0;
        float y = 0;

        if (pos.x < 0 + horizontalEdgePercent)
        {
            x = -1;
        }
        else if (pos.x > Screen.width - horizontalEdgePercent)
        {
            x = 1;
        }

        if (pos.y < 0 + verticalEdgePercent)
        {
            y = -1;
        }
        else if (pos.y > Screen.height - verticalEdgePercent)
        {
            y = 1;
        }

        moveCamera(x, y);
    }

    private void InteractWithWorld(Vector3 pos)
    {
        int layer;
        GameObject obj;
        Vector3 worldPos = GetWorldPosFromScreen(pos, out layer, out obj);
        if (OnPositionClick != null && layer != -1)
        {
            OnPositionClick(layer, worldPos, obj);
        }
    }

    private void CancelInteraction()
    {
        if (OnCancelClick != null)
        {
            OnCancelClick();
        }
    }

    public Vector3 GetWorldPosFromScreen(Vector3 pos, out int layer, out GameObject obj)
    {
        Ray r = _camera.ScreenPointToRay(pos);
        RaycastHit hitInfo;

        if (Physics.Raycast(r, out hitInfo, _raycastDist, _clickLayers))
        {
            obj = hitInfo.collider.gameObject;
            layer = obj.layer;

            return hitInfo.point;
        }

        obj = null;
        layer = -1;
        return Vector3.zero;
    }

    public void moveCamera(float translationH, float translationV)
	{
		translationH *= Time.unscaledDeltaTime * _scrollSpeed * _currentZoomLevel;
		translationV *= Time.unscaledDeltaTime * _scrollSpeed * _currentZoomLevel;
		
		// Move camera
		transform.Translate(translationH, 0, translationV, Space.World);
		
		// Keep it in bounds
		transform.position = new Vector3(Mathf.Clamp(transform.position.x, _leftEdge.position.x, _rightEdge.position.x), transform.position.y, Mathf.Clamp(transform.position.z, _bottomEdge.position.z, _topEdge.position.z));

		// update
		_activeZoomPosition = transform.position;
	}

	// center the camera on a given position
	public void CenterOnObject(Transform obj)
	{
		// TODO
        // move camera to obj pos with some offset
        // zoom all the way in
	}

	// called from game manager
	private void Zoom(ZoomDirection direction)
	{
		switch (direction)
		{
		case ZoomDirection.IN:
			if (_currentZoomLevel > _maxZoomLevel)
			{
				_currentZoomLevel--;
				_activeZoomPosition += transform.forward * _zoomAmount;
			}
			break;

		case ZoomDirection.OUT:
			if (_currentZoomLevel < _minZoomLevel)
			{
				_currentZoomLevel++;
				_activeZoomPosition -= transform.forward * _zoomAmount;
			}
			break;

		default:
			Debug.LogError("Invalid value to camera zoom");
			break;
		}

		transform.position = _activeZoomPosition;
	}
}