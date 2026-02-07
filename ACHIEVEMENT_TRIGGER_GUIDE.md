## 成就触发器实现指南

### 触发器接口说明

所有触发器都必须实现 `IAchievementTrigger` 接口：

```csharp
public interface IAchievementTrigger
{
    // 触发器类型（用于分类）
    AchievementTriggerType TriggerType { get; }
    
    // 获取符合条件的玩家列表
    IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context);
    
    // 计算单个玩家的进度值
    int CalculateProgress(AchievementTriggerContext context, string playerId);
}
```

### 触发上下文

`AchievementTriggerContext` 提供了触发器所需的所有信息：

```csharp
public class AchievementTriggerContext
{
    public ServerBattle? Battle { get; set; }              // 战斗对象
    public List<BattleReward>? PlayerRewards { get; set; } // 玩家奖励列表
    public Dictionary<string, object> ExtraData { get; set; } // 额外数据
}
```

### 实现示例

#### 1. 简单的战斗结束成就（无伤胜利）

```csharp
public sealed class PerfectVictoryTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        var victoryTeam = context.Battle.WinnerCamp;
        if (victoryTeam == null)
            return Enumerable.Empty<string>();

        // 获取获胜队伍的所有无伤玩家
        return context.Battle.GetAllPlayers()
            .Where(p => p.Camp == victoryTeam && p.CurrentHP == p.MaxHP)
            .Select(p => p.PlayerId);
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        return 1; // 触发条件满足则进度为1
    }
}
```

#### 2. 累积型成就（长考）

```csharp
public sealed class LongThinkingTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        var winningPlayers = context.Battle.GetPlayersEligibleForLongThinkingAchievement();
        return winningPlayers;
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        if (context.Battle == null)
            return 0;

        // 计算对手总行动时间（秒）
        var opponentTime = context.Battle.GetOpponentTotalActionTime(playerId);
        return (int)opponentTime.TotalSeconds; // 返回对手行动时间
    }
}
```

成就定义中需要设置 `RequiredProgress` 为目标值（如600秒=10分钟）。

#### 3. 跨战斗成就（绝对幸运）

```csharp
public sealed class AbsoluteLuckTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.Manual;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        // 跨战斗成就不通过自动检测，需要手动更新
        return Enumerable.Empty<string>();
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 进度由客户端或其他系统手动更新
        return 0;
    }
}
```

### 集成到成就定义

每个成就定义需要包含触发器实例：

```csharp
public sealed class PerfectVictoryAchievement : IAchievementDefinition
{
    private static readonly IAchievementTrigger _trigger = new PerfectVictoryTrigger();

    public string Id => "perfect_victory";
    public string Name => "绝对碾压";
    public string Description => "这还是攻，这还是防";
    public string LockedHint => "以压倒性优势获胜";
    public string UnlockedHint => "己方无人受伤的情况下获胜";
    public string Icon => "achievement_perfect_victory";
    public int RequiredProgress => 1;
    
    public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>
    {
        new RewardDto { Type = "Item", ItemId = "ascension_proof", Quantity = 1 }
    };
    
    public IAchievementTrigger Trigger => _trigger; // ← 关键
}
```

### 触发流程详解

#### 战斗结束时的自动检查

```csharp
// 在 GameServer.BroadcastBattleEndAsync() 中
var achievementContext = new AchievementTriggerContext
{
    Battle = battle,
    PlayerRewards = playerRewards
};

// 调用管理器进行检查
var completedAchievements = _achievementManager.CheckBattleEndAchievements(achievementContext);

// 将完成的成就加入奖励
foreach (var (playerId, achievementId) in completedAchievements)
{
    var reward = playerRewards.FirstOrDefault(r => r.PlayerId == playerId);
    if (reward != null && !reward.AchievementsUnlocked.Contains(achievementId))
    {
        reward.AchievementsUnlocked.Add(achievementId);
    }
}
```

#### 管理器的检查逻辑

```csharp
public List<(string PlayerId, string AchievementId)> CheckBattleEndAchievements(AchievementTriggerContext context)
{
    var completedAchievements = new List<(string PlayerId, string AchievementId)>();

    foreach (var achievementId in AchievementCatalog.DefaultIds)
    {
        var definition = GetDefinition(achievementId);
        var trigger = definition.Trigger;

        // 只处理战斗结束类型的触发器
        if (trigger.TriggerType != AchievementTriggerType.BattleEnd)
            continue;

        // 获取符合条件的玩家
        var eligiblePlayers = trigger.GetEligiblePlayers(context);

        foreach (var playerId in eligiblePlayers)
        {
            // 计算进度
            int progress = trigger.CalculateProgress(context, playerId);

            // 更新成就
            var (success, isCompleted, _, _) = UpdateAchievementProgress(
                playerId, 
                achievementId, 
                progress
            );

            if (success && isCompleted)
            {
                completedAchievements.Add((playerId, achievementId));
            }
        }
    }

    return completedAchievements;
}
```

### 添加新成就的完整步骤

1. **创建文件夹**
   ```
   EonVientianeServer/Achievements/YourAchievement/
   ```

2. **创建触发器类** (YourAchievementTrigger.cs)
   ```csharp
   public sealed class YourAchievementTrigger : IAchievementTrigger
   {
       public AchievementTriggerType TriggerType => /* 选择类型 */;
       
       public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
       {
           // 实现检查逻辑
       }
       
       public int CalculateProgress(AchievementTriggerContext context, string playerId)
       {
           // 实现进度计算
       }
   }
   ```

3. **创建成就定义** (YourAchievementDefinition.cs)
   ```csharp
   public sealed class YourAchievement : IAchievementDefinition
   {
       private static readonly IAchievementTrigger _trigger = new YourAchievementTrigger();
       
       public string Id => "your_achievement";
       public string Name => "成就名称";
       // ... 其他属性
       public IAchievementTrigger Trigger => _trigger;
   }
   ```

4. **注册到目录** (AchievementCatalog.cs)
   ```csharp
   private static readonly List<IAchievementDefinition> Definitions = new()
   {
       // ... 现有成就
       new YourAchievement.YourAchievement() // 添加新成就
   };
   ```

### 最佳实践

✅ **Do**
- 使用 `TriggerType` 清晰地表达触发时机
- 在 `GetEligiblePlayers()` 中进行过滤，只返回符合条件的玩家
- 在 `CalculateProgress()` 中计算具体的进度值
- 给触发器类添加清晰的注释
- 对复杂的检查逻辑提取出辅助方法

❌ **Don't**
- 在触发器中修改任何游戏状态
- 在 `CalculateProgress()` 中进行玩家过滤（这应该在 `GetEligiblePlayers()` 中完成）
- 忽略 `null` 检查
- 使用硬编码的魔数，应该在成就定义中配置

### 调试技巧

1. **查看成就检查日志**
   - 在 `AchievementManager.CheckBattleEndAchievements()` 中添加日志
   - 输出每个检查的结果

2. **验证触发器逻辑**
   - 为每个触发器编写单元测试
   - 测试边界情况和异常输入

3. **追踪成就完成**
   - 在 `UpdateAchievementProgress()` 中已有详细的日志
   - 查看 `server_log.txt` 获取完成情况
