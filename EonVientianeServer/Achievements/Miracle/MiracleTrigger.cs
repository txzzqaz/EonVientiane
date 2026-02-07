using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.Miracle;

public sealed class MiracleTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        var eligiblePlayers = new List<string>();

        // 获取满足成就条件的玩家
        // （使用飞羽骰子进行闪避连续成功5次）
        var miraclePlayers = context.Battle.GetPlayersEligibleForMiracleAchievement();
        eligiblePlayers.AddRange(miraclePlayers);

        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件完成，进度为1
        return 1;
    }
}
