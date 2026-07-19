namespace Starward.Features.GameRecord;

internal sealed record GameRecordAutoRefreshResult(
    int TotalRoles,
    int RefreshedRoles,
    int FailedRoles,
    int SuccessfulOperations,
    int FailedOperations)
{
    public bool HasAnySuccess => RefreshedRoles > 0 || SuccessfulOperations > 0;
}
