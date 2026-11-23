using Starward.Helpers;
using System;

namespace Starward.Features.GameRecord;


internal static class GameRecordAutoRefreshSchedule
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromHours(2);

    public static DateTimeOffset GetNextDueTime(
        GameRecordAutoRefreshInterval interval,
        DateTimeOffset lastSuccess,
        DateTimeOffset lastAttempt,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        if (interval is not (GameRecordAutoRefreshInterval.Daily
            or GameRecordAutoRefreshInterval.Weekly
            or GameRecordAutoRefreshInterval.Monthly))
        {
            return DateTimeOffset.MaxValue;
        }

        // A complete run records success after its attempt. A newer attempt
        // therefore means a failed, partial, or interrupted run needs a retry.
        if (lastAttempt > lastSuccess)
        {
            return lastAttempt.Add(RetryDelay);
        }

        if (lastSuccess == default)
        {
            return now;
        }

        DateTime localLast = TimeZoneInfo.ConvertTime(lastSuccess, timeZone).Date;
        return interval switch
        {
            GameRecordAutoRefreshInterval.Daily => CalendarTime.GetStartOfDay(localLast.AddDays(1), timeZone),
            GameRecordAutoRefreshInterval.Weekly => lastSuccess.AddDays(7),
            GameRecordAutoRefreshInterval.Monthly => CalendarTime.GetStartOfDay(
                new DateTime(localLast.Year, localLast.Month, 1).AddMonths(1), timeZone),
            _ => DateTimeOffset.MaxValue,
        };
    }
}
