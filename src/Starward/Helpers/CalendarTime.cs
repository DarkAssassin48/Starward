using System;
using System.Linq;

namespace Starward.Helpers;


internal static class CalendarTime
{
    /// <summary>
    /// Returns the first instant of a local date, including time zones whose
    /// daylight-saving transition skips or repeats midnight.
    /// </summary>
    public static DateTimeOffset GetStartOfDay(DateTime date, TimeZoneInfo timeZone)
    {
        date = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(date))
        {
            date = date.AddMinutes(1);
        }

        TimeSpan offset = timeZone.IsAmbiguousTime(date)
            ? timeZone.GetAmbiguousTimeOffsets(date).Max()
            : timeZone.GetUtcOffset(date);
        return new DateTimeOffset(date, offset);
    }
}
