namespace Starward.Features.GameRecord;

/// <summary>
/// Automatic refresh schedule for HoYoLAB game records.
/// </summary>
public enum GameRecordAutoRefreshInterval
{
    Disabled = 0,
    OnStartup = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4,
}
