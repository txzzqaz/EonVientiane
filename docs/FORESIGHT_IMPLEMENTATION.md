# 预见（Foresight）饰品 - 规划系统实现文档

## 概述

成功实现了"预见"饰品的完整规划系统。该系统允许玩家在装备"预见"饰品后，在战斗中提前规划自己的行动，并在对应回合自动执行。

## 功能描述

### 核心机制

当玩家装备"预见"饰品时：

1. **双行规划框始终可见**
   - AD规划框（上行）：用于提前规划攻击防守回合的行动
   - PD规划框（下行）：用于提前规划防守响应回合的行动

2. **规划流程**
   - 玩家可以在两个规划框中点击骰子，添加到规划队列
   - 每个骰子支持多个序号（最多10个显示）
   - 序号以圆圈数字（①②③④⑤...）标注在骰子右上角

3. **自动执行**
   - 轮到对应的行动回合时，系统自动执行序号1的行动
   - 其他序号自动递减（序号2变成1，序号3变成2，等等）

## 技术实现

### 新增类

#### PlannedAction.cs
- `PlannedAction`：代表单个计划行动的数据类
  - `DiceName`: 骰子名称
  - `TargetPlayerId`: 目标玩家ID（可选）
  - `CustomValue`: 自定义点数值（可选）
  - `CreatedTick`: 创建时间戳

- `PlannedActionSequence`：管理一个骰子的多个计划行动
  - `DiceName`: 骰子名称
  - `Actions`: 按顺序存储的行动列表
  - 提供方法：
    - `GetNextSequenceNumber()`: 获取下一个序号
    - `AddAction()`: 添加行动
    - `RemoveActionAt()`: 移除指定序号的行动
    - `GetAndRemoveFirstAction()`: 获取并移除第一个行动
    - `Clear()`: 清空所有行动

### 修改的类

#### Player.cs

新增属性：
- `PlannedActionsAD`: Dictionary<string, PlannedActionSequence> - 存储AD回合的规划
- `PlannedActionsPD`: Dictionary<string, PlannedActionSequence> - 存储PD回合的规划

新增方法：
- `AddPlannedActionAD()`: 为AD回合添加计划行动
- `AddPlannedActionPD()`: 为PD回合添加计划行动
- `GetNextPlannedActionAD()`: 获取AD回合的下一个计划行动
- `GetNextPlannedActionPD()`: 获取PD回合的下一个计划行动
- `ClearAllPlannedActions()`: 清空所有计划行动
- `HasForesightAccessory()`: 检查是否装备"预见"饰品

#### BattleManager.cs

新增字段：
- `_plannedDiceSequenceNumbersAD`: 存储AD行的序号显示
- `_plannedDiceSequenceNumbersPD`: 存储PD行的序号显示
- `_manualInputForPlanning`: 标记当前手动输入是否用于规划
- `_manualInputForPlanningAD`: 标记手动输入是否为AD规划

新增方法：
- `HandleForesightPlanningInput()`: 处理规划框的点击输入
- `DrawForesightPlannedActions()`: 绘制双行规划框和序号
- `UpdatePlannedActionSequenceNumbers()`: 更新序号显示
- `AddPlannedAction()`: 添加一个计划行动
- `TryExecutePlannedAction()`: 尝试自动执行预设行动
- `ExecutePlannedAction()`: 执行单个预设行动
- `HasForesightAccessory()`: 检查是否装备预见

修改的方法：
- `Draw()`: 添加规划框的绘制调用
- `HandleInput()`: 添加规划输入处理
- `ConfirmManualInput()`: 支持规划系统的手动输入
- `ClearManualInputState()`: 清除规划相关状态
- `ApplyServerBattleState()`: 在需要输入时尝试自动执行规划

## 使用流程

### 玩家操作
1. 装备"预见"饰品
2. 在战斗中看到两行规划框
3. 点击AD框中的骰子，将其添加到AD规划队列
4. 点击PD框中的骰子，将其添加到PD规划队列
5. 需要手动输入点数的骰子会弹出输入框
6. 在对应回合到达时，系统自动执行序号1的规划行动

### 内部执行流程
1. 服务器通知客户端轮到玩家行动
2. `ApplyServerBattleState()` 收到行动请求
3. `TryExecutePlannedAction()` 检查是否有预设行动
4. 如果有，`ExecutePlannedAction()` 自动发送行动请求给服务器
5. 序号自动递减，等待下一个回合

## 用户界面

### 规划框显示

**AD规划框（蓝色，上行）**
```
┌─────────────────────────────────────────────────┐
│ AD规划（提前规划攻击防守）                        │
│ [骰子1] [骰子2] [骰子3] ...                      │
│  ②③     ①      ①②                             │
└─────────────────────────────────────────────────┘
```

**PD规划框（绿色，下行）**
```
┌─────────────────────────────────────────────────┐
│ PD规划（提前规划防守响应）                        │
│ [骰子1] [骰子2] [骰子3] ...                      │
│  ①      ①②③    ①                              │
└─────────────────────────────────────────────────┘
```

序号使用圆圈数字（①②③④⑤⑥⑦⑧⑨⑩）表示，最多显示10个。

## 特殊处理

### 需要选择目标的骰子
如果骰子需要选择对手目标，规划时会显示提示"需要选择目标进行AD规划"。

### 需要手动输入的骰子
如果骰子需要手动输入点数，会弹出输入框，完成输入后将规划加入队列。

### 防止重复执行
每次执行规划行动后，系统会自动移除该行动，并递减其他序号。

## 文件清单

### 新增文件
- `EonVientiane/PlannedAction.cs` - 规划行动数据类

### 修改的文件
- `EonVientiane/Player.cs` - 添加规划行动存储和管理方法
- `EonVientiane/BattleManager.cs` - 添加规划UI和输入处理逻辑
- `EonVientiane/Accessories/ForesightAccessory.cs` - CanPlannedAction属性现已被使用

## 测试建议

1. **基础功能测试**
   - [ ] 装备"预见"饰品后，规划框能否正常显示
   - [ ] 点击骰子能否正确添加到规划队列
   - [ ] 序号是否正确显示

2. **执行测试**
   - [ ] 在AD回合是否自动执行AD规划中序号1的骰子
   - [ ] 在PD回合是否自动执行PD规划中序号1的骰子
   - [ ] 执行后序号是否正确递减

3. **边界情况**
   - [ ] 多个骰子都有规划时，是否按正确顺序执行
   - [ ] 规划队列空时是否回到正常选择模式
   - [ ] 手动输入骰子的规划是否正常工作

4. **交互性**
   - [ ] 是否可以在战斗过程中持续添加新规划
   - [ ] 是否可以通过UI清除规划（如有此功能）
   - [ ] 战斗结束时规划是否正确清除

## 版本历史

- v1.0 (2024-02-04): 初始实现，完整支持双行规划框、序号显示和自动执行
