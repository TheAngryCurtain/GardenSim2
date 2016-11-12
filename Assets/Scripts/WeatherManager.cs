using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeatherManager : MonoBehaviour
{
    public enum eWeatherType
    {
        Sunny,
        Cloudy,
        Rain,
        Snow
    };

	public class WeatherInfo
    {
        public eWeatherType WeatherType;

        public float WindSpeed;
        public Vector3 WindDirection;

        public float AverageTemp;
        public float DailyVariation;
        public float NightVariation;

        public float ChanceOfPrecipitation;
        public int Severity; // 0 - 10 scale, 10 being the worst
        public int StartTime; // hour (0 - 24)
        public int Duration; // hours
        public bool MultiDay;
    }

	public System.Action<WeatherInfo> OnWeatherChanged;

    [SerializeField] private WindZone _windZone;
    [SerializeField] private AnimationCurve _temperatureCurve;
    [SerializeField] private GameObject _rainPrefab;
    [SerializeField] private GameObject _snowPrefab;

    private int _forecastLength = 15;
    private int _maxWindAngle = 15;
    private int _minWeatherDuration = 3;
    private int _maxWeatherDuration = 18;
    private float _rainWindModifier = 5f;
    private float _snowWindModifier = 8f;

    private Light _sun;
    private float _sunnyShadowStrength = 1f;
    private float _cloudyShadowStrength = 0.25f;

    private int _timeScale;
    private int _weatherSeed;
    private int _currentDay;
    private int _currentMonth;
    private Season _currentSeason;
    private Vector3 _baseWindDirection;
    private ParticleSystem _rain;
    private ParticleSystem _snow;

    private List<WeatherInfo> _biweeklyWeather;

    void Awake()
    {
        GameManager.Instance.WeatherManager = this;

        GameManager.Instance.TerrainManager.OnWorldCreated += OnWorldCreated;
        GameManager.Instance.TimeManager.OnTimeChanged += OnTimeChanged;

		OnWeatherChanged += UIController.Instance.OnWeatherChanged;
    }

    private void Init(int worldSeed)
    {
        System.Random prng = new System.Random(worldSeed);

        _sun = GameManager.Instance.TimeManager.Sun;
        InitWeatherPrefabs();

        TimeManager tm = GameManager.Instance.TimeManager;
        _currentDay = tm.CurrentDate.GetDay();
        _currentMonth = tm.CurrentDate.GetMonth();
        _currentSeason = tm.CurrentDate.GetSeason();

        _baseWindDirection = new Vector3(0f, prng.Next(360), 0f);
        transform.Rotate(_baseWindDirection);

        _weatherSeed = prng.Next();
        _biweeklyWeather = GenerateNextWeather(_forecastLength, _weatherSeed);

        UpdateTodaysWeather(_currentDay, 0);
    }

    private void InitWeatherPrefabs()
    {
        _rain = ((GameObject)Instantiate(_rainPrefab, Vector3.zero, _rainPrefab.transform.rotation)).GetComponent<ParticleSystem>();
        _snow = ((GameObject)Instantiate(_snowPrefab, Vector3.zero, _snowPrefab.transform.rotation)).GetComponent<ParticleSystem>();
        _rain.transform.SetParent(GameManager.Instance.CameraController.transform);
        _snow.transform.SetParent(GameManager.Instance.CameraController.transform);
        _rain.transform.localPosition = new Vector3(0f, 50f, 0f);
        _snow.transform.localPosition = new Vector3(0f, 50f, 0f);
    }

    private List<WeatherInfo> GenerateNextWeather(int numberOfDays, int weatherSeed)
    {
        System.Random weatherGen = new System.Random(weatherSeed);
        List<WeatherInfo> info = new List<WeatherInfo>(numberOfDays);
        int directionModifier = 1;
        float step = 4 / (float)360; // 4 seasons over 360 days
        float angle = 0;
        bool weatherCarryOver = false;
        int overflowHours = 0;
        int hoursPerDay = 24;
        for (int i = 0; i < numberOfDays; ++i)
        {
            WeatherInfo w = new WeatherInfo();

            // wind speed
            w.WindSpeed = (float)weatherGen.NextDouble();

            // wind direction
            directionModifier = weatherGen.Next(-1, 1 + 1);
            angle = weatherGen.Next(0, _maxWindAngle + 1);
            if (directionModifier != 0)
            {
                angle *= directionModifier;
            }

            w.WindDirection = Quaternion.Euler(0f, angle, 0f) * _baseWindDirection;

            // temperature
            int dayIndex = (_currentMonth * _currentDay) + i;
            w.AverageTemp = _temperatureCurve.Evaluate(dayIndex * step);

            w.DailyVariation = (float)weatherGen.NextDouble();
            w.NightVariation = w.DailyVariation + (1 + (int)_currentSeason);

            // precipitation
            if (weatherCarryOver)
            {
                // weather is carrying over from yesterday, just copy it
                WeatherInfo previous = info[i - 1];
                w.ChanceOfPrecipitation = previous.ChanceOfPrecipitation;
                w.StartTime = 0;
                w.Duration = overflowHours;
                w.WeatherType = previous.WeatherType;
                w.Severity = previous.Severity;

                weatherCarryOver = false;
                overflowHours = 0;
            }
            else
            {
                // new weather
                int chance = weatherGen.Next(0, 100 + 1);
                w.ChanceOfPrecipitation = chance;
                w.Severity = weatherGen.Next(0, 10 + 1) / 20;

                int actual = weatherGen.Next(0, 100 + 1); // need to do something here to reduce the chance of rain
                if (actual < chance)
                {
                    // let it precipitate!
                    w.StartTime = weatherGen.Next(0, hoursPerDay);
                    w.Duration = weatherGen.Next(_minWeatherDuration, _maxWeatherDuration);
                    if (w.StartTime + w.Duration > hoursPerDay)
                    {
                        w.MultiDay = true;
                        weatherCarryOver = true;
                        overflowHours = (w.StartTime + w.Duration) - hoursPerDay;
                    }

                    w.WeatherType = (w.AverageTemp < 0f ? eWeatherType.Snow : eWeatherType.Rain);
                }
                else
                {
                    w.Duration = 0;
                    w.StartTime = 0;
                    w.MultiDay = false;
                    w.WeatherType = (w.Severity > 5 ? eWeatherType.Cloudy : eWeatherType.Sunny);
                }
            }

            info.Add(w);
        }

        return info;
    }

    private void UpdateTodaysWeather(int day, int hour)
    {
        int weeklyIndex = day % _forecastLength;
        WeatherInfo today = _biweeklyWeather[weeklyIndex];

        if (hour == 0)
        {
            // TODO update UI
            Debug.LogFormat("Today's Weather: type: {0}, wind speed: {1}, wind direction: {2}, Avg. Temp: {3}, % precip: {4}, start time: {5}, duration: {6}, severity: {7}, multiday: {8}",
                today.WeatherType, today.WindSpeed, today.WindDirection, today.AverageTemp, today.ChanceOfPrecipitation, today.StartTime, today.Duration, today.Severity, today.MultiDay);

            // wind
            _windZone.windMain = today.WindSpeed;
            _windZone.windTurbulence = today.Severity / 10f;
            this.transform.rotation = Quaternion.Euler(today.WindDirection);

            float windForwardX = this.transform.forward.x;
            float windFowardZ = this.transform.forward.z;

            // affect particle systems from wind
            ParticleSystem.ForceOverLifetimeModule rainVelocity = _rain.forceOverLifetime;
            rainVelocity.x = windForwardX + _rainWindModifier * today.Severity;
            rainVelocity.z = windFowardZ + _rainWindModifier * today.Severity;

            ParticleSystem.ForceOverLifetimeModule snowVelocity = _snow.forceOverLifetime;
            snowVelocity.x = windForwardX + _snowWindModifier * today.Severity;
            snowVelocity.z = windFowardZ + _snowWindModifier * today.Severity;

            // light/precipitation
            if (today.WeatherType != eWeatherType.Rain && _rain.gameObject.activeInHierarchy)
            {
                _rain.gameObject.SetActive(false);
            }

            if (today.WeatherType != eWeatherType.Snow && _snow.gameObject.activeInHierarchy)
            {
                _snow.gameObject.SetActive(false);
            }

            Color nextColor = Color.white;
            float nextStrength = 0;
            switch (today.WeatherType)
            {
                case eWeatherType.Sunny:
                    nextColor = Color.white;
                    nextStrength = _sunnyShadowStrength;
                    _windZone.windTurbulence *= 0.5f;
                    break;

                case eWeatherType.Cloudy:
                    nextColor = Color.grey;
                    nextStrength = _cloudyShadowStrength;
                    _windZone.windTurbulence *= 0.5f;
                    break;

                case eWeatherType.Rain:
                    nextColor = Color.grey;
                    nextStrength = _cloudyShadowStrength;
                    break;

                case eWeatherType.Snow:
                    break;
            }

            if (_sun.color != nextColor)
            {
                StartCoroutine(LerpColor(nextColor));
            }
            if (_sun.shadowStrength != nextStrength)
            {
                StartCoroutine(LerpShadowStrength(nextStrength));
            }

            if (weeklyIndex == _forecastLength - 1)
            {
                // we need new weather!
                _biweeklyWeather = GenerateNextWeather(_forecastLength, _weatherSeed);
            }
        }

        if (today.StartTime == hour)
        {
            if (today.WeatherType == eWeatherType.Rain)
            {
                _rain.gameObject.SetActive(true);
            }
            else if (today.WeatherType == eWeatherType.Snow)
            {
                _snow.gameObject.SetActive(true);
            }
        }
        else if ((!today.MultiDay && today.StartTime + today.Duration == hour) || (today.MultiDay && today.Duration == hour))
        {
            if (today.WeatherType == eWeatherType.Rain)
            {
                _rain.gameObject.SetActive(false);
            }
            else if (today.WeatherType == eWeatherType.Snow)
            {
                _snow.gameObject.SetActive(false);
            }
        }

		if (OnWeatherChanged != null)
		{
			OnWeatherChanged(today);
		}
    }

    private IEnumerator LerpColor(Color next)
    {
        while (_sun.color != next)
        {
            _sun.color = Color.Lerp(_sun.color, next, Time.deltaTime);

            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator LerpShadowStrength(float next)
    {
        while (_sun.shadowStrength != next)
        {
            _sun.shadowStrength = Mathf.Lerp(_sun.shadowStrength, next, Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
    }

    public void OnTimeChanged(object sender, System.EventArgs args)
    {
        // TODO
        // check the day against the weather schedule to see if the weather should change
        // if it is a weather day, check the time it is scheduled to start and duration
        // if it is a day weather should stop, stop it

        TimeChangedArgs changed = (TimeChangedArgs)args;
        _currentDay = changed.dateTime.GetDay();
        _currentMonth = changed.dateTime.GetMonth();
        _currentSeason = changed.dateTime.GetSeason();

        if (changed.HourChanged)
        {
            UpdateTodaysWeather(_currentDay, changed.dateTime.GetHour());
        }
    }

    void OnDestroy()
    {
        GameManager.Instance.TerrainManager.OnWorldCreated -= OnWorldCreated;
        GameManager.Instance.TimeManager.OnTimeChanged -= OnTimeChanged;
    }

    public void OnWorldCreated()
    {
        int worldSeed = GameManager.Instance.TerrainManager.WorldSeed;
        Init(worldSeed);
    }
}
