using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    public System.Action<bool> OnTerrainModifyModeChanged;
    public System.Action OnUndo;
    public System.Action<int> OnToolSelected;

	// terrain 
    [SerializeField] Button _modifyButton;
    [SerializeField] Button _undoButton;

	// player
	[SerializeField] Text _playerWalletLabel;
	[SerializeField] Text _playerLevelLabel;
	[SerializeField] Slider _playerStaminaBar;
	[SerializeField] Slider _playerXPBar;
    [SerializeField] GameObject _playerUpdatePanel;
    [SerializeField] Text[] _updatePanelTexts;

	// time
	[SerializeField] Text _seasonLabel;
	[SerializeField] Text _monthLabel;
	[SerializeField] Text _dayLabel;
    [SerializeField] Button _playButton;
    [SerializeField] Button _pauseButton;
    [SerializeField] Button _ffButton;

	// weather
	[SerializeField] Image _weatherIcon;
	[SerializeField] Text _weatherTypeLabel;
	[SerializeField] Text _weatherPrecipLabel;
	[SerializeField] Text _weatherTempLabel;
    [SerializeField] Sprite[] _weatherIcons;

    // tool
    [SerializeField] Button _toolBoxButton;
	[SerializeField] Sprite[] _toolIcons;
	[SerializeField] Image _activeToolIcon;
    [SerializeField] Button[] _toolButtons;

    private bool _isModifying = false;
    private bool _toolsShowing = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateTimeButtonStates(Time.timeScale);
		SetActiveToolIcon(ToolData.eToolType.None);

        // test
        //UnlockTools(0); // lock all
        //UnlockToolAtIndex(0, true);
        //UnlockToolAtIndex(2, true);
    }

    public void OnTerrainModButtonClicked(int id)
    {
        switch (id)
        {
            case 0: // flatten terrain
                _isModifying = !_isModifying;
                if (OnTerrainModifyModeChanged != null)
                {
                    OnTerrainModifyModeChanged(_isModifying);
                }
                _modifyButton.GetComponentInChildren<Text>().text = (_isModifying ? "Interact" : "Modify");
                break;

            case 1: // undo terrain modify
                if (OnUndo != null)
                {
                    OnUndo();
                }
                break;
        }

        _undoButton.gameObject.SetActive(_isModifying);
    }

    public void OnTimeManipButtonClicked(int id)
    {
        GameManager.Instance.TimeManager.ManipulateTime(id);
        UpdateTimeButtonStates(Time.timeScale);
    }

    public void OnToolBoxClicked()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            // default back to hand
            SetActiveToolIcon(ToolData.eToolType.None);
            _toolsShowing = false;
            ShowToolButtons(false);

            if (OnToolSelected != null)
            {
                OnToolSelected(-1);
            }
        }
        else
        {
            _toolsShowing = !_toolsShowing;
            ShowToolButtons(_toolsShowing);
        }
    }

    private void ShowToolButtons(bool show)
    {
        for (int i = 0; i < _toolButtons.Length; ++i)
        {
            _toolButtons[i].gameObject.SetActive(show);
        }
    }

    private void UnlockTools(int count)
    {
        bool unlocked = false;
        for (int i = 0; i < _toolButtons.Length; ++i)
        {
            unlocked = (i < count);
            UnlockToolAtIndex(i, unlocked);
        }
    }

    private void UnlockToolAtIndex(int index, bool unlock)
    {
        _toolButtons[index].interactable = unlock;
    }

    private void OnToolUnlocked(int count)
    {
        UnlockTools(count);
    }

    public void OnToolButtonClicked(int id)
    {
		SetActiveToolIcon((ToolData.eToolType)id);
		ShowToolButtons(false);
		_toolsShowing = false;

		if (OnToolSelected != null)
		{
            OnToolSelected(id);
        }
    }

    private void UpdateTimeButtonStates(float currentScale)
    {
        _playButton.interactable = (currentScale == 0f || currentScale > 1f);
        _pauseButton.interactable = (currentScale != 0f);
        _ffButton.interactable = (currentScale > 0f && currentScale < 4f);

        // fast forward subscript
        Text subText = _ffButton.transform.GetChild(0).GetComponent<Text>();
        string scale;
        if (currentScale > 1f)
        {
            scale = string.Format("{0}x", currentScale);
        }
        else
        {
            scale = "";
        }

        subText.text = scale;

        OnTimeStopped(currentScale == 0f);
    }

	private void SetActiveToolIcon(ToolData.eToolType tool)
	{
		if (tool == ToolData.eToolType.None)
		{
			_activeToolIcon.gameObject.SetActive(false);
			return;
		}

		_activeToolIcon.gameObject.SetActive(true);
		_activeToolIcon.sprite = _toolIcons[(int)tool];
	}

    private void OnTimeStopped(bool stopped)
    {
        _modifyButton.interactable = !stopped;
    }

    public void OnTerrainModified(int index)
    {
        _undoButton.interactable = (index > 0);
    }

	public void OnPlayerWalletUpdated(int amount, int current)
	{
        _playerWalletLabel.text = string.Format("${0}", current);
    }

    public void OnPlayerXpUpdated(object data)
	{
        object[] fields = (object[])data;
        //int amount = (int)fields[0];
        int level = (int)fields[1];
        int total = (int)fields[2];
        int next = (int)fields[3];
        int difference = (int)fields[4];

		_playerLevelLabel.text = string.Format("Lv. {0}", level);

        if (_playerXPBar.maxValue != next)
        {
            // it's assumed that the level went up and there is a new next
            // update the min and max
            _playerXPBar.minValue = total - difference;
            _playerXPBar.maxValue = next;
        }

		_playerXPBar.value = (total / (float)next) * next;
    }

    public void OnPlayerStaminaUpdated(int amount, int current, int max)
	{
        if (_playerStaminaBar.maxValue != max)
        {
            _playerStaminaBar.maxValue = max;
        }

		_playerStaminaBar.value = (current / (float)max) * max;
    }

    // currently not used. Need to rethink the update panel
    // should probably only show for level up bonuses, but until items
    // have been designed, no point yet.
    //private void AddToPlayerUpdatePanel(int index, int value)
    //{
    //    string title = "Level UP!";
    //    _updatePanelTexts[0].text = title;

    //    string attr = string.Empty;
    //    switch (index)
    //    {
    //        case 1: attr = "Wallet"; break;
    //        case 2: attr = "Stamina"; break;
    //        case 3: attr = "<Item Name>"; break;
    //    }

    //    string symbol = (value > 0 ? "+" : "-");
    //    _updatePanelTexts[index].text = string.Format("{0} {1} {2}", attr, symbol, Mathf.Abs(value));
    //}

    //private IEnumerator ShowUpdatePanel()
    //{
    //    _playerUpdatePanel.SetActive(true);

    //    yield return new WaitForSeconds(3f);

    //    _playerUpdatePanel.SetActive(false);
    //}

	public void OnTimeChanged(object sender, System.EventArgs e)
	{
		TimeChangedArgs args = (TimeChangedArgs)e;
		if (args.DayChanged)
		{
			_dayLabel.text = args.dateTime.GetDay().ToString();
		}

		if (args.MonthChanged)
		{
			_monthLabel.text = args.dateTime.GetMonth().ToString();
		}

		if (args.SeasonChanged)
		{
			_seasonLabel.text = args.dateTime.GetSeason().ToString();
		}
	}

	public void OnWeatherChanged(WeatherManager.WeatherInfo info)
	{
        Sprite icon;
		switch(info.WeatherType)
        {
            default:
            case WeatherManager.eWeatherType.Sunny:
                icon = _weatherIcons[0]; // [1] is moon, for the hour even though we don't know the time yet
                break;

            case WeatherManager.eWeatherType.Cloudy:
                icon = _weatherIcons[2];
                break;

            case WeatherManager.eWeatherType.Rain:
                icon = _weatherIcons[3];
                break;

            case WeatherManager.eWeatherType.Snow:
                icon = _weatherIcons[4];
                break;
        }

        _weatherIcon.sprite = icon;
		_weatherTypeLabel.text = info.WeatherType.ToString();
		_weatherPrecipLabel.text = string.Format("{0}%", info.ChanceOfPrecipitation);
		_weatherTempLabel.text = string.Format("{0:0.0}°C", info.AverageTemp);
	}
}
