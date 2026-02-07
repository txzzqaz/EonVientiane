using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.BlitzVictory;

public sealed class BlitzVictoryTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        var eligiblePlayers = new List<string>();

        // 获取满足秒了条件的玩家（胜利且总行动时间在5秒内）
        var blitzVictoryPlayers = context.Battle.GetPlayersEligibleForBlitzVictoryAchievement();
        eligiblePlayers.AddRange(blitzVictoryPlayers);

        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件就完成，进度为1
        return 1;
    }
}
