# 饰品槽系统和骰子数量限制实现指南

## 概述

本文档记录了对EonVientiane游戏系统的两项重要更新：
1. **饰品槽系统** - 限制可装备饰品的总数量（12个槽位）
2. **骰子数量限制** - 最多只能装备8个骰子

## 实现详情

### 1. 饰品槽系统（AccessorySlotsCost）

#### 核心概念
- 每个饰品都有一个 `AccessorySlotsCost` 属性，表示该饰品占用的槽位数
- 初始有 **12个槽位**可用
- 玩家可以装备多个饰品，只要总槽位消耗不超过12

#### 特殊饰品配置

| 饰品名称 | 槽位消耗 | 说明 |
|---------|---------|------|
| 飞升之证 (AscensionProofAccessory) | 11 | 占用11个槽位，是最重的饰品 |
| 自我 (SelfAccessory) | 1 | 占用1个槽位 |
| 漫游者之心 (WandererHeartAccessory) | 1 | 占用1个槽位 |
| 圣火 (HolyFireAccessory) | 1 | 占用1个槽位 |
| 预见 (ForesightAccessory) | 1 | 占用1个槽位 |
| 戮力同心 (ConcertedEffortAccessory) | 1 | 占用1个槽位 |

#### 特殊说明
- 某些饰品可以配置为负数槽位消耗（目前未使用，但系统支持），以提供额外槽位

### 2. 骰子数量限制

- 最多可装备 **8个骰子**
- 当已装备骰子数达到限制时，无法再装备新的骰子

## 代码改动

### Item.cs - Equipment 类
```csharp
/// <summary>
/// 饰品槽消耗数量（仅对Accessory有效）
/// 正数：消耗的槽位数（默认1）
/// 负数：提供的额外槽位数（例如-1表示提供1个额外槽位）
/// </summary>
public int AccessorySlotsCost { get; set; } = 1;
```

### InventoryManager.cs - 新增属性和方法

#### 新增字段
```csharp
private int _maxAccessorySlots = 12;    // 初始12个槽位
private int _maxEquippedDice = 8;       // 最多8个骰子
```

#### 新增属性
```csharp
/// <summary>
/// 最多可装备骰子数量（上限8个）
/// </summary>
public int MaxEquippedDice => _maxEquippedDice;

/// <summary>
/// 当前已装备骰子数量
/// </summary>
public int EquippedDiceCount => EquippedItems.OfType<Dice>().Count();

/// <summary>
/// 最多饰品槽位数
/// </summary>
public int MaxAccessorySlots
{
    get => _maxAccessorySlots;
    set => _maxAccessorySlots = Math.Max(0, value);
}

/// <summary>
/// 当前已使用的饰品槽位数
/// </summary>
public int UsedAccessorySlots
{
    get
    {
        var equippedAccessories = EquippedItems.OfType<Accessory>().ToList();
        return equippedAccessories.Sum(a => a.AccessorySlotsCost);
    }
}

/// <summary>
/// 当前可用饰品槽位数
/// </summary>
public int AvailableAccessorySlots => Math.Max(0, MaxAccessorySlots - UsedAccessorySlots);
```

#### 修改的 EquipItem 方法
```csharp
public bool EquipItem(int inventoryIndex)
{
    if (inventoryIndex < 0 || inventoryIndex >= _inventoryItems.Count)
        return false;
    
    var stack = _inventoryItems[inventoryIndex];
    if (stack.Item is not Equipment equipment)
        return false;
    
    // 检查骰子数量限制
    if (equipment is Dice && EquippedDiceCount >= MaxEquippedDice)
        return false;
    
    // 检查饰品槽位限制
    if (equipment is Accessory accessory && accessory.AccessorySlotsCost > AvailableAccessorySlots)
        return false;
    
    _inventoryItems.RemoveAt(inventoryIndex);
    _equippedItems.Add(stack);
    return true;
}
```

### UIManager.cs - 界面显示

在 `DrawEquipmentSection` 方法中，装备栏标题后添加了两行信息显示：

1. **骰子显示**：`骰子: X/8`
   - 颜色：达到上限时显示红色，否则显示绿色

2. **槽位显示**：`槽位: X/12`
   - 颜色：超出限制时显示红色，否则显示黄色

## 使用场景示例

### 情景1：正常装备
```
初始状态：
- 装备数量：0
- 已用槽位：0/12
- 已装备骰子：0/8

装备自我(1槽)和漫游者之心(1槽)：
- 装备数量：2
- 已用槽位：2/12
- 已装备骰子：0/8

再装备一个骰子：
- 装备数量：3
- 已用槽位：2/12
- 已装备骰子：1/8
```

### 情景2：装备飞升之证
```
装备飞升之证(11槽)：
- 装备数量：1
- 已用槽位：11/12
- 剩余可用槽位：1
- 可再装备1个单槽饰品

无法再装备任何需要2个或更多槽位的饰品
```

### 情景3：达到骰子上限
```
装备8个骰子：
- 已装备骰子：8/8 (红色警告)
- 无法再装备骰子
- UI中骰子数字为红色提示

可以继续装备饰品（如果有槽位）
```

## 后续扩展建议

1. **可配置的槽位上限**：通过修改 `MaxAccessorySlots` 属性实现
2. **可配置的骰子上限**：通过修改 `MaxEquippedDice` 属性实现
3. **提供额外槽位的饰品**：创建槽位消耗为负数的饰品
4. **战斗中的槽位管理UI**：在战斗界面显示对手和自己的槽位信息
5. **成就系统集成**：根据槽位和骰子的组合方式添加新成就

## 测试检查清单

- [x] 编译无错误
- [ ] 背包UI显示骰子和槽位信息
- [ ] 装备骰子数量限制生效
- [ ] 装备饰品槽位限制生效
- [ ] 飞升之证占用11个槽位
- [ ] 卸下装备时槽位恢复
- [ ] UI颜色提示正确显示

## 文件改动列表

1. **Item.cs** - 添加 `AccessorySlotsCost` 属性到 `Equipment` 类
2. **InventoryManager.cs** - 添加槽位系统和骰子限制
3. **UIManager.cs** - 修改 `DrawEquipmentSection` 显示槽位和骰子信息
4. **Accessories/AscensionProofAccessory.cs** - 设置 `AccessorySlotsCost = 11`
5. **Accessories/SelfAccessory.cs** - 设置 `AccessorySlotsCost = 1`
6. **Accessories/WandererHeartAccessory.cs** - 设置 `AccessorySlotsCost = 1`
7. **Accessories/HolyFireAccessory.cs** - 设置 `AccessorySlotsCost = 1`
8. **Accessories/ForesightAccessory.cs** - 设置 `AccessorySlotsCost = 1`
9. **Accessories/ConcertedEffortAccessory.cs** - 设置 `AccessorySlotsCost = 1`

## 编译状态
✅ 所有改动已通过编译，无错误
