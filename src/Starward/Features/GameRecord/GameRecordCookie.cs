using Starward.Core.GameRecord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Features.GameRecord;


internal static class GameRecordCookie
{
    public static Dictionary<string, string> Parse(string? cookie)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return values;
        }

        foreach (string item in cookie.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int separator = item.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }
            string name = item[..separator].Trim();
            string value = item[(separator + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                values[name] = value;
            }
        }
        return values;
    }


    public static string Serialize(IReadOnlyDictionary<string, string> values)
    {
        return string.Join("; ", values.Select(static x => $"{x.Key}={x.Value}"));
    }


    public static bool TryGetRefreshCredentials(
        IReadOnlyDictionary<string, string> values,
        out string stokenV2,
        out string mid)
    {
        stokenV2 = GetFirstValue(values, "stoken_v2", "stoken");
        mid = GetFirstValue(values, "account_mid_v2", "ltmid_v2", "mid");
        return !string.IsNullOrWhiteSpace(stokenV2) && !string.IsNullOrWhiteSpace(mid);
    }


    public static string GetAccountKey(IReadOnlyDictionary<string, string> values)
    {
        string mid = GetFirstValue(values, "account_mid_v2", "ltmid_v2", "mid");
        if (!string.IsNullOrWhiteSpace(mid))
        {
            return $"mid:{mid}";
        }

        string accountId = GetFirstValue(values, "account_id_v2", "ltuid_v2", "account_id", "ltuid");
        return string.IsNullOrWhiteSpace(accountId) ? "" : $"uid:{accountId}";
    }


    public static string? GetCookieTokenV2(IReadOnlyDictionary<string, string> values)
    {
        return GetFirstValue(values, "cookie_token_v2");
    }


    public static string MergeRefreshedTokens(string cookie, HoyolabTokenRefreshResult result)
    {
        var values = Parse(cookie);
        string? ltokenV2 = result.GetToken(2);
        string? cookieTokenV2 = result.GetToken(4);
        if (!string.IsNullOrWhiteSpace(ltokenV2))
        {
            values["ltoken_v2"] = ltokenV2;
        }
        if (!string.IsNullOrWhiteSpace(cookieTokenV2))
        {
            values["cookie_token_v2"] = cookieTokenV2;
        }
        return Serialize(values);
    }


    private static string GetFirstValue(IReadOnlyDictionary<string, string> values, params string[] names)
    {
        foreach (string name in names)
        {
            if (values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return "";
    }
}
