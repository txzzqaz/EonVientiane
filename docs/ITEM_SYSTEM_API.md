# 物品系统重构文档

## 概述

物品系统已重构为模块化架构，每个道具都有独立的文件，并通过统一的API进行创建和管理。

## 目录结构

```
EonVientiane/
├── Item.cs                          # 基础类定义（Item、Equipment、Dice、Accessory等）
├── ItemRegistry.cs                  # 物品注册表
├── InventoryManager.cs              # 库存管理（包含ItemFactory）
├── Dices/                           # 骰子相关道具
│   ├── D6Dice.cs                   # D6六面骰
│   ├── FeatheredDice.cs            # 飞羽被动骰
│   ├── GuaShaParquetDice.cs        # 刮痧师傅骰
│   └── SpringBreezeDice.cs         # 春风主动骰
└── Accessories/                     # 饰品相关道具
    ├── SelfAccessory.cs            # 自我饰品
    ├── AscensionProofAccessory.cs  # 飞升之证饰品
    ├── HolyFireAccessory.cs        # 圣火饰品
    ├── WandererHeartAccessory.cs   # 漫游者之心饰品
    └── ForesightAccessory.cs       # 预见饰品
```

## API 文档

### 1. ItemFactory (物品工厂)

位置：`InventoryManager.cs`

#### 初始化

```csharp
// 在应用启动时调用一次
ItemFactory.Initialize();
```

#### 创建单个物品

```csharp
// 根据物品ID创建物品实例
Item item = ItemFactory.Create("d6_dice");

// 创建物品并指定自定义名称
Item item = ItemFactory.Create("gold_coin", "闪闪发光的金币");
```

#### 创建物品堆栈

```csharp
// 创建指定数量的物品堆栈
ItemStack stack = ItemFactory.CreateItemStack("gold_coin", 50);
```

#### 获取物品信息

```csharp
// 获取所有注册的物品ID
IEnumerable<string> allIds = ItemFactory.GetAllItemIds();

// 获取所有骰子ID
IEnumerable<string> diceIds = ItemFactory.GetAllDiceIds();
// 返回: "d6_dice", "feathered_dice", "guasha_parquet", "spring_breeze"

// 获取所有饰品ID
IEnumerable<string> accessoryIds = ItemFactory.GetAllAccessoryIds();
// 返回: "self_accessory", "ascension_proof", "holy_fire", "wanderer_heart", "foresight"

// 检查物品是否已注册
bool isRegistered = ItemFactory.IsItemRegistered("d6_dice");
```

#### 创建玩家起始装备

```csharp
// 创建骰子列表（用于新玩家）
List<Dice> dices = ItemFactory.CreateStarterDices();
// 返回: D6骰 + 飞羽骰

// 创建饰品列表（用于新玩家）
List<Accessory> accessories = ItemFactory.CreateStarterAccessories();
// 返回: 自我饰品
```

### 2. ItemRegistry (物品注册表)

位置：`ItemRegistry.cs`

#### 用法

```csharp
var registry = new ItemRegistry();

// 注册物品
registry.RegisterItem("custom_dice", () => new CustomDice());

// 创建物品
var item = registry.CreateItem("custom_dice");

// 获取所有注册的物品ID
var ids = registry.GetAllItemIds();

// 检查物品是否已注册
bool exists = registry.IsItemRegistered("custom_dice");

// 注销物品
registry.UnregisterItem("custom_dice");

// 清空所有注册
registry.Clear();
```

### 3. 骰子类 (Dice Classes)

#### D6Dice - 六面骰子

```csharp
var dice = new D6Dice();
var dice = new D6Dice(DiceUsageType.Active);    // 仅主动
var dice = new D6Dice(DiceUsageType.Passive);   // 仅被动
var dice = new D6Dice(DiceUsageType.Both);      // 主被动通用

// 掷骰子
int result = dice.Roll();  // 返回 1-6

// 执行主动行动
ActionResult actionResult = dice.ExecuteActiveAction(attacker, defenders);
// actionResult.Success - 是否成功
// actionResult.Message - 说明信息
// actionResult.Target - 目标
// actionResult.AttackPower - 攻击点数

// 执行防守
DefenseResult defenseResult = dice.ExecutePassiveAction(defender, attackDamage);
// defenseResult.DefensePower - 防守点数
// defenseResult.ActualDamage - 实际伤害
// defenseResult.Message - 说明信息
```

#### FeatheredDice - 飞羽骰

```csharp
var dice = new FeatheredDice();

// 特殊用法：根据攻击力计算闪避
int avoidancePoints = dice.RollWithATKP(attackPower);

// 重置计数器（游戏结束时）
dice.ResetCounter();

// 查看计数器
int counter = dice.Counter;
```

#### GuaShaParquetDice - 刮痧师傅骰

```csharp
var dice = new GuaShaParquetDice();

// 掷骰子
int atkp = dice.Roll();  // 返回 1-6

// 执行攻击（包含多轮伤害机制）
ActionResult action = dice.ExecuteActiveAction(attacker, defenders);
```

#### SpringBreezeDice - 春风骰

```csharp
var dice = new SpringBreezeDice();

// 掷骰子
int sprp = dice.Roll();  // 返回 1-4

// 执行攻击（会修改下一个骰子的计数器）
ActionResult action = dice.ExecuteActiveAction(attacker, defenders);
```

### 4. 饰品类 (Accessory Classes)

#### SelfAccessory - 自我

```csharp
var accessory = new SelfAccessory();

// 对局开始时提供20HP
accessory.OnBattleStart(battleContext);
// battleContext.PlayerHP 增加20
```

#### AscensionProofAccessory - 飞升之证

```csharp
var accessory = new AscensionProofAccessory();

// 对局开始时的效果
accessory.OnBattleStart(battleContext);
// 强制HP为0，获得护盾等于计数器数量

// 记录胜利
accessory.OnWin();  // 连续5场胜利后计数器+1

// 记录失败
accessory.OnLoss(); // 重置连续胜利计数

// 获取状态
string status = accessory.GetStatusDescription();
// 示例: "计数器: 2 | 连续胜利: 3/5"
```

#### HolyFireAccessory - 圣火

```csharp
var accessory = new HolyFireAccessory();

// 检查是否强制对手跳过
bool shouldSkip = accessory.ShouldForceOpponentSkip(actionTimeSpan);
// 如果操作时间 > 0.5秒，返回true
```

#### WandererHeartAccessory - 漫游者之心

```csharp
var accessory = new WandererHeartAccessory();

// 计算攻击倍率
double multiplier = accessory.GetAttackMultiplier(slowestActionTime);
// 0-1秒内操作，得到10-1倍的倍率
// 超过1秒，返回1倍（无加成）
```

#### ForesightAccessory - 预见

```csharp
var accessory = new ForesightAccessory();

// 检查是否可以提前规划
bool canPlan = accessory.CanPlannedAction;  // 始终返回true
```

## 物品ID 列表

### 骰子
- `d6_dice` - D6六面骰
- `feathered_dice` - 飞羽
- `guasha_parquet` - 刮痧师傅
- `spring_breeze` - 春风

### 饰品
- `self_accessory` - 自我
- `ascension_proof` - 飞升之证
- `holy_fire` - 圣火
- `wanderer_heart` - 漫游者之心
- `foresight` - 预见

### 消耗品和材料
- `health_potion` - 生命药水（堆叠上限99）
- `mana_potion` - 魔力药水（堆叠上限99）
- `gold_coin` - 金币（堆叠上限9999）

## 添加新道具的步骤

### 1. 创建新的骰子类

在 `Dices/` 目录下创建新文件，例如 `MyNewDice.cs`：

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace EonVientiane;

public class MyNewDice : Dice
{
    private Random _random;
    
    public MyNewDice()
        : base("my_new_dice", "我的新骰子", "描述", DiceUsageType.Active)
    {
        _random = new Random();
        DisplayColor = Color.White;
    }
    
    public override int Roll()
    {
        return _random.Next(1, 7);
    }
    
    public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        // 实现逻辑
        return new ActionResult(true, "消息", null, 0);
    }
    
    public override Item Clone()
    {
        return new MyNewDice()
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

### 2. 创建新的饰品类

在 `Accessories/` 目录下创建新文件，例如 `MyNewAccessory.cs`：

```csharp
using Microsoft.Xna.Framework;

namespace EonVientiane;

public class MyNewAccessory : Accessory
{
    public MyNewAccessory()
        : base("my_new_accessory", "我的新饰品", "描述")
    {
        DisplayColor = Color.White;
        Health = 10;
    }
    
    public override void OnBattleStart(BattleContext context)
    {
        // 实现对局开始时的效果
        if (context.CanGainHP)
        {
            context.PlayerHP += Health;
        }
    }
    
    public override Item Clone()
    {
        return new MyNewAccessory()
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

### 3. 在 ItemFactory 中注册

在 `InventoryManager.cs` 的 `RegisterAllItems()` 方法中添加：

```csharp
// 对于骰子
_registry.RegisterItem("my_new_dice", () => new MyNewDice());

// 对于饰品
_registry.RegisterItem("my_new_accessory", () => new MyNewAccessory());
```

### 4. 使用

```csharp
// 创建物品
var item = ItemFactory.Create("my_new_dice");

// 添加到列表中
var dice = ItemFactory.GetAllDiceIds();
```

## 使用示例

### 创建并初始化库存

```csharp
// 初始化工厂
ItemFactory.Initialize();

// 创建新玩家的起始装备
var dices = ItemFactory.CreateStarterDices();
var accessories = ItemFactory.CreateStarterAccessories();

// 添加消耗品
ItemStack goldStack = ItemFactory.CreateItemStack("gold_coin", 100);
ItemStack healthPotion = ItemFactory.CreateItemStack("health_potion", 5);
```

### 在战斗中使用

```csharp
// 获取玩家的骰子
var activeDice = player.EquippedDices[0] as Dice;

// 执行行动
var result = activeDice.ExecuteActiveAction(attacker, defenders);
if (result.Success)
{
    Debug.WriteLine(result.Message);
}

// 处理防御
var defense = defendingDice.ExecutePassiveAction(defender, result.AttackPower);
Debug.WriteLine($"伤害: {defense.ActualDamage}");
```

## 迁移指南

### 旧代码
```csharp
// 旧的创建方式
Item item = itemId switch 
{
    "d6_dice" => new D6Dice(),
    // ...
};
```

### 新代码
```csharp
// 新的创建方式（推荐）
ItemFactory.Initialize();  // 仅需一次
Item item = ItemFactory.Create("d6_dice");
```

## 注意事项

1. **初始化**: `ItemFactory.Initialize()` 应在应用启动时调用一次
2. **单例模式**: ItemFactory 使用静态方法和内部初始化，无需创建实例
3. **可扩展性**: 通过继承 `Dice` 或 `Accessory` 可轻松添加新道具
4. **克隆操作**: 所有物品都实现了 `Clone()` 方法，用于创建副本
5. **堆栈管理**: ItemStack 类负责物品堆叠逻辑

## 常见问题

**Q: 如何动态注册新物品？**  
A: 创建 ItemRegistry 实例并使用 `RegisterItem()` 方法，或者修改 ItemFactory 中的 `RegisterAllItems()` 方法。

**Q: 物品可以继承吗？**  
A: 可以。继承 `Item`、`Dice` 或 `Accessory` 类来创建特定的物品类型。

**Q: 如何在战斗中动态修改骰子属性？**  
A: 修改对象的属性（如 `Health`、`Attack`）。计数器通过公共属性暴露。

**Q: Clone() 方法的作用是什么？**  
A: 创建物品的深拷贝，用于背包中的独立实例。
