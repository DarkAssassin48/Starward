using Starward.Core;
using Starward.Core.GameRecord;
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
    internal sealed class GameRecordService : IGameRecordRefreshSource
    {
        private readonly ConcurrentDictionary<long, GameRecordRole> _roles = new();
        public event EventHandler? GameRolesChanged;
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

        public IEnumerable<(string Name, Func<Task> Run)> GetOperations(GameBiz game, GameRecordRole role, CancellationToken token)
        {
            foreach (string name in new[] { "Abyss1", "Abyss2", "Theater", "Onslaught", "Diary" })
            {
                yield return (name, () => Run(name, token));
            }
        }
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
