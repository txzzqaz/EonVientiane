using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer.Achievements;

/// <summary>
/// 成就触发器接口 - 定义成就的触发条件和检测方式
/// 每个成就都应该实现此接口来定义自己的触发逻辑
/// </summary>
public interface IAchievementTrigger
{
    /// <summary>
    /// 检查玩家是否满足成就条件
    /// </summary>
    /// <param name="context">成就触发上下文，包含战斗等信息</param>
    /// <param name="playerId">玩家ID</param>
    /// <returns>符合条件的玩家ID列表</returns>
    IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context);

    /// <summary>
    /// 计算玩家的成就进度
    /// </summary>
    /// <param name="context">成就触发上下文</param>
    /// <param name="playerId">玩家ID</param>
    /// <returns>进度值（0表示未满足）</returns>
    int CalculateProgress(AchievementTriggerContext context, string playerId);

    /// <summary>
    /// 成就的类型分类，用于不同场景的触发
    /// </summary>
    AchievementTriggerType TriggerType { get; }
}

/// <summary>
/// 成就触发器类型
/// </summary>
public enum AchievementTriggerType
{
    /// <summary>
    /// 战斗结束时触发
    /// </summary>
    BattleEnd,

    /// <summary>
    /// 玩家行动时触发
    /// </summary>
    PlayerAction,

    /// <summary>
    /// 骰子相关事件触发
    /// </summary>
    DiceEvent,

    /// <summary>
    /// 手动更新触发（通过UpdateAchievement消息）
    /// </summary>
    Manual,

    /// <summary>
    /// 其他自定义触发
    /// </summary>
    Custom
}

/// <summary>
/// 成就触发上下文 - 提供成就检测所需的游戏数据
/// </summary>
public class AchievementTriggerContext
{
    /// <summary>
    /// 完成的战斗
    /// </summary>
    public ServerBattle? Battle { get; set; }

    /// <summary>
    /// 所有玩家奖励信息
    /// </summary>
    public List<BattleReward>? PlayerRewards { get; set; }

    /// <summary>
    /// 其他上下文数据
    /// </summary>
    public Dictionary<string, object> ExtraData { get; set; } = new();
}
