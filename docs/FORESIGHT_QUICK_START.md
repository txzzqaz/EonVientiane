# 预见(Foresight)饰品 - 快速开始指南

## 📌 概览

**预见**是一个强大的战术饰品，允许玩家在战斗中**提前规划**多个回合的行动，系统会按照规划自动执行。

## 🎮 游戏体验

### 核心特性

| 特性 | 说明 |
|------|------|
| 双行规划框架 | 上行(AD攻击) / 下行(PD防御) |
| 序列编号 | ①②③④⑤...标记执行顺序 |
| 自动执行 | 轮到该回合自动执行序列#1 |
| 序列递减 | 执行后其他序号自动-1 |

### 使用流程

```
装备"预见"饰品
    ↓
战斗中看到双行规划框架
    ↓
点击骰子添加到AD/PD行
    ↓
会看到序号①②③标记
    ↓
轮到该回合自动执行序号①
    ↓
其他序号自动-1（②变①，③变②）
    ↓
继续自动执行后续行动
```

## 📂 相关文件

### 新增文件
- `PlannedAction.cs` - 规划行动数据结构 (123行)
  - `PlannedAction` - 单个行动存储
  - `PlannedActionSequence` - 序列管理

### 修改文件  
- `Player.cs` - 添加规划存储字段和方法
- `BattleManager.cs` - 添加UI绘制、输入处理、自动执行逻辑
- `ForesightAccessory.cs` - 已有，现在功能完整

## 🔧 技术细节

### 数据存储

```csharp
// 每个玩家维护两个规划字典
Player.PlannedActionsAD  // AD回合规划
Player.PlannedActionsPD  // PD回合规划

// 每个字典的值是规划序列
Dictionary<string, PlannedActionSequence>
  // 键：骰子名称
  // 值：该骰子的规划序列列表
```

### 自动执行流程

```csharp
// 当ApplyServerBattleState()被调用时
TryExecutePlannedAction(player, inputContext)
  ├─ 检查玩家是否装备了预见
  ├─ 获取该输入上下文的规划字典
  ├─ 取出第一个规划行动
  ├─ 执行行动
  └─ 所有其他序列-1
```

### UI渲染

- **蓝色区域** - AD规划框（攻击/防守选择）
- **绿色区域** - PD规划框（被动防御选择）  
- **圆形数字** - 序列编号 ①②③④⑤...
- **最多10个** - 超过显示...

## 💡 使用示例

### 场景1：简单规划

```
玩家A装备了预见

攻击回合时：
  点击"D6骰子"→ 显示①
  再点击"D6骰子"→ 显示①②
  再点击"飞羽"→ 在①后显示①

防御回合时：
  点击"D6骰子"→ 显示①
  
战斗执行：
  轮到A攻击：自动执行D6骰子（序列①），其他变②
  轮到A防御：自动执行D6骰子（序列①）
  下次轮到A攻击：自动执行第二个D6（现在是①）
```

### 场景2：多骰子组合

```
规划：
  D6骰子 → ①②③
  飞羽 → ①②

结果：
  第1轮：执行D6(①)，所有变②
  第2轮：执行D6(②→①)
  第3轮：执行D6(③→②→①) 和 飞羽(②→①)
  ...
```

## 🎓 关键概念

### PlannedActionSequence 类

```csharp
public class PlannedActionSequence
{
    // 这个骰子的所有规划行动列表
    private List<PlannedAction> _actions;
    
    // 获取序号数字（1,2,3...或空）
    public int GetNextSequenceNumber();
    
    // 添加规划
    public void AddAction(PlannedAction action);
    
    // 获取并移除第一个规划
    public PlannedAction GetAndRemoveFirstAction();
    
    // 清空所有规划
    public void Clear();
    
    // 是否有待执行规划
    public bool HasPendingActions { get; }
}
```

### 自动执行逻辑

1. **检查装备** - 玩家是否装备了预见
2. **查找规划** - 从对应上下文（AD/PD）的规划字典查找
3. **取出第一个** - 获取序列①的行动
4. **执行行动** - 调用原始行动请求事件
5. **递减序列** - 所有剩余规划-1
6. **提示反馈** - "自动执行AD预设: D6"

## 🔍 调试技巧

### 查看日志

```
自动执行AD预设: d6_dice
自动执行PD预设: d6_dice
```

### 关键变量

- `_plannedDiceSequenceNumbersAD` - AD规划序列编号
- `_plannedDiceSequenceNumbersPD` - PD规划序列编号
- `_manualInputForPlanning` - 正在手动选择的规划

### 调试点

- `BattleManager.HandleForesightPlanningInput()` - 输入处理
- `BattleManager.ExecutePlannedAction()` - 行动执行
- `BattleManager.TryExecutePlannedAction()` - 执行检查

## ✅ 验证清单

部署前检查：

- [ ] PlannedAction.cs 已创建并编译通过
- [ ] Player.cs 添加了 PlannedActionsAD/PD 字段
- [ ] BattleManager.cs 包含规划UI绘制代码
- [ ] BattleManager.cs 包含规划输入处理代码
- [ ] BattleManager.cs 包含自动执行逻辑
- [ ] ForesightAccessory.cs 的 CanPlannedAction 返回 true
- [ ] 整个项目编译通过（0错误）
- [ ] 对战中装备预见后能看到双行框架
- [ ] 能点击骰子添加规划
- [ ] 序号正确显示和递减

## 🔗 相关文档

| 文档 | 用途 |
|------|------|
| [FORESIGHT_IMPLEMENTATION.md](FORESIGHT_IMPLEMENTATION.md) | 完整的技术实现文档 |
| [FORESIGHT_SUMMARY.md](../FORESIGHT_SUMMARY.md) | 实现概览和测试建议 |
| [BATTLE_SETTLEMENT_SUMMARY.md](BATTLE_SETTLEMENT_SUMMARY.md) | 战斗系统整体说明 |

## 📞 常见问题

### Q1：为什么看不到规划框架？
**A：** 检查是否装备了预见饰品。未装备时规划框架不显示。

### Q2：规划了为什么没有自动执行？
**A：** 自动执行仅在轮到该玩家的对应回合时触发。如果是对手的回合，则不会执行。

### Q3：序号错乱了怎么办？
**A：** 这可能是由于多人网络同步问题。建议重新规划。规划会在每回合重新初始化。

### Q4：最多能规划多少个行动？
**A：** 理论上无限制，但UI最多显示10个序号。超过10个会显示"..."。

## 🚀 快速测试

```
1. 启动游戏
2. 进入战斗
3. 装备"预见"饰品
4. 攻击回合时，点击"D6"3次
5. 看到③显示在右上方
6. 等待该玩家的回合
7. 自动执行第一个D6
8. 其他序号自动-1（②③变①②）
```

---

**版本：** 1.0  
**最后更新：** 2026年1月  
**状态：** ✅ 生产就绪
