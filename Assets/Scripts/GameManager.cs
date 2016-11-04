using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public enum Menu
{
    Main,
    NewGame,
    LoadGame,
    Option
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
    private 

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
                UnloadMenu();
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

        _currentState = State.MainMenu;
    }

    private void UnloadMenu()
    {
        if (_activeMenu != null)
        {
            _activeMenu.RemoveMenuListeners();
            Destroy(_activeMenu.gameObject);
            _activeMenu = null;
        }
    }

    private void LoadMenu(Menu menuType)
    {
        UnloadMenu();

        Transform canvas = GameObject.Find("Canvas").transform;
        GameObject menu = (GameObject)GameObject.Instantiate(_menuPrefabs[(int)menuType], canvas);
        _activeMenu = menu.GetComponent<MenuUIController>();
        menu.transform.localPosition = Vector3.zero;
        menu.transform.localScale = Vector3.one;

        int type = (int)menuType;
        switch (menuType)
        {
            case Menu.Main:
                _activeMenu.SetTitle("Main Menu");
                _activeMenu.SetButtonListener(0, ContinueGame);
                _activeMenu.SetButtonListener(1, () => LoadMenu(Menu.NewGame) );
                _activeMenu.SetButtonListener(2, () => LoadMenu(Menu.LoadGame) );
                _activeMenu.SetButtonListener(3, () => LoadMenu(Menu.Option) );
                _activeMenu.SetButtonListener(4, ExitGame);

                // hide continue button if there are no saved games
                _activeMenu.ShowMenuButton(0, (FileManager.NumSavedGames > 0));
                break;

            case Menu.NewGame:
                _activeMenu.SetTitle("Create Game");
                _activeMenu.SetButtonListener(0, () => CreateNewGame(_activeMenu.GetFieldInput()) );
                _activeMenu.SetButtonListener(1, () => LoadMenu(Menu.Main));

                break;

            case Menu.LoadGame:
                _activeMenu.SetTitle("Load Game");
                _activeMenu.SetButtonListener(0, () => LoadSelectedGame(_activeMenu.GetActiveToggleIndex()) );
                _activeMenu.SetButtonListener(1, () => CopySelectedGame(_activeMenu.GetActiveToggleIndex()) );
                _activeMenu.SetButtonListener(2, () => DeleteSelectedGame(_activeMenu.GetActiveToggleIndex()) );
                _activeMenu.SetButtonListener(3, () => LoadMenu(Menu.Main) );

                if (FileManager.NumSavedGames > 0)
                {
                    PopulateSavedGamesList(type);
                }
                else
                {
                    // disable load, copy, delete
                    _activeMenu.DisableMenuButton(0, false);
                    _activeMenu.DisableMenuButton(1, false);
                    _activeMenu.DisableMenuButton(2, false);
                }
                break;

            case Menu.Option:
                break;
        }
    }

    #region menu Actions
    private void ContinueGame()
    {
        int lastGameIndex = FileManager.GetLastLoadedIndex();
        _currentGame = FileManager.LoadGame(lastGameIndex);
        Debug.LogFormat("Loaded Game -> save slot: {0}, seed: {1}", _currentGame.SaveSlot, _currentGame.WorldSeed);
        _currentState = State.Loading;
        LoadScene(1);
    }

    private void LoadSelectedGame(int gameIndex)
    {
        _currentGame = FileManager.LoadGame(gameIndex);
        _currentState = State.Loading;
        LoadScene(1);
    }

    private void CreateNewGame(string gameName)
    {
        _currentGame = FileManager.NewGame(gameName);
        Debug.LogFormat("New Game -> save slot: {0}, seed: {1}", _currentGame.SaveSlot, _currentGame.WorldSeed);
        _currentState = State.Loading;
        LoadScene(1);
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PopulateSavedGamesList(int menuIndex)
    {
        _activeMenu.SetScrollListData(menuIndex, FileManager.SavedGames);
    }

    private void CopySelectedGame(int gameIndex)
    {
        Game copy = FileManager.CopyGame(gameIndex);
        _activeMenu.CopyToggleItem(copy.SaveSlot, copy.GameName);
    }

    private void DeleteSelectedGame(int gameIndex)
    {
        FileManager.DeleteGame(gameIndex);
        _activeMenu.RemoveToggleItem(gameIndex);

        if (FileManager.NumSavedGames == 0)
        {
            // disable load, copy, delete
            _activeMenu.DisableMenuButton(0, false);
            _activeMenu.DisableMenuButton(1, false);
            _activeMenu.DisableMenuButton(2, false);
        }
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
