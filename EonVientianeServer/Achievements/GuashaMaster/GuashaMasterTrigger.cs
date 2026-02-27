using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.GuashaMaster;

public sealed class GuashaMasterTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
        {
            Console.WriteLine("[GuashaMasterTrigger] Battle is null!");
            return Enumerable.Empty<string>();
        }

        var eligiblePlayers = new List<string>();

        // 检查每个玩家是否满足成就条件
        foreach (var player in context.Battle.GetAllPlayers())
        {
            var damageSeq = context.Battle.AchievementTracker.GetDamageSequence(player.PlayerId);
            if (damageSeq.Count < 10)
            {
                continue;
            }

            bool foundSequence = false;
            for (int i = 0; i <= damageSeq.Count - 10; i++)
            {
                bool allOnes = true;
                for (int j = i; j < i + 10; j++)
                {
                    if (damageSeq[j] != 1)
                    {
                        allOnes = false;
                        break;
                    }
                }

                if (allOnes)
                {
                    foundSequence = true;
                    break;
                }
            }

            if (foundSequence)
            {
                eligiblePlayers.Add(player.PlayerId);
                Console.WriteLine($"[GuashaMasterTrigger] Player {player.PlayerId} is eligible for 'GuashaMaster' achievement");
            }
        }

        Console.WriteLine($"[GuashaMasterTrigger] Total eligible players: {eligiblePlayers.Count}");
        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件完成，进度为1
        Console.WriteLine($"[GuashaMasterTrigger] CalculateProgress called for player {playerId}");
        return 1;
    }
}

