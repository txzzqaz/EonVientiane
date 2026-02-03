# 成就系统通信 - 快速参考

## 核心类和方法

### 客户端类

#### AchievementSystem
```csharp
// 同步服务器数据
SyncWithServer(List<AchievementDto> serverData)

// 验证一致性
bool ValidateSyncState()

// 获取修改的成就
List<string> GetModifiedAchievements()

// 查询成就
Achievement? GetAchievement(string achievementId)
List<Achievement> GetAllAchievements()
List<Achievement> GetCompletedAchievements()

// 统计
(int completed, int total, float percentage) GetCompletionStats()

// 本地更新
void UpdateProgress(string achievementId, int progressDelta)
```

#### LobbyManager (Network)
```csharp
// 请求
Task GetAchievementsAsync()
Task UpdateAchievementAsync(string achievementId, int progressDelta)

// 事件
event Action<List<AchievementDto>> AchievementsReceived;
event Action<AchievementCompletedNotification> AchievementCompleted;
event Action<string> ErrorOccurred;
```

### 服务器类

#### AchievementManager
```csharp
// 查询
List<AchievementDto> GetUserAchievements(string userId)
List<RewardDto> GetCompletionRewards(string achievementId)
(int completed, int total, float percentage) GetCompletionStats(string userId)

// 更新
(bool success, bool isCompleted, int currentProgress, string? error) 
    UpdateAchievementProgress(string userId, string achievementId, int progressDelta)
```

#### GameServer
```csharp
// 消息处理
Task HandleGetAchievementsAsync(ConnectedClient client)
Task HandleUpdateAchievementAsync(ConnectedClient client, NetworkMessage message)
```

## 消息流

### 获取成就列表
```
Client                          Server
  |-- GetAchievements(userId) -->|
  |                          (验证、查询)
  |<-- GetAchievementsResponse --|
```

### 更新成就进度
```
Client                          Server
  |-- UpdateAchievement(id,△) -->|
  |                    (验证、更新、检查)
  |<-- UpdateAchievementResponse--|
  |<-- AchievementCompleted*     |  (* 仅当完成时)
```

## 数据模型

### AchievementDto
```csharp
Id              // 唯一标识
Name            // 显示名称
Description     // 描述文本
Icon            // 图标ID
Progress        // 当前进度
RequiredProgress// 完成所需
IsCompleted     // 是否完成
CompletedTime   // 完成时间
Rewards         // List<RewardDto>
```

### RewardDto
```csharp
Type            // Item/Gold/Experience
ItemId          // 物品ID(如果是Item)
Quantity        // 数量
```

## 新增成就速记

| ID | 名称 | 条件 | 奖励 |
|---|---|---|---|
| `absolute_luck` | 绝对幸运 | 连续6场胜利且每场战斗中掷出的点数保持一致（仅出现单一点数） | 饰品：戮力同心 |

## 常用操作

### 获取成就并同步
```csharp
_lobbyManager.AchievementsReceived += achievements =>
{
    _achievementSystem.SyncWithServer(achievements);
};
await _lobbyManager.GetAchievementsAsync();
```

### 更新成就并处理完成
```csharp
_lobbyManager.AchievementCompleted += notification =>
{
    // 显示完成信息
    // 更新UI
    // 发放奖励
};
await _lobbyManager.UpdateAchievementAsync("achievement_id", 1);
```

### 获取成就状态
```csharp
var achievement = _achievementSystem.GetAchievement("first_defense");
if (achievement != null)
{
    float progress = (float)achievement.Progress / achievement.RequiredProgress;
    Console.WriteLine($"Progress: {progress * 100}%");
}
```

### 验证同步状态
```csharp
if (!_achievementSystem.ValidateSyncState())
{
    Console.WriteLine("State mismatch detected, resyncing...");
    await _lobbyManager.GetAchievementsAsync();
}
```

## 事件处理

### 客户端事件

| 事件 | 参数 | 含义 |
|-----|------|------|
| AchievementsReceived | List<AchievementDto> | 成就列表已接收 |
| AchievementCompleted | AchievementCompletedNotification | 成就已完成 |
| ErrorOccurred | string | 错误发生 |
| SyncStarted | string | 同步开始 |
| SyncCompleted | string | 同步完成 |
| SyncFailed | string | 同步失败 |

### Game1.cs 事件处理

```csharp
_lobbyManager.AchievementsReceived += achievements =>
    _achievementSystem.SyncWithServer(achievements);

_lobbyManager.AchievementCompleted += OnServerAchievementCompleted;

_achievementSystem.AchievementCompleted += OnAchievementCompleted;
_achievementSystem.RewardGiven += OnRewardGiven;
```

## 错误代码

| 错误消息 | 原因 | 解决方案 |
|--------|------|--------|
| not authenticated | 用户未登录 | 重新登录 |
| 成就'X'不存在 | 成就ID错误 | 检查成就ID |
| 成就已完成 | 重复完成 | 忽略更新 |
| 网络超时 | 连接问题 | 重试 |
| 处理失败 | 服务器错误 | 查看服务器日志 |

## 日志关键词

### 服务器日志
```
[Server] HandleGetAchievements called for user
[Server] Retrieved X achievements for user
[Server] User progressed achievement from X to Y
[Server] User completed achievement
[Server] Successfully sent X achievements
```

### 客户端日志
```
[LobbyManager] Requesting achievements for user
[Client] Syncing X achievements from server
[Client] Loaded achievement: Name (Progress: X/Y)
[Client] Achievement sync completed
[LobbyManager] Achievement updated successfully
[Client] Achievement completed: Name
```

## 调试技巧

### 检查成就状态
```csharp
// 当前成就
foreach (var achievement in _achievementSystem.GetAllAchievements())
{
    Console.WriteLine($"{achievement.Name}: {achievement.Progress}/{achievement.RequiredProgress}");
}

// 完成度
var (completed, total, percentage) = _achievementSystem.GetCompletionStats();
Console.WriteLine($"{completed}/{total} ({percentage}%)");
```

### 检查网络状态
```csharp
_lobbyManager.ErrorOccurred += error =>
{
    Console.WriteLine($"Network error: {error}");
};
```

### 检查同步状态
```csharp
bool isValid = _achievementSystem.ValidateSyncState();
var modified = _achievementSystem.GetModifiedAchievements();
```

## 最佳实践

1. **总是先获取成就列表再进行游戏**
   ```csharp
   await _lobbyManager.GetAchievementsAsync();
   ```

2. **使用事件而不是轮询**
   ```csharp
   // ✓ 推荐
   _lobbyManager.AchievementCompleted += OnCompleted;
   
   // ✗ 不推荐
   while (true) CheckAchievements();
   ```

3. **处理所有可能的错误**
   ```csharp
   _lobbyManager.ErrorOccurred += error =>
   {
       Console.WriteLine($"Error: {error}");
       // 显示给用户
   };
   ```

4. **定期验证状态**
   ```csharp
   if (!_achievementSystem.ValidateSyncState())
   {
       await _lobbyManager.GetAchievementsAsync();
   }
   ```

5. **记录关键操作**
   ```csharp
   Console.WriteLine($"[Game] Achievement progress: {id} +{delta}");
   ```

## 故障排除

### 成就列表为空
- 检查: 用户是否已登录
- 检查: 服务器是否运行
- 检查: 用户是否有默认成就

### 成就无法更新
- 检查: 成就ID是否正确
- 检查: 成就是否已完成
- 检查: 网络连接是否正常
- 查看: 服务器错误日志

### 状态不一致
- 运行: `ValidateSyncState()`
- 执行: 完整重新同步
- 查看: 日志中的具体不匹配项

### 内存占用过高
- 检查: 缓存大小(应该很小)
- 检查: 事件是否正确卸载
- 监控: 成就数量

## 完整示例

```csharp
// 初始化
_achievementSystem = new AchievementSystem(_inventoryManager);
_lobbyManager = new LobbyManager(_networkClient);

// 登录
await _lobbyManager.LoginAsync("username", "password");

// 获取成就
_lobbyManager.AchievementsReceived += achievements =>
{
    _achievementSystem.SyncWithServer(achievements);
    Console.WriteLine($"Loaded {achievements.Count} achievements");
};
await _lobbyManager.GetAchievementsAsync();

// 处理完成
_lobbyManager.AchievementCompleted += notification =>
{
    Console.WriteLine($"Achievement: {notification.AchievementName}");
    foreach (var reward in notification.Rewards)
    {
        Console.WriteLine($"  Reward: {reward.Type} {reward.ItemId}");
    }
};

// 游戏中更新
await _lobbyManager.UpdateAchievementAsync("achievement_id", 1);

// 检查状态
var stats = _achievementSystem.GetCompletionStats();
Console.WriteLine($"Progress: {stats.completed}/{stats.total} ({stats.percentage}%)");

// 验证一致性
if (!_achievementSystem.ValidateSyncState())
{
    await _lobbyManager.GetAchievementsAsync(); // 重新同步
}
```

## 参考文档

- [成就系统指南](ACHIEVEMENT_GUIDE.md)
- [测试指南](ACHIEVEMENT_TESTING_GUIDE.md)
