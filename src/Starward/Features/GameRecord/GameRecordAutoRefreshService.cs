using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.GameRecord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord;

/// <summary>
/// Periodically refreshes locally cached HoYoLAB / miHoYo game records.
/// The service only works while Starward is running. Missed refreshes are
/// performed immediately on the next application launch.
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
    private readonly GameRecordService _gameRecordService;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    private readonly SemaphoreSlim _scheduleChangedSemaphore = new(0, 1);

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _backgroundTask;

    public GameRecordAutoRefreshService(
        ILogger<GameRecordAutoRefreshService> logger,
        GameRecordService gameRecordService)
    {
        _logger = logger;
        _gameRecordService = gameRecordService;
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
            // Let the main window finish loading before making network requests.
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
    /// Wakes the scheduler after the user changes an interval or a manual refresh
    /// changes the next due time. No periodic polling is used.
    /// </summary>
    public void NotifyScheduleChanged()
    {
        try
        {
            if (_scheduleChangedSemaphore.CurrentCount == 0)
            {
                _scheduleChangedSemaphore.Release();
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }


    private async Task<bool> WaitForDueTimeOrScheduleChangeAsync(TimeSpan delay, CancellationToken cancellationToken)
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


    private static DateTimeOffset GetRoleNextDueTime(
        GameBiz game,
        GameRecordRole role,
        GameRecordAutoRefreshInterval interval,
        DateTimeOffset now)
    {
        DateTimeOffset last = AppConfig.GetGameRecordLastAutoRefreshTime(game, role.Uid);
        DateTimeOffset due = last == default
            ? now
            : interval switch
            {
                GameRecordAutoRefreshInterval.Daily => new DateTimeOffset(last.LocalDateTime.Date.AddDays(1)),
                GameRecordAutoRefreshInterval.Weekly => last.AddDays(7),
                GameRecordAutoRefreshInterval.Monthly => new DateTimeOffset(
                    new DateTime(last.LocalDateTime.Year, last.LocalDateTime.Month, 1).AddMonths(1)),
                _ => DateTimeOffset.MaxValue,
            };

        DateTimeOffset lastAttempt = AppConfig.GetGameRecordLastAutoRefreshAttemptTime(game, role.Uid);
        if (due <= now && lastAttempt != default && now - lastAttempt < TimeSpan.FromHours(2))
        {
            due = lastAttempt.AddHours(2);
        }

        return due;
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
            game.ToGame(), force: true, isStartup: false, cancellationToken);
        NotifyScheduleChanged();
        return result;
    }

    public DateTimeOffset GetLastSuccessfulRefreshTime(GameBiz game)
    {
        var roles = _gameRecordService.GetGameRolesOfGame(game.ToGame());
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

            foreach (GameRecordRole role in roles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!force && !IsRoleDue(game, role, interval, isStartup))
                {
                    continue;
                }

                // Do not retry a failed automatic update too aggressively.
                DateTimeOffset lastAttempt = AppConfig.GetGameRecordLastAutoRefreshAttemptTime(game, role.Uid);
                if (!force && interval is not GameRecordAutoRefreshInterval.OnStartup &&
                    lastAttempt != default && DateTimeOffset.Now - lastAttempt < TimeSpan.FromHours(2))
                {
                    continue;
                }

                AppConfig.SetGameRecordLastAutoRefreshAttemptTime(game, role.Uid, DateTimeOffset.Now);

                var roleResult = await RefreshRoleAsync(game, role, cancellationToken);
                successfulOperations += roleResult.successfulOperations;
                failedOperations += roleResult.failedOperations;

                if (roleResult.successfulOperations > 0)
                {
                    refreshedRoles++;
                    AppConfig.SetGameRecordLastAutoRefreshTime(game, role.Uid, DateTimeOffset.Now);
                }
                else
                {
                    failedRoles++;
                }
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

            return result;
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

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

        DateTimeOffset last = AppConfig.GetGameRecordLastAutoRefreshTime(game, role.Uid);
        if (last == default)
        {
            return true;
        }

        DateTimeOffset now = DateTimeOffset.Now;
        return interval switch
        {
            GameRecordAutoRefreshInterval.Daily => now.Date > last.Date,
            GameRecordAutoRefreshInterval.Weekly => now - last >= TimeSpan.FromDays(7),
            GameRecordAutoRefreshInterval.Monthly => now.Year != last.Year || now.Month != last.Month,
            _ => false,
        };
    }

    private async Task<(int successfulOperations, int failedOperations)> RefreshRoleAsync(
        GameBiz game,
        GameRecordRole role,
        CancellationToken cancellationToken)
    {
        int successfulOperations = 0;
        int failedOperations = 0;

        _gameRecordService.Language = CultureInfo.CurrentUICulture.Name;

        // Chinese and Bilibili endpoints require the device fingerprint.
        GameBiz roleGameBiz = new(role.GameBiz);
        if (!roleGameBiz.IsGlobalServer())
        {
            try
            {
                await _gameRecordService.UpdateHyperionDeviceFpAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update device fingerprint before refreshing {gameBiz} {uid}.", role.GameBiz, role.Uid);
            }
        }

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
                _logger.LogWarning(ex, "Game record operation {operation} failed for {gameBiz} {uid}.", operation, role.GameBiz, role.Uid);
            }
        }

        switch (game.Game)
        {
            case GameBiz.hk4e:
                await RunStepAsync("SpiralAbyssCurrent", () => _gameRecordService.RefreshSpiralAbyssInfoAsync(role, 1, cancellationToken));
                await RunStepAsync("SpiralAbyssPrevious", () => _gameRecordService.RefreshSpiralAbyssInfoAsync(role, 2, cancellationToken));
                await RunStepAsync("ImaginariumTheater", () => _gameRecordService.RefreshImaginariumTheaterInfoAsync(role, cancellationToken));
                await RunStepAsync("StygianOnslaught", () => _gameRecordService.RefreshStygianOnslaughtInfosAsync(role, cancellationToken));
                await RunStepAsync("TravelersDiary", async () =>
                {
                    var currentSummary = await _gameRecordService.GetTravelersDiarySummaryAsync(role);
                    var months = new HashSet<int>(currentSummary.OptionalMonth ?? []);
                    if (currentSummary.DataMonth > 0)
                    {
                        months.Add(currentSummary.DataMonth);
                    }

                    foreach (int month in months.Where(x => x > 0).OrderBy(x => x))
                    {
                        await _gameRecordService.GetTravelersDiarySummaryAsync(role, month);
                        await _gameRecordService.GetTravelersDiaryDetailAsync(role, month, 1);
                        await _gameRecordService.GetTravelersDiaryDetailAsync(role, month, 2);
                    }
                });
                break;

            case GameBiz.hkrpg:
                await RunStepAsync("TrailblazeCalendar", async () =>
                {
                    var currentSummary = await _gameRecordService.GetTrailblazeCalendarSummaryAsync(role);
                    var months = new HashSet<string>(currentSummary.OptionalMonth ?? [], StringComparer.Ordinal);
                    if (!string.IsNullOrWhiteSpace(currentSummary.DataMonth))
                    {
                        months.Add(currentSummary.DataMonth);
                    }

                    foreach (string month in months.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x))
                    {
                        await _gameRecordService.GetTrailblazeCalendarSummaryAsync(role, month);
                        await _gameRecordService.GetTrailblazeCalendarDetailAsync(role, month, 1);
                        await _gameRecordService.GetTrailblazeCalendarDetailAsync(role, month, 2);
                    }
                });
                await RunStepAsync("SimulatedUniverse", () => _gameRecordService.GetSimulatedUniverseInfoAsync(role, detail: true));
                await RunStepAsync("ForgottenHallCurrent", () => _gameRecordService.RefreshForgottenHallInfoAsync(role, 1, cancellationToken));
                await RunStepAsync("ForgottenHallPrevious", () => _gameRecordService.RefreshForgottenHallInfoAsync(role, 2, cancellationToken));
                await RunStepAsync("PureFictionCurrent", () => _gameRecordService.RefreshPureFictionInfoAsync(role, 1, cancellationToken));
                await RunStepAsync("PureFictionPrevious", () => _gameRecordService.RefreshPureFictionInfoAsync(role, 2, cancellationToken));
                await RunStepAsync("ApocalypticShadowCurrent", () => _gameRecordService.RefreshApocalypticShadowInfoAsync(role, 1, cancellationToken));
                await RunStepAsync("ApocalypticShadowPrevious", () => _gameRecordService.RefreshApocalypticShadowInfoAsync(role, 2, cancellationToken));
                await RunStepAsync("AnomalyArbitration", () => _gameRecordService.RefreshStarRailChallengePeakDataAsync(role, cancellationToken));
                break;

            case GameBiz.nap:
                await RunStepAsync("InterKnotMonthlyReport", async () =>
                {
                    var currentSummary = await _gameRecordService.GetInterKnotReportSummaryAsync(role);
                    var months = new HashSet<string>(currentSummary.OptionalMonth ?? [], StringComparer.Ordinal);
                    if (!string.IsNullOrWhiteSpace(currentSummary.DataMonth))
                    {
                        months.Add(currentSummary.DataMonth);
                    }

                    foreach (string month in months.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x))
                    {
                        var summary = await _gameRecordService.GetInterKnotReportSummaryAsync(role, month);
                        foreach (var item in summary.MonthData.List)
                        {
                            await _gameRecordService.GetInterKnotReportDetailAsync(role, month, item.DataType);
                        }
                    }
                });
                await RunStepAsync("ShiyuDefenseCurrent", () => _gameRecordService.RefreshShiyuDefenseInfoAsync(role, 1, cancellationToken));
                await RunStepAsync("ShiyuDefensePrevious", () => _gameRecordService.RefreshShiyuDefenseInfoAsync(role, 2, cancellationToken));
                await RunStepAsync("DeadlyAssaultCurrent", () => _gameRecordService.RefreshDeadlyAssaultInfoAsync(role, 1, cancellationToken));
                await RunStepAsync("DeadlyAssaultPrevious", () => _gameRecordService.RefreshDeadlyAssaultInfoAsync(role, 2, cancellationToken));
                break;
        }

        return (successfulOperations, failedOperations);
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
        _scheduleChangedSemaphore.Dispose();
        _refreshSemaphore.Dispose();
    }
}
