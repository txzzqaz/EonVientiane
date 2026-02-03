# 物品系统快速使用指南

## 快速开始

### 1. 初始化（应用启动时）

```csharp
ItemFactory.Initialize();
```

### 2. 创建物品

```csharp
// 创建单个物品
Item dice = ItemFactory.Create("d6_dice");

// 创建物品堆栈
ItemStack coins = ItemFactory.CreateItemStack("gold_coin", 100);
```

## 常用操作速查表

### 创建骰子
```csharp
var d6 = new D6Dice();                           // D6六面骰
var feathered = new FeatheredDice();             // 飞羽（被动骰）
var guasha = new GuaShaParquetDice();            // 刮痧师傅
var springBreeze = new SpringBreezeDice();       // 春风（主动骰）
```

### 创建饰品
```csharp
var self = new SelfAccessory();                      // 自我
var ascension = new AscensionProofAccessory();       // 飞升之证
var holyFire = new HolyFireAccessory();             // 圣火
var wanderer = new WandererHeartAccessory();        // 漫游者之心
var foresight = new ForesightAccessory();           // 预见
var concerted = new ConcertedEffortAccessory();     // 戮力同心
```

### 骰子操作

```csharp
// 掷骰子
int roll = dice.Roll();

// 执行攻击
ActionResult attack = dice.ExecuteActiveAction(attacker, defenders);
if (attack.Success)
{
    applyDamage(attack.Target, attack.AttackPower);
}

// 执行防守
DefenseResult defense = dice.ExecutePassiveAction(defender, incomingDamage);
takeDamage(defense.ActualDamage);
```

### 饰品操作

```csharp
// 对局开始时调用
accessory.OnBattleStart(battleContext);

// 特殊操作（根据类型）
if (accessory is AscensionProofAccessory ascension)
{
    ascension.OnWin();   // 胜利时
    ascension.OnLoss();  // 失败时
}

if (accessory is WandererHeartAccessory wanderer)
{
    double multiplier = wanderer.GetAttackMultiplier(slowestActionTime);
}
```

## 骰子ID快速查询

| ID | 名称 | 类型 | 说明 |
|---|---|---|---|
| `d6_dice` | D6 | 主被动 | 基础六面骰 |
| `feathered_dice` | 飞羽 | 被动 | 闪避骰，计数器会增加面数 |
| `guasha_parquet` | 刮痧师傅 | 主动 | 高伤害骰，有多轮伤害机制 |
| `spring_breeze` | 春风 | 主动 | 四面骰，可减少下一个骰子的计数器 |

## 饰品ID快速查询

| ID | 名称 | 说明 |
|---|---|---|
| `self_accessory` | 自我 | 提供20HP |
| `ascension_proof` | 飞升之证 | HP为0，但每5场连胜获得护盾1层 |
| `holy_fire` | 圣火 | 对手操作超过0.5秒强制跳过 |
| `wanderer_heart` | 漫游者之心 | 快速操作获得攻击倍率加成 |
| `foresight` | 预见 | 允许提前规划行动 |
| `concerted_effort` | 戮力同心 | 连号掷骰时，本回合效果提升为 n×n |

## 消耗品ID快速查询

| ID | 名称 | 堆叠上限 |
|---|---|---|
| `health_potion` | 生命药水 | 99 |
| `mana_potion` | 魔力药水 | 99 |
| `gold_coin` | 金币 | 9999 |

## 玩家初始化

```csharp
// 获取起始骰子和饰品
List<Dice> dices = ItemFactory.CreateStarterDices();
List<Accessory> accessories = ItemFactory.CreateStarterAccessories();

// dices 包含: D6 + 飞羽
// accessories 包含: 自我
```

## 文件位置速查

| 内容 | 文件 |
|---|---|
| 基础类定义 | `Item.cs` |
| 物品注册表 | `ItemRegistry.cs` |
| 物品工厂 | `InventoryManager.cs` |
| 骰子类 | `Dices/*.cs` |
| 饰品类 | `Accessories/*.cs` |
| 完整API文档 | `docs/ITEM_SYSTEM_API.md` |

## 常见模式

### 从背包中取出物品

```csharp
ItemStack stack = inventory.GetItem("gold_coin");
if (stack != null)
{
    Item item = stack.Item;
    int quantity = stack.Quantity;
}
```

### 向背包中添加物品

```csharp
ItemStack newStack = ItemFactory.CreateItemStack("health_potion", 5);
inventory.AddItem(newStack);
```

### 检查物品是否存在

```csharp
if (ItemFactory.IsItemRegistered("my_item_id"))
{
    Item item = ItemFactory.Create("my_item_id");
}
```

### 遍历所有可用的骰子

```csharp
foreach (string diceId in ItemFactory.GetAllDiceIds())
{
    Dice dice = ItemFactory.Create(diceId) as Dice;
    // 处理骰子
}
```

## 扩展指南

要添加新的道具类型（例如新的骰子）：

1. 在 `Dices/` 目录创建新文件
2. 继承 `Dice` 类
3. 在 `InventoryManager.cs` 的 `RegisterAllItems()` 方法中注册
4. 使用 `ItemFactory.Create("your_item_id")` 创建

示例：
```csharp
// Dices/MyCustomDice.cs
public class MyCustomDice : Dice
{
    public MyCustomDice() : base("my_dice", "我的骰子", "描述", DiceUsageType.Active) {}
    // 实现具体方法
}

// InventoryManager.cs - 在 RegisterAllItems() 中
_registry.RegisterItem("my_dice", () => new MyCustomDice());
```
