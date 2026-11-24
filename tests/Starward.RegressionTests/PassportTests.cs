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
        Check.Equal("stoken=test-stoken; mid=test-mid", request.Headers.GetValues("Cookie").Single());
        Check.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(body);
        var properties = json.RootElement.EnumerateObject().ToArray();
        Check.Equal(1, properties.Length);
        Check.Equal("dst_token_types", properties[0].Name);
        Check.True(properties[0].Value.EnumerateArray().Select(t => t.GetInt32()).SequenceEqual([2, 4]),
                   "Expected both HoYoLAB web token types.");
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

    private sealed class ReplyHandler(string reply, Action<HttpRequestMessage, string> checkRequest) : HttpMessageHandler
    {
        public int Count { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            checkRequest(request, await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(reply, Encoding.UTF8, "application/json"),
            };
        }
    }
}
