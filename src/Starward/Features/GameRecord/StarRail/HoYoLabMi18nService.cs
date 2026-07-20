using Starward.Language;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Starward.Features.GameRecord.StarRail;


/// <summary>
/// Loads localized strings used by the HoYoLAB game-record pages.
/// Results are cached per locale for the lifetime of the application.
/// </summary>
internal static class HoYoLabMi18nService
{

    private const string ResourceId = "m20230509hy150knmyo";

    private static readonly HttpClient HttpClient = new(
        new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    private static readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyDictionary<string, string>?>>> LocaleCache =
        new(StringComparer.OrdinalIgnoreCase);


    public static async Task<string?> GetStringAsync(
        string key,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        string locale = NormalizeLocale((culture ?? Lang.Culture).Name);

        IReadOnlyDictionary<string, string>? values = await GetLocaleAsync(locale, cancellationToken);
        if (values?.TryGetValue(key, out string? value) is true && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!locale.Equals("en-us", StringComparison.OrdinalIgnoreCase))
        {
            values = await GetLocaleAsync("en-us", cancellationToken);
            if (values?.TryGetValue(key, out value) is true && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
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
            using HttpResponseMessage response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return "en-us";
        }

        return cultureName.Replace('_', '-').ToLowerInvariant();
    }

}
