# 联机对战服务端使用指南

## 项目结构

项目现在包含三个部分：
- **EonVientiane** - 游戏客户端（MonoGame）
- **EonVientianeServer** - 游戏服务端（控制台应用）
- **Shared** - 共享库（网络协议和数据结构）

## 启动服务端

### 方法1：使用终端
```bash
cd EonVientianeServer
dotnet run
```

### 方法2：指定端口
```bash
cd EonVientianeServer
dotnet run 8888
```

默认端口是 7777。

## 服务端命令

服务端运行后，可以使用以下命令：
- `status` - 显示服务器状态（已连接客户端、活跃房间）
- `quit` 或 `exit` - 停止服务器并退出
- `help` - 显示帮助信息

## 功能说明

### 已实现功能

#### 服务端
1. **连接管理** - 接受TCP客户端连接，管理已连接的客户端
2. **房间系统** - 创建、加入、离开房间
3. **房间列表** - 查询可用房间列表
4. **实时更新** - 房间状态变化时自动通知所有房间内玩家
5. **断线处理** - 客户端断线时自动清理房间

#### 客户端
1. **NetworkClient** - TCP网络客户端，处理与服务器的连接
2. **LobbyManager** - 大厅管理器，处理房间相关逻辑
3. **MultiplayerLobbyManager** - 联机大厅UI管理器

### 网络协议

使用基于TCP的自定义协议：
- 消息格式：4字节长度 + JSON数据
- 支持的消息类型：
  - Connect/Disconnect - 连接管理
  - GetRoomList - 获取房间列表
  - CreateRoom - 创建房间
  - JoinRoom - 加入房间
  - LeaveRoom - 离开房间
  - RoomUpdate - 房间更新通知
  - Error - 错误消息

## 下一步开发建议

### 1. 集成到游戏主界面
在Game1.cs中添加多人游戏按钮，集成MultiplayerLobbyManager。

### 2. 实现游戏同步
- 添加游戏开始消息
- 实现回合制游戏状态同步
- 添加玩家行动消息

### 3. 增强功能
- 添加聊天系统
- 实现准备/取消准备
- 添加房间设置（私密房间、密码等）
- 实现观战功能

### 4. 安全性
- 添加身份验证
- 实现防作弊机制
- 添加速率限制

## 测试

### 测试服务端
1. 启动服务端：`dotnet run --project EonVientianeServer`
2. 使用 `status` 命令查看服务器状态

### 测试客户端连接
在客户端代码中：
```csharp
var lobbyManager = new MultiplayerLobbyManager();
lobbyManager.Connect("PlayerName", "localhost", 7777);
```

## 故障排除

### 连接失败
- 检查服务端是否正在运行
- 检查防火墙设置
- 确认端口号正确

### 消息不同步
- 检查网络延迟
- 查看服务端日志
- 确认消息序列化正常

## 架构说明

### 服务端架构
```
GameServer
├── ConnectedClient - 客户端连接管理
├── GameRoom - 房间管理
└── 消息处理循环
```

### 客户端架构
```
MultiplayerLobbyManager
├── NetworkClient - 网络通信
└── LobbyManager - 大厅逻辑
```

### 通信流程
```
客户端                服务端
  |                     |
  |----Connect--------->|
  |<---Connected--------|
  |                     |
  |--GetRoomList------->|
  |<-RoomListResponse---|
  |                     |
  |--CreateRoom-------->|
  |<-CreateRoomResp-----|
  |<---RoomUpdate-------|
```
