using System.Globalization;

namespace Starward.Language;


/// <summary>
/// Formats user-visible durations with time-unit labels from the active language resources.
/// </summary>
public static class LocalizedTimeFormatter
{

    private static CultureInfo CurrentCulture => Lang.Culture ?? CultureInfo.CurrentUICulture;


    private static string HourUnit => Lang.Common_HourShort;

    private static string MinuteUnit => Lang.Common_MinuteShort;

    private static string SecondUnitText => Lang.Common_SecondShort;


    public static string SecondUnit => $" {SecondUnitText}";


    public static string FormatHoursMinutes(TimeSpan value)
    {
        value = Normalize(value);
        long hours = (long)Math.Floor(value.TotalHours);
        return Join(
            hours.ToString(CurrentCulture), HourUnit,
            value.Minutes.ToString(CurrentCulture), MinuteUnit);
    }


    public static string FormatHoursMinutesCompact(TimeSpan value)
    {
        value = Normalize(value);
        long totalMinutes = (long)Math.Round(value.TotalMinutes);
        if (totalMinutes < 1)
        {
            return Join("0", MinuteUnit);
        }
        if (totalMinutes < 60)
        {
            return Join(totalMinutes.ToString(CurrentCulture), MinuteUnit);
        }

        long hours = totalMinutes / 60;
        long minutes = totalMinutes % 60;
        return minutes == 0
            ? Join(hours.ToString(CurrentCulture), HourUnit)
            : Join(
                hours.ToString(CurrentCulture), HourUnit,
                minutes.ToString(CurrentCulture), MinuteUnit);
    }


    public static string FormatMinutesSeconds(TimeSpan value, bool padSeconds = false)
    {
        value = Normalize(value);
        long minutes = (long)Math.Floor(value.TotalMinutes);
        string seconds = padSeconds
            ? value.Seconds.ToString("D2", CurrentCulture)
            : value.Seconds.ToString(CurrentCulture);
        return Join(
            minutes.ToString(CurrentCulture), MinuteUnit,
            seconds, SecondUnitText);
    }


    public static string FormatHoursMinutesSeconds(TimeSpan value, bool padMinutesAndSeconds = false)
    {
        value = Normalize(value);
        long hours = (long)Math.Floor(value.TotalHours);
        string minutes = padMinutesAndSeconds
            ? value.Minutes.ToString("D2", CurrentCulture)
            : value.Minutes.ToString(CurrentCulture);
        string seconds = padMinutesAndSeconds
            ? value.Seconds.ToString("D2", CurrentCulture)
            : value.Seconds.ToString(CurrentCulture);
        return Join(
            hours.ToString(CurrentCulture), HourUnit,
            minutes, MinuteUnit,
            seconds, SecondUnitText);
    }


    public static string FormatHours(double minutes)
    {
        if (double.IsNaN(minutes) || double.IsInfinity(minutes) || minutes < 0)
        {
            minutes = 0;
        }

        return Join((minutes / 60d).ToString("0.#", CurrentCulture), HourUnit);
    }


    private static TimeSpan Normalize(TimeSpan value)
    {
        return value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }


    private static string Join(params string[] parts)
    {
        return string.Join(' ', parts);
    }

}
