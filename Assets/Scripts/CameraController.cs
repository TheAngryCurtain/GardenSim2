using UnityEngine;
using System.Collections;
using System;

public class CameraController : MonoBehaviour 
{	
	enum ZoomDirection { IN = 0, OUT = 1 };
	
	private Vector3 activeZoomPosition;
	private float maxZoomLevel = 1;
	private float minZoomLevel = 5;
	private float currentZoomLevel;

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
    }

    void Start ()
	{
        GameManager.Instance.InputController.AxisInput += HandleDirectionalInput;
        GameManager.Instance.InputController.MouseButtonInput += HandleMouseInput;
        GameManager.Instance.InputController.MouseScrollInput += HandleMouseScroll;

        currentZoomLevel = maxZoomLevel;
	}

    private void HandleMouseInput(Vector3 mousePosition)
    {
        Debug.Log(mousePosition);
    }

    private void HandleMouseScroll(float scroll)
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

    // Move the camera based on user input
    public void moveCamera(float translationH, float translationV, float scrollSpeed)
	{
		translationH *= Time.deltaTime * scrollSpeed * currentZoomLevel;
		translationV *= Time.deltaTime * scrollSpeed * currentZoomLevel;
		
		// Move camera
		transform.Translate(translationH, 0, translationV, Space.World);
		
		// Keep it in bounds
		transform.position = new Vector3(Mathf.Clamp(transform.position.x, _leftEdge.position.x, _rightEdge.position.x), transform.position.y, Mathf.Clamp(transform.position.z, _bottomEdge.position.z, _topEdge.position.z));

		// update
		activeZoomPosition = transform.position;
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
			if (currentZoomLevel > maxZoomLevel)
			{
				currentZoomLevel--;
				activeZoomPosition += transform.forward * 5f;
			}
			break;

		case ZoomDirection.OUT:
			if (currentZoomLevel < minZoomLevel)
			{
				currentZoomLevel++;
				activeZoomPosition -= transform.forward * 5f;
			}
			break;

		default:
			Debug.LogError("Invalid value to camera zoom");
			break;
		}

		transform.position = activeZoomPosition;
	}

	// handle directional input
	public void HandleDirectionalInput(float h, float v)
	{
		float scrollSpeed = 20f;
		moveCamera(h, v, scrollSpeed);
	}
}