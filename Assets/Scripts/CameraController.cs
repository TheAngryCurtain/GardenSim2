using UnityEngine;
using System.Collections;
using System;

public class CameraController : MonoBehaviour, IControllable
{	
	enum ZoomDirection { IN = 0, OUT = 1 };

	private float _maxZoomLevel = 1;
	private float _minZoomLevel = 5;

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

        transform.position += new Vector3(0f, 25f, 0f);
        transform.Rotate(new Vector3(45f, 0f, 0f));

		_currentZoomLevel = _maxZoomLevel;

		// give camera control
		GameManager.Instance.InputController.SetControllable(this);
    }

	public void AcceptMouseAction(MouseAction a, Vector3 mousePosition)
    {
		Debug.LogFormat("action: {0}, pos: {1}", a, mousePosition);
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
		float scrollSpeed = 20f;
		moveCamera(h, v, scrollSpeed);
	}

    // Move the camera based on user input
    public void moveCamera(float translationH, float translationV, float scrollSpeed)
	{
		translationH *= Time.deltaTime * scrollSpeed * _currentZoomLevel;
		translationV *= Time.deltaTime * scrollSpeed * _currentZoomLevel;
		
		// Move camera
		transform.Translate(translationH, 0, translationV, Space.World);
		
		// Keep it in bounds
		transform.position = new Vector3(Mathf.Clamp(transform.position.x, _leftEdge.position.x, _rightEdge.position.x), transform.position.y, Mathf.Clamp(transform.position.z, _bottomEdge.position.z, _topEdge.position.z));

		// update
		_activeZoomPosition = transform.position;
	}

	// center the camera on a given position
	public void CenterOnPosition(Vector3 position)
	{
		Vector3 newPos = new Vector3(position.x, 7f, position.z - 7f);
		this.gameObject.transform.position = newPos;
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
				_activeZoomPosition += transform.forward * 5f;
			}
			break;

		case ZoomDirection.OUT:
			if (_currentZoomLevel < _minZoomLevel)
			{
				_currentZoomLevel++;
				_activeZoomPosition -= transform.forward * 5f;
			}
			break;

		default:
			Debug.LogError("Invalid value to camera zoom");
			break;
		}

		transform.position = _activeZoomPosition;
	}


}