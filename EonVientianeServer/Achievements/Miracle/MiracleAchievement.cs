using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.Miracle;

public sealed class MiracleAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new MiracleTrigger();

    public string Id => "miracle";
    public string Name => "奇迹";
    public string Description => "注定伟大！";
    public string LockedHint => "飞羽！飞羽！飞羽！";
    public string UnlockedHint => "一局内使用飞羽骰子进行闪避连续成功5次";
    public string Icon => "achievement_miracle";
    public int RequiredProgress => 1;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "spring_breeze", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
