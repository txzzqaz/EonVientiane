using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.AttackOnly;

public sealed class AttackOnlyAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new AttackOnlyTrigger();

    public string Id => "attack_only";
    public string Name => "只攻不防";
    public string Description => "预言石上血痕现";
    public string LockedHint => "不依赖防御而胜利";
    public string UnlockedHint => "不使用PD获得胜利";
    public string Icon => "achievement_attack_only";
    public int RequiredProgress => 1;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "blood_trace", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
