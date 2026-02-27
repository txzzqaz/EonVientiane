# 两个成就的完整触发逻辑 - 快速参考

## 成就概览

| 成就 | ID | 条件 | 奖励 |
|-----|-----|-----|-----|
| 我在哪？ | where_am_i | 装备漫游者之心且不触发增益 | 预知之晶 x1 |
| 刮痧 | guasha_master | 连续10回合只造成1点伤害 | 刮痧沥 x1 |

## 实现流程

### "我在哪？"成就

```
战斗流程：
  1. 玩家装备漫游者之心
  2. 战斗中每次造成伤害时检查：
     - ApplyWandererHeartMultiplier() 检查倍率
     - 如果倍率 > 1.0，标记 _playerWandererHeartTriggered[playerId] = true
  3. 战斗结束时：
     - WhereAmITrigger.GetEligiblePlayers()
       └─> ServerBattle.IsEligibleForWhereAmIAchievement()
           ├─ 检查是否装备漫游者之心 ✓
           └─ 检查是否触发过增益 → 如果没触发 ✓ 成就完成
```

**关键代码位置：**
- 增益触发标记：[ServerBattle.cs#L544-L548]
- 检查 API：[ServerBattle.cs#L1348-L1370]
- Trigger：[WhereAmITrigger.cs]

### "刮痧"成就

```
战斗流程：
  1. 战斗中每次造成伤害时：
     - ApplyDamage() 记录伤害值到 _playerDamageSequence[playerId]
     - 包括额外伤害（如刮痧骰子效果）
  2. 战斗结束时：
     - GuashaMasterTrigger.GetEligiblePlayers()
       └─ ServerBattle.IsEligibleForGuashaMasterAchievement()
           ├─ 获取伤害序列
           ├─ 检查总伤害次数 >= 10 ✓
           └─ 查找连续10个值都是1的子序列
              → 如果找到 ✓ 成就完成
```

**关键代码位置：**
- 伤害记录：[ServerBattle.cs#L727-L741]
- 检查 API：[ServerBattle.cs#L1373-L1408]
- Trigger：[GuashaMasterTrigger.cs]

## 关键数据结构

### ServerBattle 中的追踪字典

```csharp
// 漫游者之心增益标记
private Dictionary<string, bool> _playerWandererHeartTriggered;
// 格式：{ playerId -> 是否触发过增益 }

// 伤害序列
private Dictionary<string, List<int>> _playerDamageSequence;
// 格式：{ playerId -> [伤害值列表] }
// 示例：{ "player1" -> [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3] }
```

## 初始化

在 `ServerBattle` 构造函数中：

```csharp
_playerWandererHeartTriggered = new Dictionary<string, bool>();
_playerDamageSequence = new Dictionary<string, List<int>>();

// 为每个玩家初始化
foreach (var client in clients)
{
    _playerWandererHeartTriggered[client.UserId] = false;
    _playerDamageSequence[client.UserId] = new List<int>();
}
```

## 日志调试

### "我在哪？"相关日志

```
[WhereAmI Check] {PlayerName}未装备漫游者之心
[WhereAmI Check] {PlayerName}的漫游者之心触发过增益，不符合条件
[WhereAmI Check] {PlayerName}符合'我在哪？'成就条件
```

### "刮痧"相关日志

```
[GuashaMaster Check] {PlayerId}没有伤害记录
[GuashaMaster Check] 玩家总伤害次数{Count}次，不足10次
[GuashaMaster Check] 玩家{PlayerId}找到连续10次伤害都是1点的序列（位置{i}-{i+9}）
[GuashaMaster Check] 玩家{PlayerId}未找到连续10次伤害都是1点的序列
```

## 常见问题排查

### "我在哪？"不完成

1. 玩家未装备漫游者之心？
   → 查看日志：`未装备漫游者之心`

2. 漫游者之心触发了增益？
   → 查看日志：`的漫游者之心触发过增益`
   → 检查回合内所有行动的耗时

3. 装备器数据同步有问题？
   → 检查 `InitializeBattle` 中的装备应用代码

### "刮痧"不完成

1. 伤害次数不足10次？
   → 查看日志：`玩家总伤害次数{Count}次，不足10次`

2. 伤害值不全是1？
   → 查看伤害序列
   → 检查刮痧骰子的额外伤害是否也是1点

3. 伤害不连续？
   → 例如：[1,1,1,1,1, 2, 1,1,1,1,1]
   → 这样不符合条件（中间有2点）

## 修改检查清单

- [x] ServerBattle 添加追踪字典
- [x] ServerBattle 初始化追踪字典
- [x] ServerBattle 添加漫游者之心增益标记
- [x] ServerBattle 添加伤害序列记录
- [x] ServerBattle 添加 IsEligibleForWhereAmIAchievement API
- [x] ServerBattle 添加 IsEligibleForGuashaMasterAchievement API
- [x] WhereAmITrigger 实现 GetEligiblePlayers
- [x] WhereAmITrigger 实现 CalculateProgress
- [x] GuashaMasterTrigger 实现 GetEligiblePlayers
- [x] GuashaMasterTrigger 实现 CalculateProgress
- [x] 编译验证通过
- [x] 测试数据清理完成

## 测试命令

```bash
# 清理测试数据
./clear_test_data.sh <<< "y"

# 构建服务器
dotnet build EonVientianeServer/EonVientianeServer.csproj -c Debug

# 启动本地测试
./start_local_test.sh
```

## 相关文件

- [ACHIEVEMENT_BUG_FIX.md](ACHIEVEMENT_BUG_FIX.md) - 初始问题分析
- [ACHIEVEMENT_TRIGGER_IMPLEMENTATION.md](ACHIEVEMENT_TRIGGER_IMPLEMENTATION.md) - 实现细节
- [ACHIEVEMENT_FIX_COMPLETE.md](ACHIEVEMENT_FIX_COMPLETE.md) - 完成总结
