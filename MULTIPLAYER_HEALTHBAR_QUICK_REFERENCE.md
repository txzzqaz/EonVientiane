# 多人对战血条修复 - 快速参考

## 修复内容概述
解决了多人对战中血条重叠导致无法选择攻击目标的问题。

| 问题 | 修复方式 | 文件 | 行号 |
|------|--------|------|------|
| 血条只显示每队第一个玩家 | 循环显示所有活着玩家的血条 | BattleManager.cs | 1383-1401 |
| 血条相互重叠 | 添加垂直间距(35px) | BattleManager.cs | 1565, 1387-1396 |
| 只能选择第一个对手 | 更新碰撞检测支持所有对手 | BattleManager.cs | 1163-1181 |
| 目标选择不完整 | 高亮所有可攻击对手 | BattleManager.cs | 1750-1773 |

## 关键参数

### 血条间距
```csharp
int barSpacing = 35;  // 像素
```

### 血条尺寸
- 宽度 (barW): 300px
- 高度 (barH): 20px
- 点击区域额外宽度: 20px
- 点击区域额外高度: 40px

### 垂直偏移计算
```csharp
int verticalOffset = i * barSpacing;  // i为玩家在队伍内的索引
```

## 主要修改方法

### 1. DrawPlayerHealthBar
- 新增参数: `int verticalOffset = 0`
- 使用: `int adjustedBarTop = barTop + verticalOffset;`

### 2. Draw (主绘制方法)
```csharp
// 遍历显示所有活着的Team1玩家
var team1AlivePlayer = _currentBattle.Team1Players.Where(p => !p.IsDead).ToList();
for (int i = 0; i < team1AlivePlayer.Count; i++)
{
    int verticalOffset = i * barSpacing;
    DrawPlayerHealthBar(..., verticalOffset);
}
```

### 3. HandleOpponentSelection
- 支持多个对手的碰撞检测
- 正确处理垂直偏移的点击区域

### 4. DrawBattleActions
- 高亮所有可攻击的对手
- 维护完整的 `_opponentRects` 列表

## 测试清单

- [ ] 启动多人对战（至少2v2）
- [ ] 验证所有玩家血条显示且不重叠
- [ ] 点击不同对手的血条进行选择
- [ ] 选择AD骰子后，验证所有对手被黄色高亮
- [ ] 验证死亡玩家的血条隐藏
- [ ] 验证血条信息（玩家名称、HP、护盾）正确显示

## 编译命令

```bash
# 客户端
dotnet build EonVientiane/EonVientiane.csproj

# 服务器
dotnet build EonVientianeServer/EonVientianeServer.csproj
```

## 修改统计
- **文件数**: 1 (BattleManager.cs)
- **添加行**: 约 50 行
- **删除行**: 约 10 行
- **修改行**: 约 30 行
- **编译状态**: ✅ 成功
- **错误数**: 0
- **警告数**: 9 (预期的、不相关的)

## 相关类和属性

```csharp
// Battle.cs
public List<Player> Team1Players => AllPlayers.Where(p => p.Camp == PlayerCamp.Team1).ToList();
public List<Player> Team2Players => AllPlayers.Where(p => p.Camp == PlayerCamp.Team2).ToList();
public IReadOnlyList<Player> AvailableOpponents => _currentOpponents ?? _emptyPlayerList;

// Player.cs
public string PlayerId { get; set; }
public string PlayerName { get; set; }
public PlayerCamp Camp { get; set; }
public int CurrentHP { get; set; }
public int MaxHP { get; set; }
public int ShieldLayers { get; set; }
public bool IsDead => CurrentHP <= 0;
```

## 向后兼容性
✅ 完全向后兼容。所有修改都是增强性的，不改变现有接口。

## 已知限制
- 支持最多显示 8 个玩家（基于屏幕高度和35px间距）
- 如需更多玩家，考虑缩小 `barSpacing` 或使用分页显示

---
修复日期: 2026-02-08
