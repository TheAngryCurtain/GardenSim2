using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

	// terrain 
    [SerializeField] Text _flattenButtonText;
    [SerializeField] Button _undoButton;

	// player
	[SerializeField] Text _playerWalletLabel;
	[SerializeField] Text _playerLevelLabel;
	[SerializeField] Slider _playerStaminaBar;
	[SerializeField] Slider _playerXPBar;

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
                SetFlattenText(_isModifying);
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
    }

    private void SetFlattenText(bool state)
    {
        _flattenButtonText.text = (state ? "Interact" : "Modify");
    }

    public void OnTerrainModified(int index)
    {
        _undoButton.interactable = (index > 0);
    }

	public void OnPlayerWalletUpdated(int current)
	{
		_playerWalletLabel.text = string.Format("${0}", current);
	}

	public void OnPlayerXpUpdated(int level, int total, int next, int difference)
	{
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

	public void OnPlayerStaminaUpdated(int current, int max)
	{
        if (_playerStaminaBar.maxValue != max)
        {
            _playerStaminaBar.maxValue = max;
        }

		_playerStaminaBar.value = (current / (float)max) * max;
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
