using System.Net;
using System.Text;
using System.Text.Json;
using Starward.Core;
using Starward.Core.Passport;

namespace Starward.RegressionTests;

internal static class PassportTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("Global Pass exchanges both web tokens in one request", GlobalExchangeAsync);
        yield return ("Global Pass reports an expired session without retrying", () =>
            GlobalFailureAsync("""{"retcode":-100,"message":"Invalid session"}""", -100));
        yield return ("Global Pass rejects a response with only one web token", () =>
            GlobalFailureAsync("""{"retcode":0,"data":{"tokens":[{"token_type":2,"token":"sample-ltoken"}]}}""", -1));
        yield return ("An incomplete Global Pass session is never sent", MissingSessionAsync);
        yield return ("One Global Pass session authenticates roles from all four games", GlobalGameRolesAsync);
        yield return ("China Pass keeps its existing web token exchange", ChinaExchangeAsync);
    }

    private static PassportUserInfo User(ServerRegion region) => new()
    {
        Region = region,
        Aid = 1,
        Mid = "test-mid",
        Stoken = "test-stoken",
    };

    private static async Task GlobalExchangeAsync()
    {
        using var handler = new ReplyHandler(
            """{"retcode":0,"data":{"tokens":[{"token_type":4,"token":"sample-cookie-token"},{"token_type":1,"token":"unused"},{"token_type":2,"token":"sample-ltoken"}]}}""",
            CheckGlobalRequest);
        using var http = new HttpClient(handler);
        var client = new PassportClient(http);

        var tokens = await client.ExchangeWebTokensAsync(User(ServerRegion.HoYoverse), null);

        Check.Equal("sample-ltoken", tokens.LToken);
        Check.Equal("sample-cookie-token", tokens.CookieToken);
        Check.Equal(1, handler.Count);
    }

    private static async Task GlobalFailureAsync(string reply, int expectedCode)
    {
        using var handler = new ReplyHandler(reply, CheckGlobalRequest);
        using var http = new HttpClient(handler);
        var client = new PassportClient(http);
        await ExpectFailureAsync(client, User(ServerRegion.HoYoverse), expectedCode);
        Check.Equal(1, handler.Count);
    }

    private static async Task MissingSessionAsync()
    {
        using var handler = new ReplyHandler("", (_, _) => throw new InvalidOperationException("Unexpected network request."));
        using var http = new HttpClient(handler);
        var user = User(ServerRegion.HoYoverse);
        user.Mid = "";
        await ExpectFailureAsync(new PassportClient(http), user, -100);
        Check.Equal(0, handler.Count);
    }

    private static async Task ExpectFailureAsync(PassportClient client, PassportUserInfo user, int expectedCode)
    {
        try
        {
            await client.ExchangeWebTokensAsync(user, null);
        }
        catch (miHoYoApiException ex)
        {
            Check.Equal(expectedCode, ex.ReturnCode);
            return;
        }
        throw new InvalidOperationException("Expected Pass exchange to fail.");
    }

    private static void CheckGlobalRequest(HttpRequestMessage request, string body)
    {
        Check.Equal(HttpMethod.Post, request.Method);
        Check.Equal("https://sg-public-api.hoyoverse.com/account/ma-passport/token/getBySToken", request.RequestUri!.AbsoluteUri);
        Check.Equal("c9oqaq3s3gu8", request.Headers.GetValues("x-rpc-app_id").Single());
        Check.Equal("stoken=test-stoken; ltuid_v2=1; ltmid_v2=test-mid; account_id_v2=1; account_mid_v2=test-mid; mid=test-mid",
                    request.Headers.GetValues("Cookie").Single());
        var signature = request.Headers.GetValues("DS").Single().Split(',');
        Check.Equal(3, signature.Length);
        Check.True(long.TryParse(signature[0], out _) && signature[1].Length == 6 && signature[2].Length == 32,
                   "Expected a Passport timestamp, nonce and digest.");
        Check.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(body);
        var properties = json.RootElement.EnumerateObject().ToArray();
        Check.Equal(1, properties.Length);
        Check.Equal("dst_token_types", properties[0].Name);
        Check.True(properties[0].Value.EnumerateArray().Select(t => t.GetInt32()).SequenceEqual([2, 4]),
                   "Expected both HoYoLAB web token types.");
    }

    private static async Task GlobalGameRolesAsync()
    {
        using var handler = new ReplyHandler((request, body) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                Check.Equal("https://api-account-os.hoyolab.com/binding/api/getUserGameRolesBySToken", request.RequestUri!.AbsoluteUri);
                return """
                       {"retcode":0,"data":{"list":[
                         {"game_uid":"12","game_biz":"hk4e_global","nickname":"Genshin"},
                         {"game_uid":"12","game_biz":"hkrpg_global","nickname":"Star Rail"},
                         {"game_uid":"13","game_biz":"nap_global","nickname":"ZZZ"},
                         {"game_uid":"14","game_biz":"bh3_global","nickname":"Honkai Impact"}
                       ]}}
                       """;
            }
            CheckGlobalRequest(request, body);
            return """{"retcode":0,"data":{"tokens":[{"token_type":2,"token":"sample-ltoken"},{"token_type":4,"token":"sample-cookie-token"}]}}""";
        });
        using var http = new HttpClient(handler);
        var user = User(ServerRegion.HoYoverse);
        var client = new PassportClient(http) { Language = "en-us" };
        var roles = await client.GetAuthenticatedGameRolesAsync(user, null);

        Check.Equal(2, handler.Count);
        Check.True(roles.Select(x => x.GameBiz).SequenceEqual(["hk4e_global", "hkrpg_global", "nap_global", "bh3_global"]),
                   "All supported game roles, including shared UID values, must survive the exchange.");
        foreach (var role in roles)
        {
            Check.Equal(user.Aid, role.Aid);
            Check.True(role.Cookie is not null && role.Cookie.Contains("ltoken_v2=sample-ltoken", StringComparison.Ordinal)
                       && role.Cookie.Contains("cookie_token_v2=sample-cookie-token", StringComparison.Ordinal)
                       && !role.Cookie.Contains(user.Stoken, StringComparison.Ordinal),
                       "Expected both derived web tokens but no Passport session in the game cookie.");
        }
    }

    private static async Task ChinaExchangeAsync()
    {
        var requestedTypes = new List<int>();
        using var handler = new ReplyHandler(
            """{"retcode":0,"data":{"token":{"token":"sample-cn-token"}}}""",
            (request, body) =>
            {
                Check.Equal(HttpMethod.Post, request.Method);
                Check.Equal("https://passport-api.mihoyo.com/account/ma-cn-session/app/exchange", request.RequestUri!.AbsoluteUri);
                Check.True(!request.Headers.Contains("Cookie"), "China Pass passes the session in JSON.");
                using var json = JsonDocument.Parse(body);
                Check.Equal("test-mid", json.RootElement.GetProperty("mid").GetString());
                Check.Equal("test-stoken", json.RootElement.GetProperty("src_token").GetProperty("token").GetString());
                requestedTypes.Add(json.RootElement.GetProperty("dst_token_type").GetInt32());
            });
        using var http = new HttpClient(handler);
        var client = new PassportClient(http) { DeviceId = "test", DeviceFp = "test", Language = "en-us" };

        var tokens = await client.ExchangeWebTokensAsync(User(ServerRegion.miHoYo), null);

        Check.Equal(("sample-cn-token", "sample-cn-token"), tokens);
        Check.True(requestedTypes.SequenceEqual([2, 4]), "China Pass should still request each token separately.");
    }

    private sealed class ReplyHandler(Func<HttpRequestMessage, string, string> reply) : HttpMessageHandler
    {
        public ReplyHandler(string response, Action<HttpRequestMessage, string> checkRequest)
            : this((request, body) =>
            {
                checkRequest(request, body);
                return response;
            })
        {
        }

        public int Count { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            string body = request.Content is { } content ? await content.ReadAsStringAsync(cancellationToken) : "";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(reply(request, body), Encoding.UTF8, "application/json"),
            };
        }
    }
}
