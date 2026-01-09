using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EonVientiane;

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
/// 物品基类
/// </summary>
public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ItemType Type { get; set; }
    public int MaxStackSize { get; set; } = 1;
    public Color DisplayColor { get; set; } = Color.White;
    
    /// <summary>
    /// 是否可装备
    /// </summary>
    public virtual bool IsEquippable => false;
    
    public Item(string id, string name, string description, ItemType type)
    {
        Id = id;
        Name = name;
        Description = description;
        Type = type;
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
        return new Item(Id, Name, Description, Type)
        {
            MaxStackSize = MaxStackSize,
            DisplayColor = DisplayColor
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
    
    public override bool IsEquippable => true;
    
    public Equipment(string id, string name, string description, EquipmentType equipmentType)
        : base(id, name, description, ItemType.Equipment)
    {
        EquipmentType = equipmentType;
        MaxStackSize = 1; // 装备不可堆叠
    }
    
    public override Item Clone()
    {
        return new Equipment(Id, Name, Description, EquipmentType)
        {
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Health = Health,
            Mana = Mana,
            DisplayColor = DisplayColor
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
/// 骰子使用类型
/// </summary>
public enum DiceUsageType
{
    Active,   // 主动骰子 (AD)
    Passive,  // 被动骰子 (PD)
    Both      // 主动/被动通用骰子
}

/// <summary>
/// 骰子基类
/// </summary>
public abstract class Dice : Equipment
{
    public DiceUsageType UsageType { get; set; }
    
    protected Dice(string id, string name, string description, DiceUsageType usageType)
        : base(id, name, description, EquipmentType.Dice)
    {
        UsageType = usageType;
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
/// 主动行动结果
/// </summary>
public class ActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Player Target { get; set; }
    public int AttackPower { get; set; }
    
    public ActionResult(bool success, string message, Player target = null, int attackPower = 0)
    {
        Success = success;
        Message = message;
        Target = target;
        AttackPower = attackPower;
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

/// <summary>
/// D6 - 六面骰子
/// 主动使用时，roll出点数作为ATKP（攻击点数）
/// 被动使用时，roll出点数获得DEFP（防御点数）
/// ATKP <= DEFP 则完全防御，不受伤
/// ATKP > DEFP 则受到 ATKP - DEFP 点伤害
/// </summary>
public class D6Dice : Dice
{
    private Random _random;
    
    public D6Dice(DiceUsageType usageType = DiceUsageType.Both)
        : base("d6_dice", "D6", "Reroll your destiny.", usageType)
    {
        _random = new Random();
        DisplayColor = Color.White;
    }
    
    public override int Roll()
    {
        return _random.Next(1, 7); // 返回1-6
    }
    
    /// <summary>
    /// 作为主动骰子执行攻击
    /// </summary>
    public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        if (defenders == null || defenders.Count == 0)
            return new ActionResult(false, "没有可攻击的目标");
        
        // 随机选择一个防守者
        Player target = defenders[new Random().Next(defenders.Count)];
        int atkp = Roll();
        
        return new ActionResult(true, $"D6掷出{atkp}点攻击", target, atkp);
    }
    
    /// <summary>
    /// 作为被动骰子执行防御
    /// </summary>
    public override DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        int defp = Roll();
        int actualDamage = Math.Max(0, attackDamage - defp);
        
        string message = actualDamage == 0 
            ? $"D6掷出{defp}点完全防御！" 
            : $"D6掷出{defp}点，仍受到{actualDamage}点伤害";
        
        return new DefenseResult(defp, actualDamage, message);
    }
    
    public override Item Clone()
    {
        return new D6Dice(UsageType)
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

/// <summary>
/// 飞羽骰子 - 被动骰子(PD)
/// 为一个 (计数器 + ATKP) 面的骰子
/// roll出点数获得AVOP（闪避点数）
/// ATKP > AVOP 则闪避成功，不受伤
/// ATKP <= AVOP 则闪避失败，受到全部ATKP点伤害
/// 每次使用时计数器临时+1，游戏结束后清空
/// </summary>
public class FeatheredDice : Dice
{
    private Random _random;
    public int Counter { get; set; } = 0; // 计数器，游戏结束后清空
    
    public FeatheredDice()
        : base("feathered_dice", "飞羽骰子", "一小步.", DiceUsageType.Passive)
    {
        _random = new Random();
        DisplayColor = Color.LightCyan;
    }
    
    /// <summary>
    /// 根据当前计数器和攻击点数计算面数并掷骰子
    /// </summary>
    /// <param name="atkp">对方的攻击点数</param>
    /// <returns>闪避点数</returns>
    public int RollWithATKP(int atkp)
    {
        int dicefaces = Counter + atkp;
        int result = _random.Next(1, dicefaces + 1);
        
        // 使用后计数器临时+1
        Counter++;
        
        return result;
    }
    
    public override int Roll()
    {
        // 对于基础Roll，仅使用计数器
        int dicefaces = Counter + 1;
        return _random.Next(1, dicefaces + 1);
    }
    
    /// <summary>
    /// 作为被动骰子执行防御（闪避逻辑）
    /// </summary>
    public override DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        int avop = RollWithATKP(attackDamage);
        int actualDamage;
        string message;
        
        if (attackDamage > avop)
        {
            // 闪避失败
            actualDamage = attackDamage;
            message = $"飞羽骰子掷出{avop}点，闪避失败！受到全部{attackDamage}点伤害";
        }
        else
        {
            // 闪避成功
            actualDamage = 0;
            message = $"飞羽骰子掷出{avop}点，闪避成功！";
        }
        
        return new DefenseResult(avop, actualDamage, message);
    }
    
    /// <summary>
    /// 清空计数器（游戏结束时调用）
    /// </summary>
    public void ResetCounter()
    {
        Counter = 0;
    }
    
    public override Item Clone()
    {
        return new FeatheredDice()
        {
            Counter = Counter,
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Health = Health,
            Mana = Mana,
            DisplayColor = DisplayColor
        };
    }
}

/// <summary>
/// 饰品基类
/// </summary>
public abstract class Accessory : Equipment
{
    protected Accessory(string id, string name, string description)
        : base(id, name, description, EquipmentType.Accessory)
    {
    }
    
    /// <summary>
    /// 对局开始时的效果
    /// </summary>
    public virtual void OnBattleStart(BattleContext context) { }
    
    /// <summary>
    /// 获取提供的HP（某些饰品可能修改这个行为）
    /// </summary>
    public virtual int GetProvidedHP() => Health;
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

/// <summary>
/// 饰品：自我
/// 对局开始时提供20HP（生命值）
/// </summary>
public class SelfAccessory : Accessory
{
    public SelfAccessory()
        : base("self_accessory", "自我", "这就是你自己")
    {
        Health = 20;
        DisplayColor = Color.LightGreen;
    }
    
    public override void OnBattleStart(BattleContext context)
    {
        if (context.CanGainHP)
        {
            context.PlayerHP += Health;
        }
    }
    
    public override Item Clone()
    {
        return new SelfAccessory()
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

/// <summary>
/// 饰品：飞升之证
/// 无视所有其他道具提供的HP，强制玩家在对局开始时HP为0并且无法获得任何HP
/// 携带飞升之证每连续赢得5场胜利，本道具计数器永久+1
/// 在战斗开始时，获得计数器对应数量的护盾层数
/// 每层护盾可以抵挡一次没有被闪避/完美防御的攻击
/// </summary>
public class AscensionProofAccessory : Accessory
{
    public int Counter { get; set; } = 0; // 永久计数器
    public int ConsecutiveWins { get; set; } = 0; // 连续胜利次数
    
    public AscensionProofAccessory()
        : base("ascension_proof", "飞升之证", "终局？")
    {
        Health = 0;
        DisplayColor = Color.Gold;
    }
    
    public override void OnBattleStart(BattleContext context)
    {
        // 强制HP为0且无法获得HP
        context.PlayerHP = 0;
        context.CanGainHP = false;
        
        // 获得护盾层数等于计数器数量
        context.ShieldLayers = Counter;
    }
    
    /// <summary>
    /// 记录胜利，每连续5场胜利增加计数器
    /// </summary>
    public void OnWin()
    {
        ConsecutiveWins++;
        if (ConsecutiveWins >= 5)
        {
            Counter++;
            ConsecutiveWins = 0; // 重置连续胜利计数
        }
    }
    
    /// <summary>
    /// 失败时重置连续胜利计数
    /// </summary>
    public void OnLoss()
    {
        ConsecutiveWins = 0;
    }
    
    public override int GetProvidedHP() => 0; // 不提供HP
    
    public override Item Clone()
    {
        return new AscensionProofAccessory()
        {
            Counter = Counter,
            ConsecutiveWins = ConsecutiveWins,
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Health = Health,
            Mana = Mana,
            DisplayColor = DisplayColor
        };
    }
    
    /// <summary>
    /// 获取状态描述
    /// </summary>
    public string GetStatusDescription()
    {
        return $"计数器: {Counter} | 连续胜利: {ConsecutiveWins}/5";
    }
}
