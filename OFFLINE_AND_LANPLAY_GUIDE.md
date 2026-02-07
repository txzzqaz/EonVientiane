# 离线账户和局域网联机系统 - 完整指南

## 概述

本系统已升级支持以下功能：
1. **本地离线账户** - 使用区块链风格加密，可完全离线使用
2. **局域网主机发现** - UDP广播自动发现局域网内的其他玩家
3. **局域网P2P对战** - 在没有中央服务器的情况下进行对战
4. **向后兼容** - 仍保持与中央服务器的连接支持

---

## 系统架构

### 新增核心类

#### 1. LocalAccountManager（本地账户管理器）
位置：`EonVientiane/LocalAccountManager.cs`

**功能：**
- 创建和管理本地账户
- 使用SHA-256密码哈希
- 使用RSA-2048密钥对进行数字签名
- 账户数据持久化到 `data/local_accounts/` 目录

**关键方法：**
```csharp
// 创建账户
(bool success, string message) CreateAccount(string username, string password, string email)

// 本地登录
(bool success, LocalAccount account, string message) Login(string username, string password)

// 获取账户列表
List<string> GetAllLocalUsernames()

// 删除账户
bool DeleteAccount(string username)
```

#### 2. LocalNetworkManager（局域网网络管理器）
位置：`EonVientiane/LocalNetworkManager.cs`

**功能：**
- UDP广播发现局域网内的主机
- 自动监测主机上线/离线
- 每2秒广播一次本地信息，10秒无应答则认为离线

**关键方法：**
```csharp
// 启动发现
Task StartDiscoveryAsync(LocalHost localInfo)

// 停止发现
void StopDiscovery()

// 获取发现的主机列表
List<LocalHost> DiscoveredHosts

// 获取本地IP地址
static string GetLocalIPAddress()
```

**事件：**
- `HostDiscovered` - 发现新主机
- `HostLost` - 主机离线

#### 3. LocalGameServer（本地游戏服务器）
位置：`EonVientiane/LocalGameServer.cs`

**功能：**
- 运行在主机上作为P2P游戏服务器
- 默认监听端口 18888
- 管理游戏会话（房间）

**关键方法：**
```csharp
// 启动服务器
Task StartAsync()

// 停止服务器
void Stop()

// 获取活跃游戏列表
List<LocalGameInfo> GetActiveGames()
```

**事件：**
- `GameCreated` - 游戏创建
- `GameStarted` - 游戏启动
- `GameEnded` - 游戏结束

#### 4. MultiplayerLobbyManager 扩展
位置：`EonVientiane/MultiplayerLobbyManager.cs`

**新增功能：**
- 本地游戏模式启动/停止
- 创建本地游戏
- 加入本地游戏
- 主机发现集成

---

## 使用流程

### 场景1：本地离线游戏

#### 用户A（主机）：
```csharp
// 1. 本地账户登录
var loginManager = new LoginManager();
var (success, message) = loginManager.LocalLogin("Player_A", "password123");

if (success)
{
    // 2. 启动本地游戏模式
    var lobbyManager = new MultiplayerLobbyManager();
    await lobbyManager.StartLocalGameModeAsync("Player_A");
    
    // 3. 创建本地游戏
    await lobbyManager.CreateLocalGameAsync("My Battle Arena", maxPlayers: 2);
}
```

#### 用户B（客户端）：
```csharp
// 1. 本地账户登录
var loginManager = new LoginManager();
var (success, message) = loginManager.LocalLogin("Player_B", "password456");

if (success)
{
    // 2. 启动本地游戏模式
    var lobbyManager = new MultiplayerLobbyManager();
    await lobbyManager.StartLocalGameModeAsync("Player_B");
    
    // 3. 发现局域网内的主机
    var hosts = lobbyManager.GetDiscoveredHosts();
    // hosts 中包含 Player_A 的主机信息
    
    // 4. 加入 Player_A 的游戏
    var hostToJoin = hosts.FirstOrDefault(h => h.Username == "Player_A");
    if (hostToJoin != null)
    {
        await lobbyManager.JoinLocalGameAsync("Player_A", gameId);
    }
}
```

### 场景2：本地账户创建

```csharp
var accountManager = new LocalAccountManager();

// 创建账户
var (success, message) = accountManager.CreateAccount(
    username: "MyPlayer",
    password: "SecurePassword123!",
    email: "player@example.com"
);

if (success)
{
    Console.WriteLine("账户创建成功");
    
    // 账户已保存到：
    // - data/local_accounts/accounts.json (索引)
    // - data/local_accounts/myplayer.json (账户信息)
    // - data/local_accounts/myplayer.key (私钥)
}
```

### 场景3：本地账户登录

```csharp
var loginManager = new LoginManager();

// 登录本地账户
var (success, account, message) = loginManager.LocalLogin("MyPlayer", "SecurePassword123!");

if (success)
{
    Console.WriteLine($"登录成功：{account.Username}");
    Console.WriteLine($"创建于：{account.CreatedDate}");
    Console.WriteLine($"等级：{account.ProfileData["level"]}");
    Console.WriteLine($"金币：{account.ProfileData["coins"]}");
}
else
{
    Console.WriteLine($"登录失败：{message}");
}
```

---

## 数据存储结构

### 本地账户目录
```
data/local_accounts/
├── accounts.json          # 账户索引（所有账户列表）
├── player1.json          # 账户信息（密码哈希、邮箱等）
├── player1.key           # 账户私钥（RSA-2048）
├── player2.json
├── player2.key
└── ...
```

### 账户文件格式（player1.json）
```json
{
  "username": "player1",
  "passwordHash": "base64_encoded_sha256_hash",
  "email": "player@example.com",
  "createdDate": "2026-02-07T10:30:00Z",
  "lastLogin": "2026-02-07T15:45:00Z",
  "publicKey": "rsa_public_key_xml",
  "profileData": {
    "level": "1",
    "experience": "0",
    "coins": "1000"
  }
}
```

---

## 网络协议扩展

### 新增消息类型

在 `Shared/NetworkProtocol.cs` 中添加：

```csharp
// 本地网络相关消息
LocalGameCreate              // 创建本地游戏
LocalGameCreateResponse      // 创建响应
LocalGameJoin               // 加入本地游戏
LocalGameJoinResponse       // 加入响应
LocalGameStart              // 启动本地游戏
LocalGameState              // 游戏状态
LocalGameAction             // 游戏动作
LocalGameEnd                // 游戏结束
LocalOfflineLogin           // 本地离线登录
LocalOfflineLoginResponse   // 离线登录响应
```

### 新增数据结构

```csharp
// 本地游戏信息
public class LocalGameInfo
{
    public string GameId { get; set; }
    public string GameName { get; set; }
    public string HostUsername { get; set; }
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 本地游戏玩家
public class LocalGamePlayer
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
    public int TeamId { get; set; }
}

// 本地离线登录请求
public class LocalOfflineLoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

// 本地离线登录响应
public class LocalOfflineLoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Username { get; set; }
    public string UserId { get; set; }
}
```

---

## 安全特性

### 加密机制

1. **密码存储**：SHA-256哈希，没有盐值存储明文密码
2. **账户交互**：RSA-2048密钥对支持数字签名
3. **数据隐私**：私钥存储在本地，不传输给服务器
4. **离线验证**：所有验证在本地进行，不依赖网络

### 账户隐私

- 账户信息完全存储在本地
- 只有在用户主动选择在线模式时才连接服务器
- 支持多个本地账户，安全隔离

---

## 配置和故障排除

### 端口配置

```csharp
// 修改本地游戏服务器端口（默认18888）
var gameServer = new LocalGameServer(port: 19999);
await gameServer.StartAsync();

// 修改发现服务端口（默认17777）
// 在 LocalNetworkManager.cs 中修改 DISCOVERY_PORT 常数
```

### 常见问题

**1. 无法发现其他主机**
- 检查防火墙是否阻止UDP端口 17777
- 确保两台设备在同一局域网
- 检查网络连接状态

**2. 本地登录失败**
- 检查用户名是否存在
- 验证密码是否正确
- 查看 `data/local_accounts/accounts.json` 是否存在

**3. 游戏服务器无法启动**
- 检查端口 18888 是否被占用
- 检查防火墙设置
- 查看控制台错误日志

---

## 高级功能

### 混合模式（离线+在线）

```csharp
var lobbyManager = new MultiplayerLobbyManager();
var loginManager = lobbyManager.LocalAccountManager.LocalAccountManager;

// 离线模式
await lobbyManager.StartLocalGameModeAsync("Player");
var localGames = lobbyManager.GetActiveLocalGames();

// 切换到在线模式（仍保留本地账户）
await lobbyManager.EnsureConnectedAsync();
await lobbyManager.LoginAsync("Player", "password");
var serverRooms = lobbyManager.RoomList;
```

### 跨平台支持

系统支持：
- Windows (已测试)
- Linux (通过 .NET Core)
- macOS (通过 .NET Core)

UDP广播在所有平台上都一致工作。

---

## 性能考虑

- **发现间隔**：2秒广播一次，可在 `LocalNetworkManager.BROADCAST_INTERVAL` 调整
- **超时检测**：10秒无应答判定离线，可在 `LocalNetworkManager.DISCOVERY_TIMEOUT` 调整
- **本地服务器**：可支持多个玩家连接，性能取决于游戏逻辑复杂度

---

## 总结

此系统实现了完全离线的本地游戏体验，同时保持与中央服务器的兼容性。玩家可以：

✅ 创建本地账户
✅ 完全离线游戏
✅ 自动发现局域网内的其他玩家  
✅ 进行P2P对战
✅ 数据完全加密存储

这是实现真正分布式游戏网络的第一步！
