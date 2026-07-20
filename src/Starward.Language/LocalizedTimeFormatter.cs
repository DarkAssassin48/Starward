using System.Globalization;

namespace Starward.Language;


/// <summary>
/// Formats user-visible durations with localized one-character time-unit labels.
/// Unit labels are stored in the existing Lang*.resx language packs.
/// </summary>
public static class LocalizedTimeFormatter
{

    private static CultureInfo CurrentCulture => Lang.Culture ?? CultureInfo.CurrentUICulture;


    public static string SecondUnit => GetUnit("Common_SecondShort", "s");


    public static string FormatHoursMinutes(TimeSpan value)
    {
        value = Normalize(value);
        long hours = (long)Math.Floor(value.TotalHours);
        return Join(
            hours.ToString(CurrentCulture), GetUnit("Common_HourShort", "h"),
            value.Minutes.ToString(CurrentCulture), GetUnit("Common_MinuteShort", "m"));
    }


    public static string FormatMinutesSeconds(TimeSpan value, bool padSeconds = false)
    {
        value = Normalize(value);
        long minutes = (long)Math.Floor(value.TotalMinutes);
        string seconds = padSeconds
            ? value.Seconds.ToString("D2", CurrentCulture)
            : value.Seconds.ToString(CurrentCulture);
        return Join(
            minutes.ToString(CurrentCulture), GetUnit("Common_MinuteShort", "m"),
            seconds, GetUnit("Common_SecondShort", "s"));
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
            hours.ToString(CurrentCulture), GetUnit("Common_HourShort", "h"),
            minutes, GetUnit("Common_MinuteShort", "m"),
            seconds, GetUnit("Common_SecondShort", "s"));
    }


    private static TimeSpan Normalize(TimeSpan value)
    {
        return value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }


    private static string GetUnit(string name, string fallback)
    {
        return Lang.ResourceManager.GetString(name, CurrentCulture) ?? fallback;
    }


    private static string Join(params string[] parts)
    {
        return string.Join(' ', parts);
    }

}
