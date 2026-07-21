using Starward.Language;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord.StarRail;


/// <summary>
/// Loads localized strings used by the HoYoLAB game-record pages.
/// Official labels bundled during the build are preferred, while successful
/// remote results are cached per locale for the lifetime of the application.
/// </summary>
internal static class HoYoLabMi18nService
{

    private const string ResourceId = "m20230509hy150knmyo";

    private static readonly HttpClient HttpClient = new(
        new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    private static readonly Lazy<Task<IReadOnlyDictionary<string, string>?>> BundledMechanismBuffCache =
        new(LoadBundledMechanismBuffAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyDictionary<string, string>?>>> LocaleCache =
        new(StringComparer.OrdinalIgnoreCase);


    public static async Task<string?> GetStringAsync(
        string key,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        string locale = NormalizeLocale((culture ?? Lang.Culture).Name);

        if (key.Equals("mechanism_buff", StringComparison.OrdinalIgnoreCase))
        {
            IReadOnlyDictionary<string, string>? bundledValues =
                await BundledMechanismBuffCache.Value.WaitAsync(cancellationToken);

            if (bundledValues?.TryGetValue(locale, out string? bundledValue) is true &&
                !string.IsNullOrWhiteSpace(bundledValue))
            {
                return bundledValue;
            }
        }

        IReadOnlyDictionary<string, string>? values = await GetLocaleAsync(locale, cancellationToken);

        if (values is null)
        {
            LocaleCache.TryRemove(locale, out _);
            return null;
        }

        if (values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }


    private static async Task<IReadOnlyDictionary<string, string>?> LoadBundledMechanismBuffAsync()
    {
        try
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "HoYoLabMi18n",
                "mechanism_buff.json");

            if (!File.Exists(path))
            {
                return null;
            }

            await using FileStream stream = File.OpenRead(path);
            Dictionary<string, string>? values =
                await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);

            return values is null
                ? null
                : new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }


    private static Task<IReadOnlyDictionary<string, string>?> GetLocaleAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        Lazy<Task<IReadOnlyDictionary<string, string>?>> lazyTask = LocaleCache.GetOrAdd(
            locale,
            static localeName => new(
                () => LoadLocaleAsync(localeName),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazyTask.Value.WaitAsync(cancellationToken);
    }


    private static async Task<IReadOnlyDictionary<string, string>?> LoadLocaleAsync(string locale)
    {
        try
        {
            string url = $"https://webstatic.hoyoverse.com/admin/mi18n/bbs_oversea/{ResourceId}/{ResourceId}-{locale}.json";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/144.0.0.0 Safari/537.36");
            request.Headers.Accept.ParseAdd("application/json,text/plain,*/*");

            using HttpResponseMessage response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync();
            using JsonDocument document = await JsonDocument.ParseAsync(stream);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CollectStrings(document.RootElement, values);
            return values;
        }
        catch
        {
            return null;
        }
    }


    private static void CollectStrings(JsonElement element, Dictionary<string, string> values)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    string? value = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.TryAdd(property.Name, value);
                    }
                }
                else
                {
                    CollectStrings(property.Value, values);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                CollectStrings(item, values);
            }
        }
    }


    private static string NormalizeLocale(string? cultureName)
    {
        string locale = string.IsNullOrWhiteSpace(cultureName)
            ? "en-us"
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
