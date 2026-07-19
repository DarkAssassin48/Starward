using System.Globalization;

namespace Starward.Language;


public enum PlayTimeText
{
    Title,
    StartupCount,
    ThisDay,
    ThisWeek,
    ThisMonth,
    LastTime,
}


/// <summary>
/// Provides localized time formatting and play-time labels for the application.
/// All culture-specific time text belongs to the Starward.Language project.
/// </summary>
public static class LocalizedTimeFormatter
{

    public static string FormatHoursMinutes(TimeSpan value)
    {
        value = Normalize(value);
        long hours = (long)Math.Floor(value.TotalHours);
        int minutes = value.Minutes;
        string format = GetResource("PlayTimeButton_DurationFormat", "{0} h {1} min");
        return string.Format(CurrentCulture, format, hours, minutes);
    }


    public static string FormatHoursMinutesSeconds(TimeSpan value)
    {
        value = Normalize(value);
        long hours = (long)Math.Floor(value.TotalHours);
        int minutes = value.Minutes;
        int seconds = value.Seconds;

        if (hours > 0)
        {
            string hoursMinutes = string.Format(
                CurrentCulture,
                GetResource("PlayTimeButton_DurationFormat", "{0} h {1} min"),
                hours,
                minutes);
            string secondsText = string.Format(
                CurrentCulture,
                GetResource("Common_SecondsFormat", "{0} s"),
                seconds);
            return $"{hoursMinutes.Trim()} {secondsText.Trim()}";
        }

        string format = GetResource(
            "ImaginariumTheaterPage_TotalPerformanceDurationFormat",
            "{0} min {1} s");
        return string.Format(CurrentCulture, format, minutes, seconds);
    }


    public static string GetPlayTimeText(PlayTimeText text)
    {
        string[] labels = GetPlayTimeLabels(CurrentCulture.Name);
        return labels[(int)text];
    }


    private static CultureInfo CurrentCulture => Lang.Culture ?? CultureInfo.CurrentUICulture;


    private static TimeSpan Normalize(TimeSpan value)
    {
        return value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }


    private static string GetResource(string name, string fallback)
    {
        return Lang.ResourceManager.GetString(name, CurrentCulture) ?? fallback;
    }


    private static string[] GetPlayTimeLabels(string culture)
    {
        if (culture.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return ["Игровое время", "Количество запусков", "Сегодня", "На этой неделе", "В этом месяце", "Последний запуск"];
        if (culture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            return ["Spielzeit", "Startanzahl", "Heute", "Diese Woche", "Dieser Monat", "Letzter Start"];
        if (culture.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            return ["Tiempo de juego", "Número de inicios", "Hoy", "Esta semana", "Este mes", "Último inicio"];
        if (culture.StartsWith("it", StringComparison.OrdinalIgnoreCase))
            return ["Tempo di gioco", "Numero di avvii", "Oggi", "Questa settimana", "Questo mese", "Ultimo avvio"];
        if (culture.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            return ["プレイ時間", "起動回数", "今日", "今週", "今月", "前回の起動"];
        if (culture.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            return ["플레이 시간", "실행 횟수", "오늘", "이번 주", "이번 달", "마지막 실행"];
        if (culture.StartsWith("th", StringComparison.OrdinalIgnoreCase))
            return ["เวลาเล่น", "จำนวนครั้งที่เปิด", "วันนี้", "สัปดาห์นี้", "เดือนนี้", "เปิดครั้งล่าสุด"];
        if (culture.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
            return ["Thời gian chơi", "Số lần khởi động", "Hôm nay", "Tuần này", "Tháng này", "Lần khởi động trước"];
        if (IsTraditionalChinese(culture))
            return ["遊戲時間", "啟動次數", "今日", "本週", "本月", "上次啟動"];
        if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return ["游戏时间", "启动次数", "今日", "本周", "本月", "上次启动"];

        return ["Play Time", "Startup Count", "This Day", "This Week", "This Month", "Last Time"];
    }


    private static bool IsTraditionalChinese(string culture)
    {
        return culture.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase)
            || culture.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase)
            || culture.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase);
    }

}