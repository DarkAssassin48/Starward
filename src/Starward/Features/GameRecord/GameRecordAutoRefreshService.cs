using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Helpers;
using Starward.Language;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord;


/// <summary>
/// Periodically refreshes locally cached HoYoLAB / miHoYo game records.
/// The scheduler works only while Starward is running. Missed scheduled
/// refreshes are performed when the application starts again.
/// </summary>
internal sealed class GameRecordAutoRefreshService : IDisposable
{

    private static readonly GameBiz[] SupportedGames =
    [
        GameBiz.hk4e,
        GameBiz.hkrpg,
        GameBiz.nap,
    ];

    private readonly ILogger<GameRecordAutoRefreshService> _logger;

    private readonly IGameRecordRefreshSource _gameRecordService;

    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    private readonly SemaphoreSlim _scheduleChangedSemaphore = new(0, 1);

    private CancellationTokenSource? _cancellationTokenSource;

    private Task? _backgroundTask;


    public GameRecordAutoRefreshService(
        ILogger<GameRecordAutoRefreshService> logger,
        IGameRecordRefreshSource gameRecordService)
    {
        _logger = logger;
        _gameRecordService = gameRecordService;
        _gameRecordService.GameRolesChanged += OnGameRolesChanged;
    }


    public void Start()
    {
        if (_backgroundTask is not null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _backgroundTask = Task.Run(() => RunAsync(_cancellationTokenSource.Token));
    }


    public void Stop()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch
        {
        }
    }


    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Allow the main window and local database to finish loading before
            // issuing background network requests.
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await RefreshDueGamesAsync(isStartup: true, cancellationToken);

            while (true)
            {
                TimeSpan delay = GetDelayUntilNextScheduledRefresh();
                bool due = await WaitForDueTimeOrScheduleChangeAsync(delay, cancellationToken);
                if (due)
                {
                    await RefreshDueGamesAsync(isStartup: false, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game record auto refresh background task stopped unexpectedly.");
        }
    }


    /// <summary>
    /// Wakes the scheduler after a user changes an interval or a manual refresh
    /// or account change affects the next due time. The service does not use periodic polling.
    /// </summary>
    public void NotifyScheduleChanged()
    {
        try
        {
            _scheduleChangedSemaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Several concurrent changes only need one pending wake-up.
        }
        catch (ObjectDisposedException)
        {
        }
    }


    private void OnGameRolesChanged(object? sender, EventArgs e)
    {
        NotifyScheduleChanged();
    }


    private async Task<bool> WaitForDueTimeOrScheduleChangeAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        if (delay == Timeout.InfiniteTimeSpan)
        {
            await _scheduleChangedSemaphore.WaitAsync(cancellationToken);
            return false;
        }

        if (delay <= TimeSpan.Zero)
        {
            return true;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task delayTask = Task.Delay(delay, linkedCts.Token);
        Task changedTask = _scheduleChangedSemaphore.WaitAsync(linkedCts.Token);
        Task completedTask = await Task.WhenAny(delayTask, changedTask);
        linkedCts.Cancel();

        try
        {
            await Task.WhenAll(delayTask, changedTask);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        cancellationToken.ThrowIfCancellationRequested();
        return completedTask == delayTask;
    }


    private TimeSpan GetDelayUntilNextScheduledRefresh()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset? nextDue = null;

        foreach (GameBiz game in SupportedGames)
        {
            GameRecordAutoRefreshInterval interval = AppConfig.GetGameRecordAutoRefreshInterval(game);
            if (interval is GameRecordAutoRefreshInterval.Disabled or GameRecordAutoRefreshInterval.OnStartup)
            {
                continue;
            }

            foreach (GameRecordRole role in _gameRecordService.GetGameRolesOfGame(game))
            {
                DateTimeOffset due = GetRoleNextDueTime(game, role, interval, now);
                if (nextDue is null || due < nextDue.Value)
                {
                    nextDue = due;
                }
            }
        }

        if (nextDue is null)
        {
            return Timeout.InfiniteTimeSpan;
        }

        TimeSpan delay = nextDue.Value - now;
        return delay <= TimeSpan.Zero ? TimeSpan.Zero : delay;
    }


    public DateTimeOffset GetNextScheduledRefreshTime(GameBiz game)
    {
        game = game.ToGame();
        GameRecordAutoRefreshInterval interval = AppConfig.GetGameRecordAutoRefreshInterval(game);
        if (interval is GameRecordAutoRefreshInterval.Disabled or GameRecordAutoRefreshInterval.OnStartup)
        {
            return default;
        }

        DateTimeOffset now = DateTimeOffset.Now;
        return _gameRecordService.GetGameRolesOfGame(game)
            .Select(role => GetRoleNextDueTime(game, role, interval, now))
            .DefaultIfEmpty()
            .Min();
    }


    private static DateTimeOffset GetRoleNextDueTime(
        GameBiz game,
        GameRecordRole role,
        GameRecordAutoRefreshInterval interval,
        DateTimeOffset now)
    {
        DateTimeOffset last = AppConfig.GetGameRecordLastAutoRefreshTime(game, role.Uid);
        DateTimeOffset lastAttempt = AppConfig.GetGameRecordLastAutoRefreshAttemptTime(game, role.Uid);
        return GameRecordAutoRefreshSchedule.GetNextDueTime(interval, last, lastAttempt, now, TimeZoneInfo.Local);
    }


    private async Task RefreshDueGamesAsync(bool isStartup, CancellationToken cancellationToken)
    {
        foreach (GameBiz game in SupportedGames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GameRecordAutoRefreshInterval interval = AppConfig.GetGameRecordAutoRefreshInterval(game);
            if (interval is GameRecordAutoRefreshInterval.Disabled)
            {
                continue;
            }

            if (interval is GameRecordAutoRefreshInterval.OnStartup && !isStartup)
            {
                continue;
            }

            try
            {
                await RefreshGameAsync(game, force: false, isStartup, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automatic game record refresh failed for {game}.", game);
            }
        }
    }


    public async Task<GameRecordAutoRefreshResult> RefreshGameNowAsync(
        GameBiz game,
        CancellationToken cancellationToken = default)
    {
        GameRecordAutoRefreshResult result = await RefreshGameAsync(
            game.ToGame(),
            force: true,
            isStartup: false,
            cancellationToken);
        NotifyScheduleChanged();
        return result;
    }


    public DateTimeOffset GetLastSuccessfulRefreshTime(GameBiz game)
    {
        List<GameRecordRole> roles = _gameRecordService.GetGameRolesOfGame(game.ToGame());
        if (roles.Count == 0)
        {
            return default;
        }

        return roles.Select(role => AppConfig.GetGameRecordLastAutoRefreshTime(game.ToGame(), role.Uid))
                    .DefaultIfEmpty()
                    .Max();
    }


    private async Task<GameRecordAutoRefreshResult> RefreshGameAsync(
        GameBiz game,
        bool force,
        bool isStartup,
        CancellationToken cancellationToken)
    {
        game = game.ToGame();
        GameRecordAutoRefreshInterval interval = AppConfig.GetGameRecordAutoRefreshInterval(game);
        if (!force && interval is GameRecordAutoRefreshInterval.Disabled)
        {
            return new GameRecordAutoRefreshResult(0, 0, 0, 0, 0);
        }

        await _refreshSemaphore.WaitAsync(cancellationToken);
        try
        {
            List<GameRecordRole> roles = _gameRecordService.GetGameRolesOfGame(game);
            int refreshedRoles = 0;
            int failedRoles = 0;
            int successfulOperations = 0;
            int failedOperations = 0;
            var accountResults = new List<AutoRefreshAccountNotification>();

            foreach (GameRecordRole role in roles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!force && !IsRoleDue(game, role, interval, isStartup))
                {
                    continue;
                }

                AppConfig.SetGameRecordLastAutoRefreshAttemptTime(game, role.Uid, DateTimeOffset.Now);

                (int roleSuccess, int roleFailure) = await RefreshRoleAsync(game, role, cancellationToken);
                successfulOperations += roleSuccess;
                failedOperations += roleFailure;

                if (roleSuccess > 0)
                {
                    refreshedRoles++;
                    if (roleFailure == 0)
                    {
                        AppConfig.SetGameRecordLastAutoRefreshTime(game, role.Uid, DateTimeOffset.Now);
                    }
                }
                else
                {
                    failedRoles++;
                }

                accountResults.Add(new AutoRefreshAccountNotification(
                    role.Uid,
                    role.Nickname,
                    roleSuccess,
                    roleFailure));
            }

            var result = new GameRecordAutoRefreshResult(
                roles.Count,
                refreshedRoles,
                failedRoles,
                successfulOperations,
                failedOperations);

            _logger.LogInformation(
                "Game record refresh finished for {game}: roles {refreshedRoles}/{totalRoles}, operations {successfulOperations} succeeded, {failedOperations} failed.",
                game,
                result.RefreshedRoles,
                result.TotalRoles,
                result.SuccessfulOperations,
                result.FailedOperations);

            if (result.HasAnySuccess)
            {
                WeakReferenceMessenger.Default.Send(new GameRecordAutoRefreshCompletedMessage(game));
            }

            if (!force && accountResults.Count > 0)
            {
                ShowAutomaticRefreshNotification(game, accountResults);
            }

            return result;
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }


    private static void ShowAutomaticRefreshNotification(
        GameBiz game,
        IReadOnlyList<AutoRefreshAccountNotification> accounts)
    {
        string title = $"{HoYoLabAutoRefreshText.AutomaticRefreshTitle} — {game.ToGameName()}";
        string message = string.Join(
            Environment.NewLine,
            accounts.Select(FormatAccountNotification));

        bool anySuccess = accounts.Any(x => x.SuccessfulOperations > 0);
        bool anyFailure = accounts.Any(x => x.FailedOperations > 0 || x.SuccessfulOperations == 0);

        if (!anySuccess)
        {
            InAppToast.MainWindow?.Error(title, message, duration: 10000);
        }
        else if (anyFailure)
        {
            InAppToast.MainWindow?.Warning(title, message, duration: 10000);
        }
        else
        {
            InAppToast.MainWindow?.Success(title, message, duration: 10000);
        }
    }


    private static string FormatAccountNotification(AutoRefreshAccountNotification account)
    {
        string nickname = string.IsNullOrWhiteSpace(account.Nickname)
            ? account.Uid.ToString(CultureInfo.InvariantCulture)
            : account.Nickname;

        if (account.SuccessfulOperations > 0 && account.FailedOperations == 0)
        {
            return $"✅ {nickname} (UID {account.Uid}) — " +
                   string.Format(
                       CultureInfo.CurrentCulture,
                       HoYoLabAutoRefreshText.AccountUpdateSuccessFormat,
                       account.SuccessfulOperations);
        }

        if (account.SuccessfulOperations > 0)
        {
            return $"⚠ {nickname} (UID {account.Uid}) — " +
                   string.Format(
                       CultureInfo.CurrentCulture,
                       HoYoLabAutoRefreshText.AccountUpdatePartialFormat,
                       account.SuccessfulOperations,
                       account.FailedOperations);
        }

        return $"❌ {nickname} (UID {account.Uid}) — " +
               string.Format(
                   CultureInfo.CurrentCulture,
                   HoYoLabAutoRefreshText.AccountUpdateFailedFormat,
                   account.FailedOperations);
    }


    private sealed record AutoRefreshAccountNotification(
        long Uid,
        string Nickname,
        int SuccessfulOperations,
        int FailedOperations);


    private static bool IsRoleDue(
        GameBiz game,
        GameRecordRole role,
        GameRecordAutoRefreshInterval interval,
        bool isStartup)
    {
        if (interval is GameRecordAutoRefreshInterval.OnStartup)
        {
            return isStartup;
        }

        DateTimeOffset now = DateTimeOffset.Now;
        return GetRoleNextDueTime(game, role, interval, now) <= now;
    }


    private async Task<(int successfulOperations, int failedOperations)> RefreshRoleAsync(
        GameBiz game,
        GameRecordRole role,
        CancellationToken cancellationToken)
    {
        int successfulOperations = 0;
        int failedOperations = 0;


        async Task RunStepAsync(string operation, Func<Task> action)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action();
                successfulOperations++;
                await Task.Delay(200, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedOperations++;
                _logger.LogWarning(
                    ex,
                    "Game record operation {operation} failed for {gameBiz} {uid}.",
                    operation,
                    role.GameBiz,
                    role.Uid);
            }
        }

        foreach (var (name, run) in _gameRecordService.GetOperations(game, role, cancellationToken))
        {
            await RunStepAsync(name, run);
        }

        return (successfulOperations, failedOperations);
    }


    public void Dispose()
    {
        _gameRecordService.GameRolesChanged -= OnGameRolesChanged;
        Stop();
        _cancellationTokenSource?.Dispose();
        _scheduleChangedSemaphore.Dispose();
        _refreshSemaphore.Dispose();
    }

}
