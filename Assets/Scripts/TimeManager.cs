﻿using UnityEngine;
using System.Collections;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    public event EventHandler OnTimeChanged;

    private float mTotalTimePassed;
    private TimeOfDay mCurrentTimeOfDay = TimeOfDay.EarlyMorning;
    private CustomDateTime mDateTime;

    private int mCurrentHour;
    private int mCurrentDay;
    private int mCurrentMonth;
    private int mCurrentYear;

    void Awake()
    {
        Instance = this;
    }

    public TimeOfDay GetCurrentTimeOfDay()
    {
        return mCurrentTimeOfDay;
    }

    [SerializeField]
    private int tTimeScale = 5;

    [SerializeField]
    private GameObject mSunPrefab;
    private GameObject cSun;

    public void Init(CustomDateTime dateTime)
    {
        mDateTime = dateTime;

        mCurrentTimeOfDay = TimeOfDay.EarlyMorning;
        mTotalTimePassed = 0.0f;

        mCurrentHour = mDateTime.GetHour();
        mCurrentDay = mDateTime.GetDay();
        mCurrentMonth = mDateTime.GetMonth();
        mCurrentYear = mDateTime.GetYear();

        cSun = (GameObject)Instantiate(mSunPrefab, Vector3.zero, mSunPrefab.transform.rotation);

        if (mDateTime != null)
        {
            mDateTime.OnDateChanged += new OnDateChanged(OnPlayerDateTimeChanged);
        }
    }

    void Update()
    {
        // Increment our time and apply the time scaler 
        float mTimePassedDelta = Time.deltaTime * tTimeScale;
        mTotalTimePassed += mTimePassedDelta;

        // Increment our passage of time
        mDateTime.ApplyPassageOfTime(mTimePassedDelta);
        //mDateTime.PrintDateTime();
        RotateSun();
    }

    // mDateTime.GetDayTimeSeconds() is the current time in seconds.
    // 0 degrees is sun rise. 90 degrees is 12pm noon. is 43200seconds
    private void RotateSun()
    {
        if(cSun != null)
        {
            float curTimeSeconds = mDateTime.GetDayTimeSeconds();
            float rotation = Mathf.Lerp(0.0f, 360.0f, (curTimeSeconds / TimeConstants.SECONDS_PER_DAY));
            Quaternion sunRotation = cSun.transform.rotation;
            sunRotation = Quaternion.Euler(rotation, sunRotation.y, sunRotation.z);
            cSun.transform.rotation = sunRotation;
        }
    }

    private void OnPlayerDateTimeChanged(object sender, EventArgs e)
    {
        Debug.Log("DateTimeChanged!");
        if (sender != null)
        {
            CustomDateTime timeFromEvent = (CustomDateTime)sender;
            TimeChangedArgs args = new TimeChangedArgs();
            if (timeFromEvent != null)
            {
                if(mCurrentHour != timeFromEvent.GetHour())
                {
                    args.HourChanged = true;
                }

                if (mCurrentDay != timeFromEvent.GetDay())
                {
                    args.DayChanged = true;
                }

                if (mCurrentMonth != timeFromEvent.GetMonth())
                {
                    args.MonthChanged = true;
                }

                if (mCurrentYear != timeFromEvent.GetYear())
                {
                    args.YearChanged = true;
                }

                // Store the current date values locally
                mCurrentHour = timeFromEvent.GetHour();
                mCurrentDay = timeFromEvent.GetDay();
                mCurrentMonth = timeFromEvent.GetMonth();
                mCurrentYear = timeFromEvent.GetYear();

                // update ToD
                if (args.HourChanged)
                {
                    if (mCurrentHour <= 4)
                    {
                        mCurrentTimeOfDay = TimeOfDay.Night;
                    }
                    else if (mCurrentHour <= 8)
                    {
                        mCurrentTimeOfDay = TimeOfDay.EarlyMorning;
                    }
                    else if (mCurrentHour <= 12)
                    {
                        mCurrentTimeOfDay = TimeOfDay.Morning;
                    }
                    else if (mCurrentHour <= 16)
                    {
                        mCurrentTimeOfDay = TimeOfDay.Afternoon;
                    }
                    else if (mCurrentHour <= 20)
                    {
                        mCurrentTimeOfDay = TimeOfDay.LateAfternoon;
                    }
                    else if (mCurrentHour <= 24)
                    {
                        mCurrentTimeOfDay = TimeOfDay.Dusk;
                    }
                }

                args.timeOfDay = mCurrentTimeOfDay;

                timeFromEvent.PrintDateTime();

                if (OnTimeChanged != null)
                {
                    OnTimeChanged(this, args);
                }
            }
        }
    }
} 

public enum TimeOfDay
{
    EarlyMorning = 1,       // 4AM->8AM
    Morning,                // 8AM->12PM   
    Afternoon,              // 12PM->4PM
    LateAfternoon,          // 4PM->8PM
    Dusk,                   // 8PM->12AM
    Night                   // 12AM->4AM
}

public class TimeChangedArgs : EventArgs
{
    public CustomDateTime dateTime;
    public TimeOfDay timeOfDay;
    public bool HourChanged;
    public bool DayChanged;
    public bool MonthChanged;
    public bool YearChanged;
}