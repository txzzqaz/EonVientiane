using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.LongThinking;

public sealed class LongThinkingAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new LongThinkingTrigger();

    public string Id => "long_thinking";
    public string Name => "长考";
    public string Description => "是时候终结这一切了";
    public string LockedHint => "不道德的行为";
    public string UnlockedHint => "一局游戏中敌方的总行动时间达到10分钟";
    public string Icon => "achievement_long_thinking";
    public int RequiredProgress => 600;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "holy_fire", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
