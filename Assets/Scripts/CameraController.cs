using UnityEngine;
using System.Collections;
using System;

public class CameraController : MonoBehaviour, IControllable
{
    public Action<Vector3> OnPositionClick;

	enum ZoomDirection { IN = 0, OUT = 1 };

    [SerializeField] private Camera _camera;
    [SerializeField] LayerMask _clickLayers;

	private int _maxZoomLevel = 1;
	private int _minZoomLevel = 5;
    private float _zoomAmount = 5f;
	private	float _scrollSpeed = 15f;
    private float _raycastDist = 100f;

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

        GameManager.Instance.InputController.SetControllable(this);
    }

	public void AcceptMouseAction(MouseAction a, Vector3 mousePosition)
    {
		//Debug.LogFormat("action: {0}, pos: {1}", a, mousePosition);
        if (a == MouseAction.LeftClick)
        {
            InteractWithWorld(mousePosition);
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

    private void InteractWithWorld(Vector3 pos)
    {
        Ray r = _camera.ScreenPointToRay(pos);
        RaycastHit hitInfo;
        Debug.DrawLine(r.origin, r.origin + r.direction * _raycastDist, Color.red, 10f);

        if (Physics.Raycast(r, out hitInfo, _raycastDist, _clickLayers))
        {
            GameObject debug = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debug.transform.position = hitInfo.point;
            debug.layer = LayerMask.NameToLayer("TerrainObject");

            if (OnPositionClick != null)
            {
                OnPositionClick(hitInfo.point);
            }
        }
    }

    public void moveCamera(float translationH, float translationV)
	{
		translationH *= Time.deltaTime * _scrollSpeed * _currentZoomLevel;
		translationV *= Time.deltaTime * _scrollSpeed * _currentZoomLevel;
		
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