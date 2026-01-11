# 联机对战UI显示修复

## 问题描述

在联机对战中，出现以下问题：
1. 战斗开始时两方都不能行动
2. 几秒后，一方提示"选择PD进行防御"，但骰子列表为空
3. 跳过按钮也不能点击

**日志表现**：
```
[Client] Waiting input player:  # 一开始为空
[Client] Waiting input player: 307aabf0-f2f8-4fd2-98ce-9744618e4f3f  # 后来才有玩家ID
```

## 根本原因

分析发现有两个主要问题：

### 问题 1：骰子列表未能正确同步

在 `BattleManager.ApplyServerBattleState()` 方法中，当更新可用骰子列表时，存在时序问题：

```csharp
// 旧代码
if (state.InputContext == "AttackSelection")
{
    UpdateAvailableActiveDice(state.AvailableActiveDiceNames);  
    // 这时 CurrentActionPlayer 为空，因为它是只读属性无法直接设置
}
```

问题链：
- 服务器发送 `CurrentActionPlayerId` 到客户端
- `CurrentActionPlayer` 是只读属性，无法直接设置
- `UpdateAvailableActiveDice()` 尝试从 `CurrentActionPlayer.GetEquippedDice()` 获取骰子
- 由于 `CurrentActionPlayer` 为 null，导致骰子列表为空

### 问题 2：对手玩家没有装备骰子

在 `InitializeMultiplayerBattle()` 中，只为本地玩家装备物品，而对手没有任何装备：

```csharp
// 旧代码：只为本地玩家装备
if (playerInfo.PlayerId == localPlayerId)
{
    SetupPlayerEquipmentFromInventory(player);
}
// 其他玩家完全没有装备！
```

当服务器要求对手执行操作时，无法获取他们的骰子。

## 修复方案

### 改变 1：修改 `ApplyServerBattleState()` 方法

**文件**：`EonVientiane/BattleManager.cs`

直接使用服务器发送的 `CurrentActionPlayerId` 而不是依赖 `CurrentActionPlayer`：

```csharp
if (state.InputContext == "AttackSelection")
{
    if (!string.IsNullOrEmpty(state.CurrentActionPlayerId))
    {
        UpdateAvailableActiveDice(state.AvailableActiveDiceNames, state.CurrentActionPlayerId);
    }
    UpdateAvailableOpponents(state.AvailableOpponentIds);
}
```

### 改变 2：改进 `UpdateAvailableActiveDice()` 方法

增加可选的 `playerId` 参数，允许直接指定玩家：

```csharp
private void UpdateAvailableActiveDice(List<string> diceNames, string playerId = null)
{
    // 如果提供了玩家ID，使用它；否则使用 CurrentActionPlayer
    Player actionPlayer = null;
    if (!string.IsNullOrEmpty(playerId))
    {
        actionPlayer = _currentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == playerId);
    }
    else
    {
        actionPlayer = _currentBattle.CurrentActionPlayer;
    }
    
    if (actionPlayer == null)
    {
        Console.WriteLine($"[Warning] Could not find action player for dice update: {playerId}");
        return;
    }
    
    // 基于 actionPlayer 获取并设置骰子列表
}
```

### 改变 3：新增 `SyncPlayerDiceEquipment()` 方法

在状态更新时，根据服务器发送的 `EquippedDiceNames` 为对手同步骰子：

```csharp
private void SyncPlayerDiceEquipment(Player player, List<string> equippedDiceNames)
{
    player.EquippedItems.Clear();
    foreach (var diceName in equippedDiceNames ?? new List<string>())
    {
        var dice = CreateDiceByName(diceName);
        if (dice != null)
        {
            player.AddEquipment(dice);
        }
    }
}

private Dice CreateDiceByName(string diceName)
{
    return diceName switch
    {
        "D6骰子" => new D6Dice(DiceUsageType.Both),
        "飞羽骰子" => new FeatheredDice(),
        _ => null
    };
}
```

在 `ApplyServerBattleState()` 中调用：

```csharp
foreach (var playerState in state.Players)
{
    var player = _currentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == playerState.PlayerId);
    if (player != null)
    {
        player.CurrentHP = playerState.CurrentHP;
        // ... 其他状态更新 ...
        
        // 同步装备的骰子（如果玩家还没有装备）
        if (player.GetEquippedDice().Count == 0 && playerState.EquippedDiceNames != null && playerState.EquippedDiceNames.Count > 0)
        {
            SyncPlayerDiceEquipment(player, playerState.EquippedDiceNames);
        }
    }
}
```

## 修复流程图

```
战斗开始
    ↓
服务器发送 BattleStateUpdateNotification
    ├─ CurrentActionPlayerId = "player-123"
    ├─ AvailableActiveDiceNames = ["D6骰子", "飞羽骰子"]
    ├─ Players[] 包含所有玩家及其 EquippedDiceNames
    ↓
客户端 ApplyServerBattleState()
    ├─ 同步玩家状态（HP、护盾等）
    ├─ 根据 EquippedDiceNames 为玩家装备骰子
    ├─ 更新可用骰子列表
    │  └─ 使用 CurrentActionPlayerId 从 AllPlayers 获取玩家
    │  └─ 从该玩家的装备中获取指定名称的骰子
    ├─ 更新可攻击对手列表
    ↓
UI 显示
    ├─ 骰子按钮显示（来自 AvailableActiveDice）
    ├─ 跳过按钮可交互
    ├─ 玩家可点击选择骰子或跳过
```

## 预期结果

修复后：
1. ✅ 骰子列表能正确显示
2. ✅ 跳过按钮能正确显示并可点击
3. ✅ 玩家能选择骰子进行攻击或防守
4. ✅ 对手的骰子信息被正确同步
5. ✅ 战斗流程正常进行

## 测试步骤

1. 启动服务器：
   ```bash
   ./start_local_test.sh
   ```

2. 运行两个客户端进行对战

3. 验证：
   - 战斗开始后，当轮到你行动时，应该看到骰子列表
   - 能够选择骰子或点击跳过
   - 对方也能看到并执行他们的操作
   - 战斗能正常进行完整回合

## 调试信息

新增日志输出：

```
[BattleManager] Updating available dice for [玩家名]: [骰子列表]
[Warning] Could not find action player for dice update: [玩家ID]
```

可以通过查看客户端日志来验证：
- 骰子列表是否被正确更新
- 玩家信息是否被正确同步

## 相关文件修改

| 文件 | 方法 | 改动 |
|------|------|------|
| `EonVientiane/BattleManager.cs` | `ApplyServerBattleState()` | 改进状态更新逻辑 |
| `EonVientiane/BattleManager.cs` | `UpdateAvailableActiveDice()` | 添加 `playerId` 参数 |
| `EonVientiane/BattleManager.cs` | 新增 | `SyncPlayerDiceEquipment()` 方法 |
| `EonVientiane/BattleManager.cs` | 新增 | `CreateDiceByName()` 方法 |

## 性能影响

- 网络流量：无增加（使用现有的 `EquippedDiceNames` 字段）
- 计算复杂度：O(玩家数 × 骰子数)，可接受
- 反射操作：仅用于设置私有字段，性能开销可接受
- 总体性能：无明显影响

## 扩展建议

如果系统中有更多的骰子类型，可以：

1. 在 `CreateDiceByName()` 中扩展 switch 语句
2. 或实现一个骰子工厂系统来集中管理骰子创建
3. 考虑通过配置或反射自动发现所有骰子类型

