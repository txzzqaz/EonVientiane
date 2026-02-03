using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 战斗系统API - 提供战斗控制和扩展接口
/// </summary>
public static class BattleAPI
{
    /// <summary>
    /// 战斗开始事件
    /// </summary>
    public static event Action<Battle> BattleStarted;
    
    /// <summary>
    /// 回合开始事件
    /// </summary>
    public static event Action<Battle, int> RoundStarted;
    
    /// <summary>
    /// 玩家行动前事件
    /// </summary>
    public static event Action<Battle, Player> BeforePlayerAction;
    
    /// <summary>
    /// 玩家行动后事件
    /// </summary>
    public static event Action<Battle, Player> AfterPlayerAction;
    
    /// <summary>
    /// 伤害计算前事件（可修改伤害值）
    /// </summary>
    public static event Func<Player, Player, int, int> BeforeDamageCalculation;
    
    /// <summary>
    /// 伤害造成后事件
    /// </summary>
    public static event Action<Player, Player, int> AfterDamageDealt;
    
    /// <summary>
    /// 治疗前事件（可修改治疗量）
    /// </summary>
    public static event Func<Player, int, int> BeforeHeal;
    
    /// <summary>
    /// 治疗后事件
    /// </summary>
    public static event Action<Player, int> AfterHeal;
    
    /// <summary>
    /// 效果应用前事件
    /// </summary>
    public static event Action<Player, GameEffect> BeforeEffectApplied;
    
    /// <summary>
    /// 效果应用后事件
    /// </summary>
    public static event Action<Player, GameEffect> AfterEffectApplied;
    
    /// <summary>
    /// 战斗结束事件
    /// </summary>
    public static event Action<Battle, PlayerCamp?> BattleEnded;
    
    /// <summary>
    /// 自定义战斗规则
    /// </summary>
    private static readonly List<IBattleRule> _customRules = new();
    
    /// <summary>
    /// 添加自定义战斗规则
    /// </summary>
    public static void AddBattleRule(IBattleRule rule)
    {
        if (rule != null && !_customRules.Contains(rule))
        {
            _customRules.Add(rule);
        }
    }
    
    /// <summary>
    /// 移除自定义战斗规则
    /// </summary>
    public static void RemoveBattleRule(IBattleRule rule)
    {
        _customRules.Remove(rule);
    }
    
    /// <summary>
    /// 获取所有自定义规则
    /// </summary>
    public static IReadOnlyList<IBattleRule> GetCustomRules() => _customRules;
    
    /// <summary>
    /// 触发战斗开始事件
    /// </summary>
    internal static void InvokeBattleStarted(Battle battle)
    {
        BattleStarted?.Invoke(battle);
    }
    
    /// <summary>
    /// 触发回合开始事件
    /// </summary>
    internal static void InvokeRoundStarted(Battle battle, int round)
    {
        RoundStarted?.Invoke(battle, round);
    }
    
    /// <summary>
    /// 触发玩家行动前事件
    /// </summary>
    internal static void InvokeBeforePlayerAction(Battle battle, Player player)
    {
        BeforePlayerAction?.Invoke(battle, player);
    }
    
    /// <summary>
    /// 触发玩家行动后事件
    /// </summary>
    internal static void InvokeAfterPlayerAction(Battle battle, Player player)
    {
        AfterPlayerAction?.Invoke(battle, player);
    }
    
    /// <summary>
    /// 计算修改后的伤害值
    /// </summary>
    internal static int CalculateDamage(Player attacker, Player target, int baseDamage)
    {
        int damage = baseDamage;
        
        // 应用所有伤害修改器
        if (BeforeDamageCalculation != null)
        {
            foreach (var handler in BeforeDamageCalculation.GetInvocationList())
            {
                damage = ((Func<Player, Player, int, int>)handler)(attacker, target, damage);
            }
        }
        
        return Math.Max(0, damage);
    }
    
    /// <summary>
    /// 触发伤害造成后事件
    /// </summary>
    internal static void InvokeAfterDamageDealt(Player attacker, Player target, int damage)
    {
        AfterDamageDealt?.Invoke(attacker, target, damage);
    }
    
    /// <summary>
    /// 计算修改后的治疗量
    /// </summary>
    internal static int CalculateHeal(Player target, int baseHeal)
    {
        int heal = baseHeal;
        
        // 应用所有治疗修改器
        if (BeforeHeal != null)
        {
            foreach (var handler in BeforeHeal.GetInvocationList())
            {
                heal = ((Func<Player, int, int>)handler)(target, heal);
            }
        }
        
        return Math.Max(0, heal);
    }
    
    /// <summary>
    /// 触发治疗后事件
    /// </summary>
    internal static void InvokeAfterHeal(Player target, int amount)
    {
        AfterHeal?.Invoke(target, amount);
    }
    
    /// <summary>
    /// 触发效果应用前事件
    /// </summary>
    internal static void InvokeBeforeEffectApplied(Player target, GameEffect effect)
    {
        BeforeEffectApplied?.Invoke(target, effect);
    }
    
    /// <summary>
    /// 触发效果应用后事件
    /// </summary>
    internal static void InvokeAfterEffectApplied(Player target, GameEffect effect)
    {
        AfterEffectApplied?.Invoke(target, effect);
    }
    
    /// <summary>
    /// 触发战斗结束事件
    /// </summary>
    internal static void InvokeBattleEnded(Battle battle, PlayerCamp? winner)
    {
        BattleEnded?.Invoke(battle, winner);
    }
    
    /// <summary>
    /// 清除所有事件订阅（用于重置）
    /// </summary>
    public static void ClearAllEvents()
    {
        BattleStarted = null;
        RoundStarted = null;
        BeforePlayerAction = null;
        AfterPlayerAction = null;
        BeforeDamageCalculation = null;
        AfterDamageDealt = null;
        BeforeHeal = null;
        AfterHeal = null;
        BeforeEffectApplied = null;
        AfterEffectApplied = null;
        BattleEnded = null;
        _customRules.Clear();
    }
}

/// <summary>
/// 战斗规则接口 - 用于定义自定义战斗规则
/// </summary>
public interface IBattleRule
{
    /// <summary>
    /// 规则名称
    /// </summary>
    string RuleName { get; }
    
    /// <summary>
    /// 规则优先级（数值越小越先执行）
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// 回合开始时执行
    /// </summary>
    void OnRoundStart(Battle battle);
    
    /// <summary>
    /// 回合结束时执行
    /// </summary>
    void OnRoundEnd(Battle battle);
    
    /// <summary>
    /// 检查玩家是否可以行动
    /// </summary>
    bool CanPlayerAct(Battle battle, Player player);
    
    /// <summary>
    /// 修改行动顺序
    /// </summary>
    void ModifyTurnOrder(Battle battle, List<Player> players);
}
