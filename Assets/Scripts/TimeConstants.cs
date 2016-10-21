using UnityEngine;
using System.Collections;

public static class TimeConstants 
{
    public static readonly int DAYS_PER_MONTH = 30;
    public static readonly int MONTHS_PER_YEAR = 12;
    public static readonly float HOURS_PER_TOD_CYCLE = 4.0f;
    public static readonly float SECONDS_PER_MINUTE = 60.0f;
    public static readonly float SECONDS_PER_HOUR = SECONDS_PER_MINUTE * 60.0f;
    public static readonly float SECONDS_PER_DAY = SECONDS_PER_HOUR * 24.0f;
    public static readonly float SECONDS_PER_TOD_CYCLE = SECONDS_PER_HOUR * HOURS_PER_TOD_CYCLE;
}
