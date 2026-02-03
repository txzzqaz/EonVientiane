# 快速参考：骰子和饰品系统

## 🎮 游戏规则

### 骰子限制
| 属性 | 值 |
|------|-----|
| 最多可装备骰子数 | **8个** |
| 所有骰子槽位消耗 | 0（骰子不消耗饰品槽位） |

### 饰品槽位系统
| 属性 | 值 |
|------|-----|
| 总槽位数 | **12个** |
| 默认每个饰品消耗 | **1个** |
| 飞升之证特殊消耗 | **11个** |

## 🛠️ 开发者API

### InventoryManager 类

#### 骰子相关
```csharp
// 获取最多可装备骰子数
int max = inventoryManager.MaxEquippedDice; // 返回 8

// 获取当前已装备骰子数
int count = inventoryManager.EquippedDiceCount;

// 尝试装备物品（会自动检查限制）
bool success = inventoryManager.EquipItem(inventoryIndex);
// 返回 false 如果：
// - 这是第9个骰子
// - 这是饰品但槽位不足
```

#### 饰品槽位相关
```csharp
// 最多可用槽位数
int max = inventoryManager.MaxAccessorySlots; // 返回 12

// 已使用的槽位数
int used = inventoryManager.UsedAccessorySlots;

// 剩余可用槽位数
int available = inventoryManager.AvailableAccessorySlots;

// 动态修改最大槽位（用于特殊机制）
inventoryManager.MaxAccessorySlots = 15;
```

### Equipment 类

#### 所有装备都有的属性
```csharp
Equipment equipment = /* ... */;

// 获取该装备的槽位消耗
int cost = equipment.AccessorySlotsCost;

// 检查是否是饰品
if (equipment is Accessory accessory)
{
    Console.WriteLine($"需要 {accessory.AccessorySlotsCost} 个槽位");
}

// 检查是否是骰子
if (equipment is Dice dice)
{
    Console.WriteLine("骰子不占用饰品槽位");
}
```

## 📋 物品槽位配置表

```csharp
// 在构造函数中设置
public class CustomAccessory : Accessory
{
    public CustomAccessory()
        : base("custom_id", "自定义饰品", "描述")
    {
        // 设置槽位消耗
        AccessorySlotsCost = 2;  // 消耗2个槽位
        // 或
        AccessorySlotsCost = -1; // 提供额外1个槽位
    }
}
```

## 🎯 常见操作

### 检查是否可以装备物品
```csharp
Equipment equipment = /* 要装备的物品 */;

// 检查骰子
if (equipment is Dice)
{
    if (inventoryManager.EquippedDiceCount >= 8)
    {
        Debug.WriteLine("骰子已满！");
        return false;
    }
}

// 检查饰品
if (equipment is Accessory accessory)
{
    if (accessory.AccessorySlotsCost > inventoryManager.AvailableAccessorySlots)
    {
        Debug.WriteLine($"槽位不足，需要 {accessory.AccessorySlotsCost}，可用 {inventoryManager.AvailableAccessorySlots}");
        return false;
    }
}

// 如果都通过了，可以装备
return inventoryManager.EquipItem(inventoryIndex);
```

### 获取装备统计
```csharp
var inv = inventoryManager;

Debug.WriteLine($"骰子: {inv.EquippedDiceCount}/{inv.MaxEquippedDice}");
Debug.WriteLine($"槽位: {inv.UsedAccessorySlots}/{inv.MaxAccessorySlots}");
Debug.WriteLine($"可用: {inv.AvailableAccessorySlots}");
```

### 卸下装备并检查变化
```csharp
int beforeSlots = inventoryManager.UsedAccessorySlots;

inventoryManager.UnequipItem(equippedIndex);

int afterSlots = inventoryManager.UsedAccessorySlots;
int freedSlots = beforeSlots - afterSlots;

Debug.WriteLine($"释放了 {freedSlots} 个槽位");
```

## 🖼️ UI显示参考

### 装备栏头部信息
```
已装备 (3)                    骰子: 8/8     槽位: 12/12
```

**颜色规则**：
- 骰子数为红色 → 已达到上限（8/8）
- 骰子数为绿色 → 未达到上限
- 槽位数为红色 → 已超出或达到限制（≥12）
- 槽位数为黄色 → 正常使用中

## ⚙️ 配置改动

如需调整系统参数，修改 InventoryManager.cs：

```csharp
// 改变骰子上限
private int _maxEquippedDice = 10; // 从 8 改为 10

// 改变初始槽位数
private int _maxAccessorySlots = 20; // 从 12 改为 20
```

## 🐛 常见问题

**Q: 为什么骰子不占用饰品槽位？**  
A: 设计上骰子和饰品是两个独立的系统，骰子有单独的8个上限。

**Q: 能否为饰品提供负数槽位？**  
A: 可以，设置 `AccessorySlotsCost = -1` 来提供额外1个槽位。

**Q: 飞升之证为什么占11个槽位？**  
A: 这是游戏平衡设计，11个槽位意味着最多只能再装备1个单槽饰品。

**Q: 能否在游戏中动态改变槽位上限？**  
A: 可以，设置 `inventoryManager.MaxAccessorySlots = newValue`。

## 📚 相关文档

- [完整实现指南](ACCESSORY_SYSTEM.md)
- [实现总结](IMPLEMENTATION_SUMMARY_ACCESSORY_SLOTS.md)
