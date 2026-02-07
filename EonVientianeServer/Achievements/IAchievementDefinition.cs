using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements;

/// <summary>
/// 成就定义接口 - 包含成就的基本信息和触发器
/// </summary>
public interface IAchievementDefinition
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string LockedHint { get; }
    string UnlockedHint { get; }
    string Icon { get; }
    int RequiredProgress { get; }
    IReadOnlyList<RewardDto> Rewards { get; }

    /// <summary>
    /// 成就的触发器 - 定义何时及如何触发此成就
    /// </summary>
    IAchievementTrigger Trigger { get; }
}
