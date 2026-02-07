using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.LongThinking;

public sealed class LongThinkingTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        var eligiblePlayers = new List<string>();

        // 获取获胜的玩家（对手总行动时间长的玩家）
        var winningPlayers = context.Battle.GetPlayersEligibleForLongThinkingAchievement();
        eligiblePlayers.AddRange(winningPlayers);

        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        if (context.Battle == null)
            return 0;

        // 计算对手的总行动时间（秒）
        var opponentTime = context.Battle.GetOpponentTotalActionTime(playerId);
        return (int)opponentTime.TotalSeconds;
    }
}
