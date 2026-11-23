# Regression checks

Run from the repository root with the SDK specified by `global.json`:

```powershell
dotnet run --project tests/Starward.RegressionTests -c Release
```

The executable exits with a nonzero code if any check fails. CI runs it before
the Windows application build matrix.

The checks use the application's date aggregation and scheduler sources directly.
Only the scheduler's database/API/settings/UI boundaries are replaced with in-memory
fakes. They never access saved accounts, cookies, user databases, or game servers.

Coverage includes local/UTC midnight, leap years, daylight-saving changes,
millisecond conservation, partial refresh failures, retry backoff, concurrent
wake-up signals, and adding an account while the background scheduler is idle.
The idle-scheduler check includes its normal five-second startup delay.

WinUI rendering and real HoYoLAB login still require manual verification of a
Windows build; they are not covered by this headless runner.
