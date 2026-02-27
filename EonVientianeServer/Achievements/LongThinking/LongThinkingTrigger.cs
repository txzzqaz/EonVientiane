using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.LongThinking;

public sealed class LongThinkingTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
        {
            Console.WriteLine("[LongThinkingTrigger] Battle is null!");
            return Enumerable.Empty<string>();
        }

        var eligiblePlayers = new List<string>();

        // 检查每个玩家是否满足成就条件
        foreach (var player in context.Battle.GetAllPlayers())
        {
            var opponentTime = context.Battle.GetOpponentTotalActionTime(player.PlayerId);
            if (opponentTime.TotalSeconds >= 600)
            {
                eligiblePlayers.Add(player.PlayerId);
                Console.WriteLine($"[LongThinkingTrigger] Player {player.PlayerId} is eligible for 'LongThinking' (opponentTime={opponentTime.TotalSeconds:F1}s)");
            }
        }

        Console.WriteLine($"[LongThinkingTrigger] Total eligible players: {eligiblePlayers.Count}");
        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件完成，进度为1
        Console.WriteLine($"[LongThinkingTrigger] CalculateProgress called for player {playerId}");
        return 1;
    }
}
