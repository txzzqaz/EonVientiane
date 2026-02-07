# 游戏结束时自动退出房间修复

## 问题描述
游戏结束后，客户端仍然没有退出已经不存在的房间，导致玩家停留在已关闭的房间中。

## 根本原因
在 `Game1.OnBattleEnded()` 方法中，战斗结束通知被处理并保存到本地历史，但从未向服务器发送退出房间请求。只有当用户手动点击"返回大厅"按钮时，`OnReturnToLobbyRequested()` 才会被触发，此时才会调用 `LeaveRoom()`。

## 解决方案

### 1. 修改 `Game1.cs` - `OnBattleEnded()` 方法
**文件**: [EonVientiane/Game1.cs](EonVientiane/Game1.cs#L1131-L1138)

在 `OnBattleEnded()` 方法的末尾添加自动退出房间的逻辑：

```csharp
// 在多人游戏中，战斗结束后自动退出房间
if (_lobbyManager != null && _lobbyManager.State == LobbyState.InRoom)
{
    Console.WriteLine($"[Client] Battle ended - automatically leaving the room");
    _lobbyManager.LeaveRoom();
}
```

**更改位置**: 第1131-1138行（在成就更新之后）

### 2. 强化 `MultiplayerLobbyManager.cs` - `LeaveRoom()` 方法
**文件**: [EonVientiane/MultiplayerLobbyManager.cs](EonVientiane/MultiplayerLobbyManager.cs#L268-L281)

添加详细的日志记录，便于调试：

```csharp
public async void LeaveRoom()
{
    Console.WriteLine($"[MultiplayerLobbyManager] LeaveRoom called, current state: {_state}");
    
    if (_state != LobbyState.InRoom)
    {
        _statusMessage = "未在房间中";
        Console.WriteLine($"[MultiplayerLobbyManager] Cannot leave room - not in room (current state: {_state})");
        return;
    }
        
    _statusMessage = "Leaving room...";
    Console.WriteLine($"[MultiplayerLobbyManager] Sending leave room request");
    await _lobbyManager.LeaveRoomAsync();
    Console.WriteLine($"[MultiplayerLobbyManager] Leave room request sent");
}
```

## 修改的文件

| 文件 | 修改内容 | 行数 |
|-----|--------|------|
| `EonVientiane/Game1.cs` | 在 `OnBattleEnded()` 末尾添加自动退出房间 | 1131-1138 |
| `EonVientiane/MultiplayerLobbyManager.cs` | 强化 `LeaveRoom()` 的日志记录 | 268-281 |

## 工作流程

```
游戏结束 (BattleEndNotification)
    ↓
OnBattleEnded() 调用
    ├─ 保存对战记录
    ├─ 更新成就进度
    └─ 【新增】自动调用 LeaveRoom()
         ↓
    LeaveRoom() 检查状态
         ├─ 状态是否为 InRoom？
         ├─ 是 → 发送 LeaveRoomAsync()
         │        ↓
         │    服务器处理 LeaveRoom 请求
         │        ├─ 从房间移除玩家
         │        ├─ 房间为空则关闭房间
         │        └─ 发送 LeaveRoomResponse
         │              ↓
         │        客户端收到响应
         │              ↓
         │        更新状态为 InLobby
         │
         └─ 否 → 记录错误日志，不执行任何操作
```

## 调试信息

修复后会输出以下日志序列：

```
[Client] ========== Battle Ended ==========
[Client] Battle ended - automatically leaving the room
[MultiplayerLobbyManager] LeaveRoom called, current state: InRoom
[MultiplayerLobbyManager] Sending leave room request
[MultiplayerLobbyManager] Leave room request sent
[Server] <PlayerName> left room
[Server] Room <RoomId> closed (empty)
```

## 测试方法

1. 启动服务器
2. 两名玩家加入同一房间
3. 开始游戏
4. 等待游戏结束
5. 验证日志中出现"automatically leaving the room"消息
6. 验证玩家状态自动从 `InRoom` 变为 `InLobby`
7. 验证房间已从服务器移除

## 相关文件

- [EonVientiane/Game1.cs](EonVientiane/Game1.cs) - 客户端主游戏类
- [EonVientiane/MultiplayerLobbyManager.cs](EonVientiane/MultiplayerLobbyManager.cs) - 联机大厅管理器
- [EonVientiane/Network/LobbyManager.cs](EonVientiane/Network/LobbyManager.cs) - 网络层大厅管理器
- [EonVientianeServer/GameServer.cs](EonVientianeServer/GameServer.cs) - 服务器端房间管理

## 状态

✅ **已修复** - 编译成功，无错误

## 验证清单

- [x] 代码编译成功（0 errors，4 warnings）
- [x] 在 `OnBattleEnded()` 添加自动退出逻辑
- [x] 检查 `LobbyState` 状态
- [x] 添加详细的日志记录
- [x] 保持向后兼容性（手动点击按钮仍可工作）
