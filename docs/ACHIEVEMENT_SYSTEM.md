# 成就系统实现文档

## 系统概述

EonVientiane 成就系统是一个完整的游戏成就管理系统，包括客户端和服务端实现。玩家完成特定条件可以获得成就，同时获得物品、金币等奖励。

### 核心特性

- ✅ **5个预定义成就**：首次胜利、战斗好手、装备收集家、无敌战士、时间旅者
- ✅ **进度跟踪**：实时跟踪每个成就的进度
- ✅ **奖励系统**：完成成就获得物品、金币等奖励
- ✅ **与账号绑定**：所有成就数据与服务端账号绑定
- ✅ **同步机制**：客户端和服务端自动同步成就数据
- ✅ **UI界面**：专门的成就界面，显示在按钮4

## 系统架构

### 客户端组件

```
Game1.cs (主游戏类)
├── AchievementSystem (成就管理系统)
│   ├── Achievement (成就数据类)
│   ├── Reward (奖励数据类)
│   └── AchievementType (成就类型枚举)
└── UIManager
    └── DrawAchievementPanel (成就界面绘制)
```

### 服务端组件

```
GameServer.cs (游戏服务器)
├── AchievementManager (服务端成就管理)
│   ├── UserAchievements (用户成就数据)
│   └── AchievementProgress (进度跟踪)
└── 网络消息处理
    ├── GetAchievements
    ├── UpdateAchievement
    └── AchievementCompleted
```

### 网络传输

```
NetworkProtocol.cs
├── GetAchievementsRequest/Response
├── UpdateAchievementRequest/Response
├── AchievementCompletedNotification
├── AchievementDto (数据传输对象)
└── RewardDto (奖励数据对象)
```

## 预定义成就列表

| 成就ID | 成就名称 | 描述 | 目标值 | 奖励 |
|--------|--------|------|--------|------|
| first_victory | 初露锋芒 | 赢得第一场战斗 | 1 | 初心者之剑 + 100金币 |
| battle_master | 战斗好手 | 赢得10场战斗 | 10 | 战神之盾 + 500金币 |
| item_collector | 装备收集家 | 收集20件装备 | 20 | 收集家之冠 + 300金币 |
| no_death_warrior | 无敌战士 | 完成5场无死亡战斗 | 5 | 无敌甲胄 + 200金币 |
| time_traveler | 时间旅者 | 游戏时间累计10小时 | 10 | 时间护符 + 400金币 |

## 使用指南

### 在游戏中添加成就

```csharp
// 初始化（已在 Game1.cs 中完成）
_achievementSystem = new AchievementSystem(_inventoryManager);
_achievementSystem.SetUserId(_currentUser.UserId);

// 订阅事件
_achievementSystem.AchievementCompleted += OnAchievementCompleted;
_achievementSystem.RewardGiven += OnRewardGiven;
```

### 更新成就进度

```csharp
// 当玩家完成某个操作时，更新对应成就
// 例如：玩家赢得战斗
_achievementSystem.UpdateProgress("first_victory", 1);
_achievementSystem.UpdateProgress("battle_master", 1);

// 进度会自动检查是否达到目标，如果达到则自动完成
```

### 获取成就信息

```csharp
// 获取所有成就
var allAchievements = _achievementSystem.GetAllAchievements();

// 获取已完成的成就
var completedAchievements = _achievementSystem.GetCompletedAchievements();

// 获取特定成就
var achievement = _achievementSystem.GetAchievement("first_victory");

// 获取完成统计
var (completed, total, percentage) = _achievementSystem.GetCompletionStats();
```

### 与服务端同步

```csharp
// 导出成就数据用于保存到服务端
var achievementData = _achievementSystem.ExportToServer();

// 从服务端加载成就
var serverData = GetFromServer(); // 从服务端获取数据
_achievementSystem.LoadFromServer(serverData);
```

## 成就界面

### 位置
- 菜单栏：按钮4（第4个中间按钮）
- 快捷键：点击菜单栏的"按钮4"

### UI 显示内容

1. **总进度条**
   - 显示完成的成就数量和百分比
   - 实时更新

2. **成就列表**
   - 所有成就的详细信息
   - 进度条（当前进度/目标值）
   - 已完成标记
   - 可以滚动查看更多成就

3. **成就条目信息**
   - 成就名称
   - 成就描述
   - 当前进度和目标值
   - 进度条可视化
   - 完成状态

## 事件和回调

### AchievementCompleted 事件

```csharp
_achievementSystem.AchievementCompleted += (achievement) =>
{
    // 成就完成时触发
    Console.WriteLine($"成就完成: {achievement.Name}");
    // 在这里可以播放动画、音效等
};
```

### RewardGiven 事件

```csharp
_achievementSystem.RewardGiven += (reward) =>
{
    // 奖励发放时触发
    Console.WriteLine($"获得奖励: {reward.Type} x{reward.Quantity}");
    // 更新UI显示
};
```

## 网络通信流程

### 获取成就列表

```
客户端                          服务端
   |----GetAchievements---->|
   |                    查询用户成就
   |<---GetAchievementsResponse---|
   |     返回成就列表
```

### 更新成就进度

```
客户端                          服务端
   |----UpdateAchievement---->|
   |                    更新进度
   |<---UpdateAchievementResponse---|
   |     返回更新结果
   |
   | 如果成就完成:
   |<---AchievementCompleted---|
   |     返回完成通知和奖励
```

## 数据持久化

### 服务端存储

成就数据在服务端的 `AchievementManager` 中被管理：

```csharp
private readonly Dictionary<string, UserAchievements> _userAchievements = new();

private class AchievementProgress
{
    public string Id { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedTime { get; set; }
}
```

### 数据同步时序

1. 用户登录 → 加载成就数据
2. 游戏运行中 → 定期同步进度
3. 成就完成 → 立即通知客户端
4. 用户登出 → 保存最终数据

## 代码实现细节

### 客户端主要类

#### AchievementSystem

```csharp
public class AchievementSystem
{
    // 初始化并加载默认成就
    public AchievementSystem(InventoryManager inventoryManager)
    
    // 设置用户ID
    public void SetUserId(string userId)
    
    // 更新成就进度
    public void UpdateProgress(string achievementId, int progressDelta)
    
    // 获取成就信息
    public Achievement? GetAchievement(string achievementId)
    public List<Achievement> GetAllAchievements()
    public List<Achievement> GetCompletedAchievements()
    
    // 同步
    public void LoadFromServer(List<AchievementData> serverData)
    public List<AchievementData> ExportToServer()
}
```

### 服务端主要类

#### AchievementManager

```csharp
public class AchievementManager
{
    // 获取用户成就列表
    public List<AchievementDto> GetUserAchievements(string userId)
    
    // 更新成就进度
    public (bool success, bool isCompleted, int currentProgress, string? error) 
        UpdateAchievementProgress(string userId, string achievementId, int progressDelta)
    
    // 获取奖励
    public List<RewardDto> GetCompletionRewards(string achievementId)
    
    // 获取完成统计
    public (int completed, int total, float percentage) GetCompletionStats(string userId)
}
```

## 扩展指南

### 添加新成就

1. 在 `AchievementSystem.InitializeDefaultAchievements()` 中添加：

```csharp
_achievements["new_achievement"] = new Achievement
{
    Id = "new_achievement",
    Name = "新成就",
    Description = "完成新任务",
    Icon = "achievement_new",
    RequiredProgress = 1,
    Rewards = new List<Reward>
    {
        new Reward { Type = "Item", ItemId = "item_reward_new", Quantity = 1 }
    }
};
```

2. 在 `AchievementManager.GetAchievementName()` 中添加对应的名称

3. 在 `AchievementManager.GetAchievementRequirement()` 中添加要求值

4. 在游戏中适当位置调用 `UpdateProgress()`

### 自定义奖励

1. 在 `CreateRewardItem()` 中添加新物品创建逻辑
2. 在 `GetCompletionRewards()` 中定义奖励内容

### 连接其他系统

```csharp
// 战斗系统
if (battleResult.isVictory)
{
    _achievementSystem.UpdateProgress("first_victory", 1);
    _achievementSystem.UpdateProgress("battle_master", 1);
}

// 库存系统
_inventoryManager.OnItemAdded += (item) =>
{
    _achievementSystem.UpdateProgress("item_collector", 1);
};

// 时间系统
_gameTimer.Elapsed1Hour += () =>
{
    _achievementSystem.UpdateProgress("time_traveler", 1);
};
```

## 测试建议

参考 [AchievementSystemExample.cs](./AchievementSystemExample.cs) 中的示例：

1. `BasicUsageExample()` - 基本初始化和使用
2. `UpdateProgressExample()` - 更新进度
3. `CheckAchievementStatusExample()` - 检查成就状态
4. `GetCompletedAchievementsExample()` - 获取已完成成就
5. `SyncWithServerExample()` - 与服务端同步
6. `GameIntegrationExample()` - 游戏集成示例

## 常见问题

### Q: 成就数据如何持久化？
A: 成就数据与用户账号绑定，存储在服务端。每次登录时加载，游戏中自动同步。

### Q: 如何重置用户的成就？
A: 在服务端 `AchievementManager` 中调用初始化方法，或手动删除用户数据。

### Q: 可以有多个相同的成就吗？
A: 目前设计是单线程，每个成就ID对应一个。如果需要多个相同类型的成就，建议使用不同的ID。

### Q: 如何处理成就完成但奖励未发放的情况？
A: 在 `OnAchievementCompleted()` 中再次检查库存，或在服务端重新发放奖励。

## 文件清单

| 文件 | 描述 |
|-----|------|
| AchievementSystem.cs | 客户端成就系统 |
| AchievementSystemExample.cs | 使用示例 |
| AchievementManager.cs | 服务端成就管理 |
| NetworkProtocol.cs | 网络通信定义（已更新） |
| Game1.cs | 主游戏类（已集成） |
| UIManager.cs | UI管理器（已集成） |
| MultiplayerLobbyManager.cs | 联机管理器（已集成） |
| Network/LobbyManager.cs | 网络大厅管理器（已集成） |
| GameServer.cs | 游戏服务器（已集成） |

## 总结

这个成就系统提供了：
- 完整的客户端-服务端架构
- 自动的进度跟踪和完成检测
- 灵活的奖励系统
- 实时的UI反馈
- 与游戏账号深度绑定

玩家可以在按钮4界面随时查看自己的成就进度，完成成就自动获得奖励，所有数据都与服务端账号同步。
