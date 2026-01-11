# 多人对战修复 - 详细修改清单

## 修改概览

这次修复针对多人对战中防守骰子（PD）不显示、跳过无效等问题进行了三个关键修改。

---

## 修改1：本地玩家装备初始化

### 文件
📄 `EonVientiane/BattleManager.cs`

### 修改位置
- 方法：`InitializeMultiplayerBattle()` (第81-108行)
- 修改内容：在创建玩家对象后，为本地玩家装备背包物品

### 代码修改
```csharp
// 为本地玩家装备当前背包中的物品
if (playerInfo.PlayerId == localPlayerId)
{
    SetupPlayerEquipmentFromInventory(player);
}
```

### 修改前后对比
**修改前**：
```csharp
foreach (var playerInfo in playerInfoList)
{
    PlayerCamp camp = playerInfo.TeamId == 1 ? PlayerCamp.Team1 : PlayerCamp.Team2;
    var player = new Player(playerInfo.PlayerId, playerInfo.PlayerName, camp);
    _currentBattle.AddPlayer(player);  // ❌ 没有装备任何物品
}
```

**修改后**：
```csharp
foreach (var playerInfo in playerInfoList)
{
    PlayerCamp camp = playerInfo.TeamId == 1 ? PlayerCamp.Team1 : PlayerCamp.Team2;
    var player = new Player(playerInfo.PlayerId, playerInfo.PlayerName, camp);
    
    if (playerInfo.PlayerId == localPlayerId)
    {
        SetupPlayerEquipmentFromInventory(player);  // ✅ 装备本地玩家
    }
    
    _currentBattle.AddPlayer(player);
}
```

### 为什么需要这个修改
- 在多人战斗中，客户端需要保存本地玩家的装备以显示可用的骰子
- 防守时，`UpdateAvailablePassiveDice()`需要从本地玩家的装备中查找PD骰子
- 如果玩家没有装备，就无法找到任何可用的骰子

---

## 修改2：日志索引跟踪

### 文件
📄 `EonVientianeServer/ServerBattle.cs`

### 修改1：添加私有字段（第77行）
```csharp
private int _lastSentLogIndex = 0;
```

### 修改2：添加公开方法（第682-690行）
```csharp
/// <summary>
/// 获取新的战斗日志（自上次调用以来）
/// </summary>
public List<string> GetNewBattleLogs()
{
    if (_lastSentLogIndex >= BattleLog.Count)
        return new List<string>();
    
    var newLogs = BattleLog.Skip(_lastSentLogIndex).ToList();
    _lastSentLogIndex = BattleLog.Count;
    return newLogs;
}
```

### 工作原理
1. `_lastSentLogIndex`：记录上次广播时BattleLog的长度
2. `GetNewBattleLogs()`：
   - 检查是否有新日志（当前长度 > 上次发送位置）
   - 返回从上次位置到现在的所有新日志
   - 更新索引为当前长度

### 为什么需要这个修改
- 战斗有多个状态更新，每次都发送所有日志会造成重复
- 只发送新日志能减少网络传输
- 客户端能正确累积接收所有战斗日志

---

## 修改3：广播新日志

### 文件
📄 `EonVientianeServer/GameServer.cs`

### 修改位置
- 方法：`BroadcastBattleStateAsync()` (第1105-1108行)
- 位置：在设置完所有可用选项之后，创建消息之前

### 代码修改
```csharp
// 获取新的战斗日志
var newLogs = battle.GetNewBattleLogs();
notification.NewBattleLogs = newLogs;

var message = NetworkMessage.Create(MessageType.BattleStateUpdate, notification);
```

### 修改前后对比
**修改前**：
```csharp
if (battle.CurrentDefenderPlayerId != null)
{
    var availablePD = battle.GetAvailablePassiveDice(battle.CurrentDefenderPlayerId);
    notification.AvailablePassiveDiceNames = availablePD.Select(d => d.Name).ToList();
}

var message = NetworkMessage.Create(MessageType.BattleStateUpdate, notification);
// ❌ notification.NewBattleLogs 保持为空
```

**修改后**：
```csharp
if (battle.CurrentDefenderPlayerId != null)
{
    var availablePD = battle.GetAvailablePassiveDice(battle.CurrentDefenderPlayerId);
    notification.AvailablePassiveDiceNames = availablePD.Select(d => d.Name).ToList();
}

// 获取新的战斗日志
var newLogs = battle.GetNewBattleLogs();
notification.NewBattleLogs = newLogs;  // ✅ 填充新日志

var message = NetworkMessage.Create(MessageType.BattleStateUpdate, notification);
```

### 为什么需要这个修改
- 客户端需要接收战斗日志以显示战斗进程
- `BattleStateUpdateNotification.NewBattleLogs`必须被填充
- 没有日志的话，客户端的BattleLog.Add()将收不到任何数据

---

## 数据流追踪

### 场景：防守骰子显示流程

```
客户端                          服务器
  |                               |
  |── 进入多人战斗 ─────────────>|
  |   (调用InitializeMultiplayerBattle)
  |   ✅ 本地玩家装备背包物品      |
  |                               |
  |<─── 战斗状态更新 ────────────|
  |     (BattleStateUpdateNotification)
  |     包含：                     |
  |     - AvailablePassiveDiceNames   (来自GetAvailablePassiveDice)
  |     - NewBattleLogs              (来自GetNewBattleLogs)
  |                               |
  |── UpdateAvailablePassiveDice ─┘
  |   ✅ 找到本地玩家的PD骰子
  |   (SetValue _currentPassiveDiceChoices)
  |
  |── 绘制防守UI
  |   ✅ 显示可用的PD骰子
  |
  |── 玩家选择PD或点击跳过
  |   |
  |── BattleDefenseRequested ────>|
  |                               | ProcessPlayerDefenseChoice
  |<─── 状态更新 ────────────────|
```

---

## 验证清单

修改前需要验证的点：

- [ ] 编译成功
  ```bash
  dotnet build EonVientiane/BattleManager.cs
  dotnet build EonVientianeServer/ServerBattle.cs
  dotnet build EonVientianeServer/GameServer.cs
  ```

修改后需要测试的点：

- [ ] 多人战斗初始化时，本地玩家拥有已装备的物品
- [ ] 防守回合时，能看到PD骰子名称（如"d6"）
- [ ] 可以点击PD骰子进行防守
- [ ] 可以点击"跳过"跳过防守
- [ ] 战斗日志实时更新
- [ ] 没有重复的日志记录

---

## 相关代码引用

### 调用链
1. `Game1.OnGameStarted()` → 调用 `BattleManager.InitializeMultiplayerBattle()`
2. `Game1.OnBattleStateUpdated()` → 调用 `BattleManager.ApplyServerBattleState()`
3. `BattleManager.UpdateAvailablePassiveDice()` → 使用反射设置 `Battle._currentPassiveDiceChoices`
4. `BattleManager.DrawBattleActions()` → 绘制 `_currentBattle.AvailablePassiveDice`

### 关键数据结构
```csharp
// 服务器端
public class ServerBattle {
    private int _lastSentLogIndex = 0;  // 修改2添加
    public List<string> GetNewBattleLogs() { ... }  // 修改2添加
}

// 网络协议
public class BattleStateUpdateNotification {
    public List<string> AvailablePassiveDiceNames { get; set; } = new();
    public List<string> NewBattleLogs { get; set; } = new();
}
```

---

## 已知局限

- 修复仅适用于多人战斗模式
- 单人战斗（vs电脑）使用不同的逻辑，不受影响
- 目前多人战斗中所有玩家都是真实玩家（无AI）

---

## 回滚说明

如果需要回滚修改：

1. **修改1**：删除 `if (playerInfo.PlayerId == localPlayerId) { SetupPlayerEquipmentFromInventory(player); }`
2. **修改2**：删除 `_lastSentLogIndex` 字段和 `GetNewBattleLogs()` 方法
3. **修改3**：删除 `var newLogs = ...` 和 `notification.NewBattleLogs = newLogs;` 两行

---

## 性能考虑

- **修改1**：只影响初始化时的一次操作，性能无影响
- **修改2&3**：日志检查和发送为O(n)，其中n为新日志数量（通常很小）
  - 广播频率：1秒一次（由服务器定时）
  - 平均新日志数：3-5条/秒
  - 网络影响：可接受

---

## 相关文档

- 测试计划：[MULTIPLAYER_TEST_PLAN.md](MULTIPLAYER_TEST_PLAN.md)
- 完整总结：[MULTIPLAYER_FIX_SUMMARY.md](MULTIPLAYER_FIX_SUMMARY.md)
