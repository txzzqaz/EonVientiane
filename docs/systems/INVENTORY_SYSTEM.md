# 背包装备系统说明

## 概述
已为按钮2界面实现了一个完整的背包装备系统，包含左侧背包和右侧装备栏。

## 主要功能

### 1. 物品系统 (Item.cs)
- **物品基类 (Item)**: 定义了物品的基础属性
  - Id: 物品唯一标识
  - Name: 物品名称
  - Description: 物品描述
  - Type: 物品类型（消耗品、装备、材料、任务物品等）
  - MaxStackSize: 最大堆叠数量
  - DisplayColor: 显示颜色

- **装备类 (Equipment)**: 继承自Item，添加装备特有属性
  - EquipmentSlot: 装备槽位（头部、胸甲、腿部、鞋子、武器、盾牌、饰品1-3）
  - Attack/Defense/Speed/Health/Mana: 装备属性加成

- **物品堆叠 (ItemStack)**: 背包中的物品实例，支持堆叠管理

### 2. 背包管理器 (InventoryManager.cs)
- **背包功能**:
  - `AddItem()`: 添加物品到背包
  - `RemoveItem()`: 从背包移除物品
  - `GetItemCount()`: 获取指定物品数量
  - `HasItem()`: 检查是否拥有物品
  - `ClearInventory()`: 清空背包
  
- **装备功能**:
  - `EquipItem()`: 装备物品
  - `UnequipItem()`: 卸下装备
  - `GetEquippedItem()`: 获取指定槽位装备
  - `UnequipAll()`: 卸下所有装备
  - `GetTotalStats()`: 计算总属性加成

- **容量管理**:
  - `MaxCapacity`: 背包容量上限（-1表示无限制，预留接口）
  - `UsedSlots`: 当前占用槽位数
  - `IsFull`: 是否已满

### 3. UI界面 (UIManager.cs)
- **DrawInventoryPanel()**: 绘制背包界面主面板
- **DrawInventorySection()**: 绘制左侧背包区域
  - 显示物品列表（名称、数量、描述）
  - 装备物品有[装备]标记
  - 选中高亮显示
  
- **DrawEquipmentSection()**: 绘制右侧装备区域
  - 9个装备槽位（头、胸、腿、鞋、武器、盾、饰品x3）
  - 显示已装备物品及其属性
  - 显示总属性加成统计

### 4. 游戏集成 (Game1.cs)
- **初始化**: 在Initialize()中创建InventoryManager并加载测试数据
- **输入处理**: HandleInventoryInput()方法处理背包交互
  - 单击选中物品/装备槽
  - 双击背包物品进行装备
  - 双击装备槽卸下装备

## 使用方法

### 运行游戏
1. 启动游戏
2. 点击左侧菜单的"按钮2"
3. 进入背包装备界面

### 操作说明
- **装备物品**: 
  - 在左侧背包中单击选中装备
  - 再次点击同一装备即可装备（双击效果）
  
- **卸下装备**:
  - 在右侧装备栏中单击选中已装备的物品
  - 再次点击同一槽位即可卸下（双击效果）

## 测试数据
系统包含以下测试物品：
- **铁剑** (武器): 攻击+10
- **钢铠** (胸甲): 防御+15, 生命+50
- **皮靴** (鞋子): 防御+5, 速度+3
- **魔法戒指** (饰品1): 魔力+30, 攻击+5
- **生命药水** (消耗品): x15
- **金币** (材料): x250

## 扩展接口

### 容量限制
```csharp
// 设置背包容量上限
_inventoryManager.MaxCapacity = 50; // 设置为50格
```

### 添加新物品
```csharp
// 创建消耗品
var potion = new Item("health_potion_large", "大型生命药水", "恢复100点生命", ItemType.Consumable)
{
    MaxStackSize = 99,
    DisplayColor = Color.Red
};
_inventoryManager.AddItem(potion, 5);

// 创建装备
var sword = new Equipment("legendary_sword", "传说之剑", "威力强大的神器", EquipmentSlot.Weapon)
{
    Attack = 50,
    Speed = 10,
    DisplayColor = Color.Gold
};
_inventoryManager.AddItem(sword);
```

### 物品操作
```csharp
// 检查物品数量
int potionCount = _inventoryManager.GetItemCount("health_potion");

// 移除物品
_inventoryManager.RemoveItem("gold_coin", 100);

// 获取装备属性
var stats = _inventoryManager.GetTotalStats();
Console.WriteLine($"总攻击: {stats.attack}");
```

## 架构特点
1. **模块化设计**: 物品、背包、UI分离，便于维护和扩展
2. **预留接口**: 容量限制、物品使用等功能已预留接口
3. **类型安全**: 使用枚举和强类型，减少错误
4. **可扩展性**: 易于添加新的物品类型、装备槽位和属性

## 下一步可扩展功能
- [ ] 物品拖放功能
- [ ] 物品使用功能（消耗品）
- [ ] 物品排序和过滤
- [ ] 背包滚动支持（当物品过多时）
- [ ] 物品tooltip详细信息
- [ ] 物品品质系统（普通、稀有、史诗等）
- [ ] 装备套装系统
- [ ] 物品交易功能
