using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.PerfectVictory;

public sealed class PerfectVictoryAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new PerfectVictoryTrigger();

    public string Id => "perfect_victory";
    public string Name => "绝对碾压";
    public string Description => "这还是攻，这还是防";
    public string LockedHint => "以压倒性优势获胜";
    public string UnlockedHint => "己方无人受伤的情况下获胜";
    public string Icon => "achievement_perfect_victory";
    public int RequiredProgress => 1;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "ascension_proof", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
