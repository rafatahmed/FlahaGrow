using System.Globalization;
using System.Text.RegularExpressions;

namespace FlahaGrow.Core.PlantLight;

/// <summary>365-day local-standard hourly intervals; clock labels denote interval starts.</summary>
public static class AnnualTime
{
    public static string Alignment(int utcOffsetMinutes)
    {
        if (Math.Abs((long)utcOffsetMinutes) > 840) throw new ArgumentOutOfRangeException(nameof(utcOffsetMinutes), "UTC offset must be -840 to +840 minutes.");
        var minutes = Math.Abs(utcOffsetMinutes);
        return $"annual-365;UTC{(utcOffsetMinutes < 0 ? "-" : "+")}{minutes / 60:00}:{minutes % 60:00};local-standard;hourly";
    }
    public static int HourIndex(int month, int day, int hour)
    {
        if (hour < 0 || hour > 23) throw new ArgumentOutOfRangeException(nameof(hour));
        return (new DateTime(2001, month, day).DayOfYear - 1) * 24 + hour;
    }
    public static string Label(int index)
    {
        if (index < 0 || index >= 8760) throw new ArgumentOutOfRangeException(nameof(index));
        var start = new DateTime(2001, 1, 1).AddHours(index);
        return start.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture) + "–" + start.AddHours(1).ToString("HH:mm", CultureInfo.InvariantCulture) + " local standard time (365-day year)";
    }
    public static string ValidateAxis(string value)
    {
        value = value.Trim();
        if (value.Length == 0) return value;
        if (!Regex.IsMatch(value, @"^annual-365;UTC[+-](?:0\d|1[0-4]):[0-5]\d;local-standard;hourly$"))
            throw new ArgumentException("Alignment is not a selected date/hour. Leave blank for a single source, or connect Hour Index → Alignment with the actual simulation UTC offset. Do not connect Date here.");
        if (value.Contains("14:") && !value.Contains("14:00")) throw new ArgumentException("UTC offset cannot exceed 14 hours.");
        return value;
    }
}
