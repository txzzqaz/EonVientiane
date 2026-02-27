using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane;

namespace EonVientianeServer.Achievements.WhereAmI;

public sealed class WhereAmITrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
        {
            Console.WriteLine("[WhereAmITrigger] Battle is null!");
            return Enumerable.Empty<string>();
        }

        var eligiblePlayers = new List<string>();

        // 检查每个玩家是否满足成就条件
        foreach (var player in context.Battle.GetAllPlayers())
        {
            bool hasWandererHeart = player.GetEquippedAccessories()
                .OfType<WandererHeartAccessory>()
                .Any();

            if (!hasWandererHeart)
            {
                continue;
            }

            if (!context.Battle.AchievementTracker.HasWandererHeartTriggered(player.PlayerId))
            {
                eligiblePlayers.Add(player.PlayerId);
                Console.WriteLine($"[WhereAmITrigger] Player {player.PlayerId} is eligible for 'WhereAmI' achievement");
            }
        }

        Console.WriteLine($"[WhereAmITrigger] Total eligible players: {eligiblePlayers.Count}");
        return eligiblePlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 满足条件完成，进度为1
        Console.WriteLine($"[WhereAmITrigger] CalculateProgress called for player {playerId}");
        return 1;
    }
}

