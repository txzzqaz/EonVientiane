# 成就系统架构设计说明

## 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                   AchievementManager                            │
│                  (成就触发总管理器)                              │
├─────────────────────────────────────────────────────────────────┤
│  • 在战斗结束时调用 TriggerAchievements()                        │
│  • 为每个成就创建 AchievementTriggerContext                      │
│  • 调用各个 Trigger 的 GetEligiblePlayers()                     │
│  • 调用各个 Trigger 的 CalculateProgress()                      │
│  • 保存成就到数据库                                             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
          ┌───────────────────┴───────────────────┐
          ↓                                         ↓
┌─────────────────────────────────┐  ┌─────────────────────────────────┐
│   WhereAmITrigger               │  │   GuashaMasterTrigger          │
│   (我在哪？成就触发器)          │  │   (刮痧成就触发器)             │
├─────────────────────────────────┤  ├─────────────────────────────────┤
│ GetEligiblePlayers()            │  │ GetEligiblePlayers()           │
│  └─ 调用 API 检查条件           │  │  └─ 调用 API 检查条件          │
│ CalculateProgress()             │  │ CalculateProgress()            │
│  └─ 返回 1                      │  │  └─ 返回 1                     │
└─────────────────────────────────┘  └─────────────────────────────────┘
          ↓                                         ↓
          └───────────────────┬───────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                     ServerBattle                                │
│                  (战斗数据持有者)                                │
├─────────────────────────────────────────────────────────────────┤
│ IsEligibleForWhereAmIAchievement(playerId)                     │
│  ├─ 检查装备的饰品                                             │
│  ├─ 检查 _playerWandererHeartTriggered                          │
│  └─ 返回 bool                                                   │
│                                                                 │
│ IsEligibleForGuashaMasterAchievement(playerId)                │
│  ├─ 检查 _playerDamageSequence                                  │
│  ├─ 查找连续10个1                                              │
│  └─ 返回 bool                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## 数据流向

### 漫游者之心增益追踪

```
ProcessPlayerAttackChoice()
  │
  └─→ ApplyWandererHeartMultiplier()
       │
       ├─ 计算倍率
       │
       └─ 如果倍率 > 1.0:
           _playerWandererHeartTriggered[playerId] = true
           
战斗结束:
  │
  └─→ IsEligibleForWhereAmIAchievement()
       │
       └─ 检查 _playerWandererHeartTriggered[playerId]
           ├─ false → 符合条件 ✓
           └─ true  → 不符合条件 ✗
```

### 伤害序列追踪

```
ResolveAttackResult()
  │
  └─→ ApplyDamage()
       │
       ├─ 计算伤害
       │
       ├─ _playerDamageSequence[attacker].Add(actualDamage)
       │
       └─ 如果有额外伤害:
           _playerDamageSequence[attacker].Add(extraDamage)

战斗结束:
  │
  └─→ IsEligibleForGuashaMasterAchievement()
       │
       └─ 遍历 _playerDamageSequence
           └─ 查找连续10个1的子序列
               ├─ 找到 → 符合条件 ✓
               └─ 未找到 → 不符合条件 ✗
```

## 核心设计原则

### 1. 关注点分离 (Separation of Concerns)

| 组件 | 职责 |
|-----|------|
| Trigger | 实现成就触发接口，调用检查 API |
| ServerBattle | 存储战斗数据，提供检查 API |
| AchievementManager | 协调成就触发流程 |

**优点**:
- 易于理解：每个类只做一件事
- 易于维护：修改一个组件不影响其他
- 易于测试：可以独立测试每个组件

### 2. 依赖注入 (Dependency Injection)

```csharp
public class WhereAmITrigger : IAchievementTrigger
{
    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        // 通过 context 获得 battle 实例
        var battle = context.Battle;
        
        // 使用 battle 提供的 API
        if (battle.IsEligibleForWhereAmIAchievement(playerId))
        {
            // ...
        }
    }
}
```

**优点**:
- 松耦合：Trigger 不需要直接创建 ServerBattle
- 易于测试：可以传入 mock 对象
- 易于扩展：可以支持不同的战斗实现

### 3. 数据集中管理

所有战斗相关数据都存在 `ServerBattle` 中：
- `_playerWandererHeartTriggered`
- `_playerDamageSequence`
- `_playerDamageDealt`
- `_playerDamageTaken`
- 等等

**优点**:
- 单一数据源：一致性有保证
- 便于查询：所有数据在一个地方
- 便于扩展：添加新数据不需要修改多个地方

### 4. API 清晰化

提供清晰的公共 API：

```csharp
public bool IsEligibleForWhereAmIAchievement(string playerId)
public bool IsEligibleForGuashaMasterAchievement(string playerId)
```

**优点**:
- 接口清晰：一目了然
- 实现隐藏：调用者不需要知道实现细节
- 便于重构：改变实现不影响调用者

## 扩展性考虑

### 添加新的成就

假设要添加新成就"闪电手"：条件是一局内使用3次D4

**步骤**:

1. 在 `ServerBattle` 中添加追踪:
```csharp
private Dictionary<string, int> _playerD4Usage;
```

2. 在战斗过程中记录:
```csharp
if (selectedDice is D4Dice)
{
    _playerD4Usage[playerId]++;
}
```

3. 添加 API:
```csharp
public bool IsEligibleForLightningHandAchievement(string playerId)
{
    return _playerD4Usage.GetValueOrDefault(playerId, 0) >= 3;
}
```

4. 创建 Trigger:
```csharp
public class LightningHandTrigger : IAchievementTrigger
{
    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        var eligible = new List<string>();
        foreach (var player in context.Battle.GetAllPlayers())
        {
            if (context.Battle.IsEligibleForLightningHandAchievement(player.PlayerId))
                eligible.Add(player.PlayerId);
        }
        return eligible;
    }
    
    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        return 1;
    }
}
```

就这么简单！

## 性能考虑

### 数据结构选择

#### `_playerWandererHeartTriggered` - Dictionary<string, bool>
- **时间复杂度**: O(1) 查询，O(1) 更新
- **空间复杂度**: O(n) 其中 n 是玩家数（通常 2-4）
- **适合**: 布尔值标记

#### `_playerDamageSequence` - Dictionary<string, List<int>>
- **时间复杂度**: O(1) 添加，O(n) 查询其中 n 是伤害数
- **空间复杂度**: O(n) 其中 n 是伤害数
- **适合**: 序列数据

### 查询优化

`IsEligibleForGuashaMasterAchievement` 的查询：

```csharp
// 时间复杂度: O(n) 其中 n 是伤害数
// 空间复杂度: O(1)
for (int i = 0; i <= damageSeq.Count - 10; i++)
{
    bool foundSequence = true;
    for (int j = i; j < i + 10; j++)
    {
        if (damageSeq[j] != 1)
        {
            foundSequence = false;
            break;
        }
    }
    if (foundSequence)
        return true;
}
```

**优化点**:
- 早期返回：找到目标立即返回
- 最小空间：不需要额外数据结构

## 日志和调试

### 添加的日志

#### "我在哪？"成就
```
[WhereAmI Check] {PlayerName}未装备漫游者之心
[WhereAmI Check] {PlayerName}的漫游者之心触发过增益，不符合条件
[WhereAmI Check] {PlayerName}符合'我在哪？'成就条件
```

#### "刮痧"成就
```
[GuashaMaster Check] {PlayerId}没有伤害记录
[GuashaMaster Check] 玩家总伤害次数{Count}次，不足10次
[GuashaMaster Check] 玩家{PlayerId}找到连续10次伤害都是1点的序列（位置{i}-{i+9}）
[GuashaMaster Check] 玩家{PlayerId}未找到连续10次伤害都是1点的序列
```

### 调试技巧

1. 查看伤害序列:
```
搜索日志中的 "[GuashaMaster Check] 玩家XX找到连续10次伤害..."
或者在 VS 中下断点
```

2. 检查漫游者之心触发:
```
搜索日志中的 "漫游者之心触发！"
查看是否触发过增益
```

## 总结

这个设计通过以下方式确保了成就系统的正确性和可维护性：

1. **清晰的架构**: Trigger 负责接口，Battle 负责数据和逻辑
2. **完整的数据追踪**: 所有必要的战斗数据都被记录
3. **灵活的 API**: 提供清晰的、易于扩展的接口
4. **优秀的可维护性**: 易于理解、修改和扩展
5. **良好的可测试性**: 每个组件都可以独立测试
