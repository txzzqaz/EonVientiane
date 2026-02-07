using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.GuashaMaster;

public sealed class GuashaMasterAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new GuashaMasterTrigger();

    public string Id => "guasha_master";
    public string Name => "刮痧";
    public string Description => "养生";
    public string LockedHint => "1111111111";
    public string UnlockedHint => "一局游戏内连续10回合造成了并且只造成1点伤害";
    public string Icon => "achievement_guasha_master";
    public int RequiredProgress => 1;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "guasha_parquet", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
