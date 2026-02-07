# PVE 挑战系统实现文档

## 概述
已成功添加单机 PVE（玩家vs环境）功能到 EonVientiane 项目中。

## 添加的功能

### 1. 核心类
- **PVEChallenge.cs** - PVE 挑战数据模型
  - 包含挑战 ID、名称、描述、难度等级
  - 包含对手信息（名称、骰子列表）
  - 包含奖励和完成状态

- **PVEChallengeManager.cs** - PVE 挑战管理器
  - 管理挑战列表
  - 跟踪已完成的挑战
  - 提供挑战查询和完成标记功能
  - 包含默认示例挑战

### 2. UI 界面
在 UIManager.cs 中添加了两个方法：
- **DrawPVEChallengePanel()** - 绘制 PVE 挑战列表界面
  - 显示所有可用挑战
  - 支持滚动
  - 显示挑战难度、完成状态、奖励等信息
  - 支持选中高亮显示

- **DrawPVEChallengeDetail()** - 绘制选中挑战的详情面板

### 3. 菜单集成
- 在菜单中添加了"挑战"按钮，位置在"背包"按钮下方
- 菜单按钮顺序：联机大厅 → 背包 → **挑战** → 对战历史 → 图鉴 → 战斗

### 4. 游戏状态管理
在 Game1.cs 中添加：
- `PVEChallengeManager _pveChallengeManager` - PVE 管理器实例
- `_pveScrollOffset` - 滚动偏移量
- `_selectedChallengeIndex` - 当前选中的挑战索引
- `ContentView.Button6` - 新的内容视图枚举值

### 5. 输入处理
添加了 `HandlePVEChallengeInput()` 方法处理：
- 鼠标左键点击选中挑战
- 鼠标滚轮滚动浏览挑战列表
- 双击启动战斗（预留接口）

### 6. 默认示例挑战
初始化了一个示例挑战：
- **ID**: pve_beginner_01
- **名称**: 初级挑战 - 自我对阵
- **难度**: 1 星（最低难度）
- **对手名称**: 新手对手
- **对手骰子**: d6、自我
- **奖励**: 100 金币

## 文件修改清单

### 新建文件
1. `/EonVientiane/PVEChallenge.cs` - PVE 挑战数据模型
2. `/EonVientiane/PVEChallengeManager.cs` - PVE 挑战管理器

### 修改文件
1. **GameEnums.cs**
   - 在 `ContentView` 枚举中添加 `Button6 = 8` 用于 PVE 挑战视图

2. **MenuManager.cs**
   - 修改菜单按钮标签数组，将"按钮3"改为"挑战"，"按钮4"改为"对战历史"

3. **UIManager.cs**
   - 添加 `DrawPVEChallengePanel()` 方法
   - 添加 `DrawPVEChallengeDetail()` 方法

4. **Game1.cs**
   - 添加 PVEChallengeManager 实例和相关状态变量
   - 修改菜单点击处理逻辑，使用 switch 语句处理所有按钮
   - 添加 ContentView.Button6 的输入处理
   - 添加 ContentView.Button6 的界面绘制
   - 添加 `HandlePVEChallengeInput()` 方法
   - 添加 `StartPVEBattle()` 方法（预留接口）

## 界面特性
- **背景色**: 深蓝色 (MidnightBlue)
- **挑战列表显示**:
  - 挑战名称（金色）
  - 难度星级标记
  - 完成状态（✓ 已完成 / 未完成）
  - 对手名称和奖励金币数

- **颜色标记**:
  - 已完成挑战: 深绿色背景
  - 未完成挑战: 深蓝色背景
  - 选中挑战: 亮绿色高亮

## 下一步集成计划
1. 与战斗系统集成 - 实现 `StartPVEBattle()` 方法
2. 添加更多示例挑战
3. 实现难度等级不同的对手 AI
4. 添加战斗结果处理和奖励发放
5. 添加挑战解锁系统

## 编译状态
✅ 编译成功 - 0 错误，7 个警告（均为既有代码的警告）
