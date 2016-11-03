using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public enum Menu
{
    Main
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum State
    {
        Unknown, // just for default at game start
        Init,
        MainMenu,
        Loading,
        InGame
    }

	[SerializeField] private Texture2D _cursor;
    [SerializeField] private GameObject[] _menuPrefabs;
    [SerializeField] private GameObject[] _managerPrefabs;

    [HideInInspector]
    [SerializeField] private TerrainManager _terrainManager;
    public TerrainManager TerrainManager { get { return _terrainManager; } set { _terrainManager = value; } }

    [HideInInspector]
    [SerializeField] private InputController _inputController;
    public InputController InputController { get { return _inputController; } set { _inputController = value; } }

    [HideInInspector]
    [SerializeField] private CameraController _cameraController;
    public CameraController CameraController { get { return _cameraController; } set { _cameraController = value; } }

    [HideInInspector]
    [SerializeField]
    private TimeManager _timeManager;
    public TimeManager TimeManager { get { return _timeManager; } set { _timeManager = value; } }

    [HideInInspector]
    [SerializeField]
    private WeatherManager _weatherManager;
    public WeatherManager WeatherManager { get { return _weatherManager; } set { _weatherManager = value; } }

    [HideInInspector]
    [SerializeField]
    private TileManager _tileManager;
    public TileManager TileManager { get { return _tileManager; } set { _tileManager = value; } }

    private Game _currentGame;
    private State _currentState;
    private State _previousState = State.Unknown;
    private MenuUIController _activeMenu;
    private Menu _previousMenuType;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        Instance = this;

        _currentState = State.Init;
    }

    void Update()
    {
        if (_currentState != _previousState)
        {
            _previousState = _currentState;
            ActOnState();
        }
    }

    private void ActOnState()
    {
        switch (_currentState)
        {
            case State.Init:
                Init();
                break;

            case State.MainMenu:
                LoadMenu(Menu.Main);
                break;

            case State.Loading:
                UnloadMenu(_previousMenuType);
                break;

            case State.InGame:
                InitMap();
                break;
        }
    }

    private void Init()
    {
        Cursor.SetCursor(_cursor, Vector2.zero, CursorMode.ForceSoftware);

        FileManager.LoadSavedGames();

        // load first game for now
        //_currentGame = FileManager.LoadGame(0);

        _currentState = State.MainMenu;
    }

    private void UnloadMenu(Menu type)
    {
        if (_activeMenu != null)
        {
            _activeMenu.RemoveSubMenuListeners((int)_previousMenuType);
            Destroy(_activeMenu.gameObject);
            _activeMenu = null;
        }
    }

    private void LoadMenu(Menu menuType)
    {
        UnloadMenu(_previousMenuType);

        Transform canvas = GameObject.Find("Canvas").transform;
        GameObject menu = (GameObject)GameObject.Instantiate(_menuPrefabs[(int)menuType], canvas);
        _activeMenu = menu.GetComponent<MenuUIController>();
        _previousMenuType = menuType;
        menu.transform.localPosition = Vector3.zero;
        menu.transform.localScale = Vector3.one;

        switch (menuType)
        {
            case Menu.Main:
                int main = (int)menuType;
                _activeMenu.SetButtonListener(main, 0, OnContinueGameClicked);
                _activeMenu.SetButtonListener(main, 1, OnLoadGameClicked);
                _activeMenu.SetButtonListener(main, 2, OnNewGameClicked);
                _activeMenu.SetButtonListener(main, 3, OnOptionsClicked);
                _activeMenu.SetButtonListener(main, 4, OnExitClicked);

                // hide continue button if there are no saved games
                _activeMenu.ShowMenuButton(main, 0, (FileManager.NumSavedGames > 0));
                break;
        }
    }

    #region Main Menu Actions
    private void OnContinueGameClicked()
    {
        int lastGameIndex = FileManager.GetLastLoadedIndex();
        _currentGame = FileManager.LoadGame(lastGameIndex);
        Debug.LogFormat("Loaded Game -> save slot: {0}, seed: {1}", _currentGame.SaveSlot, _currentGame.WorldSeed);
        _currentState = State.Loading;
        LoadScene(1);
    }

    private void OnLoadGameClicked()
    {
        // TODO change to the load game sub menu
        Debug.Log("TODO");
    }

    private void OnNewGameClicked()
    {
        _currentGame = FileManager.NewGame();
        Debug.LogFormat("New Game -> save slot: {0}, seed: {1}", _currentGame.SaveSlot, _currentGame.WorldSeed);
        _currentState = State.Loading;
        LoadScene(1);
    }

    private void OnOptionsClicked()
    {
        // TODO change to the options sub menu
        Debug.Log("TODO");
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
#endregion

    #region Scene Loading
    private void LoadScene(int sceneIndex)
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene(sceneIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        _currentState = State.InGame;
    }
    #endregion

    private void InitMap()
    {
        CreateManagers();

        TerrainManager.LoadMap(_currentGame.WorldSeed);
        TileManager.InitializeTiles(TerrainManager.MapSize);
    }

    private void CreateManagers()
    {
        for (int i = 0; i < _managerPrefabs.Length; ++i)
        {
            GameObject.Instantiate(_managerPrefabs[i], Vector3.zero, Quaternion.identity);
        }
    }
}
