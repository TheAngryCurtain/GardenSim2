using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

	[SerializeField] private Texture2D _cursor;
    [SerializeField] private GameObject[] _managers;

    // TODO
    // Create all objects that need to be in the scene
    // hook up the events listeners/handlers

    [HideInInspector]
    [SerializeField] private TerrainManager _terrainManager;
    public TerrainManager TerrainManager { get { return _terrainManager; } set { _terrainManager = value; } }

    [HideInInspector]
    [SerializeField] private InputController _inputController;
    public InputController InputController { get { return _inputController; } set { _inputController = value; } }

    [HideInInspector]
    [SerializeField] private CameraController _cameraController;
    public CameraController CameraController { get { return _cameraController; } set { _cameraController = value; } }

    void Awake()
    {
        Instance = this;
    }

	void Start()
	{
		Cursor.SetCursor(_cursor, Vector2.zero, CursorMode.ForceSoftware);

        CreateManagers();

		//// create tile manager -> creates new tiles and have them subscribe to events
		////					   -> tiles need to be able to listen to events from tile manager and send events to back to it

	}

    private void CreateManagers()
    {
        for (int i = 0; i < _managers.Length; ++i)
        {
            GameObject.Instantiate(_managers[i], Vector3.zero, Quaternion.identity);
        }
    }
}
