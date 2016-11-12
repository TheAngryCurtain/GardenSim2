using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

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

    private bool _isModifying = false;
    private float _updateDelayTime = 1f;
    private float _remainingDelayTime = 0f;
    private float _countDownIncrement = 0.1f;
    private bool _countDownRunning = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateTimeButtonStates(Time.timeScale);
    }

    public void OnTerrainModButtonClicked(int id)
    {
        switch (id)
        {
            case 0: // flatten terrain
                _isModifying = GameManager.Instance.TerrainManager.ToggleInteractMode();
                _modifyButton.GetComponentInChildren<Text>().text = (_isModifying ? "Interact" : "Modify");
                break;

            case 1: // undo terrain modify
                GameManager.Instance.TerrainManager.UndoLastModify();
                break;
        }

        _undoButton.gameObject.SetActive(_isModifying);
    }

    public void OnTimeManipButtonClicked(int id)
    {
        GameManager.Instance.TimeManager.ManipulateTime(id);
        UpdateTimeButtonStates(Time.timeScale);
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
        StartUpdateCountDown();
        _playerWalletLabel.text = string.Format("${0}", current);

        AddToPlayerUpdatePanel(2, amount);
    }

    public void OnPlayerXpUpdated(object data)
	{
        object[] fields = (object[])data;
        int amount = (int)fields[0];
        int level = (int)fields[1];
        int total = (int)fields[2];
        int next = (int)fields[3];
        int difference = (int)fields[4];

		_playerLevelLabel.text = string.Format("Lv. {0}", level);

        StartUpdateCountDown();
        if (_playerXPBar.maxValue != next)
        {
            // it's assumed that the level went up and there is a new next
            // update the min and max
            _playerXPBar.minValue = total - difference;
            _playerXPBar.maxValue = next;

            AddToPlayerUpdatePanel(0, level);
        }

		_playerXPBar.value = (total / (float)next) * next;
        AddToPlayerUpdatePanel(1, amount);
    }

    public void OnPlayerStaminaUpdated(int amount, int current, int max)
	{
        StartUpdateCountDown();
        if (_playerStaminaBar.maxValue != max)
        {
            _playerStaminaBar.maxValue = max;
        }

		_playerStaminaBar.value = (current / (float)max) * max;
        AddToPlayerUpdatePanel(3, amount);
    }

    private void StartUpdateCountDown()
    {
        if (!_countDownRunning)
        {
            _countDownRunning = true;
            _remainingDelayTime = _updateDelayTime;

            // empty out texts
            for (int i = 0; i < _updatePanelTexts.Length; ++i)
            {
                _updatePanelTexts[i].text = string.Empty;
            }

            InvokeRepeating("CheckUpdatePanelTimer", 0f, _countDownIncrement);
        }
    }

    private void CheckUpdatePanelTimer()
    {
        // general idea:
        // once any player modification has happened, it will start the timer with startUpdateCountDown
        // any other modifications that happen after have _updateDelayTime seconds to get their updates in before it's shown
        // once zero is hit, the panel is shown

        _remainingDelayTime -= _countDownIncrement;
        if (_remainingDelayTime <= 0f)
        {
            _countDownRunning = false;
            CancelInvoke("CheckUpdatePanelTimer");
            StartCoroutine(ShowUpdatePanel());
        }
    }

    private void AddToPlayerUpdatePanel(int index, int value)
    {
        string attr = string.Empty;
        switch (index)
        {
            case 0: attr = "Level"; break;
            case 1: attr = "XP"; break;
            case 2: attr = "Wallet"; break;
            case 3: attr = "Stamina"; break;
            case 4: attr = "???"; break;
        }

        string symbol = (value > 0 ? "+" : "-");
        _updatePanelTexts[index].text = string.Format("{0} {1} {2}", attr, symbol, Mathf.Abs(value));
    }

    private IEnumerator ShowUpdatePanel()
    {
        _playerUpdatePanel.SetActive(true);

        yield return new WaitForSeconds(3f);

        _playerUpdatePanel.SetActive(false);
    }

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
