# 道具创建完整指南

## 概述
本文档描述了在EonVientiane游戏中创建新道具的完整流程和注意事项。遵循本指南可以确保新道具与游戏系统正确集成。

---

## 第一部分：道具分类

### 1. 骰子（Dice）- 战斗核心道具
**文件位置**: `EonVientiane/Dices/` 目录

#### 1.1 骰子类型分类
- **主动骰子(AD - Active Dice)**: 用于发动攻击
  - 示例: D6（普通六面骰）、春风、倒悬、刮痧师傅、要来咯
  
- **被动骰子(PD - Passive Dice)**: 用于防御/闪避
  - 示例: 飞羽、血痕、莫问、轮回、不动
  
- **双向骰子(Both)**: 既可攻击又可防御
  - 示例: D6（最基础的通用骰子）

#### 1.2 骰子属性与机制
- **DiceUsageType**: 骰子使用类型（Active/Passive/Both）
- **Roll()**: 基础掷骰方法，返回面数范围内的随机数
- **ExecuteActiveAction()**: 执行主动攻击逻辑
- **ExecutePassiveAction()**: 执行被动防御逻辑
- **Counter**: 某些骰子的计数器（如飞羽），游戏结束后清空
- **DisplayColor**: 骰子在UI上的显示颜色

#### 1.3 骰子关键数值
- **Attack**: 攻击力加成
- **Defense**: 防御力加成
- **Speed**: 速度加成
- **Health**: 生命值加成
- **Mana**: 魔力值加成

### 2. 饰品（Accessory）- 被动增益道具
**文件位置**: `EonVientiane/Accessories/` 目录

#### 2.1 饰品特点
- 提供被动属性加成
- 支持多个饰品同时装备（受饰品槽位限制）
- 可能在特定事件触发效果（如战斗开始、受伤等）

#### 2.2 饰品关键方法
- **OnBattleStart(BattleContext)**: 战斗开始时触发
- **OnHit()**: 受伤时触发
- **OnVictory()**: 获胜时触发
- **OnDefeat()**: 失败时触发

#### 2.3 饰品槽位系统
```
AccessorySlotsCost = 1;    // 正数：消耗的槽位数
AccessorySlotsCost = -1;   // 负数：提供额外槽位（提供1个槽位）
```

### 3. 消耗品（Consumable）
**示例**: 生命药水、魔力药水

#### 3.1 消耗品特点
- MaxStackSize通常较大（如99）
- 可以堆叠存放
- 用完后消失

### 4. 其他道具类型
- **材料(Material)**: 用于合成或交易
- **任务物品(Quest)**: 任务相关
- **其他(Other)**: 其他类型

---

## 第二部分：新道具创建流程

### 第1步：规划道具

#### 1.1 确定道具类型
- 是骰子还是饰品？
- 如果是骰子，是主动还是被动？
- 道具的游戏作用是什么？

#### 1.2 设计核心参数
```
道具名称: 
道具ID: (snake_case格式，例：feathered_dice)
创作者: 
描述: 
基础属性:
  - Attack:
  - Defense:
  - Speed:
  - Health:
  - Mana:
```

### 第2步：创建道具类

#### 2.1 创建骰子类
**示例模板** (继承Dice类):
```csharp
namespace EonVientiane;

/// <summary>
/// 骰子名称 - 类型(AD/PD)
/// 核心效果描述
/// </summary>
public class YourDiceName : Dice
{
    private Random _random;
    
    public YourDiceName()
        : base("your_dice_id", "Your Dice Name", "Motto", DiceUsageType.Active) // 或Passive/Both
    {
        _random = new Random();
        DisplayColor = Color.YourColor;
        Attack = 0;      // 根据实际设计调整
        Defense = 0;
        Speed = 0;
        Health = 0;
        Mana = 0;
    }
    
    public override int Roll()
    {
        return _random.Next(1, 7); // 根据设计调整面数
    }
    
    public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        // 实现主动攻击逻辑
        // 返回: new ActionResult(bool success, string message, Player target, int atkp)
    }
    
    public override DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        // 实现被动防御逻辑
        // 返回: new DefenseResult(int defp, int actualDamage, string message)
    }
    
    public override Item Clone()
    {
        return new YourDiceName()
        {
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Health = Health,
            Mana = Mana,
            DisplayColor = DisplayColor
        };
    }
}
```

#### 2.2 创建饰品类
**示例模板** (继承Accessory类):
```csharp
namespace EonVientiane;

/// <summary>
/// 饰品：名称
/// 效果描述
/// </summary>
public class YourAccessoryName : Accessory
{
    public YourAccessoryName()
        : base("your_accessory_id", "Your Accessory Name", "Description")
    {
        Attack = 0;      // 根据设计调整
        Defense = 0;
        Speed = 0;
        Health = 0;
        Mana = 0;
        DisplayColor = Color.YourColor;
        AccessorySlotsCost = 1;  // 调整槽位消耗
    }
    
    public override void OnBattleStart(BattleContext context)
    {
        // 战斗开始时的效果
    }
    
    public override Item Clone()
    {
        return new YourAccessoryName()
        {
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Health = Health,
            Mana = Mana,
            DisplayColor = DisplayColor
        };
    }
}
```

### 第3步：在ItemFactory中注册道具

**文件**: `EonVientiane/InventoryManager.cs`

在 `ItemFactory` 类的 `RegisterAllItems()` 方法中添加注册代码：

```csharp
private static void RegisterAllItems()
{
    // 骰子注册示例
    _registry.RegisterItem("your_dice_id", () => new YourDiceName());
    
    // 饰品注册示例
    _registry.RegisterItem("your_accessory_id", () => new YourAccessoryName());
}
```

### 第4步：在ItemInitializer中添加道具信息

**文件**: `EonVientianeServer/ItemInitializer.cs`

#### 4.1 添加到GetAllItems()方法
```csharp
public static List<(string ItemId, string ItemName)> GetAllItems()
{
    return new List<(string ItemId, string ItemName)>
    {
        // 现有骰子...
        ("your_dice_id", "Your Dice Name"),
        
        // 现有饰品...
        ("your_accessory_id", "Your Accessory Name"),
        
        // 其他道具...
    };
}
```

#### 4.2 添加到CreateItemFromStackData()方法（如果需要）
```csharp
public static Equipment? CreateItemFromStackData(InventoryStackRecord stackData)
{
    return stackData.ItemId switch
    {
        "your_dice_id" => new YourDiceName(),
        "your_accessory_id" => new YourAccessoryName(),
        // ... 其他现有道具
        _ => null
    };
}
```

### 第5步：设置初始获取方式

#### 5.1 测试账号初始化
如果需要在测试账号中发放：在 `ItemInitializer.GetTestAccountInventory()` 中添加

#### 5.2 新用户初始化
如果需要在新用户中发放：在 `ItemInitializer.GetInitialInventory()` 中添加

#### 5.3 成就奖励
如果作为成就奖励：在 `AchievementSystem.CreateRewardItem()` 中添加

### 第6步：编译和测试

```bash
# 编译项目
dotnet build

# 运行本地测试
./start_local_test.sh

# 测试新道具的功能
# - 检查道具是否正确显示
# - 测试战斗逻辑
# - 验证属性是否生效
```

---

## 第三部分：注意事项

### 命名规范
- **ID规范**: 使用snake_case格式，全小写 (例：your_dice_name)
- **类名规范**: 使用PascalCase格式 (例：YourDiceName)
- **描述长度**: 保持简洁，不超过50个字符

### 道具ID注册位置检查清单
- [ ] `ItemFactory.RegisterAllItems()` - 注册工厂
- [ ] `ItemInitializer.GetAllItems()` - 添加到全道具列表
- [ ] `ItemInitializer.CreateItemFromStackData()` - 如果是装备类
- [ ] 如需初始发放：`ItemInitializer.GetInitialInventory()`
- [ ] 如为成就奖励：`AchievementSystem.CreateRewardItem()`

### 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|--------|
| 道具不显示 | ID未注册 | 检查ItemFactory中是否注册 |
| 战斗时错误 | ExecuteActiveAction/PassiveAction返回值错误 | 确保返回正确的ActionResult/DefenseResult |
| 属性不生效 | 属性未在Constructor中设置 | 在构造函数中正确初始化Attack/Defense等 |
| 计数器未清空 | 没有在游戏结束时重置 | 在BattleManager中添加清空逻辑 |

### 最佳实践

1. **继承正确的基类**
   - 骰子继承 `Dice` 类
   - 饰品继承 `Accessory` 类
   - 其他继承 `Item` 或 `Equipment` 类

2. **实现Clone()方法**
   - 确保所有属性都被复制
   - 这对道具的堆叠系统很重要

3. **设置DisplayColor**
   - 为道具设置适当的UI显示颜色
   - 便于玩家快速识别道具类型

4. **编写清晰的Summary注释**
   - 在类和方法上添加XML注释
   - 说明道具的效果和机制

5. **测试特殊情况**
   - 道具与其他道具的交互
   - 计数器/状态的清除
   - 极端输入值的处理

6. **版本控制**
   - 在CSV道具表中标记 `*` 表示已实现
   - 在成就系统中更新新道具的获取条件

### 性能注意事项

- 避免在Roll()中进行复杂计算
- 对于频繁调用的方法进行缓存
- Random对象应该在构造函数中初始化，不要频繁创建

### 服务器同步

对于客户端和服务器都需要的道具逻辑：
- 在 `EonVientianeServer/` 目录中创建对应的服务器类
- 确保客户端和服务器的逻辑保持一致
- 使用API进行数据同步

---

## 第四部分：示例 - 完整的新道具创建案例

### 案例：创建"幸运骰"(Lucky Dice)

#### 步骤1：规划
```
名称: 幸运骰
ID: lucky_dice
类型: 主动骰子(AD)
效果: D6，但掷出6的概率翻倍
描述: 幸运眷顾的骰子
基础属性: Attack +2
```

#### 步骤2：创建类文件
文件: `EonVientiane/Dices/LuckyDice.cs`
```csharp
namespace EonVientiane;

public class LuckyDice : Dice
{
    private Random _random;
    
    public LuckyDice()
        : base("lucky_dice", "幸运骰", "运气就在你这边", DiceUsageType.Active)
    {
        _random = new Random();
        Attack = 2;
        DisplayColor = Color.Gold;
    }
    
    public override int Roll()
    {
        // 50%概率直接掷出6，50%概率正常掷D6
        if (_random.NextDouble() < 0.5)
            return 6;
        return _random.Next(1, 7);
    }
    
    // ... 其他方法实现
}
```

#### 步骤3-6：注册并测试
按照上述注册流程进行...

---

## 常用工具类参考

### ActionResult (主动骰子返回值)
```csharp
public class ActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Player Target { get; set; }
    public int ATKP { get; set; }
}
```

### DefenseResult (被动骰子返回值)
```csharp
public class DefenseResult
{
    public int DEFP { get; set; }
    public int ActualDamage { get; set; }
    public string Message { get; set; }
}
```

### BattleContext (战斗上下文)
```csharp
public class BattleContext
{
    public Player Player { get; set; }
    public int PlayerHP { get; set; }
    public int EnemyHP { get; set; }
    public bool CanGainHP { get; set; }
    public int RoundCount { get; set; }
    public bool IsVictory { get; set; }
}
```

---

## 快速参考表

| 功能 | 文件位置 | 方法/属性 |
|------|--------|---------|
| 注册道具 | InventoryManager.cs | ItemFactory.RegisterAllItems() |
| 道具列表 | ItemInitializer.cs | GetAllItems() |
| 战斗逻辑 | 道具类 | ExecuteActiveAction/PassiveAction |
| 初始装备 | ItemInitializer.cs | GetRecommendedEquipment() |
| 成就奖励 | AchievementSystem.cs | CreateRewardItem() |
| UI显示 | Item.cs | DisplayColor |

---

## 更新日志

**v1.0** (2026-01-23)
- 创建初始道具创建指南
- 包含5种已实现的骰子和7种已实现的饰品
- 提供完整的创建流程和注意事项

---

## 联系与支持

对于道具创建的问题，请参考：
- 现有道具实现（`Dices/` 和 `Accessories/` 目录）
- ItemFactory 类的注册示例
- 成就系统的道具创建示例

有任何问题欢迎提出Issue或进行代码审查！
