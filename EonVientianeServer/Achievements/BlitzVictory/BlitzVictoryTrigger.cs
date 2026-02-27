using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.BlitzVictory;

public sealed class BlitzVictoryTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
        {
            Console.WriteLine("[BlitzVictoryTrigger] Battle is null!");
            return Enumerable.Empty<string>();
        }

        var eligiblePlayers = new List<string>();

        // 检查每个玩家是否满足成就条件
        var winner = context.Battle.WinnerCamp;
        foreach (var player in context.Battle.GetAllPlayers())
        {
            if (winner == null || player.Camp != winner)
            {
                continue;
            }

            var actionTime = context.Battle.GetPlayerTotalActionTime(player.PlayerId);
            if (actionTime.TotalSeconds <= 5.0)
            {
                eligiblePlayers.Add(player.PlayerId);
                Console.WriteLine($"[BlitzVictoryTrigger] Player {player.PlayerId} is eligible for 'BlitzVictory' (actionTime={actionTime.TotalSeconds:F2}s)");
            }
        }

        Console.WriteLine($"[BlitzVictoryTrigger] Total eligible players: {eligiblePlayers.Count}");
        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件完成，进度为1
        Console.WriteLine($"[BlitzVictoryTrigger] CalculateProgress called for player {playerId}");
        return 1;
    }
}
