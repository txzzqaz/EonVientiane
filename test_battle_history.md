# 战斗历史记录测试计划

## 调试步骤

### 1. 启动服务器和客户端
```bash
./start_local_test.sh
```

### 2. 观察日志中的关键信息

服务器端应该显示：
- `[Server.Battle] EndBattle called` - 战斗结束被触发
- `[Server.Battle] IsBattleOver set to true` - 标记设置
- `[Server] ===== BroadcastBattleEndAsync: ...` - 广播开始
- `[Server] Sending BattleEnd to X players` - 消息发送

客户端端应该显示：
- `[Network.LobbyManager] HandleBattleEnd called` - 网络层收到消息
- `[Network.LobbyManager] Invoking BattleEnded event` - 事件触发
- `[Client] ========== Battle Ended ==========` - OnBattleEnded执行
- `[Client] Team1 Players: ...` - Team信息提取
- `[BattleHistoryManager] Battle record added` - 记录保存成功
- `[BattleHistoryManager] Battle history saved successfully` - 文件写入成功

### 3. 查看生成的文件
```bash
cat ~/.config/EonVientiane/BattleHistory/battle_history.json | jq '.'
```

## 预期结果

- 新战斗记录应该出现在文件中
- 记录应该包含Team1Players和Team2Players字段
- 记录的RecordId应该是新的（比之前的时间戳大）
