using Starward.Helpers;
using System;
using System.Collections.Generic;

namespace Starward.Features.PlayTime;


internal static class PlayTimeDailyAggregation
{
    public static void AddSession(
        Dictionary<DateOnly, long> timePerDay,
        long startMilliseconds,
        long endMilliseconds,
        TimeZoneInfo timeZone,
        int localTimeOffsetHours = 0)
    {
        if (endMilliseconds <= startMilliseconds)
        {
            return;
        }

        DateTimeOffset start = DateTimeOffset.FromUnixTimeMilliseconds(startMilliseconds);
        DateTimeOffset end = DateTimeOffset.FromUnixTimeMilliseconds(endMilliseconds);
        int offset = localTimeOffsetHours is >= -23 and <= 23 ? localTimeOffsetHours : 0;
        while (start < end)
        {
            DateTime localDate = TimeZoneInfo.ConvertTime(start, timeZone).DateTime.AddHours(offset).Date;
            // Use the first instant of the next boundary even when the local clock repeats it.
            if (start < CalendarTime.GetLocalBoundary(localDate.AddHours(-offset), timeZone))
            {
                localDate = localDate.AddDays(-1);
            }
            DateTimeOffset nextDay = CalendarTime.GetLocalBoundary(localDate.AddDays(1).AddHours(-offset), timeZone);
            if (nextDay <= start)
            {
                localDate = localDate.AddDays(1);
                nextDay = CalendarTime.GetLocalBoundary(localDate.AddDays(1).AddHours(-offset), timeZone);
            }
            DateTimeOffset segmentEnd = nextDay < end ? nextDay : end;

            // Half-open intervals preserve every millisecond and do not count
            // the following day when a session ends exactly at midnight.
            long duration = (segmentEnd - start).Ticks / TimeSpan.TicksPerMillisecond;
            var day = DateOnly.FromDateTime(localDate);
            timePerDay[day] = timePerDay.GetValueOrDefault(day) + duration;
            start = segmentEnd;
        }
    }
}
