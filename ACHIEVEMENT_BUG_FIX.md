# 成就系统误触发问题修复

## 问题描述

在战斗结束后，发现以下问题：
- 客户端日志显示完成了3个成就：秒了、绝对碾压、第一次防御 ✓
- 服务端同步返回显示5个成就已完成，额外包含：刮痧、我在哪？ ✗
- **这两个额外的成就的触发条件并未满足，但仍被错误地完成**

## 根本原因

检查源码发现，以下两个成就的 Trigger 实现存在严重缺陷：

### 1. WhereAmITrigger（我在哪？）

**成就条件**: 携带饰品'漫游者之心'而一整局都没有触发过增益

**错误实现**:
```csharp
public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
{
    var eligiblePlayers = new List<string>();
    
    // ❌ 返回所有参与战斗的玩家，未检查任何条件
    foreach (var player in context.Battle.GetAllPlayers())
    {
        eligiblePlayers.Add(player.PlayerId);
    }
    
    return eligiblePlayers;
}

public int CalculateProgress(AchievementTriggerContext context, string playerId)
{
    // ❌ 直接返回 1，未检查条件
    return 1;
}
```

### 2. GuashaMasterTrigger（刮痧）

**成就条件**: 一局游戏内连续10回合造成了并且只造成1点伤害

**错误实现**:
```csharp
public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
{
    var eligiblePlayers = new List<string>();
    
    // ❌ 返回所有参与战斗的玩家，未检查任何条件
    foreach (var player in context.Battle.GetAllPlayers())
    {
        eligiblePlayers.Add(player.PlayerId);
    }
    
    return eligiblePlayers;
}

public int CalculateProgress(AchievementTriggerContext context, string playerId)
{
    // ❌ 直接返回 1，未检查条件
    return 1;
}
```

**问题**: 这两个 Trigger 的实现只是占位代码，导致每场战斗结束后所有玩家都会获得这两个成就。

## 修复方案

### 临时修复（已实施）

将这两个成就的触发器暂时禁用，返回空列表和进度0，避免误触发：

#### WhereAmITrigger.cs
```csharp
public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
{
    if (context.Battle == null)
    {
        Console.WriteLine("[WhereAmITrigger] Battle is null!");
        return Enumerable.Empty<string>();
    }

    var eligiblePlayers = new List<string>();

    // TODO: 实现检查逻辑：
    // 1. 检查玩家是否携带了饰品'漫游者之心'
    // 2. 检查整局战斗中该饰品是否从未触发过增益
    // 当前暂时返回空列表，避免误触发
    Console.WriteLine($"[WhereAmITrigger] Achievement requires 'wanderer_heart' item check - not yet implemented");
    
    return eligiblePlayers;
}

public int CalculateProgress(AchievementTriggerContext context, string playerId)
{
    // TODO: 当检测逻辑实现后，满足条件返回1
    Console.WriteLine($"[WhereAmITrigger] CalculateProgress called for player {playerId}");
    return 0;
}
```

#### GuashaMasterTrigger.cs
```csharp
public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
{
    if (context.Battle == null)
    {
        Console.WriteLine("[GuashaMasterTrigger] Battle is null!");
        return Enumerable.Empty<string>();
    }

    var eligiblePlayers = new List<string>();

    // TODO: 实现检查逻辑：
    // 1. 遍历每个玩家的战斗记录
    // 2. 检查是否有连续10回合每回合都恰好只造成1点伤害
    // 当前暂时返回空列表，避免误触发
    Console.WriteLine($"[GuashaMasterTrigger] Achievement requires consecutive 10 rounds damage check - not yet implemented");
    
    return eligiblePlayers;
}

public int CalculateProgress(AchievementTriggerContext context, string playerId)
{
    // TODO: 当检测逻辑实现后，满足条件返回1
    Console.WriteLine($"[GuashaMasterTrigger] CalculateProgress called for player {playerId}");
    return 0;
}
```

### 长期修复（待实施）

#### 1. "我在哪？" 成就

需要实现：
- 检查玩家装备的饰品列表
- 验证是否携带"漫游者之心"（wanderer_heart）
- 追踪战斗期间该饰品的触发记录
- 如果全程未触发增益效果，则完成成就

#### 2. "刮痧" 成就

需要实现：
- 在战斗系统中追踪每个玩家每回合造成的伤害
- 检测连续10回合的伤害值是否都恰好为1点
- 满足条件时触发成就

参考实现可以参照 `MiracleTrigger`，它正确地调用了 `ServerBattle.GetPlayersEligibleForMiracleAchievement()`。

## 影响范围

- ✓ 已修复：WhereAmITrigger、GuashaMasterTrigger
- ✓ 已验证正常：其他成就触发器（FirstDefense, PerfectVictory, BlitzVictory, LongThinking, Miracle, AbsoluteLuck）

## 测试验证

已清理所有测试数据：
```bash
./clear_test_data.sh
```

重新构建服务器：
```bash
dotnet build EonVientianeServer/EonVientianeServer.csproj -c Debug
```

**验证结果**: 构建成功，0 错误

## 后续工作

1. 实现"我在哪？"成就的完整检测逻辑
2. 实现"刮痧"成就的完整检测逻辑
3. 在 `ServerBattle` 中添加相应的数据追踪功能
4. 完成实现后，进行全面测试验证

## 修改文件列表

- [EonVientianeServer/Achievements/WhereAmI/WhereAmITrigger.cs](EonVientianeServer/Achievements/WhereAmI/WhereAmITrigger.cs)
- [EonVientianeServer/Achievements/GuashaMaster/GuashaMasterTrigger.cs](EonVientianeServer/Achievements/GuashaMaster/GuashaMasterTrigger.cs)
