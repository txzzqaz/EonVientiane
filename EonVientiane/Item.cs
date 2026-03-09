using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EonVientiane;

/// ═══════════════════════════════════════════════════════════════════════════════
/// ████████████████████████  道具系统核心定义  ████████████████████████
/// ═══════════════════════════════════════════════════════════════════════════════
/// 
/// 【快速导航】
/// • 物品基类 (Item): 第50行左右
/// • 装备基类 (Equipment): 第110行左右
/// • 骰子基类 (Dice): 第205行左右
/// • 饰品基类 (Accessory): 第230行左右
/// 
/// 【道具创建完整指南】
/// 📄 详见: docs/ITEM_CREATION_GUIDE.md
/// 
/// 【快速创建步骤】
/// 1. 创建新文件: EonVientiane/Dices/YourDice.cs 或 Accessories/YourAccessory.cs
/// 2. 继承合适的基类 (Dice 或 Accessory)
/// 3. 在 InventoryManager.cs 的 ItemFactory.RegisterAllItems() 中注册
/// 4. 在 ItemInitializer.cs 中添加到 GetAllItems()
/// 5. 编译 → 测试
/// 
/// 【类继承关系】
/// Item (基类)
///  ├─ Equipment (装备基类)
///  │   ├─ Dice (骰子)
///  │   └─ Accessory (饰品)
///  └─ 其他物品类型
/// 
/// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Consumable,  // 消耗品
    Equipment,   // 装备
    Material,    // 材料
    Quest,       // 任务物品
    Other        // 其他
}

/// <summary>
/// 装备类型枚举
/// </summary>
public enum EquipmentType
{
    None,        // 非装备
    Dice,        // 骰子
    Accessory    // 饰品
}

/// <summary>
/// 骰子使用类型
/// </summary>
public enum DiceUsageType
{
    Active,   // 主动骰子 (AD)
    Passive,  // 被动骰子 (PD)
    Both      // 主动/被动通用骰子
}

/// <summary>
/// 物品基类
/// </summary>
public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Function { get; set; } // 功能说明
    public ItemType Type { get; set; }
    public int MaxStackSize { get; set; } = 1;
    public Color DisplayColor { get; set; } = Color.White;
    public string Creator { get; set; } = "qaz"; // 创作者，默认为qaz
    public string IconAsset { get; set; } = string.Empty; // 图标资源路径（Content/ 下相对路径）
    public string IconRendererKey { get; set; } = string.Empty; // 自定义图标渲染器键（用于动画/特效）
    
    /// <summary>
    /// 是否可装备
    /// </summary>
    public virtual bool IsEquippable => false;
    
    public Item(string id, string name, string description, ItemType type, string creator = "qaz", string function = "")
    {
        Id = id;
        Name = name;
        Description = description;
        Function = function;
        Type = type;
        Creator = creator;
    }
    
    /// <summary>
    /// 物品使用（可由子类重写）
    /// </summary>
    public virtual void Use()
    {
        System.Diagnostics.Debug.WriteLine($"使用物品: {Name}");
    }
    
    /// <summary>
    /// 克隆物品
    /// </summary>
    public virtual Item Clone()
    {
        return new Item(Id, Name, Description, Type, Creator, Function)
        {
            MaxStackSize = MaxStackSize,
            DisplayColor = DisplayColor,
            IconAsset = IconAsset,
            IconRendererKey = IconRendererKey
        };
    }
}

/// <summary>
/// 装备类
/// </summary>
public class Equipment : Item
{
    public EquipmentType EquipmentType { get; set; }
    
    // 装备属性
    public int Attack { get; set; } = 0;
    public int Defense { get; set; } = 0;
    public int Speed { get; set; } = 0;
    public int Health { get; set; } = 0;
    public int Mana { get; set; } = 0;
    
    /// <summary>
    /// 饰品槽消耗数量（仅对Accessory有效）
    /// 正数：消耗的槽位数（默认1）
    /// 负数：提供的额外槽位数（例如-1表示提供1个额外槽位）
    /// </summary>
    public int AccessorySlotsCost { get; set; } = 1;
    
    public override bool IsEquippable => true;
    
    public Equipment(string id, string name, string description, EquipmentType equipmentType, string creator = "qaz", string function = "")
        : base(id, name, description, ItemType.Equipment, creator, function)
    {
        EquipmentType = equipmentType;
        MaxStackSize = 1; // 装备不可堆叠
    }
    
    public override Item Clone()
    {
        return new Equipment(Id, Name, Description, EquipmentType, Creator, Function)
        {
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Health = Health,
            Mana = Mana,
            DisplayColor = DisplayColor,
            AccessorySlotsCost = AccessorySlotsCost,
            IconAsset = IconAsset,
            IconRendererKey = IconRendererKey
        };
    }
    
    /// <summary>
    /// 获取装备属性描述
    /// </summary>
    public string GetStatsDescription()
    {
        var stats = new System.Collections.Generic.List<string>();
        if (Attack > 0) stats.Add($"攻击+{Attack}");
        if (Defense > 0) stats.Add($"防御+{Defense}");
        if (Speed > 0) stats.Add($"速度+{Speed}");
        if (Health > 0) stats.Add($"生命+{Health}");
        if (Mana > 0) stats.Add($"魔力+{Mana}");
        return string.Join(", ", stats);
    }
}

/// <summary>
/// 物品堆叠（背包中的物品实例）
/// </summary>
public class ItemStack
{
    public Item Item { get; set; }
    public int Quantity { get; set; }
    public string StackId { get; set; }
    
    public ItemStack(Item item, int quantity = 1, string stackId = null)
    {
        Item = item;
        Quantity = Math.Clamp(quantity, 1, item.MaxStackSize);
        StackId = string.IsNullOrWhiteSpace(stackId) ? Guid.NewGuid().ToString("N") : stackId;
    }
    
    /// <summary>
    /// 是否可以继续堆叠
    /// </summary>
    public bool CanStack => Quantity < Item.MaxStackSize;
    
    /// <summary>
    /// 添加数量
    /// </summary>
    public int AddQuantity(int amount)
    {
        int canAdd = Math.Min(amount, Item.MaxStackSize - Quantity);
        Quantity += canAdd;
        return amount - canAdd; // 返回剩余无法添加的数量
    }
    
    /// <summary>
    /// 移除数量
    /// </summary>
    public bool RemoveQuantity(int amount)
    {
        if (amount > Quantity) return false;
        Quantity -= amount;
        return true;
    }
}

/// <summary>
/// 支持手动录入点数的骰子
/// </summary>
public interface IManualRollDice
{
    /// <summary>
    /// 是否需要手动输入（用于客户端弹窗判断）
    /// </summary>
    bool RequiresManualInput { get; }

    /// <summary>
    /// 设置本次掷骰子使用的手动点数
    /// </summary>
    /// <param name="value">手动输入的点数，null表示无效</param>
    void SetManualRoll(int? value);
}

/// <summary>
/// 支持计数器的骰子
/// </summary>
public interface ICounterDice
{
    /// <summary>
    /// 计数器数值（允许为负）
    /// </summary>
    int Counter { get; set; }
}

/// ╔════════════════════════════════════════════════════════════════════════╗
/// ║                    ★ 骰子基类 - 战斗核心系统 ★                        ║
/// ╠════════════════════════════════════════════════════════════════════════╣
/// ║ 【创建新骰子的完整流程】                                             ║
/// ║                                                                        ║
/// ║ 1️⃣  创建类文件                                                        ║
/// ║     路径: EonVientiane/Dices/YourDiceName.cs                          ║
/// ║     继承: public class YourDiceName : Dice                            ║
/// ║                                                                        ║
/// ║ 2️⃣  实现三个关键方法                                                  ║
/// ║     • Roll() - 返回掷骰子结果                                         ║
/// ║     • ExecuteActiveAction() - 主动攻击逻辑(AD骰)                      ║
/// ║     • ExecutePassiveAction() - 被动防御逻辑(PD骰)                     ║
/// ║     • Clone() - 复制物品实例                                          ║
/// ║                                                                        ║
/// ║ 3️⃣  注册道具                                                          ║
/// ║     在 InventoryManager.cs 中找到 RegisterAllItems()                 ║
/// ║     添加: _registry.RegisterItem("dice_id", () => new YourDiceName()); ║
/// ║                                                                        ║
/// ║ 4️⃣  服务器同步                                                        ║
/// ║     在 ItemInitializer.cs 中：                                        ║
/// ║     • GetAllItems() - 添加到道具列表                                  ║
/// ║     • CreateItemFromStackData() - 添加创建逻辑                        ║
/// ║                                                                        ║
/// ║ 5️⃣  测试                                                              ║
/// ║     编译 → 运行 → 验证战斗逻辑                                        ║
/// ║                                                                        ║
/// ╚════════════════════════════════════════════════════════════════════════╝
/// 
/// <summary>
/// 骰子基类
/// </summary>
public abstract class Dice : Equipment
{
    public DiceUsageType UsageType { get; set; }
    
    protected Dice(string id, string name, string description, DiceUsageType usageType, string creator = "qaz", string function = "")
        : base(id, name, description, EquipmentType.Dice, creator, function)
    {
        UsageType = usageType;
        IconAsset = $"Icons/Dice/{id}"; // 默认骰子图标路径（若不存在则不显示）
    }
    
    /// <summary>
    /// 掷骰子，返回结果点数
    /// </summary>
    public abstract int Roll();
    
    /// <summary>
    /// 作为主动骰子(AD)执行行动
    /// 返回null表示跳过行动，否则返回执行结果信息
    /// </summary>
    public virtual ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        return null; // 子类覆写实现
    }
    
    /// <summary>
    /// 作为被动骰子(PD)执行防御
    /// 返回null表示跳过防御，否则返回防御结果
    /// </summary>
    public virtual DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        return null; // 子类覆写实现
    }

    /// <summary>
    /// 攻击点数结算时触发（用于骰子动态加成）
    /// </summary>
    public virtual void OnAttackPowerCalculation(DiceAttackContext context) { }
    
    /// <summary>
    /// 获取骰子类型标签
    /// </summary>
    public string GetDiceTypeLabel() => UsageType switch
    {
        DiceUsageType.Active => "[AD]",
        DiceUsageType.Passive => "[PD]",
        DiceUsageType.Both => "[AD/PD]",
        _ => "[?]"
    };
}

/// <summary>
/// 骰子攻击结算上下文
/// </summary>
public class DiceAttackContext
{
    private readonly List<string> _logs = new();

    public Player Attacker { get; }
    public int AttackPower { get; set; }
    public IReadOnlyList<string> Logs => _logs;

    public DiceAttackContext(Player attacker, int attackPower)
    {
        Attacker = attacker;
        AttackPower = attackPower;
    }

    public void AddLog(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _logs.Add(message);
        }
    }
}

/// <summary>
/// 主动行动结果
/// </summary>
public class ActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Player Target { get; set; }
    public int AttackPower { get; set; }
    public bool TriggersDefense { get; set; }
    
    public ActionResult(bool success, string message, Player target = null, int attackPower = 0, bool triggersDefense = true)
    {
        Success = success;
        Message = message;
        Target = target;
        AttackPower = attackPower;
        TriggersDefense = triggersDefense;
    }
}

/// <summary>
/// 防御结果
/// </summary>
public class DefenseResult
{
    public int DefensePower { get; set; }
    public int ActualDamage { get; set; }
    public string Message { get; set; }
    
    public DefenseResult(int defensePower, int actualDamage, string message)
    {
        DefensePower = defensePower;
        ActualDamage = actualDamage;
        Message = message;
    }
}

/// ╔════════════════════════════════════════════════════════════════════════╗
/// ║                    ★ 饰品基类 - 被动增益系统 ★                        ║
/// ╠════════════════════════════════════════════════════════════════════════╣
/// ║ 【创建新饰品的完整流程】                                             ║
/// ║                                                                        ║
/// ║ 1️⃣  创建类文件                                                        ║
/// ║     路径: EonVientiane/Accessories/YourAccessoryName.cs               ║
/// ║     继承: public class YourAccessoryName : Accessory                  ║
/// ║                                                                        ║
/// ║ 2️⃣  实现事件回调方法                                                  ║
/// ║     • OnBattleStart() - 战斗开始时触发                                ║
/// ║     • OnHit() - 受到攻击时触发                                        ║
/// ║     • OnVictory() - 获胜时触发                                        ║
/// ║     • OnDefeat() - 失败时触发                                         ║
/// ║     • Clone() - 复制物品实例                                          ║
/// ║                                                                        ║
/// ║ 3️⃣  设置饰品槽位                                                      ║
/// ║     AccessorySlotsCost = 1;   // 消耗1个槽位(默认)                    ║
/// ║     AccessorySlotsCost = -1;  // 提供1个额外槽位                      ║
/// ║                                                                        ║
/// ║ 4️⃣  注册和同步（同骰子流程）                                          ║
/// ║     在 ItemFactory.RegisterAllItems() 中注册                          ║
/// ║     在 ItemInitializer.cs 中同步                                      ║
/// ║                                                                        ║
/// ║ 5️⃣  测试                                                              ║
/// ║     编译 → 运行 → 验证属性生效                                        ║
/// ║                                                                        ║
/// ╚════════════════════════════════════════════════════════════════════════╝
/// 
/// <summary>
/// 饰品基类
/// </summary>
public abstract class Accessory : Equipment
{
    protected Accessory(string id, string name, string description, string creator = "qaz", string function = "")
        : base(id, name, description, EquipmentType.Accessory, creator, function)
    {
    }
    
    /// <summary>
    /// 对局开始时的效果
    /// </summary>
    public virtual void OnBattleStart(BattleContext context) { }

    /// <summary>
    /// 攻击点数结算时触发（用于饰品动态加成）
    /// </summary>
    public virtual void OnAttackPowerCalculation(AccessoryAttackContext context) { }
    
    /// <summary>
    /// 获取提供的HP（某些饰品可能修改这个行为）
    /// </summary>
    public virtual int GetProvidedHP() => Health;
}

/// <summary>
/// 饰品攻击触发阶段
/// </summary>
public enum AccessoryAttackTriggerPhase
{
    PreBloodTraceBonus,
    PostBloodTraceBonus
}

/// <summary>
/// 饰品攻击结算上下文
/// </summary>
public class AccessoryAttackContext
{
    private readonly List<string> _logs = new();

    public Player Attacker { get; }
    public int RollValue { get; }
    public TimeSpan SlowestActionTime { get; }
    public AccessoryAttackTriggerPhase Phase { get; }
    public int AttackPower { get; set; }
    public bool WandererHeartTriggered { get; set; }
    public IReadOnlyList<string> Logs => _logs;

    public AccessoryAttackContext(Player attacker, int rollValue, int attackPower, TimeSpan slowestActionTime, AccessoryAttackTriggerPhase phase)
    {
        Attacker = attacker;
        RollValue = rollValue;
        AttackPower = attackPower;
        SlowestActionTime = slowestActionTime;
        Phase = phase;
    }

    public void AddLog(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _logs.Add(message);
        }
    }
}

/// <summary>
/// 战斗上下文，用于传递战斗相关的状态信息
/// </summary>
public class BattleContext
{
    public int PlayerHP { get; set; }
    public int ShieldLayers { get; set; }
    public bool CanGainHP { get; set; } = true;
    
    public BattleContext()
    {
        PlayerHP = 0;
        ShieldLayers = 0;
    }
}
