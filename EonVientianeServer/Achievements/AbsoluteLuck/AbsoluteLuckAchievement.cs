using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.AbsoluteLuck;

public sealed class AbsoluteLuckAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new AbsoluteLuckTrigger();

    public string Id => "absolute_luck";
    public string Name => "绝对幸运";
    public string Description => "你是怎么到这里的？";
    public string LockedHint => "这根本不可能？也许吧。";
    public string UnlockedHint => "连胜6局并期间所有掷出骰子点数均相同";
    public string Icon => "achievement_absolute_luck";
    public int RequiredProgress => 6;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "concerted_effort", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
