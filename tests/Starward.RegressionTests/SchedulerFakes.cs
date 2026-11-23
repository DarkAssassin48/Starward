using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;
using Starward.Core.GameRecord.ZZZ.InterKnotReport;
using Starward.Features.GameRecord;
using System.Collections.Concurrent;

// Only external boundaries are replaced: no real accounts, database, network,
// or WinUI. The scheduler, schedule calculation, and date aggregation are linked
// directly from the application sources by the test project.
namespace Starward
{
    internal static class AppConfig
    {
        private static readonly ConcurrentDictionary<string, GameRecordAutoRefreshInterval> Intervals = new();
        private static readonly ConcurrentDictionary<(string, long), DateTimeOffset> Successes = new();
        private static readonly ConcurrentDictionary<(string, long), DateTimeOffset> Attempts = new();
        public static string Language => "en-US";
        public static GameRecordAutoRefreshInterval GetGameRecordAutoRefreshInterval(GameBiz game) => Intervals.GetValueOrDefault(game.Game);
        public static void SetInterval(GameBiz game, GameRecordAutoRefreshInterval interval) => Intervals[game.Game] = interval;
        public static DateTimeOffset GetGameRecordLastAutoRefreshTime(GameBiz game, long uid) => Successes.GetValueOrDefault((game.Game, uid));
        public static void SetGameRecordLastAutoRefreshTime(GameBiz game, long uid, DateTimeOffset value) => Successes[(game.Game, uid)] = value;
        public static DateTimeOffset GetGameRecordLastAutoRefreshAttemptTime(GameBiz game, long uid) => Attempts.GetValueOrDefault((game.Game, uid));
        public static void SetGameRecordLastAutoRefreshAttemptTime(GameBiz game, long uid, DateTimeOffset value) => Attempts[(game.Game, uid)] = value;
        public static void Reset()
        {
            Intervals.Clear();
            Successes.Clear();
            Attempts.Clear();
        }
    }
}

namespace Starward.Features.GameRecord
{
    internal sealed class GameRecordService
    {
        private readonly ConcurrentDictionary<long, GameRecordRole> _roles = new();
        public event EventHandler? GameRolesChanged;
        public string Language { get; set; } = "en-US";
        public Action? RolesRead { get; set; }
        public Action<string>? OperationStarted { get; set; }
        public string? FailOperation { get; set; }

        public void AddRole(GameRecordRole role)
        {
            _roles[role.Uid] = role;
            GameRolesChanged?.Invoke(this, EventArgs.Empty);
        }

        public List<GameRecordRole> GetGameRolesOfGame(GameBiz game)
        {
            var result = _roles.Values.Where(x => new GameBiz(x.GameBiz).Game == game.Game).ToList();
            RolesRead?.Invoke();
            return result;
        }

        private Task Run(string operation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OperationStarted?.Invoke(operation);
            return FailOperation == operation || FailOperation == "*"
                ? Task.FromException(new InvalidOperationException("Simulated API failure: " + operation))
                : Task.CompletedTask;
        }

        public Task UpdateHyperionDeviceFpAsync(CancellationToken cancellationToken) => Run("Device", cancellationToken);
        public Task RefreshSpiralAbyssInfoAsync(GameRecordRole role, int schedule, CancellationToken token) => Run("Abyss" + schedule, token);
        public Task RefreshImaginariumTheaterInfoAsync(GameRecordRole role, CancellationToken token) => Run("Theater", token);
        public Task RefreshStygianOnslaughtInfosAsync(GameRecordRole role, CancellationToken token) => Run("Onslaught", token);
        public async Task<TravelersDiarySummary> GetTravelersDiarySummaryAsync(GameRecordRole role, int month = 0)
        {
            await Run("Diary");
            return new TravelersDiarySummary { OptionalMonth = [], DataMonth = 0 };
        }
        public Task GetTravelersDiaryDetailAsync(GameRecordRole role, int month, int type) => Run("DiaryDetail");
        public Task<TrailblazeCalendarSummary> GetTrailblazeCalendarSummaryAsync(GameRecordRole role, string month = "") =>
            Task.FromResult(new TrailblazeCalendarSummary { OptionalMonth = [], DataMonth = "" });
        public Task GetTrailblazeCalendarDetailAsync(GameRecordRole role, string month, int type) => Run("CalendarDetail");
        public Task GetSimulatedUniverseInfoAsync(GameRecordRole role, bool detail) => Run("Universe");
        public Task RefreshForgottenHallInfoAsync(GameRecordRole role, int schedule, CancellationToken token) => Run("Hall", token);
        public Task RefreshPureFictionInfoAsync(GameRecordRole role, int schedule, CancellationToken token) => Run("Fiction", token);
        public Task RefreshApocalypticShadowInfoAsync(GameRecordRole role, int schedule, CancellationToken token) => Run("Shadow", token);
        public Task RefreshStarRailChallengePeakDataAsync(GameRecordRole role, CancellationToken token) => Run("Peak", token);
        public Task<InterKnotReportSummary> GetInterKnotReportSummaryAsync(GameRecordRole role, string month = "") =>
            Task.FromResult(new InterKnotReportSummary { OptionalMonth = [], DataMonth = "" });
        public Task GetInterKnotReportDetailAsync(GameRecordRole role, string month, string type) => Run("ReportDetail");
        public Task RefreshShiyuDefenseInfoAsync(GameRecordRole role, int schedule, CancellationToken token) => Run("Shiyu", token);
        public Task RefreshDeadlyAssaultInfoAsync(GameRecordRole role, int schedule, CancellationToken token) => Run("Assault", token);
    }
}

namespace Starward.Helpers
{
    internal sealed class InAppToast
    {
        public static InAppToast? MainWindow => null;
        public void Error(string title, string message, int duration) { }
        public void Warning(string title, string message, int duration) { }
        public void Success(string title, string message, int duration) { }
    }
}
