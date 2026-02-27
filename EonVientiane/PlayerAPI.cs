using System;
using System.Collections.Generic;
using System.Linq;

using static EonVientiane.RankTier;

namespace EonVientiane;

/// <summary>
/// 玩家系统API扩展 - 为Player类添加更多扩展方法
/// </summary>
public static class PlayerAPI
{
    /// <summary>
    /// 玩家属性变化事件
    /// </summary>
    public static event Action<Player, string, object, object> PropertyChanged;
    
    /// <summary>
    /// 玩家死亡事件
    /// </summary>
    public static event Action<Player> PlayerDied;
    
    /// <summary>
    /// 玩家复活事件
    /// </summary>
    public static event Action<Player> PlayerRevived;
    
    /// <summary>
    /// 装备变化事件
    /// </summary>
    public static event Action<Player, Equipment, bool> EquipmentChanged; // bool: true=装备, false=卸下
    
    /// <summary>
    /// 获取玩家当前所有主动骰子
    /// </summary>
    public static List<Dice> GetActiveDice(this Player player)
    {        return player.EquippedItems
            .OfType<Dice>()
            .ToList();
    }
    
    /// <summary>
    /// 获取玩家当前所有被动骰子
    /// </summary>
    public static List<Dice> GetPassiveDice(this Player player)
    {
        return player.EquippedItems
            .OfType<Dice>()
            .ToList();
    }
    
    /// <summary>
    /// 获取玩家当前所有饰品
    /// </summary>
    public static List<Accessory> GetAccessories(this Player player)
    {
        return player.EquippedItems
            .OfType<Accessory>()
            .ToList();
    }
    
    /// <summary>
    /// 检查玩家是否装备了特定物品
    /// </summary>
    public static bool HasEquipment(this Player player, string itemId)
    {
        return player.EquippedItems.Any(e => e.Id == itemId);
    }
    
    /// <summary>
    /// 检查玩家是否有特定效果（按名称）
    /// </summary>
    public static bool HasEffectByName(this Player player, string effectName)
    {
        return player.ActiveEffects.Any(e => e.Name == effectName);
    }
    
    /// <summary>
    /// 获取特定名称的所有效果
    /// </summary>
    public static List<GameEffect> GetEffectsByName(this Player player, string effectName)
    {
        return player.ActiveEffects
            .Where(e => e.Name == effectName)
            .ToList();
    }
    
    /// <summary>
    /// 检查玩家是否有特定类型的效果
    /// </summary>
    public static bool HasEffectType(this Player player, EffectType effectType)
    {
        return player.ActiveEffects.Any(e => e.EffectType == effectType);
    }
    
    /// <summary>
    /// 获取玩家总攻击力（包括装备和效果加成）
    /// </summary>
    public static int GetTotalAttackPower(this Player player)
    {
        int basePower = 0;
        
        // 计算装备提供的攻击力
        foreach (var equipment in player.EquippedItems)
        {
            if (equipment is Dice dice)
            {
                // 这里可以根据骰子类型计算攻击力
            }
        }
        
        // 计算效果加成 - 通过效果名称判断
        foreach (var effect in player.ActiveEffects)
        {
            if (effect.Name.Contains("攻击") || effect.Name.Contains("Attack"))
            {
                // 从效果中提取数值 - 可以根据具体效果实现
            }
        }
        
        return basePower;
    }
    
    /// <summary>
    /// 获取玩家总防御力
    /// </summary>
    public static int GetTotalDefense(this Player player)
    {
        int defense = player.ShieldLayers;
        
        foreach (var effect in player.ActiveEffects)
        {
            if (effect.Name.Contains("防御") || effect.Name.Contains("Defense"))
            {
                // 从效果中提取数值
            }
        }
        
        return defense;
    }
    
    /// <summary>
    /// 检查玩家是否处于无敌状态
    /// </summary>
    public static bool IsInvulnerable(this Player player)
    {
        return player.HasEffectByName("无敌") || player.HasEffectByName("Invulnerable");
    }
    
    /// <summary>
    /// 检查玩家是否被冻结
    /// </summary>
    public static bool IsFrozen(this Player player)
    {
        return player.HasEffectByName("冰冻") || player.HasEffectByName("Frozen");
    }
    
    /// <summary>
    /// 检查玩家是否被眩晕
    /// </summary>
    public static bool IsStunned(this Player player)
    {
        return player.HasEffectByName("眩晕") || player.HasEffectByName("Stunned");
    }
    
    /// <summary>
    /// 获取玩家生命值百分比
    /// </summary>
    public static float GetHealthPercentage(this Player player)
    {
        if (player.MaxHP <= 0) return 0;
        return (float)player.CurrentHP / player.MaxHP;
    }
    
    /// <summary>
    /// 检查玩家是否濒死（生命值低于阈值）
    /// </summary>
    public static bool IsLowHealth(this Player player, float threshold = 0.3f)
    {
        return player.GetHealthPercentage() <= threshold;
    }
    
    /// <summary>
    /// 移除特定名称的所有效果
    /// </summary>
    public static void RemoveEffectsByName(this Player player, string effectName)
    {
        player.ActiveEffects.RemoveAll(e => e.Name == effectName);
    }
    
    /// <summary>
    /// 清除所有负面效果
    /// </summary>
    public static void ClearDebuffs(this Player player)
    {
        player.ActiveEffects.RemoveAll(e => e.EffectType == EffectType.Negative);
    }
    
    /// <summary>
    /// 清除所有正面效果
    /// </summary>
    public static void ClearBuffs(this Player player)
    {
        player.ActiveEffects.RemoveAll(e => e.EffectType == EffectType.Positive);
    }
    
    /// <summary>
    /// 触发属性变化事件
    /// </summary>
    internal static void InvokePropertyChanged(Player player, string propertyName, object oldValue, object newValue)
    {
        PropertyChanged?.Invoke(player, propertyName, oldValue, newValue);
    }
    
    /// <summary>
    /// 触发玩家死亡事件
    /// </summary>
    internal static void InvokePlayerDied(Player player)
    {
        PlayerDied?.Invoke(player);
    }
    
    /// <summary>
    /// 触发玩家复活事件
    /// </summary>
    internal static void InvokePlayerRevived(Player player)
    {
        PlayerRevived?.Invoke(player);
    }
    
    /// <summary>
    /// 触发装备变化事件
    /// </summary>
    internal static void InvokeEquipmentChanged(Player player, Equipment equipment, bool equipped)
    {
        EquipmentChanged?.Invoke(player, equipment, equipped);
    }
}

/// <summary>
/// 玩家构建器 - 用于方便地创建和配置玩家
/// </summary>
public class PlayerBuilder
{
    private string _playerId;
    private string _playerName;
    private PlayerCamp _camp;
    private int _maxHP = 100;
    private int _currentHP = 100;
    private int _shieldLayers = 0;
    private readonly List<Equipment> _equipment = new();
    private readonly List<GameEffect> _effects = new();
    
    public PlayerBuilder WithId(string id)
    {
        _playerId = id;
        return this;
    }
    
    public PlayerBuilder WithName(string name)
    {
        _playerName = name;
        return this;
    }
    
    public PlayerBuilder InCamp(PlayerCamp camp)
    {
        _camp = camp;
        return this;
    }
    
    public PlayerBuilder WithMaxHP(int maxHP)
    {
        _maxHP = maxHP;
        return this;
    }
    
    public PlayerBuilder WithCurrentHP(int currentHP)
    {
        _currentHP = currentHP;
        return this;
    }
    
    public PlayerBuilder WithShield(int layers)
    {
        _shieldLayers = layers;
        return this;
    }
    
    public PlayerBuilder WithEquipment(Equipment equipment)
    {
        _equipment.Add(equipment);
        return this;
    }
    
    public PlayerBuilder WithEffect(GameEffect effect)
    {
        if (effect != null)
        {
            _effects.Add(effect);
        }
        return this;
    }
    
    private RankTier _rankTier = RankTier.Stardust;
    private int _rankScore = 0;

    public PlayerBuilder WithRankTier(RankTier tier)
    {
        _rankTier = tier;
        return this;
    }

    public PlayerBuilder WithRankScore(int score)
    {
        _rankScore = score;
        return this;
    }

    public Player Build()
    {
        var player = new Player(_playerId, _playerName, _camp)
        {
            MaxHP = _maxHP,
            CurrentHP = _currentHP,
            ShieldLayers = _shieldLayers,
            RankTier = _rankTier,
            RankScore = _rankScore
        };
        
        foreach (var equipment in _equipment)
        {
            player.AddEquipment(equipment);
        }
        
        foreach (var effect in _effects)
        {
            player.AddEffect(effect);
        }
        
        return player;
    }
}
