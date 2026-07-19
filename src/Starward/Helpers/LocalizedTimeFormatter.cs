using System;
using System.Globalization;

namespace Starward.Helpers;

internal static class LocalizedTimeFormatter
{
    public static string FormatHoursMinutes(TimeSpan value)
    {
        long hours = (long)Math.Floor(value.TotalHours);
        int minutes = value.Minutes;
        string culture = CultureInfo.CurrentUICulture.Name;

        if (culture.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return $"{hours} ч {minutes} мин";
        if (culture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            return $"{hours} Std. {minutes} Min.";
        if (culture.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            return $"{hours} h {minutes} min";
        if (culture.StartsWith("it", StringComparison.OrdinalIgnoreCase))
            return $"{hours} h {minutes} min";
        if (culture.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            return $"{hours}時間 {minutes}分";
        if (culture.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            return $"{hours}시간 {minutes}분";
        if (culture.StartsWith("th", StringComparison.OrdinalIgnoreCase))
            return $"{hours} ชม. {minutes} นาที";
        if (culture.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
            return $"{hours} giờ {minutes} phút";
        if (IsTraditionalChinese(culture))
            return $"{hours} 時 {minutes} 分";
        if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return $"{hours} 时 {minutes} 分";

        return $"{hours} h {minutes} min";
    }


    public static string FormatHoursMinutesSeconds(TimeSpan value)
    {
        long hours = (long)Math.Floor(value.TotalHours);
        int minutes = value.Minutes;
        int seconds = value.Seconds;
        string culture = CultureInfo.CurrentUICulture.Name;

        if (culture.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours} ч {minutes} мин {seconds} с" : $"{minutes} мин {seconds} с";
        if (culture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours} Std. {minutes} Min. {seconds} Sek." : $"{minutes} Min. {seconds} Sek.";
        if (culture.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours} h {minutes} min {seconds} s" : $"{minutes} min {seconds} s";
        if (culture.StartsWith("it", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours} h {minutes} min {seconds} s" : $"{minutes} min {seconds} s";
        if (culture.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours}時間 {minutes}分 {seconds}秒" : $"{minutes}分 {seconds}秒";
        if (culture.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours}시간 {minutes}분 {seconds}초" : $"{minutes}분 {seconds}초";
        if (culture.StartsWith("th", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours} ชม. {minutes} นาที {seconds} วินาที" : $"{minutes} นาที {seconds} วินาที";
        if (culture.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours} giờ {minutes} phút {seconds} giây" : $"{minutes} phút {seconds} giây";
        if (IsTraditionalChinese(culture))
            return hours > 0 ? $"{hours} 小時 {minutes} 分 {seconds} 秒" : $"{minutes} 分 {seconds} 秒";
        if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return hours > 0 ? $"{hours} 小时 {minutes} 分 {seconds} 秒" : $"{minutes} 分 {seconds} 秒";

        return hours > 0 ? $"{hours} h {minutes} min {seconds} s" : $"{minutes} min {seconds} s";
    }


    private static bool IsTraditionalChinese(string culture)
    {
        return culture.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase)
            || culture.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase)
            || culture.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase);
    }
}
