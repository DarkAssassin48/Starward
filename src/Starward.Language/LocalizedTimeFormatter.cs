using System.Globalization;
using System.Resources;

namespace Starward.Language;


/// <summary>
/// Formats user-visible durations with localized short time-unit labels.
/// Localization strings are stored in TimeUnits*.resx files in this project.
/// </summary>
public static class LocalizedTimeFormatter
{

    private static readonly ResourceManager resourceManager = new(
        "Starward.Language.TimeUnits",
        typeof(LocalizedTimeFormatter).Assembly);


    private static CultureInfo CurrentCulture => Lang.Culture ?? CultureInfo.CurrentUICulture;


    public static string FormatHoursMinutes(TimeSpan value)
    {
        value = Normalize(value);
        long hours = (long)Math.Floor(value.TotalHours);
        return string.Format(
            CurrentCulture,
            GetString("HoursMinutesShortFormat", "{0} h {1} min"),
            hours,
            value.Minutes);
    }


    public static string FormatMinutesSeconds(TimeSpan value, bool padSeconds = false)
    {
        value = Normalize(value);
        long minutes = (long)Math.Floor(value.TotalMinutes);
        string resourceName = padSeconds
            ? "MinutesSecondsPaddedShortFormat"
            : "MinutesSecondsShortFormat";
        string fallback = padSeconds
            ? "{0} min {1:D2} s"
            : "{0} min {1} s";

        return string.Format(
            CurrentCulture,
            GetString(resourceName, fallback),
            minutes,
            value.Seconds);
    }


    public static string FormatHoursMinutesSeconds(TimeSpan value, bool padMinutesAndSeconds = false)
    {
        value = Normalize(value);
        long hours = (long)Math.Floor(value.TotalHours);
        string resourceName = padMinutesAndSeconds
            ? "HoursMinutesSecondsPaddedShortFormat"
            : "HoursMinutesSecondsShortFormat";
        string fallback = padMinutesAndSeconds
            ? "{0} h {1:D2} min {2:D2} s"
            : "{0} h {1} min {2} s";

        return string.Format(
            CurrentCulture,
            GetString(resourceName, fallback),
            hours,
            value.Minutes,
            value.Seconds);
    }


    private static TimeSpan Normalize(TimeSpan value)
    {
        return value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }


    private static string GetString(string name, string fallback)
    {
        return resourceManager.GetString(name, CurrentCulture) ?? fallback;
    }

}
