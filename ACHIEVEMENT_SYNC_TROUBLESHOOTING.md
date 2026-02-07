# 成就系统同步修复 - 故障排除指南

## 快速问题诊断

### 症状 1：成就更新无响应
**症状**：
- 客户端显示成就更新
- 服务器没有显示"progressed"日志
- 刷新后成就进度恢复到 0

**诊断步骤**：
1. 检查客户端日志是否包含 `[AchievementSystem] Syncing achievement`
2. 检查客户端是否已连接到服务器
3. 查看网络错误日志

**解决方案**：
```csharp
// 在 Game1.Initialize() 中确保调用了：
_achievementSystem.SetLobbyManager(_lobbyManager);

// 检查 _lobbyManager 是否已正确初始化
if (_lobbyManager == null)
{
    Console.WriteLine("ERROR: LobbyManager is null!");
}
```

### 症状 2：没有看到"Syncing"日志
**症状**：
```
[Client] Achievement progress updated: first_defense
[AchievementSystem] Updated achievement 'first_defense': 0 + 1 -> 1/1
【没有看到】[AchievementSystem] Syncing achievement...
```

**原因分析**：
| 原因 | 检查方法 |
|------|---------|
| LobbyManager 未设置 | 查看初始化日志中是否有 `LobbyManager set` |
| LobbyManager 为 null | 在 SetLobbyManager 中添加 Debug.Assert |
| 网络断开 | 检查 `_lobbyManager.IsConnected` |
| Task 异常 | 检查是否有异常输出：`Failed to sync achievement` |

**修复步骤**：
1. 检查 Game1.cs 第 110 行是否调用了 SetLobbyManager
2. 确保该调用在 LobbyManager 初始化之后
3. 检查 LobbyManager 的连接状态

### 症状 3：编译错误
**错误消息**：
```
error CS0103: The name 'Task' does not exist in the current context
```

**解决方案**：
检查 AchievementSystem.cs 第 4 行是否有：
```csharp
using System.Threading.Tasks;
```

如果没有，添加此行。

### 症状 4：运行时异常
**错误消息**：
```
[AchievementSystem] Failed to sync achievement to server: Exception...
```

**可能的原因和解决方案**：

| 异常类型 | 可能原因 | 解决方案 |
|---------|--------|---------|
| `NullReferenceException` | LobbyManager 为空 | 检查 SetLobbyManager 是否被调用 |
| `InvalidOperationException` | 网络状态异常 | 检查连接状态 |
| `TimeoutException` | 服务器响应缓慢 | 检查服务器状态 |
| `NetworkException` | 网络连接中断 | 重新连接服务器 |

## 日志分析

### 正常流程日志
```
[Client] AchievementSystem LobbyManager set for network sync
└─ 说明：初始化成功

[AchievementSystem] Updated achievement 'first_defense': 0 + 1 -> 1/1 (第一次防御)
└─ 说明：本地成就更新成功

[AchievementSystem] Achievement 'first_defense' progress target reached!
└─ 说明：成就进度达到要求

[AchievementSystem] Syncing achievement 'first_defense' to server
└─ 说明：开始网络同步

[Server] HandleUpdateAchievement called for user 'admin', achievement 'first_defense', delta 1
└─ 说明：服务器收到更新请求

[Server] User 'admin' completed achievement 'first_defense' (第一次防御)
└─ 说明：服务器处理成功，成就完成
```

### 异常日志分析

#### 日志 A：没有看到"Syncing"
```
[AchievementSystem] Updated achievement 'first_defense': 0 + 1 -> 1/1
【缺少】[AchievementSystem] Syncing achievement...
```
**原因**：LobbyManager 未设置或为 null  
**检查**：Game1.Initialize() 中是否有 SetLobbyManager 调用

#### 日志 B：看到"Syncing"但服务器无响应
```
[AchievementSystem] Syncing achievement 'first_defense' to server
【没有】[Server] HandleUpdateAchievement called...
```
**原因**：网络连接问题或消息未正确序列化  
**检查**：
1. 客户端连接状态
2. UpdateAchievementRequest 的序列化
3. MessageType.UpdateAchievement 的定义

#### 日志 C：同步失败异常
```
[AchievementSystem] Failed to sync achievement to server: Connection refused
```
**原因**：服务器未运行或网络不可达  
**检查**：
```bash
# 检查服务器是否运行
netstat -tlnp | grep 7777

# 检查连接
telnet localhost 7777
```

## 网络调试

### 检查连接状态
```csharp
// 在 Game1 或测试代码中添加
public void DebugAchievementNetwork()
{
    Console.WriteLine($"LobbyManager Connected: {_lobbyManager.IsConnected}");
    Console.WriteLine($"LobbyManager Authenticated: {_lobbyManager.IsAuthenticated}");
    Console.WriteLine($"LobbyManager State: {_lobbyManager.CurrentState}");
    
    // 尝试手动同步
    _ = _lobbyManager.UpdateAchievementAsync("first_defense", 1);
}
```

### 网络监听（Linux）
```bash
# 监听 localhost:7777
tcpdump -i lo -n 'tcp port 7777'

# 查看 TCP 连接状态
netstat -tnap | grep -E "(LISTEN|ESTABLISHED|7777)"

# 使用 nc 测试连接
nc -zv localhost 7777
```

### Wireshark 分析
1. 启动 Wireshark
2. 选择 Loopback 接口
3. 过滤器：`tcp.port == 7777`
4. 查看 UpdateAchievementRequest 消息

## 服务器端调试

### 启用详细日志
在 GameServer.cs 的 HandleUpdateAchievementAsync 中：
```csharp
Console.WriteLine($"[Server] DEBUG: Message type: {message.MessageType}");
Console.WriteLine($"[Server] DEBUG: Message data: {JsonSerializer.Serialize(request)}");
```

### 检查 AchievementManager 状态
```csharp
// 在命令行或测试中
var stats = _achievementManager.GetCompletionStats("admin");
Console.WriteLine($"Achievements: {stats.completed}/{stats.total} completed");
```

## 性能调试

### 异步任务监控
```csharp
// 在 AchievementSystem.UpdateProgress 中
var taskStartTime = DateTime.UtcNow;
_ = Task.Run(async () =>
{
    try
    {
        var elapsed = DateTime.UtcNow - taskStartTime;
        Console.WriteLine($"[Perf] Sync task started after {elapsed.TotalMilliseconds:F2}ms");
        
        await _lobbyManager.UpdateAchievementAsync(achievementId, progressDelta);
        
        elapsed = DateTime.UtcNow - taskStartTime;
        Console.WriteLine($"[Perf] Sync completed in {elapsed.TotalMilliseconds:F2}ms");
    }
    catch (Exception ex)
    {
        // ...
    }
});
```

### 内存监控
```csharp
// 检查是否有内存泄漏
var before = GC.GetTotalMemory(true);
for (int i = 0; i < 1000; i++)
{
    _achievementSystem.UpdateProgress("first_defense", 1);
}
var after = GC.GetTotalMemory(true);
Console.WriteLine($"Memory diff: {(after - before) / 1024}KB");
```

## 测试用例

### 测试 1：基本同步
```csharp
[Test]
public void TestBasicAchievementSync()
{
    // 1. 初始化
    var lobbyManager = new MultiplayerLobbyManager();
    var achievementSystem = new AchievementSystem(inventoryManager);
    achievementSystem.SetLobbyManager(lobbyManager);
    
    // 2. 更新成就
    achievementSystem.UpdateProgress("first_defense", 1);
    
    // 3. 等待异步完成
    Task.Delay(100).Wait();
    
    // 4. 验证
    Assert.AreEqual(1, achievementSystem.GetAchievement("first_defense")?.Progress);
}
```

### 测试 2：网络不可用
```csharp
[Test]
public void TestAchievementSyncNetworkUnavailable()
{
    var achievementSystem = new AchievementSystem(inventoryManager);
    // 不设置 LobbyManager
    
    achievementSystem.UpdateProgress("first_defense", 1);
    
    // 应该在本地更新成功，但记录警告
    Assert.AreEqual(1, achievementSystem.GetAchievement("first_defense")?.Progress);
}
```

## 常见陷阱

### 陷阱 1：忘记在 Initialize 中调用 SetLobbyManager
**表现**：成就不同步，但没有错误  
**解决**：在 Game1.Initialize() 中添加调用

### 陷阱 2：SetLobbyManager 调用过早
**表现**：_lobbyManager 还未初始化导致 null  
**解决**：确保在 `_lobbyManager = new MultiplayerLobbyManager()` 之后

### 陷阱 3：假设 Task.Run 会立即执行
**表现**：测试中同步调用和异步调用顺序混乱  
**解决**：测试中需要等待异步任务完成

### 陷阱 4：异常被吞掉
**表现**：没有看到任何错误日志，但同步失败  
**解决**：异常已被捕获并记录，检查 `Failed to sync achievement` 日志

## 参考资源

- [AchievementSystem.cs](EonVientiane/AchievementSystem.cs) - 核心实现
- [Game1.cs](EonVientiane/Game1.cs) - 初始化
- [LobbyManager.cs](EonVientiane/Network/LobbyManager.cs) - 网络层
- [GameServer.cs](EonVientianeServer/GameServer.cs) - 服务器处理

## 获取帮助

### 收集调试信息
```bash
# 1. 获取完整日志
./start_local_test.sh 2>&1 | tee debug.log

# 2. 过滤成就相关日志
grep -i achievement debug.log

# 3. 查看完整错误信息
grep -E "(ERROR|Exception|Failed)" debug.log
```

### 提交 Issue
如果问题无法自行解决，请提供：
1. 完整的日志输出
2. 成就 ID 和 delta 值
3. 客户端和服务器的版本
4. 网络配置（本地/远程）
5. 重现步骤

---
**最后更新**：2024-02-05  
**维护者**：@copilot  
**版本**：1.0
