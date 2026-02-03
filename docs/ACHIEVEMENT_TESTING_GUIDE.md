# 成就系统通信测试指南

## 快速测试清单

### 1. 基础通信测试

#### 测试1.1: 获取成就列表
- **前置条件**: 客户端已登录
- **操作步骤**:
  1. 在Game1中调用 `await _lobbyManager.GetAchievementsAsync();`
  2. 观察日志输出
- **预期结果**:
  ```
  [LobbyManager] Requesting achievements for user {userId}
  [Server] HandleGetAchievements called for user '{userId}'
  [Server] Retrieved {count} achievements for user '{userId}'
  [LobbyManager] Received {count} achievements from server
  ```
- **验证方式**:
  - 检查返回的成就列表不为空
  - 验证每个成就都有完整的信息(Id, Name, Description, Icon, Progress等)
  - 检查AchievementsReceived事件被触发

#### 测试1.2: 更新成就进度
- **前置条件**: 客户端已获取成就列表
- **操作步骤**:
  1. 调用 `await _lobbyManager.UpdateAchievementAsync("first_defense", 1);`
  2. 观察日志输出
- **预期结果**:
  ```
  [LobbyManager] Updating achievement 'first_defense' with delta 1
  [Server] HandleUpdateAchievement called for user '{userId}', achievement 'first_defense', delta 1
  [Server] User '{userId}' progressed achievement 'first_defense' from 0 to 1/1
  [Server] Achievement update response sent. Completed: true, Progress: 1
  ```
- **验证方式**:
  - 检查UpdateAchievementResponse成功
  - 如果完成，验证IsCompleted=true
  - 检查返回的Progress与预期一致

#### 测试1.3: 成就完成通知
- **前置条件**: 测试1.2通过，成就已完成
- **操作步骤**:
  1. 继续观察前一个测试的日志
- **预期结果**:
  ```
  [Server] User '{userId}' completed achievement 'first_defense' with {count} rewards
  [LobbyManager] Achievement completed: {name} (ID: {id})
  [LobbyManager] Rewards count: {count}
  [Client] Server achievement completed: {name} (ID: {id})
  [Client] Reward: Item {itemId} x{quantity}
  ```
- **验证方式**:
  - 检查AchievementCompleted事件被触发
  - 验证通知中包含正确的成就ID和名称
  - 验证奖励列表不为空且内容正确

### 2. 数据同步测试

#### 测试2.1: 本地缓存验证
- **操作步骤**:
  1. 登录后获取成就列表
  2. 检查客户端缓存
- **验证代码**:
  ```csharp
  var allAchievements = _achievementSystem.GetAllAchievements();
  var completionStats = _achievementSystem.GetCompletionStats();
  Console.WriteLine($"Total: {completionStats.total}, Completed: {completionStats.completed}");
  ```
- **预期结果**:
  - 缓存中包含所有从服务器下发的成就
  - 完成度统计正确

#### 测试2.2: 状态一致性验证
- **操作步骤**:
  1. 完成若干成就
  2. 调用验证方法:
     ```csharp
     bool isValid = _achievementSystem.ValidateSyncState();
     ```
- **预期结果**:
  - 如果一致: 返回true，日志显示"validation passed"
  - 如果不一致: 返回false，日志显示具体的不一致项

#### 测试2.3: 修改检测
- **操作步骤**:
  1. 更新一些成就进度
  2. 调用:
     ```csharp
     var modified = _achievementSystem.GetModifiedAchievements();
     ```
- **预期结果**:
  - 返回列表包含所有修改过的成就ID

### 3. 错误处理测试

#### 测试3.1: 未认证请求
- **操作步骤**:
  1. 在未登录状态下调用 `GetAchievementsAsync()`
- **预期结果**:
  ```
  [LobbyManager] Cannot get achievements: not authenticated
  ```
- **验证方式**:
  - 确保没有发送网络请求

#### 测试3.2: 无效的成就ID
- **操作步骤**:
  1. 调用 `UpdateAchievementAsync("invalid_id", 1)`
- **预期结果**:
  ```
  [Server] Achievement 'invalid_id' not found for user '{userId}'
  [LobbyManager] Update achievement failed: 成就'invalid_id'不存在
  ```
- **验证方式**:
  - 检查ErrorOccurred事件被触发
  - 验证错误信息准确

#### 测试3.3: 已完成的成就更新
- **操作步骤**:
  1. 再次更新已完成的成就
  2. 调用 `UpdateAchievementAsync("first_defense", 1)`(假设已完成)
- **预期结果**:
  ```
  [Server] Achievement 'first_defense' already completed for user '{userId}'
  [LobbyManager] Achievement updated successfully. IsCompleted: true, Progress: 1
  ```
- **验证方式**:
  - 确保返回success=true
  - IsCompleted仍为true
  - Progress不会超过RequiredProgress

### 4. 网络异常测试

#### 测试4.1: 网络超时
- **操作步骤**:
  1. 关闭服务器
  2. 尝试获取成就列表
- **预期结果**:
  - 客户端应该检测到超时
  - 触发ErrorOccurred事件
  - 显示用户友好的错误消息

#### 测试4.2: 连接丢失后恢复
- **操作步骤**:
  1. 建立连接并获取成就列表
  2. 断开连接(模拟网络丢失)
  3. 重新连接
  4. 验证状态一致性
- **验证方式**:
  ```csharp
  bool isValid = _achievementSystem.ValidateSyncState();
  if (!isValid) await _lobbyManager.GetAchievementsAsync(); // 重新同步
  ```

### 5. UI集成测试

#### 测试5.1: 成就列表UI
- **操作步骤**:
  1. 进入成就面板
  2. 观察是否显示所有成就
  3. 检查显示的信息是否完整
- **验证项目**:
  - 成就名称、描述、图标是否显示
  - 进度条是否正确
  - 已完成的成就是否标记
  - 奖励是否显示

#### 测试5.2: 成就完成提示
- **操作步骤**:
  1. 执行导致成就完成的操作
  2. 观察是否显示完成提示
- **验证项目**:
  - 提示窗口是否出现
  - 成就名称是否正确
  - 奖励显示是否正确
  - 特效是否播放

#### 测试5.3: 背包更新
- **操作步骤**:
  1. 完成成就获得奖励
  2. 检查背包中的物品
- **验证项目**:
  - 奖励物品是否出现在背包中
  - 数量是否正确
  - 物品属性是否正确

### 6. 压力测试

#### 测试6.1: 快速多次更新
- **操作步骤**:
  ```csharp
  for (int i = 0; i < 10; i++)
  {
      await _lobbyManager.UpdateAchievementAsync("achievement_id", 1);
      await Task.Delay(100); // 100ms延迟
  }
  ```
- **验证项目**:
  - 所有更新都被处理
  - 没有数据丢失
  - 最终状态正确

#### 测试6.2: 大量成就处理
- **前置条件**: 模拟大量成就(20+)
- **操作步骤**:
  1. 获取所有成就
  2. 更新多个成就
- **验证项目**:
  - 响应时间是否可接受
  - 内存使用是否合理
  - 没有明显的性能下降

### 7. 端到端测试场景

#### 场景1: 新玩家首次游戏
1. 新玩家注册和登录
2. 获取初始成就列表
3. 完成第一场战斗
4. 触发"first_victory"成就
5. 获得奖励物品
6. 刷新背包验证

**预期结果**: 整个流程无错误，用户能看到成就完成和奖励

#### 场景2: 多成就同时进行
1. 玩家登录
2. 获取成就列表
3. 完成多场战斗
4. 同时更新多个相关成就(battle_count, victory_count等)
5. 其中一个成就完成
6. 验证只有完成的成就发送通知

**预期结果**: 各个成就独立更新，完成的成就正确通知

#### 场景3: 连接不稳定的网络
1. 在网络波动的环境中游戏
2. 更新成就
3. 发生连接中断
4. 重新连接
5. 继续游戏

**预期结果**: 系统正确处理连接问题，成就状态最终一致

## 测试报告模板

### 测试环境
- OS: [Windows/Linux]
- .NET版本: [版本号]
- 游戏版本: [版本号]
- 服务器状态: [运行中/已停止]

### 测试结果

| 测试项 | 状态 | 备注 |
|-------|------|------|
| 测试1.1 | ✓/✗ | |
| 测试1.2 | ✓/✗ | |
| 测试1.3 | ✓/✗ | |
| ... | | |

### 问题列表

| 问题ID | 描述 | 严重程度 | 状态 |
|--------|------|---------|------|
| BUG001 | 描述 | High/Medium/Low | Open/Fixed |
| ... | | | |

### 签名

测试员: _____________ 日期: _____________
审核员: _____________ 日期: _____________

## 自动化测试脚本示例

```csharp
public class AchievementSystemTests
{
    [TestMethod]
    public async Task TestGetAchievements()
    {
        // Arrange
        var client = new NetworkClient();
        var lobbyManager = new LobbyManager(client);
        await lobbyManager.LoginAsync("testuser", "password");

        // Act
        await lobbyManager.GetAchievementsAsync();

        // Assert
        Assert.IsTrue(lobbyManager.IsAuthenticated);
        // 需要等待异步操作完成
        await Task.Delay(500);
    }

    [TestMethod]
    public async Task TestUpdateAchievement()
    {
        // Arrange
        var achievementSystem = new AchievementSystem(inventoryManager);
        achievementSystem.SetUserId("testuser");

        // Act
        achievementSystem.UpdateProgress("first_defense", 1);

        // Assert
        var achievement = achievementSystem.GetAchievement("first_defense");
        Assert.IsNotNull(achievement);
        Assert.AreEqual(1, achievement.Progress);
    }

    [TestMethod]
    public void TestValidateSyncState()
    {
        // Arrange
        var achievementSystem = new AchievementSystem(inventoryManager);

        // Act
        bool isValid = achievementSystem.ValidateSyncState();

        // Assert
        Assert.IsTrue(isValid);
    }
}
```

## 日志分析

### 关键日志模式

#### 成功的请求
```
[LobbyManager] Requesting achievements for user {userId}
[Server] HandleGetAchievements called for user '{userId}'
[Server] Retrieved {count} achievements for user '{userId}'
[LobbyManager] Received {count} achievements from server
```

#### 错误的请求
```
[Server] UpdateAchievementRequest is null
[Server] Error in HandleUpdateAchievementAsync: {error}
[LobbyManager] Update achievement failed: {errorMsg}
```

#### 网络问题
```
[NetworkClient] Connection timeout
[LobbyManager] Cannot send message: not connected
[Client] Attempting to reconnect...
```

## 调试建议

1. **启用详细日志**:
   - 在开发环境中启用所有Console.WriteLine输出
   - 使用Trace或Logger级别为DEBUG

2. **使用断点**:
   - 在HandleGetAchievementsResponse处设置断点
   - 在UpdateProgress处设置断点
   - 在SyncWithServer处设置断点

3. **检查网络流量**:
   - 使用Fiddler或类似工具
   - 验证消息序列化和反序列化

4. **验证数据库**:
   - 检查服务器端的成就存储
   - 验证持久化是否正确

## 常见问题排查

### 问题: 成就列表为空
- 检查用户是否已创建初始成就
- 验证GetUserAchievements是否返回数据
- 检查网络连接

### 问题: 成就无法完成
- 检查RequiredProgress的值
- 验证Progress是否正确更新
- 检查是否已经完成

### 问题: 奖励未发放
- 检查GetCompletionRewards是否返回数据
- 验证奖励物品是否在系统中定义
- 检查InventoryManager是否工作正常

### 问题: 状态不一致
- 调用ValidateSyncState()检查
- 触发完整同步: GetAchievementsAsync()
- 检查服务器和客户端的时钟同步
