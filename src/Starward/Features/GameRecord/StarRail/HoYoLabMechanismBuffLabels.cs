using System;
using System.Collections.Generic;
using System.Globalization;

namespace Starward.Features.GameRecord.StarRail;


/// <summary>
/// Localized values of the official HoYoLAB <c>mechanism_buff</c> key.
/// Source: m20230509hy150knmyo locale JSON files published by HoYoverse.
/// </summary>
internal static class HoYoLabMechanismBuffLabels
{

    private static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en-us"] = "Grit Mechanics",
            ["ru-ru"] = "Механика Боевого духа",
            ["de-de"] = "Kampfgeistmechanik",
            ["es-es"] = "Mecanismo de Coraje",
            ["ja-jp"] = "戦意メカニズム",
            ["ko-kr"] = "전의 메커니즘",
            ["th-th"] = "กลไกวิญญาณนักสู้",
            ["vi-vn"] = "Cơ Chế Chiến Ý",
            ["zh-cn"] = "战意机制",
            ["zh-tw"] = "戰意機制",
        };


    /// <summary>
    /// Resolves the label from the language selected in Starward settings.
    /// When Starward follows the system language, installed UI culture is used.
    /// </summary>
    public static string GetCurrent()
    {
        string? configuredLanguage = AppConfig.Language;

        string?[] candidates =
        [
            configuredLanguage,
            CultureInfo.InstalledUICulture.Name,
            CultureInfo.CurrentUICulture.Name,
            CultureInfo.CurrentCulture.Name,
        ];

        foreach (string? candidate in candidates)
        {
            string locale = NormalizeLocale(candidate);
            if (Labels.TryGetValue(locale, out string? value))
            {
                return value;
            }

            if (locale.Equals("zh-hk", StringComparison.OrdinalIgnoreCase) &&
                Labels.TryGetValue("zh-tw", out value))
            {
                return value;
            }
        }

        return Labels["en-us"];
    }


    private static string NormalizeLocale(string? cultureName)
    {
        string locale = string.IsNullOrWhiteSpace(cultureName)
            ? string.Empty
            : cultureName.Replace('_', '-').ToLowerInvariant();

        return locale switch
        {
            "en" => "en-us",
            "ru" => "ru-ru",
            "de" => "de-de",
            "es" => "es-es",
            "it" => "it-it",
            "ja" => "ja-jp",
            "ko" => "ko-kr",
            "th" => "th-th",
            "vi" => "vi-vn",
            "zh" => "zh-cn",
            "zh-hans" => "zh-cn",
            "zh-hant" => "zh-tw",
            _ => locale,
        };
    }

}
