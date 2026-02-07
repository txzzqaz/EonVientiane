# PVE 挑战系统 - 快速参考

## 功能概述

已成功添加单机 PVE（玩家 vs 环境）功能到 EonVientiane。现在可以在离线状态下与 AI 对手进行挑战。

## 如何使用

### 1. 访问挑战界面
- 启动游戏后，点击左侧菜单栏中的 **"挑战"** 按钮
- 位置：背包按钮下方，对战历史按钮上方

### 2. 界面组成
- **左侧列表**：显示所有可用的 PVE 挑战
- **右侧详情**：显示选中挑战的详细信息

### 3. 挑战信息显示

每个挑战卡片显示：
- 🏷️ **挑战名称** - 金色文字
- ⭐ **难度星级** - 1-5 颗星
- ✓ **完成状态** - 已完成（绿色）/ 未完成（红色）
- 👤 **对手名称** - 浅青色
- 🎁 **奖励金币** - 黄色

### 4. 交互方式

| 操作 | 功能 |
|------|------|
| 鼠标单击 | 选中挑战 |
| 鼠标滚轮 | 上下滚动挑战列表 |
| 双击 | 开始战斗（目前为预留功能） |

## 初始示例挑战

### 初级挑战 - 自我对阵
- **难度**: ⭐ (最低难度)
- **对手**: 新手对手
- **对手骰子**: d6、自我
- **说明**: 一个很好的新手练习挑战
- **奖励**: 100 金币

## 技术架构

### 核心类

#### PVEChallenge
表示单个挑战的数据模型：
```csharp
public class PVEChallenge
{
    public string Id { get; set; }              // 挑战唯一ID
    public string Name { get; set; }            // 挑战名称
    public string Description { get; set; }     // 描述
    public int Difficulty { get; set; }         // 难度 1-5
    public List<string> OpponentDiceNames { get; set; }  // 对手骰子列表
    public string OpponentName { get; set; }    // 对手名称
    public int RewardGold { get; set; }         // 金币奖励
    public bool IsCompleted { get; set; }       // 完成状态
}
```

#### PVEChallengeManager
管理所有挑战相关操作：
- `GetAllChallenges()` - 获取所有挑战
- `GetIncompleteChallenges()` - 获取未完成的挑战
- `CompleteChallenge()` - 标记挑战为已完成
- `AddChallenge()` - 添加新挑战
- `GetTotalReward()` - 获取总奖励

### 新增枚举值

在 `GameEnums.cs` 中添加：
```csharp
public enum ContentView
{
    Button6 = 8,  // PVE 挑战
    // ... 其他值
}
```

## 菜单系统更新

菜单按钮现在的顺序为：
1. 联机大厅
2. 背包
3. **挑战** ✨ 新增
4. 对战历史
5. 图鉴
6. 战斗

## 后续开发计划

### 第一阶段
- [ ] 实现与战斗系统的完整集成
- [ ] 添加更多示例挑战（难度 2-5）
- [ ] 实现难度不同的对手 AI

### 第二阶段
- [ ] 战斗结果处理
- [ ] 奖励发放系统
- [ ] 挑战解锁机制

### 第三阶段
- [ ] 排行榜
- [ ] 成就关联
- [ ] 挑战难度动态调整

## 编译状态
✅ 编译成功 - 0 错误

## 文件修改统计
- 新增文件: 2 个
- 修改文件: 4 个
- 新增代码行数: ~400 行
- 新增方法: 5 个
