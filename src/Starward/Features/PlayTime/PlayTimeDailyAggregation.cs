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
        TimeZoneInfo timeZone)
    {
        if (endMilliseconds <= startMilliseconds)
        {
            return;
        }

        DateTimeOffset start = DateTimeOffset.FromUnixTimeMilliseconds(startMilliseconds);
        DateTimeOffset end = DateTimeOffset.FromUnixTimeMilliseconds(endMilliseconds);
        while (start < end)
        {
            DateTime localDate = TimeZoneInfo.ConvertTime(start, timeZone).Date;
            DateTimeOffset nextDay = CalendarTime.GetStartOfDay(localDate.AddDays(1), timeZone);
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
