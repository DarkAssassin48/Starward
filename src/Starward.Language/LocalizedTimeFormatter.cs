using System.Globalization;

namespace Starward.Language;


/// <summary>
/// Formats user-visible durations with localized one-character time-unit labels.
/// Starward resources are preferred when present; fallbacks keep this feature
/// independent from upstream Crowdin resource-file changes.
/// </summary>
public static class LocalizedTimeFormatter
{

    private static CultureInfo CurrentCulture => Lang.Culture ?? CultureInfo.CurrentUICulture;


    public static string SecondUnit => $" {GetUnit("Common_SecondShort", "s")}";


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
        string? resource = Lang.ResourceManager.GetString(name, CurrentCulture);
        if (!string.IsNullOrWhiteSpace(resource))
        {
            return resource;
        }

        if (string.Equals(CurrentCulture.TwoLetterISOLanguageName, "ru", StringComparison.OrdinalIgnoreCase))
        {
            return name switch
            {
                "Common_HourShort" => "ч",
                "Common_MinuteShort" => "м",
                "Common_SecondShort" => "с",
                _ => fallback,
            };
        }

        return fallback;
    }


    private static string Join(params string[] parts)
    {
        return string.Join(' ', parts);
    }

}
