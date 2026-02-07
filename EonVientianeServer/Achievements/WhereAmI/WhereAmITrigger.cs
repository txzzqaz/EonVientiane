using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.WhereAmI;

public sealed class WhereAmITrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        var eligiblePlayers = new List<string>();

        // 简化版本：所有参与战斗的玩家
        // 实际的"漫游者之心"和"增益"检查需要通过战斗数据实现
        foreach (var player in context.Battle.GetAllPlayers())
        {
            eligiblePlayers.Add(player.PlayerId);
        }

        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件完成，进度为1
        return 1;
    }
}
