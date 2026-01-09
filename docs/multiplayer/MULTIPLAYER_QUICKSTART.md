# 联机对战系统 - 快速开始

## 项目概览

已成功创建三个项目：

1. **EonVientianeServer** - 游戏服务端（TCP服务器）
2. **Shared** - 共享库（网络协议）
3. **EonVientiane** - 游戏客户端（已集成网络功能）

## 快速启动

### 方法 1：使用启动脚本

```bash
./start_server.sh        # 使用默认端口7777
./start_server.sh 8888   # 使用自定义端口8888
```

### 方法 2：手动启动

```bash
cd EonVientianeServer
dotnet run              # 默认端口7777
dotnet run 8888         # 自定义端口8888
```

## 服务端功能

服务端启动后，可以使用以下命令：

- `status` - 显示服务器状态
  - 已连接的客户端数量
  - 活跃的房间列表
  - 每个房间的玩家数和状态

- `quit` 或 `exit` - 停止服务器

## 客户端集成

客户端已包含以下网络组件：

### 1. NetworkClient (Network/NetworkClient.cs)
TCP网络客户端，处理与服务器的底层通信。

### 2. LobbyManager (Network/LobbyManager.cs)
大厅管理器，提供以下功能：
- 获取房间列表
- 创建房间
- 加入/离开房间
- 接收房间更新通知

### 3. MultiplayerLobbyManager (MultiplayerLobbyManager.cs)
高级大厅管理器，包含UI状态管理。

## 使用示例

### 在Game1.cs中集成联机功能

```csharp
// 1. 添加字段
private MultiplayerLobbyManager _multiplayerLobby;

// 2. 在Initialize或LoadContent中初始化
_multiplayerLobby = new MultiplayerLobbyManager();

// 3. 连接到服务器
_multiplayerLobby.Connect("玩家名称", "localhost", 7777);

// 4. 创建房间
_multiplayerLobby.CreateRoom("我的房间", 2);

// 5. 刷新房间列表
_multiplayerLobby.RefreshRoomList();

// 6. 加入房间
_multiplayerLobby.JoinRoom(roomId);

// 7. 获取当前房间信息
var currentRoom = _multiplayerLobby.CurrentRoom;
var players = _multiplayerLobby.CurrentRoomPlayers;
```

## 网络协议说明

### 消息类型

- **Connect/Disconnect** - 连接管理
- **GetRoomList** - 获取房间列表
- **CreateRoom** - 创建房间
- **JoinRoom** - 加入房间
- **LeaveRoom** - 离开房间
- **RoomUpdate** - 房间状态更新（自动推送）

### 房间状态

- **Waiting** - 等待中（可加入）
- **Full** - 已满（不可加入）
- **InGame** - 游戏中（不可加入）

## 测试流程

### 1. 启动服务端

```bash
./start_server.sh
```

看到以下输出表示成功：
```
=================================
  EonVientiane Game Server
=================================

Starting server...
[Server] Started on port 7777

Server is running. Available commands:
  status  - Show server status
  quit    - Stop server and exit

>
```

### 2. 测试服务端命令

在服务端控制台输入：
```
> status
```

输出示例：
```
=== Server Status ===
Connected Clients: 2
Active Rooms: 1

Rooms:
  - 测试房间 (2/2) [Waiting]
=====================
```

## 架构说明

```
客户端                           服务端
┌─────────────────┐            ┌──────────────┐
│MultiplayerLobby │            │ GameServer   │
│    Manager      │            │              │
├─────────────────┤            ├──────────────┤
│ LobbyManager    │◄──JSON──►  │ GameRoom     │
├─────────────────┤  over TCP  │              │
│ NetworkClient   │◄─────────► │ Connected    │
│                 │            │   Client     │
└─────────────────┘            └──────────────┘
```

## 下一步开发

### 1. UI集成
在MenuManager中添加"联机对战"按钮，打开大厅界面。

### 2. 游戏同步
实现游戏状态的网络同步：
- 回合开始/结束
- 玩家行动
- 战斗结果

### 3. 增强功能
- 添加聊天系统
- 实现准备机制
- 添加观战功能
- 房间设置（密码、最大人数等）

### 4. 错误处理
- 网络断线重连
- 超时处理
- 错误提示UI

## 常见问题

### Q: 连接失败怎么办？
A: 确保：
1. 服务端正在运行
2. 防火墙允许端口访问
3. IP和端口正确

### Q: 如何在局域网内联机？
A: 将 "localhost" 替换为服务器的局域网IP地址。

### Q: 如何支持互联网联机？
A: 需要：
1. 公网IP或域名
2. 端口转发配置
3. 可能需要NAT穿透

## 文件清单

### 新增文件
- `EonVientianeServer/Program.cs` - 服务端主程序
- `EonVientianeServer/GameServer.cs` - 服务器核心逻辑
- `EonVientianeServer/GameRoom.cs` - 房间管理
- `EonVientianeServer/ConnectedClient.cs` - 客户端连接管理
- `Shared/NetworkProtocol.cs` - 网络协议定义
- `EonVientiane/Network/NetworkClient.cs` - 客户端网络通信
- `EonVientiane/Network/LobbyManager.cs` - 大厅逻辑
- `EonVientiane/MultiplayerLobbyManager.cs` - 大厅UI管理
- `start_server.sh` - 服务端启动脚本

### 修改文件
- `EonVientiane/EonVientiane.csproj` - 添加Shared项目引用
- `EonVientianeServer/EonVientianeServer.csproj` - 添加Shared项目引用

## 性能建议

1. **连接池管理** - 限制最大连接数
2. **消息队列** - 避免消息发送阻塞
3. **心跳检测** - 定期Ping/Pong检测连接
4. **数据压缩** - 对大数据包进行压缩

## 安全建议

1. 添加用户认证
2. 实现消息签名验证
3. 防止DDoS攻击
4. 数据加密传输（TLS/SSL）

---

创建时间: 2026-01-09
作者: GitHub Copilot
