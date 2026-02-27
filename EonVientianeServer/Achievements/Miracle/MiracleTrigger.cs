using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.Miracle;

public sealed class MiracleTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
        {
            Console.WriteLine("[MiracleTrigger] Battle is null!");
            return Enumerable.Empty<string>();
        }

        var eligiblePlayers = new List<string>();

        // 检查每个玩家是否满足成就条件
        foreach (var player in context.Battle.GetAllPlayers())
        {
            var streak = context.Battle.AchievementTracker.GetFeatheredDodgeStreak(player.PlayerId);
            if (streak >= 5)
            {
                eligiblePlayers.Add(player.PlayerId);
                Console.WriteLine($"[MiracleTrigger] Player {player.PlayerId} is eligible for 'Miracle' (streak={streak})");
            }
        }

        Console.WriteLine($"[MiracleTrigger] Total eligible players: {eligiblePlayers.Count}");
        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件完成，进度为1
        Console.WriteLine($"[MiracleTrigger] CalculateProgress called for player {playerId}");
        return 1;
    }
}
