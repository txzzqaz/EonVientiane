using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements.WhereAmI;

public sealed class WhereAmIAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new WhereAmITrigger();

    public string Id => "where_am_i";
    public string Name => "我在哪？";
    public string Description => "伟大的第一步是开始思考";
    public string LockedHint => "减速带";
    public string UnlockedHint => "携带饰品'漫游者之心'而一整局都没有触发过增益";
    public string Icon => "achievement_where_am_i";
    public int RequiredProgress => 1;
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "foresight", Quantity = 1 }
    };
    public IAchievementTrigger Trigger => _trigger;
}
