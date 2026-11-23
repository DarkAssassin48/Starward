using Microsoft.Extensions.Logging.Abstractions;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Features.GameRecord;
using System.Reflection;

namespace Starward.RegressionTests;


internal static class ScheduleTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-09-06T12:00:00+03:00");
        yield return Due("New account is due immediately", GameRecordAutoRefreshInterval.Daily, default, default, now, now);
        yield return Due("Complete daily run is not delayed by retry cooldown", GameRecordAutoRefreshInterval.Daily,
            DateTimeOffset.Parse("2026-09-05T23:55:00+03:00"), DateTimeOffset.Parse("2026-09-05T23:54:00+03:00"),
            now, DateTimeOffset.Parse("2026-09-06T00:00:00+03:00"));
        yield return Due("Daily schedule converts a saved UTC timestamp", GameRecordAutoRefreshInterval.Daily,
            DateTimeOffset.Parse("2026-09-05T22:00:00Z"), default, now, DateTimeOffset.Parse("2026-09-07T00:00:00+03:00"));
        yield return Due("Weekly successful schedule", GameRecordAutoRefreshInterval.Weekly, now, now.AddMinutes(-1), now, now.AddDays(7));
        yield return Due("Monthly schedule crosses year boundary in local time", GameRecordAutoRefreshInterval.Monthly,
            DateTimeOffset.Parse("2025-12-31T22:00:00Z"), default, now, DateTimeOffset.Parse("2026-02-01T00:00:00+03:00"));

        foreach (var interval in new[] { GameRecordAutoRefreshInterval.Daily, GameRecordAutoRefreshInterval.Weekly, GameRecordAutoRefreshInterval.Monthly })
        {
            yield return Due($"{interval}: partial or failed run retries in two hours", interval,
                now.AddHours(-4), now.AddMinutes(-5), now, now.AddHours(2).AddMinutes(-5));
            yield return Due($"{interval}: overdue retry stays due", interval,
                now.AddDays(-2), now.AddHours(-3), now, now.AddHours(-1));
        }
        yield return Due("Failed first run respects retry delay", GameRecordAutoRefreshInterval.Monthly,
            default, now, now, now.AddHours(2));
        yield return Due("Disabled schedule has no due time", GameRecordAutoRefreshInterval.Disabled,
            default, now, now, DateTimeOffset.MaxValue);
        yield return Due("Startup-only schedule has no periodic retry", GameRecordAutoRefreshInterval.OnStartup,
            default, now, now, DateTimeOffset.MaxValue);
        yield return ("Daily schedule respects daylight saving at next midnight", () =>
        {
            var last = DateTimeOffset.Parse("2026-03-08T00:30:00-05:00");
            var due = GameRecordAutoRefreshSchedule.GetNextDueTime(GameRecordAutoRefreshInterval.Daily,
                last, default, last, CalendarTests.Eastern);
            Check.Equal(DateTimeOffset.Parse("2026-03-09T00:00:00-04:00"), due);
            return Task.CompletedTask;
        });
        yield return ("Real scheduler preserves success timestamp on partial failure", PartialFailureAsync);
        yield return ("Real scheduler does not mark a failed run successful", FailedRunAsync);
        yield return ("Concurrent scheduler notifications coalesce safely", () =>
        {
            AppConfig.Reset();
            using var scheduler = Create(new GameRecordService());
            Parallel.For(0, 1000, _ => scheduler.NotifyScheduleChanged());
            return Task.CompletedTask;
        });
        yield return ("New account wakes the idle background scheduler", AccountWakeUpAsync);
    }

    private static (string, Func<Task>) Due(string name, GameRecordAutoRefreshInterval interval,
        DateTimeOffset success, DateTimeOffset attempt, DateTimeOffset now, DateTimeOffset expected)
    {
        return (name, () =>
        {
            Check.Equal(expected, GameRecordAutoRefreshSchedule.GetNextDueTime(interval, success, attempt, now, CalendarTests.Moscow));
            return Task.CompletedTask;
        });
    }

    private static GameRecordRole Role() => new() { GameBiz = GameBiz.hk4e_global, Uid = 123, Nickname = "Test" };
    private static GameRecordAutoRefreshService Create(GameRecordService service) => new(NullLogger<GameRecordAutoRefreshService>.Instance, service);

    private static async Task PartialFailureAsync()
    {
        AppConfig.Reset();
        var service = new GameRecordService { FailOperation = "Theater" };
        var role = Role();
        service.AddRole(role);
        AppConfig.SetInterval(GameBiz.hk4e, GameRecordAutoRefreshInterval.Monthly);
        DateTimeOffset previousSuccess = DateTimeOffset.Now.AddHours(-4);
        AppConfig.SetGameRecordLastAutoRefreshTime(GameBiz.hk4e, role.Uid, previousSuccess);
        using var scheduler = Create(service);

        var result = await scheduler.RefreshGameNowAsync(GameBiz.hk4e);
        Check.Equal(4, result.SuccessfulOperations);
        Check.Equal(1, result.FailedOperations);
        Check.Equal(previousSuccess, scheduler.GetLastSuccessfulRefreshTime(GameBiz.hk4e));
        DateTimeOffset attempt = AppConfig.GetGameRecordLastAutoRefreshAttemptTime(GameBiz.hk4e, role.Uid);
        Check.Equal(attempt.AddHours(2), scheduler.GetNextScheduledRefreshTime(GameBiz.hk4e));

        service.FailOperation = null;
        result = await scheduler.RefreshGameNowAsync(GameBiz.hk4e);
        Check.Equal(0, result.FailedOperations);
        DateTimeOffset completed = scheduler.GetLastSuccessfulRefreshTime(GameBiz.hk4e);
        Check.True(completed > previousSuccess, "Full success must advance the timestamp.");
        DateTime nextMonth = new DateTime(completed.LocalDateTime.Year, completed.LocalDateTime.Month, 1).AddMonths(1);
        Check.Equal(nextMonth, scheduler.GetNextScheduledRefreshTime(GameBiz.hk4e).LocalDateTime);
    }

    private static async Task FailedRunAsync()
    {
        AppConfig.Reset();
        var service = new GameRecordService { FailOperation = "*" };
        service.AddRole(Role());
        AppConfig.SetInterval(GameBiz.hk4e, GameRecordAutoRefreshInterval.Daily);
        using var scheduler = Create(service);
        var result = await scheduler.RefreshGameNowAsync(GameBiz.hk4e);
        Check.Equal(0, result.SuccessfulOperations);
        Check.Equal(5, result.FailedOperations);
        Check.Equal(default, scheduler.GetLastSuccessfulRefreshTime(GameBiz.hk4e));
        Check.True(scheduler.GetNextScheduledRefreshTime(GameBiz.hk4e) > DateTimeOffset.Now.AddMinutes(119), "Failed first run must back off.");
    }

    private static async Task AccountWakeUpAsync()
    {
        AppConfig.Reset();
        AppConfig.SetInterval(GameBiz.hk4e, GameRecordAutoRefreshInterval.Daily);
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        var service = new GameRecordService
        {
            RolesRead = () => { if (Interlocked.Increment(ref reads) >= 2) idle.TrySetResult(); },
            OperationStarted = _ => started.TrySetResult(),
        };
        using var scheduler = Create(service);
        scheduler.Start();
        try
        {
            await idle.Task.WaitAsync(TimeSpan.FromSeconds(15));
            service.AddRole(Role());
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            scheduler.Stop();
            // Wait for cancellation before disposing semaphores. Keep this
            // test-only access out of the application's public service API.
            var background = (Task)typeof(GameRecordAutoRefreshService)
                .GetField("_backgroundTask", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scheduler)!;
            await background.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
