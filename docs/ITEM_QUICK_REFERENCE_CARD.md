# 道具创建快速参考卡

## 📋 新增道具五步流程

```
第1步 ─→ 创建类文件 ─→ 第2步 ─→ 注册 ─→ 第3步 ─→ 同步 ─→ 第4步 ─→ 发放 ─→ 第5步 ─→ 测试
```

---

## 1️⃣  创建类文件

### 骰子
```
路径: EonVientiane/Dices/YourDiceName.cs
继承: public class YourDiceName : Dice
```

**最小实现:**
```csharp
public class MyDice : Dice
{
    private Random _random;
    
    public MyDice()
        : base("my_dice_id", "My Dice", "Motto", DiceUsageType.Active)
    {
        _random = new Random();
        DisplayColor = Color.YourColor;
    }
    
    public override int Roll() => _random.Next(1, 7);
    
    public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        // 实现攻击逻辑
    }
    
    public override DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        // 实现防御逻辑
    }
    
    public override Item Clone() { /* 复制属性 */ }
}
```

### 饰品
```
路径: EonVientiane/Accessories/YourAccessoryName.cs
继承: public class YourAccessoryName : Accessory
```

**最小实现:**
```csharp
public class MyAccessory : Accessory
{
    public MyAccessory()
        : base("my_accessory_id", "My Accessory", "Description")
    {
        Attack = 2;
        Defense = 1;
        DisplayColor = Color.YourColor;
        AccessorySlotsCost = 1;
    }
    
    public override void OnBattleStart(BattleContext context)
    {
        // 战斗开始时效果
    }
    
    public override Item Clone() { /* 复制属性 */ }
}
```

---

## 2️⃣  注册（ItemFactory）

**文件**: `EonVientiane/InventoryManager.cs`

**位置**: 找到 `ItemFactory` 类中的 `RegisterAllItems()` 方法

**添加:**
```csharp
// 骰子
_registry.RegisterItem("my_dice_id", () => new MyDice());

// 饰品
_registry.RegisterItem("my_accessory_id", () => new MyAccessory());
```

---

## 3️⃣  服务器同步（ItemInitializer）

**文件**: `EonVientianeServer/ItemInitializer.cs`

### 步骤 3.1 - 添加到GetAllItems()
```csharp
// 在 GetAllItems() 方法中的适当分类下添加：
("my_dice_id", "My Dice"),
// 或
("my_accessory_id", "My Accessory"),
```

### 步骤 3.2 - 添加到CreateItemFromStackData()（装备类）
```csharp
public static Equipment? CreateItemFromStackData(InventoryStackRecord stackData)
{
    return stackData.ItemId switch
    {
        "my_dice_id" => new MyDice(),
        "my_accessory_id" => new MyAccessory(),
        // ... 其他现有道具
        _ => null
    };
}
```

---

## 4️⃣  设置获取方式

### 方式A: 新用户初始发放
**文件**: `EonVientianeServer/ItemInitializer.cs` → `GetInitialInventory()`
```csharp
items.Add(new InitialInventoryItem 
{ 
    ItemId = "my_item_id", 
    ItemName = "My Item", 
    Quantity = 1 
});
```

### 方式B: 成就奖励
**文件**: `EonVientiane/AchievementSystem.cs` → `CreateRewardItem()`
```csharp
"my_item_id" => new MyAccessory(),
```

### 方式C: 测试账号
**文件**: `EonVientianeServer/ItemInitializer.cs` → `GetTestAccountInventory()`
（自动包含所有道具）

---

## 5️⃣  编译和测试

```bash
# 编译
dotnet build

# 运行本地测试
./start_local_test.sh

# 验证清单
✓ 道具在背包中显示
✓ 属性值正确应用
✓ 如是骰子：战斗逻辑正确
✓ 如是饰品：事件回调正确
✓ Clone方法工作正常
```

---

## 🔍 常见错误排查

| 症状 | 可能原因 | 检查项 |
|------|--------|--------|
| 道具不显示 | ID未注册 | ItemFactory.RegisterAllItems() |
| 战斗报错 | 返回值类型错误 | ActionResult/DefenseResult 构造参数 |
| 属性不生效 | 未在构造函数设置 | Attack/Defense/Health等初始值 |
| 服务器报错 | ItemInitializer中未同步 | GetAllItems() + CreateItemFromStackData() |
| 无法保存/加载 | CreateItemFromStackData缺少case | 检查switch语句 |

---

## 📍 关键位置快速导航

| 功能 | 文件 | 方法/类 | 行号参考 |
|------|------|--------|---------|
| 注册工厂 | InventoryManager.cs | ItemFactory.RegisterAllItems() | ~430行 |
| 道具列表 | ItemInitializer.cs | GetAllItems() | ~17行 |
| 装备创建 | ItemInitializer.cs | CreateItemFromStackData() | ~145行 |
| 骰子基类 | Item.cs | public abstract class Dice | ~230行 |
| 饰品基类 | Item.cs | public abstract class Accessory | ~345行 |
| 初始装备 | ItemInitializer.cs | GetRecommendedEquipment() | ~130行 |
| 成就奖励 | AchievementSystem.cs | CreateRewardItem() | ~200行 |

---

## 📝 命名规范

```
ID格式:        snake_case (全小写) → my_dice_name
类名格式:      PascalCase         → MyDiceName
描述长度:      简洁不超过50字符
颜色变量:      使用 Color.XxxColor 或 new Color(R,G,B)
```

---

## 🎮 战斗相关术语

- **AD**: Active Dice (主动骰子) - 用于攻击
- **PD**: Passive Dice (被动骰子) - 用于防御
- **ATKP**: Attack Power (攻击点数)
- **DEFP**: Defense Power (防御点数)
- **AVOP**: Avoid Power (闪避点数)
- **ActionResult**: 主动攻击的返回结果
- **DefenseResult**: 被动防御的返回结果

---

## 💾 属性参考

### Equipment 可用属性
```csharp
Attack = 0;        // 攻击加成
Defense = 0;       // 防御加成
Speed = 0;         // 速度加成
Health = 0;        // 生命值加成
Mana = 0;          // 魔力值加成
DisplayColor;      // UI显示颜色
MaxStackSize = 1;  // 堆叠数量（装备通常为1）
```

### Accessory 专用
```csharp
AccessorySlotsCost = 1;   // 消耗1个槽位
AccessorySlotsCost = -1;  // 提供1个额外槽位
```

---

## 🔗 文档链接

- **完整指南**: `docs/ITEM_CREATION_GUIDE.md`
- **D6参考实现**: `EonVientiane/Dices/D6Dice.cs`
- **SelfAccessory参考**: `EonVientiane/Accessories/SelfAccessory.cs`
- **道具CSV表**: `TODO/道具表-骰子.csv`

---

## ✅ 完整检查清单

- [ ] 创建了类文件
- [ ] 继承了正确的基类 (Dice 或 Accessory)
- [ ] 实现了所有抽象方法
- [ ] 在 RegisterAllItems() 中注册
- [ ] 在 GetAllItems() 中添加
- [ ] 如是装备类，在 CreateItemFromStackData() 中添加
- [ ] 设置了获取方式（初始/成就/其他）
- [ ] 编译成功（无错误）
- [ ] 本地测试通过
- [ ] 更新了CSV道具表
- [ ] 在主干代码中标记实现状态

---

## 🚀 快速示例

### 创建最简单的骰子

```csharp
// 文件: EonVientiane/Dices/SimpleDice.cs
public class SimpleDice : Dice
{
    private Random _random;
    
    public SimpleDice() : base("simple_dice", "简易骰", "Simple", DiceUsageType.Both)
    {
        _random = new Random();
    }
    
    public override int Roll() => _random.Next(1, 7);
    
    public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        var target = defenders[0];
        int atkp = Roll();
        return new ActionResult(true, $"掷出{atkp}点", target, atkp);
    }
    
    public override DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        int defp = Roll();
        int damage = Math.Max(0, attackDamage - defp);
        return new DefenseResult(defp, damage, $"防御{defp}点");
    }
    
    public override Item Clone() => new SimpleDice() 
    { 
        Attack = Attack, 
        Defense = Defense, 
        Speed = Speed, 
        Health = Health, 
        Mana = Mana 
    };
}
```

然后在 `ItemFactory.RegisterAllItems()` 中添加：
```csharp
_registry.RegisterItem("simple_dice", () => new SimpleDice());
```

---

**最后更新**: 2026-01-23  
**版本**: 1.0
