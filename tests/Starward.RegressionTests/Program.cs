using Starward.RegressionTests;

int failed = 0;
var tests = CalendarTests.Cases().Concat(ScheduleTests.Cases()).ToArray();
foreach (var (name, run) in tests)
{
    try
    {
        await run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {ex}");
    }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} regression checks passed.");
return failed == 0 ? 0 : 1;

namespace Starward.RegressionTests
{
    internal static class Check
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"Expected {expected}, got {actual}.");
            }
        }

        public static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
