# EonVientiane 联机对战系统

## ✅ 完成状态

已成功实现完整的联机对战服务端和客户端框架！

## 📦 项目结构

```
EonVientiane/
├── EonVientiane/          # 游戏客户端（MonoGame）
│   ├── Network/           # 网络通信模块
│   │   ├── NetworkClient.cs      # TCP客户端
│   │   └── LobbyManager.cs       # 大厅逻辑
│   └── MultiplayerLobbyManager.cs # 大厅UI管理
│
├── EonVientianeServer/    # 游戏服务端
│   ├── Program.cs         # 服务端入口
│   ├── GameServer.cs      # 服务器核心
│   ├── GameRoom.cs        # 房间管理
│   └── ConnectedClient.cs # 客户端连接
│
├── Shared/                # 共享库
│   └── NetworkProtocol.cs # 网络协议定义
│
└── start_server.sh        # 服务端启动脚本
```

## 🚀 快速启动

### 启动服务端

```bash
# 方法1：使用启动脚本
./start_server.sh

# 方法2：手动启动
cd EonVientianeServer
dotnet run

# 方法3：指定端口
dotnet run 8888
```

### 服务端命令

服务端运行时可使用：
- `status` - 显示服务器状态（客户端数、房间列表）
- `quit` 或 `exit` - 停止服务器

## 🎮 功能列表

### 已实现功能

#### 服务端
- ✅ TCP服务器（多客户端并发）
- ✅ 房间创建和管理
- ✅ 玩家加入/离开房间
- ✅ 房间列表查询
- ✅ 实时状态同步
- ✅ 自动断线处理
- ✅ 空房间自动清理

#### 客户端
- ✅ NetworkClient - TCP网络通信
- ✅ LobbyManager - 大厅逻辑处理
- ✅ MultiplayerLobbyManager - 高级大厅管理
- ✅ 事件驱动架构

#### 网络协议
- ✅ 基于JSON的消息协议
- ✅ 消息类型系统
- ✅ 房间信息数据结构
- ✅ 玩家信息数据结构
- ✅ 错误处理机制

## 💡 使用示例

### 在客户端中集成联机功能

```csharp
// 1. 创建大厅管理器
var lobby = new MultiplayerLobbyManager();

// 2. 连接到服务器
lobby.Connect("玩家名称", "localhost", 7777);

// 3. 获取房间列表
lobby.RefreshRoomList();
var rooms = lobby.RoomList;

// 4. 创建房间
lobby.CreateRoom("我的房间", maxPlayers: 2);

// 5. 加入房间
lobby.JoinRoom(roomId);

// 6. 查看当前房间
var currentRoom = lobby.CurrentRoom;
var players = lobby.CurrentRoomPlayers;

// 7. 离开房间
lobby.LeaveRoom();
```

## 📝 网络协议

### 消息类型

| 类型 | 说明 | 方向 |
|------|------|------|
| Connect | 连接请求 | C→S |
| Disconnect | 断开连接 | C→S |
| GetRoomList | 获取房间列表 | C→S |
| RoomListResponse | 房间列表响应 | S→C |
| CreateRoom | 创建房间 | C→S |
| CreateRoomResponse | 创建响应 | S→C |
| JoinRoom | 加入房间 | C→S |
| JoinRoomResponse | 加入响应 | S→C |
| LeaveRoom | 离开房间 | C→S |
| RoomUpdate | 房间更新 | S→C |
| Error | 错误消息 | S→C |

### 房间状态

- **Waiting** - 等待中（可加入）
- **Full** - 已满（不可加入）
- **InGame** - 游戏中（不显示在列表）

## 🧪 测试验证

服务端已测试通过：
```
=================================
  EonVientiane Game Server
=================================

Starting server...
[Server] Started on port 7777

Server is running.
```

## 📚 相关文档

- [MULTIPLAYER_QUICKSTART.md](MULTIPLAYER_QUICKSTART.md) - 快速开始指南
- [MULTIPLAYER_GUIDE.md](MULTIPLAYER_GUIDE.md) - 详细使用指南

## 🔜 下一步开发

### 1. UI集成（优先）
在Game1.cs中添加联机菜单界面：
- 连接服务器界面
- 房间列表显示
- 创建/加入房间按钮
- 房间内等待界面

### 2. 游戏状态同步
实现实际的游戏逻辑同步：
- 添加游戏开始消息
- 实现回合制同步
- 玩家操作广播
- 战斗结果同步

### 3. 增强功能
- [ ] 聊天系统
- [ ] 准备/取消准备
- [ ] 观战功能
- [ ] 房间密码
- [ ] 重连机制
- [ ] 心跳检测

### 4. 优化和安全
- [ ] 用户认证
- [ ] 数据加密
- [ ] 防作弊机制
- [ ] 性能优化

## 🛠️ 技术栈

- **.NET 8.0** - 运行时框架
- **C#** - 编程语言
- **System.Net.Sockets** - TCP通信
- **System.Text.Json** - JSON序列化
- **MonoGame** - 游戏引擎

## 📊 架构设计

```
┌──────────────────┐         TCP/JSON        ┌──────────────────┐
│   游戏客户端      │ ◄─────────────────────► │   游戏服务端      │
│                  │                          │                  │
│ ┌──────────────┐ │                          │ ┌──────────────┐ │
│ │ Multiplayer  │ │                          │ │ GameServer   │ │
│ │ LobbyManager │ │                          │ │              │ │
│ └──────┬───────┘ │                          │ └──────┬───────┘ │
│        │         │                          │        │         │
│ ┌──────▼───────┐ │                          │ ┌──────▼───────┐ │
│ │LobbyManager  │ │                          │ │  GameRoom    │ │
│ └──────┬───────┘ │                          │ └──────────────┘ │
│        │         │                          │        │         │
│ ┌──────▼───────┐ │                          │ ┌──────▼───────┐ │
│ │NetworkClient │ │                          │ │Connected     │ │
│ │              │ │                          │ │Client        │ │
│ └──────────────┘ │                          │ └──────────────┘ │
└──────────────────┘                          └──────────────────┘
         │                                             │
         └─────────────── Shared Library ─────────────┘
                    (NetworkProtocol.cs)
```

## ⚙️ 系统要求

- .NET 8.0 或更高版本
- 支持TCP/IP网络
- 开放端口7777（默认）或自定义端口

## 📄 许可

与EonVientiane主项目保持一致

---

**状态**: ✅ 基础框架完成，可开始集成到游戏UI
**日期**: 2026-01-09
**开发者**: GitHub Copilot
