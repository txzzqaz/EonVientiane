# 道具构造函数格式规范

## 概述

为了提高代码的可读性和可维护性，所有道具（骰子和饰品）的构造函数应使用**多行命名参数格式**。

## ✅ 推荐格式

### 骰子 (Dice)

```csharp
public class YourDice : Dice
{
    public YourDice()
        : base(
            id: "your_dice_id",
            name: "骰子名称",
            description: "骰子描述",
            usageType: DiceUsageType.Active,  // 或 Passive、Both
            creator: "qaz"  // 可选，默认为 "qaz"
        )
    {
        _random = new Random();
        DisplayColor = Color.White;
    }
}
```

### 饰品 (Accessory)

```csharp
public class YourAccessory : Accessory
{
    public YourAccessory()
        : base(
            id: "your_accessory_id",
            name: "饰品名称",
            description: "饰品描述",
            creator: "qaz"  // 可选，默认为 "qaz"
        )
    {
        DisplayColor = Color.White;
        AccessorySlotsCost = 1;
        Health = 10;  // 可选属性
    }
}
```

## 参数说明

### 骰子基类参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 道具的唯一标识符，使用小写+下划线，如 "guasha_parquet" |
| `name` | string | ✅ | 道具的显示名称，如 "刮痧师傅" |
| `description` | string | ✅ | 道具的简短描述，如 "驽马十驾，功在不舍" |
| `usageType` | DiceUsageType | ✅ | 骰子类型：Active/Passive/Both |
| `creator` | string | ⚪ | 创作者标识，默认 "qaz" |
| `function` | string | ⚪ | 功能说明，默认空字符串 |

### 饰品基类参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 道具的唯一标识符 |
| `name` | string | ✅ | 道具的显示名称 |
| `description` | string | ✅ | 道具的简短描述 |
| `creator` | string | ⚪ | 创作者标识，默认 "qaz" |
| `function` | string | ⚪ | 功能说明，默认空字符串 |

## 常用属性设置

在构造函数体内设置这些属性：

```csharp
// 显示颜色
DisplayColor = Color.Orange;

// 饰品槽位消耗（仅饰品）
AccessorySlotsCost = 3;  // 正数：消耗槽位，负数：提供槽位

// 装备属性（可选）
Attack = 5;
Defense = 10;
Speed = 3;
Health = 20;
Mana = 15;
```

## 实际示例

### 示例 1: 刮痧师傅骰子

```csharp
public class GuaShaParquetDice : Dice
{
    private Random _random;
    
    public GuaShaParquetDice()
        : base(
            id: "guasha_parquet",
            name: "刮痧师傅",
            description: "驽马十驾，功在不舍",
            usageType: DiceUsageType.Active,
            creator: "yyzh"
        )
    {
        _random = new Random();
        DisplayColor = Color.Orange;
    }
    
    public override int Roll()
    {
        return _random.Next(1, 7);
    }
}
```

### 示例 2: 漫游者之心饰品

```csharp
public class WandererHeartAccessory : Accessory
{
    public WandererHeartAccessory()
        : base(
            id: "wanderer_heart",
            name: "漫游者之心",
            description: "纯粹"
        )
    {
        DisplayColor = Color.Cyan;
        AccessorySlotsCost = 3;
    }
    
    public override Item Clone()
    {
        return new WandererHeartAccessory()
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

### 示例 3: 戮力同心饰品（带创作者）

```csharp
public class ConcertedEffortAccessory : Accessory
{
    public ConcertedEffortAccessory()
        : base(
            id: "concerted_effort",
            name: "戮力同心",
            description: "运，赢！",
            creator: "yyzh"
        )
    {
        DisplayColor = Color.Goldenrod;
        AccessorySlotsCost = 1;
    }
}
```

## ❌ 避免的旧格式

```csharp
// ❌ 不推荐：所有参数挤在一行
public GuaShaParquetDice()
    : base("guasha_parquet", "刮痧师傅", "驽马十驾，功在不舍", DiceUsageType.Active, "yyzh")
{
}
```

## 优势

1. **更清晰** - 每个参数独占一行，一目了然
2. **易修改** - 可以轻松添加、删除或修改参数
3. **减少错误** - 命名参数避免了参数顺序错误
4. **易于阅读** - 代码审查和维护更加容易
5. **自文档化** - 参数名称说明了其用途

## 迁移现有代码

所有现有的骰子和饰品已经更新为新格式：

- ✅ GuaShaParquetDice
- ✅ FeatheredDice
- ✅ SpringBreezeDice
- ✅ D6Dice
- ✅ ErrorDice
- ✅ WandererHeartAccessory
- ✅ HolyFireAccessory
- ✅ SelfAccessory
- ✅ AscensionProofAccessory
- ✅ ForesightAccessory
- ✅ ConcertedEffortAccessory

## 相关文档

- [道具创建完整指南](ITEM_CREATION_GUIDE.md)
- [道具系统快速参考](ITEM_QUICK_REFERENCE.md)
- [骰子系统 API](ITEM_SYSTEM_API.md)
