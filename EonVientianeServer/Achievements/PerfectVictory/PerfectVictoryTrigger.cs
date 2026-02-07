using System.Collections.Generic;
using System.Linq;
using EonVientiane;

namespace EonVientianeServer.Achievements.PerfectVictory;

public sealed class PerfectVictoryTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        var eligiblePlayers = new List<string>();

        // 获取获胜队伍
        var victoryTeam = context.Battle.WinnerCamp;
        if (victoryTeam == null)
            return eligiblePlayers;

        // 检查获胜队伍的所有成员是否都未受伤
        var winningPlayers = context.Battle.GetAllPlayers()
            .Where(p => p.Camp == victoryTeam)
            .ToList();

        if (winningPlayers.All(p => p.CurrentHP == p.MaxHP))
        {
            eligiblePlayers.AddRange(winningPlayers.Select(p => p.PlayerId));
        }

        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 无伤胜利完成，进度为1
        return 1;
    }
}
