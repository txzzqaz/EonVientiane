using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.FirstDefense;

public sealed class FirstDefenseAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new FirstDefenseTrigger();

    public string Id => "first_defense";
    public string Name => "第一次防御";
    public string Description => "这是攻，这是防";
    public string LockedHint => "学会如何保护自己";
    public string UnlockedHint => "进行第一次防御";
    public string Icon => "achievement_first_defense";
    public int RequiredProgress => 1;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "feathered_dice", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
