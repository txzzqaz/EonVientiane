# 成就系统同步调试指南

## 症状检查表

### 之前的症状（修复前）
- [ ] 客户端显示成就更新日志
- [ ] 服务器没有显示"progressed"日志
- [ ] 服务器没有显示"completed"日志
- [ ] 客户端的成就列表显示进度，但服务器的成就列表不更新

### 修复后的预期表现
- [ ] 客户端显示 `[AchievementSystem] Syncing achievement 'first_defense' to server`
- [ ] 服务器显示 `[Server] User 'xxx' progressed achievement 'first_defense' from 0 to 1/1`
- [ ] 当进度达到要求时，服务器显示 `[Server] User 'xxx' completed achievement 'first_defense'`
- [ ] 客户端收到完成通知并显示奖励

## 日志追踪流程

### 1. 战斗结束阶段
```
[Client] Battle ended with result: Win
[Client] Generating battle statistics...
```

### 2. 客户端成就更新
```
[Client] Achievement progress updated: first_defense (blocked damage: XX)
[AchievementSystem] Updated achievement 'first_defense': 0 + 1 -> 1/1 (第一次防御)
[AchievementSystem] Achievement 'first_defense' progress target reached!
```

### 3. 网络同步（新增）
```
[AchievementSystem] Syncing achievement 'first_defense' to server
[LobbyManager] Updating achievement 'first_defense' with delta 1
```

### 4. 服务器处理
```
[Server] HandleUpdateAchievement called for user 'admin', achievement 'first_defense', delta 1
[Server] User 'admin' progressed achievement 'first_defense' from 0 to 1/1
[Server] User 'admin' completed achievement 'first_defense' (第一次防御)
```

### 5. 客户端接收完成通知
```
[Server] User 'admin' completed 'first_defense' with 1 rewards
```

## 常见问题排查

### 问题 1：没有看到"Syncing achievement"日志
**可能原因**：
- `_lobbyManager` 没有被正确传递
- 网络连接断开

**解决方案**：
1. 检查 Game1.Initialize() 中是否调用了 `SetLobbyManager()`
2. 检查客户端是否已连接到服务器
3. 查看是否有异常导致 Task 失败

### 问题 2：服务器没有响应
**可能原因**：
- 服务器的 `HandleUpdateAchievementAsync` 没有被调用
- 消息类型不匹配

**解决方案**：
1. 检查 `UpdateAchievementRequest` 是否被正确构建
2. 检查 `MessageType.UpdateAchievement` 是否正确
3. 查看服务器日志中是否有异常

### 问题 3：成就数据不一致
**可能原因**：
- 同步延迟
- 客户端和服务器的初始化数据不同

**解决方案**：
1. 等待一段时间（网络延迟）
2. 调用 `GetAchievementsAsync()` 重新同步
3. 检查 `ValidateSyncState()` 的输出

## 代码位置参考

| 组件 | 位置 | 关键方法 |
|------|------|---------|
| 客户端成就系统 | `EonVientiane/AchievementSystem.cs` | `UpdateProgress()`, `SetLobbyManager()` |
| 客户端游戏主类 | `EonVientiane/Game1.cs` | `Initialize()` |
| 网络管理 | `EonVientiane/Network/LobbyManager.cs` | `UpdateAchievementAsync()` |
| 服务器处理 | `EonVientianeServer/GameServer.cs` | `HandleUpdateAchievementAsync()` |
| 服务器成就管理 | `EonVientianeServer/AchievementManager.cs` | `UpdateAchievementProgress()` |

## 完整测试流程

### 1. 启动测试环境
```bash
./start_local_test.sh
```

### 2. 登录两个客户端
- 客户端1：用户 "admin" 或 "user"
- 客户端2：用户 "test"

### 3. 创建房间并开始战斗
- 一个用户创建房间
- 另一个用户加入房间
- 开始战斗

### 4. 触发 first_defense 成就
使用飞羽骰子进行防守/闪避，确保 `TotalDamageBlocked > 0`

### 5. 查看日志输出
**客户端**：
```
[Client] Achievement progress updated: first_defense (blocked damage: XX)
[AchievementSystem] Syncing achievement 'first_defense' to server
```

**服务器**：
```
[Server] HandleUpdateAchievement called for user 'xxx', achievement 'first_defense', delta 1
[Server] User 'xxx' completed achievement 'first_defense' (第一次防御)
```

## 性能注意事项

- 异步同步不会阻塞客户端主线程 ✓
- 使用 `Task.Run()` 在线程池中执行 ✓
- 网络延迟由 LobbyManager 内部处理 ✓

## 相关成就（同时修复）

以下成就现在都能正确同步：
1. `first_defense` - 进行防守时 +1
2. `perfect_victory` - 绝对碾压时 +1
3. `blitz_victory` - 5秒内获胜时 +1
4. `where_am_i` - 每次击杀 +1
5. `long_thinking` - 累积对手思考时间（秒）
6. `guasha_master` - 连续造成1点伤害的回合数
7. `miracle` - 飞羽连续闪避成功次数
8. `absolute_luck` - 连胜次数

## 验证清单

完成以下检查，确认修复有效：

- [ ] 代码成功编译（无错误）
- [ ] 本地测试启动成功
- [ ] 客户端成就更新时显示"Syncing"日志
- [ ] 服务器接收并处理更新请求
- [ ] 成就完成时获得奖励物品
- [ ] 刷新成就列表显示最新状态
- [ ] 客户端和服务器的成就状态一致

---
**修复版本**：Achievement Sync Fix v1.0
**测试日期**：[待测试]
**状态**：Ready for Testing ✓
