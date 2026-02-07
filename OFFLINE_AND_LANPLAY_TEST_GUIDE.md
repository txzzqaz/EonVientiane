# 离线和局域网游戏 - 快速测试指南

## 编译和运行

### 1. 编译项目
```bash
cd /path/to/EonVientiane
dotnet build
```

### 2. 运行客户端
```bash
cd EonVientiane
dotnet run
```

---

## 测试场景

### 测试1：本地账户创建和登录

#### 第一次运行
1. 启动游戏客户端
2. 在登录界面，点击 "创建本地账户" 或 "离线游戏"
3. 输入用户名：`TestPlayer1`
4. 输入密码：`Test123456`
5. 输入邮箱：`test@example.com`
6. 点击 "创建账户"

**预期结果：**
- 账户创建成功
- 自动登录
- 显示 "离线模式" 标签
- 可以看到初始数据：等级 1，金币 1000

#### 第二次运行
1. 启动游戏客户端
2. 输入用户名：`TestPlayer1`
3. 输入密码：`Test123456`
4. 点击 "本地登录"

**预期结果：**
- 登录成功，显示之前的游戏数据
- 账户信息已从本地文件恢复

---

### 测试2：局域网主机发现

#### 设置（需要两台电脑或虚拟机在同一网络）

**电脑A（主机）：**
```
1. 启动游戏
2. 创建或登录本地账户
3. 输入用户名：HostPlayer
4. 进入大厅 → 启动本地游戏模式
   控制台输出：
   [LocalNetwork] 启动局域网发现，监听端口 17777
   [LocalNetwork] 开始广播本地主机: HostPlayer
   [LocalGameServer] 本地游戏服务器已启动，监听端口 18888
```

**电脑B（客户端）：**
```
1. 启动游戏
2. 创建或登录本地账户
3. 输入用户名：ClientPlayer  
4. 进入大厅 → 启动本地游戏模式
   控制台输出：
   [LocalNetwork] 启动局域网发现，监听端口 17777
   [LocalNetwork] 开始广播本地主机: ClientPlayer
   [LocalNetwork] 发现主机: HostPlayer (192.168.x.x:18888)
   [LocalHostDiscovered] HostPlayer 已发现
```

**预期结果：**
- 电脑B的界面显示发现的主机列表
- 列表中包含 "HostPlayer"
- 显示主机IP和端口信息

---

### 测试3：创建和加入本地游戏

#### 主机端（A）：
```
1. 本地登录
2. 启动本地游戏模式
3. 点击 "创建游戏"
4. 输入游戏名称："My Arena"
5. 设置最大玩家数：2
6. 点击确认

控制台输出：
[Lobby] 创建本地游戏: My Arena, 最大玩家数: 2
[LocalGameServer] 游戏创建: <gameId> - My Arena
游戏列表显示：
  - My Arena (主机: HostPlayer, 1/2 玩家)
```

#### 客户端（B）：
```
1. 本地登录
2. 启动本地游戏模式
3. 等待发现主机（约5秒内）
4. 在主机列表中看到 "HostPlayer"
5. 点击加入
6. 输入玩家名称（或自动使用登录名）

控制台输出：
[LocalNetwork] 发现主机: HostPlayer (192.168.x.x:18888)
[Lobby] 加入本地游戏: HostPlayer
加入成功提示
```

#### 主机端（A）确认：
```
房间列表更新：
  - My Arena (主机: HostPlayer, 2/2 玩家) [满员]
  
玩家列表：
  - HostPlayer (主机，未准备)
  - ClientPlayer (客户端，未准备)
```

**预期结果：**
- 两个客户端都可以看到对方
- 游戏状态从 "等待中" 变为 "满员"
- 可以看到双方的玩家信息

---

### 测试4：游戏启动流程

#### 主机端（A）：
```
1. 等待所有玩家加入
2. 点击 "准备" 按钮
3. 看到自己的状态变为 "已准备"

若所有玩家都准备：
  [LocalGameServer] 游戏即将开始: <gameId>
  倒计时开始（通常3秒）
  [LocalGameServer] 游戏已启动: <gameId>
```

#### 客户端（B）：
```
1. 等待主机操作
2. 看到主机的状态变为 "已准备"  
3. 也点击 "准备"

若所有玩家都准备：
  收到游戏启动通知
  游戏场景加载
  进入对战界面
```

**预期结果：**
- 双方都看到倒计时
- 倒计时结束后都进入游戏
- 游戏同步状态

---

### 测试5：本地数据持久化

```
运行后检查文件系统：
ls -la data/local_accounts/

应该看到：
drwxr-xr-x  data/local_accounts/
  -rw-r--r--  accounts.json        # 账户索引
  -rw-r--r--  testplayer1.json    # 账户信息
  -rw-r--r--  testplayer1.key     # RSA私钥
  -rw-r--r--  hostplayer.json
  -rw-r--r--  hostplayer.key
  -rw-r--r--  clientplayer.json
  -rw-r--r--  clientplayer.key

accounts.json 内容示例：
[
  {
    "username": "TestPlayer1",
    "passwordHash": "<base64_hash>",
    "email": "test@example.com",
    "createdDate": "2026-02-07T10:30:00Z",
    "lastLogin": "2026-02-07T10:35:00Z",
    "publicKey": "<rsa_public_key>",
    "profileData": {
      "level": "1",
      "experience": "0",
      "coins": "1000"
    }
  }
]
```

---

### 测试6：离线模式（无网络）

```
场景：网络断开或服务器不可用

1. 创建本地账户和游戏
2. 断开网络
3. 启动本地游戏模式
4. 创建/加入本地游戏
5. 进行对战

预期结果：
- 所有操作正常工作
- 不依赖中央服务器
- 数据保存到本地
```

---

## 调试技巧

### 1. 启用详细日志
在游戏启动时添加调试输出：

```csharp
Console.WriteLine("[DEBUG] 本地账户管理器初始化");
var accountManager = new LocalAccountManager("data/local_accounts");
Console.WriteLine($"[DEBUG] 已加载 {accountManager.GetAllLocalUsernames().Count} 个账户");

Console.WriteLine("[DEBUG] 局域网管理器初始化");
var networkManager = new LocalNetworkManager();
await networkManager.StartDiscoveryAsync(hostInfo);
```

### 2. 检查网络连接
```bash
# 检查UDP端口是否可用
netstat -an | grep 17777
netstat -an | grep 18888

# 测试UDP连接
# 在主机A上：
nc -u -l 17777

# 在主机B上：
echo "test" | nc -u <host_a_ip> 17777
```

### 3. 查看文件权限
```bash
# 确保本地账户文件可读写
chmod 600 data/local_accounts/*.key
chmod 644 data/local_accounts/*.json
```

### 4. 清理旧数据进行完整测试
```bash
# 删除本地账户数据重新开始
rm -rf data/local_accounts/
```

---

## 预期的控制台输出顺序

### 本地账户创建时
```
[LocalAccount] 账户创建成功: TestPlayer1
[LocalAccount] 账户已保存: TestPlayer1
```

### 本地登录时
```
[LocalAccount] 本地登录成功: TestPlayer1
[LoginManager] 本地离线登录成功
```

### 启动本地游戏模式时
```
[LocalNetwork] 局域网管理器已初始化
[LocalNetwork] 启动局域网发现，监听端口 17777
[LocalNetwork] 开始广播本地主机: <username>
[LocalGameServer] 本地游戏服务器初始化，端口: 18888
[LocalGameServer] 本地游戏服务器已启动，监听端口 18888
```

### 发现主机时
```
[LocalNetwork] 发现主机: <hostname> (192.168.x.x:18888)
[Lobby] LocalHostDiscovered: <hostname>
```

---

## 已知限制

1. **UDP广播范围**：需要在同一子网内
2. **防火墙**：UDP端口17777和18888需要开放
3. **单机测试**：如果只有一台电脑，可以用虚拟机模拟第二台设备
4. **跨域网络**：VPN环境下需要特殊配置

---

## 成功标志

✅ 本地账户创建成功  
✅ 本地登录成功  
✅ 启动本地游戏模式成功  
✅ 发现局域网内的其他主机  
✅ 创建和加入本地游戏成功  
✅ 游戏启动并同步成功  
✅ 数据已持久化到本地  
✅ 完全离线运行成功  

---

## 下一步

- 在UI层面集成这些新功能
- 添加游戏进度同步机制
- 实现离线对战回放功能
- 支持多房间同时运行
- 添加本地聊天功能

