using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.BlitzVictory;

public sealed class BlitzVictoryAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new BlitzVictoryTrigger();

    public string Id => "blitz_victory";
    public string Name => "秒了";
    public string Description => "人生啊~能不能放过我这一次~";
    public string LockedHint => "速战速决";
    public string UnlockedHint => "己方总行动时间在5秒内的情况下胜利";
    public string Icon => "achievement_blitz_victory";
    public int RequiredProgress => 1;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "wanderer_heart", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
