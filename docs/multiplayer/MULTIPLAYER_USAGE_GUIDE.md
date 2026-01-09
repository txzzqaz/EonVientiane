# 联机大厅使用指南

## 快速开始

### 1. 启动服务器
```csharp
var server = new GameServer(7777);
await server.StartAsync();
```

### 2. 客户端登录
```csharp
// 创建大厅管理器
var lobbyManager = new MultiplayerLobbyManager();

// 注册新用户（可选）
await lobbyManager.RegisterAsync("newuser", "password123", "user@example.com");

// 登录
await lobbyManager.LoginAsync("admin", "admin");

// 登录成功后自动连接到大厅
```

### 3. 刷新房间列表
```csharp
lobbyManager.RefreshRoomList();

// 获取房间列表
var rooms = lobbyManager.RoomList;
```

### 4. 创建房间
```csharp
// 创建一个最多4个玩家的房间
lobbyManager.CreateRoom("My Game Room", maxPlayers: 4);
```

### 5. 加入房间
```csharp
string roomId = "room-id-here";
lobbyManager.JoinRoom(roomId);
```

### 6. 准备游戏
```csharp
// 切换准备状态
lobbyManager.ToggleReady();
```

### 7. 离开房间
```csharp
lobbyManager.LeaveRoom();
```

### 8. 获取初始物品
```csharp
await lobbyManager.GetInitialInventoryAsync();
```

## 事件订阅

```csharp
var lobbyManager = new MultiplayerLobbyManager();

// 登录相关
lobbyManager.LoginSuccess += () => Console.WriteLine("登录成功");
lobbyManager.RegisterSuccess += () => Console.WriteLine("注册成功");

// 房间相关
lobbyManager.RoomListUpdated += () => Console.WriteLine($"房间列表已更新，共{lobbyManager.RoomList.Count}个房间");
lobbyManager.RoomJoined += () => Console.WriteLine($"已加入房间: {lobbyManager.CurrentRoom?.RoomName}");
lobbyManager.RoomLeft += () => Console.WriteLine("已离开房间");
lobbyManager.RoomUpdated += () => Console.WriteLine("房间已更新");

// 错误处理
lobbyManager.ErrorOccurred += (error) => Console.WriteLine($"错误: {error}");
```

## 预置测试账户

| 用户名 | 密码 | 说明 |
|--------|------|------|
| admin | admin | 管理员账户 |
| user | user | 普通用户 |
| test | test | 测试账户 |

## 错误处理

所有操作都会通过 `ErrorOccurred` 事件返回错误信息：

```csharp
lobbyManager.ErrorOccurred += (error) => 
{
    MessageBox.Show($"操作失败: {error}");
};
```

常见错误信息：
- "请先登录" - 未认证就尝试访问大厅
- "房间不存在" - 尝试加入不存在的房间
- "房间已满" - 房间人数已达上限
- "游戏已开始，无法加入" - 房间内游戏已经开始

## 状态管理

```csharp
// 检查连接状态
if (lobbyManager.State == LobbyState.InLobby)
{
    // 在大厅中，可以创建/加入房间
}
else if (lobbyManager.State == LobbyState.InRoom)
{
    // 在房间中，可以设置准备状态
}
else if (lobbyManager.State == LobbyState.Disconnected)
{
    // 未连接或登录失败
}

// 获取状态信息
string status = lobbyManager.StatusMessage;
```

## 常见问题

### Q: 登录失败提示"用户名或密码错误"
A: 检查用户名和密码是否正确。预置账户的用户名和密码相同。

### Q: 创建房间提示"您已经在另一个房间中"
A: 需要先离开当前房间才能创建新房间。

### Q: 加入房间提示"房间不存在"
A: 房间可能已被删除或房间ID输入错误。

### Q: 获取初始物品失败
A: 需要先成功登录并连接到服务器。

## 调试技巧

### 查看服务器状态
```csharp
server.PrintStatus(); // 输出服务器状态和房间列表
```

### 监听所有事件
```csharp
lobbyManager.LoginSuccess += () => Debug.WriteLine("✓ 登录成功");
lobbyManager.RegisterSuccess += () => Debug.WriteLine("✓ 注册成功");
lobbyManager.RoomListUpdated += () => Debug.WriteLine($"✓ 房间列表更新: {lobbyManager.RoomList.Count}个房间");
lobbyManager.RoomJoined += () => Debug.WriteLine("✓ 加入房间成功");
lobbyManager.RoomLeft += () => Debug.WriteLine("✓ 离开房间成功");
lobbyManager.RoomUpdated += () => Debug.WriteLine("✓ 房间信息更新");
lobbyManager.ErrorOccurred += (e) => Debug.WriteLine($"✗ 错误: {e}");
```
