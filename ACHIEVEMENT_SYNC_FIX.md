# 成就系统同步修复报告

## 问题描述
成就系统（如 `first_defense`）在客户端更新后，没有同步到服务器，导致服务器端一直没有响应成就完成。

## 根本原因
`AchievementSystem.UpdateProgress()` 方法只在客户端本地更新成就进度，但**没有将更新异步发送到服务器**。这导致服务器端的成就管理器没有收到任何更新请求。

### 具体流程问题：
1. 客户端在 `Game1.cs` 中调用 `_achievementSystem.UpdateProgress("first_defense", 1)`
2. 这只更新本地 `_achievements` 字典中的进度
3. 但是**没有调用** `_lobbyManager.UpdateAchievementAsync()` 来发送网络请求
4. 导致服务器的 `AchievementManager` 从未收到更新

## 修复方案

### 1. 在 AchievementSystem 中添加 LobbyManager 参考
**文件**: [EonVientiane/AchievementSystem.cs](EonVientiane/AchievementSystem.cs)

```csharp
// 添加私有字段
private MultiplayerLobbyManager? _lobbyManager;

// 添加初始化方法
public void SetLobbyManager(MultiplayerLobbyManager lobbyManager)
{
    _lobbyManager = lobbyManager;
    Console.WriteLine("[Client] AchievementSystem LobbyManager set for network sync");
}
```

### 2. 修改 UpdateProgress 方法实现异步同步
**文件**: [EonVientiane/AchievementSystem.cs](EonVientiane/AchievementSystem.cs#L140)

在本地更新成就后，异步调用服务器同步：

```csharp
// 异步同步到服务器
if (_lobbyManager != null)
{
    _ = Task.Run(async () =>
    {
        try
        {
            Console.WriteLine($"[AchievementSystem] Syncing achievement '{achievementId}' to server");
            await _lobbyManager.UpdateAchievementAsync(achievementId, progressDelta);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AchievementSystem] Failed to sync achievement to server: {ex.Message}");
        }
    });
}
else
{
    Console.WriteLine($"[AchievementSystem] WARNING: LobbyManager not set, achievement '{achievementId}' will not be synced to server");
}
```

### 3. 在 Game1.cs 中传递 LobbyManager
**文件**: [EonVientiane/Game1.cs](EonVientiane/Game1.cs#L113)

在初始化时将 `_lobbyManager` 传递给 `_achievementSystem`：

```csharp
// 在 Initialize() 方法中，订阅事件之前
_achievementSystem.SetLobbyManager(_lobbyManager);
```

### 4. 添加必要的命名空间
**文件**: [EonVientiane/AchievementSystem.cs](EonVientiane/AchievementSystem.cs#L1)

```csharp
using System.Threading.Tasks;
```

## 同步流程图
```
客户端战斗结束
    ↓
Game1.OnBattleEnded() 
    ↓
_achievementSystem.UpdateProgress("first_defense", 1)
    ├─→ 更新本地成就数据 ✓
    └─→ 异步调用 _lobbyManager.UpdateAchievementAsync()
        ↓
    NetworkMessage 发送到服务器
        ↓
    GameServer.HandleUpdateAchievementAsync()
        ↓
    AchievementManager.UpdateAchievementProgress()
        ↓
    服务器成就进度更新 ✓
        ↓
    如果成就完成，发送 AchievementCompletedNotification 给客户端
```

## 验证方法

1. **启动本地测试**：
```bash
./start_local_test.sh
```

2. **进行战斗并产生防守动作**（如使用飞羽骰子闪避）

3. **检查日志**：
   - 客户端应显示：`[AchievementSystem] Syncing achievement 'first_defense' to server`
   - 服务器应显示：`[Server] User 'xxx' progressed achievement 'first_defense' from 0 to 1/1`

4. **检查成就完成**：
   - 当条件满足时，服务器应显示：`[Server] User 'xxx' completed achievement 'first_defense'`
   - 客户端应显示：`[Server] AchievementCompleted notification received for 'first_defense'`

## 受影响的成就
所有通过 `UpdateProgress()` 更新的成就现在都能正确同步到服务器：
- ✓ `first_defense` - 初次防御
- ✓ `perfect_victory` - 绝对碾压
- ✓ `blitz_victory` - 秒了
- ✓ `where_am_i` - 我在哪
- 以及其他通过客户端触发的成就

## 测试建议
1. 测试单个成就完成流程
2. 测试多个成就同时更新
3. 验证网络延迟下的同步
4. 确保服务器和客户端状态一致

## 代码变更统计
- **修改文件**：2 个
  - `EonVientiane/AchievementSystem.cs`：添加网络同步逻辑
  - `EonVientiane/Game1.cs`：初始化 LobbyManager 关联
- **新增方法**：1 个 (`SetLobbyManager`)
- **修改方法**：1 个 (`UpdateProgress`)
- **新增命名空间**：1 个 (`System.Threading.Tasks`)
