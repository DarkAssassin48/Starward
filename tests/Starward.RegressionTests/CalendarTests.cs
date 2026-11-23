using Starward.Features.PlayTime;

namespace Starward.RegressionTests;


internal static class CalendarTests
{
    internal static readonly TimeZoneInfo Moscow = TimeZoneInfo.CreateCustomTimeZone(
        "Test/Moscow", TimeSpan.FromHours(3), "UTC+03", "UTC+03");

    internal static readonly TimeZoneInfo Eastern = TimeZoneInfo.CreateCustomTimeZone(
        "Test/Eastern", TimeSpan.FromHours(-5), "Eastern", "EST", "EDT",
        [TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2020, 1, 1), new DateTime(2030, 12, 31), TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday))]);

    private static readonly TimeZoneInfo MidnightDst = TimeZoneInfo.CreateCustomTimeZone(
        "Test/Midnight", TimeSpan.FromHours(-5), "Midnight", "Standard", "Daylight",
        [TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2020, 1, 1), new DateTime(2030, 12, 31), TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 3, 1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 1, 0, 0), 11, 1))]);

    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return Session("Moscow early morning stays on the local date", Moscow,
            "2026-09-01T01:00:00+03:00", "2026-09-01T02:00:00+03:00", ("2026-09-01", 60));
        yield return Session("UTC midnight does not split a Moscow day", Moscow,
            "2026-09-01T02:30:00+03:00", "2026-09-01T03:30:00+03:00", ("2026-09-01", 60));
        yield return Session("Local midnight splits a session", Moscow,
            "2026-08-31T23:30:00+03:00", "2026-09-01T00:30:00+03:00", ("2026-08-31", 30), ("2026-09-01", 30));
        yield return Session("Negative UTC offset keeps the local date", Eastern,
            "2026-09-01T20:00:00-04:00", "2026-09-01T21:00:00-04:00", ("2026-09-01", 60));
        yield return Session("UTC calendar", TimeZoneInfo.Utc,
            "2026-08-31T23:30:00Z", "2026-09-01T00:30:00Z", ("2026-08-31", 30), ("2026-09-01", 30));
        yield return Session("Leap day is included", Moscow,
            "2024-02-28T23:30:00+03:00", "2024-03-01T00:30:00+03:00",
            ("2024-02-28", 30), ("2024-02-29", 1440), ("2024-03-01", 30));
        yield return Session("Year boundary", Moscow,
            "2025-12-31T23:30:00+03:00", "2026-01-01T00:30:00+03:00", ("2025-12-31", 30), ("2026-01-01", 30));
        yield return Session("Midnight end does not add a played day", Moscow,
            "2026-08-31T23:00:00+03:00", "2026-09-01T00:00:00+03:00", ("2026-08-31", 60));
        yield return Session("Spring daylight-saving day has 23 hours", Eastern,
            "2026-03-08T00:00:00-05:00", "2026-03-09T00:00:00-04:00", ("2026-03-08", 1380));
        yield return Session("Autumn daylight-saving day has 25 hours", Eastern,
            "2026-11-01T00:00:00-04:00", "2026-11-02T00:00:00-05:00", ("2026-11-01", 1500));
        yield return Session("Skipped midnight", MidnightDst,
            "2026-02-28T23:30:00-05:00", "2026-03-01T01:30:00-04:00", ("2026-02-28", 30), ("2026-03-01", 30));
        yield return Session("Repeated midnight belongs to the new date", MidnightDst,
            "2026-10-31T23:30:00-04:00", "2026-11-01T01:30:00-05:00", ("2026-10-31", 30), ("2026-11-01", 150));
        yield return ("Millisecond precision and multiple sessions", () =>
        {
            var days = new Dictionary<DateOnly, long>();
            Add(days, "2026-08-31T23:59:59.999+03:00", "2026-09-01T00:00:00.001+03:00", Moscow);
            Add(days, "2026-09-01T00:00:00.001+03:00", "2026-09-01T00:00:00.003+03:00", Moscow);
            Check.Equal(1L, days[new DateOnly(2026, 8, 31)]);
            Check.Equal(3L, days[new DateOnly(2026, 9, 1)]);
            return Task.CompletedTask;
        });
        yield return ("Empty or reversed sessions do not add played days", () =>
        {
            var days = new Dictionary<DateOnly, long>();
            PlayTimeDailyAggregation.AddSession(days, 1000, 1000, Moscow);
            PlayTimeDailyAggregation.AddSession(days, 1000, 999, Moscow);
            Check.Equal(0, days.Count);
            return Task.CompletedTask;
        });
    }

    private static (string, Func<Task>) Session(string name, TimeZoneInfo zone, string start, string end,
        params (string Date, long Minutes)[] expected)
    {
        return (name, () =>
        {
            var days = new Dictionary<DateOnly, long>();
            Add(days, start, end, zone);
            Check.Equal(expected.Length, days.Count);
            foreach (var (date, minutes) in expected)
            {
                Check.Equal(minutes * 60_000, days[DateOnly.Parse(date)]);
            }
            Check.Equal((long)(DateTimeOffset.Parse(end) - DateTimeOffset.Parse(start)).TotalMilliseconds, days.Values.Sum());
            Check.True(days.Values.All(x => x > 0), "Every counted day must have positive duration.");
            return Task.CompletedTask;
        });
    }

    private static void Add(Dictionary<DateOnly, long> days, string start, string end, TimeZoneInfo zone)
    {
        PlayTimeDailyAggregation.AddSession(days, DateTimeOffset.Parse(start).ToUnixTimeMilliseconds(),
            DateTimeOffset.Parse(end).ToUnixTimeMilliseconds(), zone);
    }
}
