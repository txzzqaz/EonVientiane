using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.AttackOnly;

public sealed class AttackOnlyTrigger : IAchievementTrigger
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

        // 检查获胜队伍的所有成员是否都未使用过PD
        var winningPlayers = context.Battle.GetAllPlayers()
            .Where(p => p.Camp == victoryTeam)
            .ToList();

        foreach (var player in winningPlayers)
        {
            if (!context.Battle.AchievementTracker.HasUsedPD(player.PlayerId))
            {
                eligiblePlayers.Add(player.PlayerId);
            }
        }

        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 不使用PD获胜完成，进度为1
        return 1;
    }
}
