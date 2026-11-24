using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Features.GameRecord.Genshin;
using Starward.Features.GameRecord.StarRail;
using Starward.Features.GameRecord.ZZZ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord;

internal sealed class GameRecordRefreshSource(
    GameRecordGenshinService _genshin,
    GameRecordStarRailService _starRail,
    GameRecordZZZService _zzz) : IGameRecordRefreshSource
{
    public event EventHandler? GameRolesChanged
    {
        add => GameRecordRoleService.GameRolesChanged += value;
        remove => GameRecordRoleService.GameRolesChanged -= value;
    }

    public List<GameRecordRole> GetGameRolesOfGame(GameBiz game) => GameRecordRoleService.GetGameRolesOfGame(game);

    public IEnumerable<(string Name, Func<Task> Run)> GetOperations(GameBiz game, GameRecordRole role, CancellationToken cancellationToken)
    {
        switch (game.Game)
        {
            case GameBiz.hk4e:
                yield return ("SpiralAbyssCurrent", () => _genshin.RefreshSpiralAbyssInfoAsync(role, 1, cancellationToken));
                yield return ("SpiralAbyssPrevious", () => _genshin.RefreshSpiralAbyssInfoAsync(role, 2, cancellationToken));
                yield return ("ImaginariumTheater", () => _genshin.RefreshImaginariumTheaterInfoAsync(role, cancellationToken));
                yield return ("StygianOnslaught", () => _genshin.RefreshStygianOnslaughtInfosAsync(role, cancellationToken));
                yield return ("TravelersDiary", async () =>
                {
                    var currentSummary = await _genshin.GetTravelersDiarySummaryAsync(role, cancellationToken: cancellationToken);
                    var months = new HashSet<int>(currentSummary.OptionalMonth ?? []);
                    if (currentSummary.DataMonth > 0)
                    {
                        months.Add(currentSummary.DataMonth);
                    }

                    foreach (int month in months.Where(x => x > 0).OrderBy(x => x))
                    {
                        await _genshin.GetTravelersDiarySummaryAsync(role, month, cancellationToken);
                        await _genshin.GetTravelersDiaryDetailAsync(role, month, 1, cancellationToken: cancellationToken);
                        await _genshin.GetTravelersDiaryDetailAsync(role, month, 2, cancellationToken: cancellationToken);
                    }
                });
                break;

            case GameBiz.hkrpg:
                yield return ("TrailblazeCalendar", async () =>
                {
                    var currentSummary = await _starRail.GetTrailblazeCalendarSummaryAsync(role, cancellationToken: cancellationToken);
                    var months = new HashSet<string>(currentSummary.OptionalMonth ?? [], StringComparer.Ordinal);
                    if (!string.IsNullOrWhiteSpace(currentSummary.DataMonth))
                    {
                        months.Add(currentSummary.DataMonth);
                    }

                    foreach (string month in months.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x))
                    {
                        await _starRail.GetTrailblazeCalendarSummaryAsync(role, month, cancellationToken);
                        await _starRail.GetTrailblazeCalendarDetailAsync(role, month, 1, cancellationToken: cancellationToken);
                        await _starRail.GetTrailblazeCalendarDetailAsync(role, month, 2, cancellationToken: cancellationToken);
                    }
                });
                yield return ("SimulatedUniverse", () => _starRail.GetSimulatedUniverseInfoAsync(role, detail: true, cancellationToken));
                yield return ("ForgottenHallCurrent", () => _starRail.RefreshForgottenHallInfoAsync(role, 1, cancellationToken));
                yield return ("ForgottenHallPrevious", () => _starRail.RefreshForgottenHallInfoAsync(role, 2, cancellationToken));
                yield return ("PureFictionCurrent", () => _starRail.RefreshPureFictionInfoAsync(role, 1, cancellationToken));
                yield return ("PureFictionPrevious", () => _starRail.RefreshPureFictionInfoAsync(role, 2, cancellationToken));
                yield return ("ApocalypticShadowCurrent", () => _starRail.RefreshApocalypticShadowInfoAsync(role, 1, cancellationToken));
                yield return ("ApocalypticShadowPrevious", () => _starRail.RefreshApocalypticShadowInfoAsync(role, 2, cancellationToken));
                yield return ("AnomalyArbitration", () => _starRail.RefreshStarRailChallengePeakDataAsync(role, cancellationToken));
                break;

            case GameBiz.nap:
                yield return ("InterKnotMonthlyReport", async () =>
                {
                    var currentSummary = await _zzz.GetInterKnotReportSummaryAsync(role, cancellationToken: cancellationToken);
                    var months = new HashSet<string>(currentSummary.OptionalMonth ?? [], StringComparer.Ordinal);
                    if (!string.IsNullOrWhiteSpace(currentSummary.DataMonth))
                    {
                        months.Add(currentSummary.DataMonth);
                    }

                    foreach (string month in months.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x))
                    {
                        var summary = await _zzz.GetInterKnotReportSummaryAsync(role, month, cancellationToken);
                        foreach (var item in summary.MonthData.List)
                        {
                            await _zzz.GetInterKnotReportDetailAsync(role, month, item.DataType, cancellationToken: cancellationToken);
                        }
                    }
                });
                yield return ("ShiyuDefenseCurrent", () => _zzz.RefreshShiyuDefenseInfoAsync(role, 1, cancellationToken));
                yield return ("ShiyuDefensePrevious", () => _zzz.RefreshShiyuDefenseInfoAsync(role, 2, cancellationToken));
                yield return ("DeadlyAssaultCurrent", () => _zzz.RefreshDeadlyAssaultInfoAsync(role, 1, cancellationToken));
                yield return ("DeadlyAssaultPrevious", () => _zzz.RefreshDeadlyAssaultInfoAsync(role, 2, cancellationToken));
                break;
        }
    }
}
