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

    private class WeatherInfo
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

        public float Temp { get { return AverageTemp + DailyVariation; } }
    }

    [SerializeField] private WindZone _windZone;
    [SerializeField] private AnimationCurve _temperatureCurve;
    [SerializeField] private GameObject _rainPrefab;
    [SerializeField] private GameObject _snowPrefab;

    private int _forecastLength = 14;
    private int _maxWindAngle = 15;
    private int _minWeatherDuration = 3;
    private int _maxWeatherDuration = 18;

    private Light _sun;
    private float _sunnyShadowStrength = 1f;
    private float _cloudyShadowStrength = 0.25f;

    private int _weatherSeed;
    private int _currentDay;
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
    }

    private void Init(int worldSeed)
    {
        System.Random prng = new System.Random(worldSeed);

        _sun = GameManager.Instance.TimeManager.Sun;
        _rain = ((GameObject)Instantiate(_rainPrefab, _rainPrefab.transform.position, _rainPrefab.transform.rotation)).GetComponent<ParticleSystem>();
        _snow = ((GameObject)Instantiate(_snowPrefab, _snowPrefab.transform.position, _snowPrefab.transform.rotation)).GetComponent<ParticleSystem>();

        _currentDay = GameManager.Instance.TimeManager.CurrentDate.GetDay();
        _currentSeason = GameManager.Instance.TimeManager.CurrentDate.GetSeason();

        _baseWindDirection = new Vector3(0f, prng.Next(360), 0f);
        transform.Rotate(_baseWindDirection);

        _weatherSeed = prng.Next();
        _biweeklyWeather = GenerateNextWeather(_forecastLength, _weatherSeed);
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
        int hoursPerDay = (int)TimeConstants.HOURS_PER_TOD_CYCLE;
        for (int i = 0; i < numberOfDays; ++i)
        {
            WeatherInfo w = new WeatherInfo();

            // wind speed
            w.WindSpeed = (float)weatherGen.NextDouble() + 1;

            // wind direction
            directionModifier = weatherGen.Next(-1, 1 + 1);
            angle = weatherGen.Next(0, _maxWindAngle + 1);
            if (directionModifier != 0)
            {
                angle *= directionModifier;
            }

            w.WindDirection = Quaternion.Euler(0f, angle, 0f) * _baseWindDirection;

            // temperature
            int dayIndex = _currentDay + i;
            w.AverageTemp = _temperatureCurve.Evaluate(dayIndex * step);

            w.DailyVariation = (float)weatherGen.NextDouble() + 1;
            w.NightVariation = w.DailyVariation + (int)_currentSeason;

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
                w.Severity = chance / 10;

                int actual = weatherGen.Next(0, 100 + 1);
                if (actual > chance)
                {
                    // Ollie says "IT'S GONNA RAIN!"
                    w.StartTime = weatherGen.Next(0, hoursPerDay);
                    w.Duration = weatherGen.Next(_minWeatherDuration, _maxWeatherDuration);
                    if (w.StartTime + w.Duration > hoursPerDay)
                    {
                        w.MultiDay = true;
                        weatherCarryOver = true;
                        overflowHours = hoursPerDay - (w.StartTime + w.Duration);
                    }

                    w.WeatherType = (w.Temp < 0f ? eWeatherType.Snow : eWeatherType.Rain);
                }
                else
                {
                    w.Duration = 0;
                    w.StartTime = 0;
                    w.MultiDay = false;
                    w.WeatherType = (w.Severity > 5 ? eWeatherType.Cloudy : eWeatherType.Sunny);
                }
            }
        }

        return info;
    }

    private void UpdateTodaysWeather(int day)
    {
        int weeklyIndex = day % _forecastLength;
        WeatherInfo today = _biweeklyWeather[weeklyIndex];

        // TODO update UI

        // wind
        _windZone.windMain = today.WindSpeed;
        _windZone.windTurbulence = today.Severity / 10f;
        this.transform.rotation = Quaternion.Euler(today.WindDirection);

        // light/precipitation
        _rain.gameObject.SetActive(false);
        _snow.gameObject.SetActive(false);

        switch (today.WeatherType)
        {
            case eWeatherType.Sunny:
                _sun.color = Color.white;
                _sun.shadowStrength = _sunnyShadowStrength;
                break;

            case eWeatherType.Cloudy:
                _sun.color = Color.grey;
                _sun.shadowStrength = _cloudyShadowStrength;
                break;

            case eWeatherType.Rain:
                _sun.color = Color.grey;
                _sun.shadowStrength = _cloudyShadowStrength;

                _rain.gameObject.SetActive(true);
                break;

            case eWeatherType.Snow:
                _snow.gameObject.SetActive(true);
                break;
        }

        if (weeklyIndex == _forecastLength - 1)
        {
            // we need new weather!
            _biweeklyWeather = GenerateNextWeather(_forecastLength, _weatherSeed);
        }
    }

    private void OnTimeChanged(object sender, System.EventArgs args)
    {
        // TODO
        // check the day against the weather schedule to see if the weather should change
        // if it is a weather day, check the time it is scheduled to start and duration
        // if it is a day weather should stop, stop it

        TimeChangedArgs changed = (TimeChangedArgs)args;
        int day = changed.dateTime.GetDay();
        if (_currentDay != day)
        {
            _currentDay = day;
            UpdateTodaysWeather(_currentDay);
        }
    }

    void OnDestroy()
    {
        GameManager.Instance.TerrainManager.OnWorldCreated -= OnWorldCreated;
    }

    private void OnWorldCreated()
    {
        int worldSeed = GameManager.Instance.TerrainManager.WorldSeed;
        //Init(worldSeed);
    }
}
