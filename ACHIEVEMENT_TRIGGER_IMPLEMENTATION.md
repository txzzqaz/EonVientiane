# 成就触发逻辑实现完成

## 修复概况

完成了两个成就的正确触发逻辑实现：
1. **"我在哪？"** (where_am_i)
2. **"刮痧"** (guasha_master)

## 1. "我在哪？" 成就实现

### 成就条件
携带饰品'漫游者之心'而一整局都没有触发过增益

### 实现方案

#### A. ServerBattle 中的数据追踪

在 `ServerBattle` 中添加：
```csharp
// 成就追踪 - 漫游者之心触发检测（用于"我在哪"成就）
private Dictionary<string, bool> _playerWandererHeartTriggered; // 追踪每个玩家的漫游者之心是否触发过增益
```

#### B. 漫游者之心增益触发标记

在 `ApplyWandererHeartMultiplier` 方法中：
```csharp
if (multiplier > 1.0)
{
    triggered = true;
}
```

在 `ProcessPlayerAttackChoice` 中记录触发：
```csharp
int finalAttackPower = ApplyWandererHeartMultiplier(attacker, boostedAttackPower, out bool wandererTriggered);
if (wandererTriggered)
{
    _playerWandererHeartTriggered[playerId] = true;
    AddLog($"漫游者之心触发！根据回合内最慢一步({_playerRoundSlowestActionTime[playerId].TotalSeconds:F2}秒)，攻击力调整为{finalAttackPower}");
}
```

#### C. 公共 API - IsEligibleForWhereAmIAchievement

```csharp
public bool IsEligibleForWhereAmIAchievement(string playerId)
{
    if (!_players.TryGetValue(playerId, out var player))
        return false;
    
    // 1. 检查是否装备了漫游者之心
    var hasWandererHeart = player.GetEquippedAccessories()
        .OfType<WandererHeartAccessory>()
        .Any();
    
    if (!hasWandererHeart)
        return false;
    
    // 2. 检查是否触发过漫游者之心的增益
    bool wandererTriggered = _playerWandererHeartTriggered.GetValueOrDefault(playerId, false);
    
    if (wandererTriggered)
        return false;
    
    return true;
}
```

#### D. Trigger 实现

```csharp
public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
{
    var eligiblePlayers = new List<string>();
    
    // 检查每个玩家是否满足成就条件
    foreach (var player in context.Battle.GetAllPlayers())
    {
        if (context.Battle.IsEligibleForWhereAmIAchievement(player.PlayerId))
        {
            eligiblePlayers.Add(player.PlayerId);
        }
    }
    
    return eligiblePlayers;
}
```

## 2. "刮痧" 成就实现

### 成就条件
一局游戏内连续10回合造成了并且只造成1点伤害

### 实现方案

#### A. ServerBattle 中的伤害序列追踪

在 `ServerBattle` 中添加：
```csharp
// 成就追踪 - 连续伤害统计（用于"刮痧"成就）
private Dictionary<string, List<int>> _playerDamageSequence; // 每个玩家造成伤害的序列
```

#### B. 伤害记录

在 `ApplyDamage` 方法中记录伤害序列：
```csharp
if (actualDamage > 0)
{
    _playerTookDamage[defender.PlayerId] = true;
    _playerDamageTaken[defender.PlayerId] += actualDamage;
    _playerDamageDealt[attacker.PlayerId] += actualDamage;
    
    // 记录伤害序列（用于"刮痧"成就检测）
    if (!_playerDamageSequence.ContainsKey(attacker.PlayerId))
    {
        _playerDamageSequence[attacker.PlayerId] = new List<int>();
    }
    _playerDamageSequence[attacker.PlayerId].Add(actualDamage);
}
```

额外伤害也要记录：
```csharp
if (extraActualDamage > 0)
{
    _playerDamageTaken[defender.PlayerId] += extraActualDamage;
    _playerDamageDealt[attacker.PlayerId] += extraActualDamage;
    
    // 继续记录伤害序列
    _playerDamageSequence[attacker.PlayerId].Add(extraActualDamage);
}
```

#### C. 公共 API - IsEligibleForGuashaMasterAchievement

```csharp
public bool IsEligibleForGuashaMasterAchievement(string playerId)
{
    if (!_playerDamageSequence.TryGetValue(playerId, out var damageSeq))
        return false;
    
    // 需要至少10次伤害
    if (damageSeq.Count < 10)
        return false;
    
    // 检查是否有连续10个伤害值都是1
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
        {
            AddLog($"玩家{playerId}找到连续10次伤害都是1点的序列（位置{i}-{i+9}）");
            return true;
        }
    }
    
    return false;
}
```

#### D. Trigger 实现

```csharp
public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
{
    var eligiblePlayers = new List<string>();
    
    // 检查每个玩家是否满足成就条件
    foreach (var player in context.Battle.GetAllPlayers())
    {
        if (context.Battle.IsEligibleForGuashaMasterAchievement(player.PlayerId))
        {
            eligiblePlayers.Add(player.PlayerId);
        }
    }
    
    return eligiblePlayers;
}
```

## 修改文件清单

### 1. ServerBattle.cs
- 添加 `_playerDamageSequence` 字典来追踪伤害序列
- 修改 `ApplyDamage` 方法记录每次伤害到序列中
- 添加 `IsEligibleForWhereAmIAchievement` 公共方法
- 添加 `IsEligibleForGuashaMasterAchievement` 公共方法

### 2. WhereAmITrigger.cs
- 实现 `GetEligiblePlayers` 方法，调用 `ServerBattle.IsEligibleForWhereAmIAchievement`
- 实现 `CalculateProgress` 方法，满足条件返回1

### 3. GuashaMasterTrigger.cs
- 实现 `GetEligiblePlayers` 方法，调用 `ServerBattle.IsEligibleForGuashaMasterAchievement`
- 实现 `CalculateProgress` 方法，满足条件返回1

## 代码架构

```
成就检测流程：
  Trigger.GetEligiblePlayers() 
    └─> ServerBattle.IsEligibleForXxxAchievement()
         └─> 检查具体的条件

这个设计保证了：
1. 触发逻辑的复杂性在 Trigger 中处理
2. 具体的数据获取和检查在 ServerBattle 中处理
3. 易于维护和测试
```

## 测试验证

1. ✓ 代码编译成功（0 错误）
2. ✓ 清理测试数据准备进行测试
3. 待进行的测试：
   - 战斗中不触发漫游者之心增益时，完成"我在哪？"成就
   - 战斗中有连续10回合造成1点伤害时，完成"刮痧"成就
   - 战斗中触发漫游者之心增益时，不完成"我在哪？"成就
   - 战斗中未有连续10回合造成1点伤害时，不完成"刮痧"成就

## 日志调试

添加了大量调试日志：
- `[WhereAmI Check]` - "我在哪？"成就检查过程
- `[GuashaMaster Check]` - "刮痧"成就检查过程

这些日志会在战斗结束时输出到控制台，便于排查问题。
