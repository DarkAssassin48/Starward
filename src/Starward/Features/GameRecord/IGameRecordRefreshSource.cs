using Starward.Core;
using Starward.Core.GameRecord;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord;

internal interface IGameRecordRefreshSource
{
    event EventHandler? GameRolesChanged;
    List<GameRecordRole> GetGameRolesOfGame(GameBiz game);
    IEnumerable<(string Name, Func<Task> Run)> GetOperations(GameBiz game, GameRecordRole role, CancellationToken cancellationToken);
}
