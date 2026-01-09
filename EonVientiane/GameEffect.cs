using System;

namespace EonVientiane;

/// <summary>
/// 效果类型枚举
/// </summary>
public enum EffectType
{
    Positive,   // 增益
    Negative    // 减益
}

/// <summary>
/// 游戏效果基类
/// </summary>
public abstract class GameEffect
{
    /// <summary>
    /// 效果名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 效果描述
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// 效果类型
    /// </summary>
    public EffectType EffectType { get; set; }
    
    /// <summary>
    /// 效果剩余持续时间（回合数）
    /// </summary>
    public int DurationRemaining { get; protected set; }
    
    /// <summary>
    /// 效果总持续时间（回合数）
    /// </summary>
    public int TotalDuration { get; set; }
    
    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => DurationRemaining <= 0;
    
    protected GameEffect(string name, string description, EffectType effectType, int duration)
    {
        Name = name;
        Description = description;
        EffectType = effectType;
        TotalDuration = duration;
        DurationRemaining = duration;
    }
    
    /// <summary>
    /// 每回合更新效果
    /// </summary>
    public virtual void Update()
    {
        DurationRemaining--;
    }
    
    /// <summary>
    /// 应用效果到玩家
    /// </summary>
    public abstract void ApplyEffect(Player player);
    
    /// <summary>
    /// 移除效果时的处理
    /// </summary>
    public virtual void OnRemove(Player player) { }
}

/// <summary>
/// 伤害效果（持续伤害）
/// </summary>
public class DamageOverTimeEffect : GameEffect
{
    /// <summary>
    /// 每回合造成的伤害
    /// </summary>
    public int DamagePerTurn { get; set; }
    
    public DamageOverTimeEffect(string name, string description, int damagePerTurn, int duration)
        : base(name, description, EffectType.Negative, duration)
    {
        DamagePerTurn = damagePerTurn;
    }
    
    public override void ApplyEffect(Player player)
    {
        if (player != null && DamagePerTurn > 0)
        {
            player.TakeDamage(DamagePerTurn);
        }
    }
}

/// <summary>
/// 治疗效果（持续治疗）
/// </summary>
public class HealOverTimeEffect : GameEffect
{
    /// <summary>
    /// 每回合恢复的生命值
    /// </summary>
    public int HealPerTurn { get; set; }
    
    public HealOverTimeEffect(string name, string description, int healPerTurn, int duration)
        : base(name, description, EffectType.Positive, duration)
    {
        HealPerTurn = healPerTurn;
    }
    
    public override void ApplyEffect(Player player)
    {
        if (player != null && HealPerTurn > 0)
        {
            player.Heal(HealPerTurn);
        }
    }
}

/// <summary>
/// 护盾效果（增加护盾层数）
/// </summary>
public class ShieldEffect : GameEffect
{
    /// <summary>
    /// 增加的护盾层数
    /// </summary>
    public int ShieldLayers { get; set; }
    
    public ShieldEffect(string name, string description, int shieldLayers, int duration)
        : base(name, description, EffectType.Positive, duration)
    {
        ShieldLayers = shieldLayers;
    }
    
    public override void ApplyEffect(Player player)
    {
        if (player != null)
        {
            player.ShieldLayers += ShieldLayers;
        }
    }
    
    public override void OnRemove(Player player)
    {
        if (player != null && ShieldLayers > 0)
        {
            player.ShieldLayers -= ShieldLayers;
            player.ShieldLayers = Math.Max(0, player.ShieldLayers);
        }
    }
}

/// <summary>
/// 属性增强效果（临时增加属性）
/// </summary>
public class StatBoostEffect : GameEffect
{
    public int AttackBoost { get; set; }
    public int DefenseBoost { get; set; }
    public int SpeedBoost { get; set; }
    
    public StatBoostEffect(string name, string description, int attackBoost, int defenseBoost, int speedBoost, int duration)
        : base(name, description, EffectType.Positive, duration)
    {
        AttackBoost = attackBoost;
        DefenseBoost = defenseBoost;
        SpeedBoost = speedBoost;
    }
    
    public override void ApplyEffect(Player player)
    {
        // 该效果的计算在战斗逻辑中进行
        // 这里仅作为标记效果存在
    }
}

/// <summary>
/// 属性削弱效果（临时降低属性）
/// </summary>
public class StatDebuffEffect : GameEffect
{
    public int AttackDebuff { get; set; }
    public int DefenseDebuff { get; set; }
    public int SpeedDebuff { get; set; }
    
    public StatDebuffEffect(string name, string description, int attackDebuff, int defenseDebuff, int speedDebuff, int duration)
        : base(name, description, EffectType.Negative, duration)
    {
        AttackDebuff = attackDebuff;
        DefenseDebuff = defenseDebuff;
        SpeedDebuff = speedDebuff;
    }
    
    public override void ApplyEffect(Player player)
    {
        // 该效果的计算在战斗逻辑中进行
        // 这里仅作为标记效果存在
    }
}

/// <summary>
/// 眩晕效果（无法行动）
/// </summary>
public class StunEffect : GameEffect
{
    public StunEffect(string name, string description, int duration)
        : base(name, description, EffectType.Negative, duration)
    {
    }
    
    public override void ApplyEffect(Player player)
    {
        // 眩晕效果在回合逻辑中检查
    }
}

/// <summary>
/// 毒性效果（每回合递增伤害）
/// </summary>
public class PoisonEffect : GameEffect
{
    /// <summary>
    /// 初始伤害
    /// </summary>
    public int BaseDamage { get; set; }
    
    /// <summary>
    /// 每回合递增伤害
    /// </summary>
    public int DamageIncrement { get; set; }
    
    private int _currentDamage;
    
    public PoisonEffect(string name, string description, int baseDamage, int damageIncrement, int duration)
        : base(name, description, EffectType.Negative, duration)
    {
        BaseDamage = baseDamage;
        DamageIncrement = damageIncrement;
        _currentDamage = baseDamage;
    }
    
    public override void ApplyEffect(Player player)
    {
        if (player != null && _currentDamage > 0)
        {
            player.TakeDamage(_currentDamage);
            _currentDamage += DamageIncrement;
        }
    }
}

/// <summary>
/// 免疫效果（免疫指定效果类型）
/// </summary>
public class ImmunityEffect : GameEffect
{
    /// <summary>
    /// 免疫的效果类型
    /// </summary>
    public EffectType ImmunityTo { get; set; }
    
    public ImmunityEffect(string name, string description, EffectType immunityTo, int duration)
        : base(name, description, EffectType.Positive, duration)
    {
        ImmunityTo = immunityTo;
    }
    
    public override void ApplyEffect(Player player)
    {
        // 免疫效果在添加新效果时检查
    }
}
