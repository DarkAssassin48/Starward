using Starward.Core.HoYoPlay;
using Starward.Core;
using System.Net;
using System.Text;

namespace Starward.RegressionTests;

internal static class HoYoPlayTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("Nexus Anima release and preregistration use their own launchers", () =>
        {
            Check.Equal(LauncherId.NexusAnimaStandalone, LauncherId.FromGameId(NexusRelease()));
            Check.Equal(LauncherId.GlobalOfficial, LauncherId.FromGameId(new GameId { Id = "yneO4yGc4x", GameBiz = "abc_global" }));
            Check.Equal(LauncherId.BilibiliStarRail, LauncherId.FromGameId(GameId.FromGameBiz("hkrpg_bilibili")!));
            return Task.CompletedTask;
        });
        yield return ("Standalone catalog supports a CHUNK release without archives", StandaloneCatalogAsync);
    }

    private static GameId NexusRelease() => new() { Id = "3qXq1I0N6o", GameBiz = "abc_global" };

    private static async Task StandaloneCatalogAsync()
    {
        using var handler = new NexusCatalogHandler();
        using var http = new HttpClient(handler);
        var gameId = NexusRelease();
        string launcherId = LauncherId.FromGameId(gameId)!;
        var client = new HoYoPlayClient(http)
        {
            LauncherConfig = new LauncherConfig(launcherId, 1, 1, ServerRegion.HoYoverse),
            Language = "ru-RU",
        };

        var games = await client.GetGameInfoAsync();
        Check.Equal(gameId.Id, games.Single().Id);
        var config = (await client.GetGameConfigAsync([gameId])).Single();
        Check.Equal(DownloadMode.DOWNLOAD_MODE_CHUNK, config.DefaultDownloadMode);
        var branch = (await client.GetGameBranchAsync([gameId])).Single();
        Check.Equal("0.75.0", branch.Main.Tag);
        Check.Equal(0, (await client.GetGamePackageAsync([gameId])).Count);
    }

    private sealed class NexusCatalogHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri!;
            Check.Equal("sg-hyp-api.hoyoverse.com", uri.Host);
            Check.True(uri.Query.Contains("launcher_id=JMNdqmbl9I", StringComparison.Ordinal), "Expected Nexus Anima's standalone launcher.");
            string endpoint = uri.Segments[^1];
            if (endpoint != "getGames")
            {
                Check.True(Uri.UnescapeDataString(uri.Query).Contains("game_ids[]=3qXq1I0N6o", StringComparison.Ordinal),
                    "Expected the standalone game ID in the request.");
            }
            string data = endpoint switch
            {
                "getGames" => """{"games":[{"id":"3qXq1I0N6o","biz":"abc_global","display_status":"LAUNCHER_GAME_DISPLAY_STATUS_AVAILABLE"}]}""",
                "getGameConfigs" => """{"launch_configs":[{"game":{"id":"3qXq1I0N6o","biz":"abc_global"},"default_download_mode":"DOWNLOAD_MODE_CHUNK"}]}""",
                "getGameBranches" => """{"game_branches":[{"game":{"id":"3qXq1I0N6o","biz":"abc_global"},"main":{"tag":"0.75.0"}}]}""",
                "getGamePackages" => """{"game_packages":[]}""",
                _ => throw new InvalidOperationException($"Unexpected endpoint {endpoint}."),
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"retcode":0,"message":"OK","data":{{data}}}""", Encoding.UTF8, "application/json"),
            });
        }
    }
}
