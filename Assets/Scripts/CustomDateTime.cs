using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization;

public delegate void OnDateChanged(object sender, EventArgs e);

public class CustomDateTime
{
    private int mHour;
    private int mDay;
    private int mMonth;
    private int mYear;
    private Season mSeason;
    private float mDayTimeSeconds;

    public int GetHour()
    {
        return mHour;
    }

    public int GetDay()
    {
        return mDay;
    }

    public int GetMonth()
    {
        return mMonth;
    }

    public int GetYear()
    {
        return mYear;
    }

    public Season GetSeason()
    {
        return mSeason;
    }

    public float GetDayTimeSeconds()
    {
        return mDayTimeSeconds;
    }

    // Default ctor
    public CustomDateTime()
    {
        mHour = 0;
        mDay = 1;
        mMonth = 1;
        mYear = 1;
        mSeason = Season.Spring;
        mDayTimeSeconds = 0.0f;
    }

    // Specific DateTime ctor
    public CustomDateTime(int hour, int day, int month, int year, Season season, float dayTimeSeconds)
    {
        mHour = hour;
        mDay = day;
        mMonth = month;
        mYear = year;
        mSeason = season;
        mDayTimeSeconds = dayTimeSeconds;
    }

    public event OnDateChanged OnDateChanged;

    /// <summary>
    /// We have 12 Months, each one has 30 days.
    /// Each day has 24 hours.
    /// </summary>
    /// <param name="timePassed"></param>
    public void ApplyPassageOfTime(float timePassed)
    {
        bool dateChanged = false;
        mDayTimeSeconds += timePassed;

        if (mDayTimeSeconds >= (mHour + 1) * TimeConstants.SECONDS_PER_HOUR)
        {
            mHour++;
            dateChanged = true;

            if (mDayTimeSeconds >= TimeConstants.SECONDS_PER_DAY)
            {
                mDay++;
                mHour = 0;
                mDayTimeSeconds = 0.0f;

                if (mDay > TimeConstants.DAYS_PER_MONTH)
                {
                    mMonth++;
                    mDay = 1;

                    if (mMonth >= TimeConstants.MONTHS_PER_SEASON * ((int)mSeason + 1))
                    {
                        mSeason = (Season)(mSeason + 1);
                    }

                    if (mMonth >= TimeConstants.MONTHS_PER_YEAR)
                    {
                        mYear++;
                        mMonth = 1;
                    }
                }
            }
        }

        if (dateChanged)
        {
            if (OnDateChanged != null)
            {
                OnDateChanged(this, EventArgs.Empty);
            }
        }
    }

    public void PrintDateTime()
    {
        string debugPrint = string.Format("(CurrentTime) - [Year: {0}] [Season: {1}] [Month: {2}] [Day: {3}] [Hour: {4}]", mYear, mSeason.ToString(), mMonth, mDay, mHour);
        Debug.Log(debugPrint);
        debugPrint = null;
    }
}
