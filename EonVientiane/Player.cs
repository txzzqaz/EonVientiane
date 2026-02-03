using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 玩家阵营枚举
/// </summary>
public enum PlayerCamp
{
    Team1,
    Team2
}

/// <summary>
/// 玩家类
/// </summary>
public class Player
{
    /// <summary>
    /// 玩家唯一标识
    /// </summary>
    public string PlayerId { get; set; }
    
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; set; }
    
    /// <summary>
    /// 玩家所属阵营
    /// </summary>
    public PlayerCamp Camp { get; set; }
    
    /// <summary>
    /// 当前生命值
    /// </summary>
    public int CurrentHP { get; set; }
    
    /// <summary>
    /// 最大生命值
    /// </summary>
    public int MaxHP { get; set; }
    
    /// <summary>
    /// 护盾层数（可以抵挡伤害）
    /// </summary>
    public int ShieldLayers { get; set; }
    
    /// <summary>
    /// 玩家装备的物品（骰子、饰品等）
    /// </summary>
    public List<Equipment> EquippedItems { get; private set; }
    
    /// <summary>
    /// 当前生效的增益减益效果
    /// </summary>
    public List<GameEffect> ActiveEffects { get; private set; }
    
    /// <summary>
    /// 回合顺序（越小越先行动）
    /// </summary>
    public int TurnOrder { get; set; }
    
    /// <summary>
    /// 是否已在本回合行动过
    /// </summary>
    public bool HasActedThisRound { get; set; }
    
    /// <summary>
    /// 是否正在等待防御骰子响应
    /// </summary>
    public bool IsWaitingForDefense { get; set; }

    /// <summary>
    /// AD回合的预设行动序列（使用"预见"饰品时）
    /// Key: 骰子名称, Value: 该骰子的多个计划行动
    /// </summary>
    public Dictionary<string, PlannedActionSequence> PlannedActionsAD { get; private set; }

    /// <summary>
    /// PD回合的预设行动序列（使用"预见"饰品时）
    /// Key: 骰子名称, Value: 该骰子的多个计划行动
    /// </summary>
    public Dictionary<string, PlannedActionSequence> PlannedActionsPD { get; private set; }

    // 记录是否曾经受到过伤害（用于判定HP<=0的存活逻辑）
    private bool _hasTakenDamage;
    
    public Player(string playerId, string playerName, PlayerCamp camp)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        Camp = camp;
        CurrentHP = 0;
        MaxHP = 0;
        ShieldLayers = 0;
        EquippedItems = new List<Equipment>();
        ActiveEffects = new List<GameEffect>();
        HasActedThisRound = false;
        IsWaitingForDefense = false;
        PlannedActionsAD = new Dictionary<string, PlannedActionSequence>();
        PlannedActionsPD = new Dictionary<string, PlannedActionSequence>();
        _hasTakenDamage = false;
    }
    
    /// <summary>
    /// 添加装备
    /// </summary>
    public void AddEquipment(Equipment equipment)
    {
        EquippedItems.Add(equipment);
    }
    
    /// <summary>
    /// 移除装备
    /// </summary>
    public bool RemoveEquipment(Equipment equipment)
    {
        return EquippedItems.Remove(equipment);
    }
    
    /// <summary>
    /// 获取所有装备的骰子
    /// </summary>
    public List<Dice> GetEquippedDice()
    {
        return EquippedItems.OfType<Dice>().ToList();
    }
    
    /// <summary>
    /// 获取所有装备的饰品
    /// </summary>
    public List<Accessory> GetEquippedAccessories()
    {
        return EquippedItems.OfType<Accessory>().ToList();
    }
    
    /// <summary>
    /// 计算总属性加成（来自装备）
    /// </summary>
    public (int attack, int defense, int speed, int health, int mana) GetTotalStats()
    {
        int attack = 0, defense = 0, speed = 0, health = 0, mana = 0;
        
        foreach (var equipment in EquippedItems)
        {
            attack += equipment.Attack;
            defense += equipment.Defense;
            speed += equipment.Speed;
            health += equipment.Health;
            mana += equipment.Mana;
        }
        
        return (attack, defense, speed, health, mana);
    }
    
    /// <summary>
    /// 添加效果
    /// </summary>
    public void AddEffect(GameEffect effect)
    {
        if (effect != null)
        {
            ActiveEffects.Add(effect);
        }
    }
    
    /// <summary>
    /// 移除效果
    /// </summary>
    public bool RemoveEffect(GameEffect effect)
    {
        return ActiveEffects.Remove(effect);
    }
    
    /// <summary>
    /// 更新所有效果的持续时间
    /// </summary>
    public void UpdateEffects()
    {
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            var effect = ActiveEffects[i];
            effect.Update();
            
            if (effect.IsExpired)
            {
                ActiveEffects.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <returns>实际受到的伤害（可能被护盾抵挡）</returns>
    public int TakeDamage(int damage)
    {
        if (damage <= 0)
            return 0;
        
        int actualDamage = damage;
        
        // 护盾优先抵挡伤害
        if (ShieldLayers > 0)
        {
            ShieldLayers--;
            return 0; // 被护盾完全挡住，实际伤害为0
        }
        
        // 没有护盾则直接扣血
        CurrentHP -= actualDamage;
        _hasTakenDamage = true;
        
        return actualDamage;
    }
    
    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void Heal(int amount)
    {
        if (amount > 0)
        {
            CurrentHP = Math.Min(CurrentHP + amount, MaxHP);
        }
    }
    
    /// <summary>
    /// 检查是否已死亡
    /// </summary>
    public bool IsDead => _hasTakenDamage && CurrentHP <= 0;
    
    /// <summary>
    /// 重置回合状态
    /// </summary>
    public void ResetRoundState()
    {
        HasActedThisRound = false;
        IsWaitingForDefense = false;
    }

    /// <summary>
    /// 为AD回合添加计划行动
    /// </summary>
    public void AddPlannedActionAD(string diceName, string targetPlayerId = null, int customValue = 0)
    {
        if (!PlannedActionsAD.ContainsKey(diceName))
        {
            PlannedActionsAD[diceName] = new PlannedActionSequence(diceName);
        }
        PlannedActionsAD[diceName].AddAction(new PlannedAction(diceName, targetPlayerId, customValue));
    }

    /// <summary>
    /// 为PD回合添加计划行动
    /// </summary>
    public void AddPlannedActionPD(string diceName, string targetPlayerId = null, int customValue = 0)
    {
        if (!PlannedActionsPD.ContainsKey(diceName))
        {
            PlannedActionsPD[diceName] = new PlannedActionSequence(diceName);
        }
        PlannedActionsPD[diceName].AddAction(new PlannedAction(diceName, targetPlayerId, customValue));
    }

    /// <summary>
    /// 获取AD回合的下一个计划行动
    /// </summary>
    public PlannedAction GetNextPlannedActionAD(string diceName)
    {
        if (PlannedActionsAD.ContainsKey(diceName))
        {
            return PlannedActionsAD[diceName].GetAndRemoveFirstAction();
        }
        return null;
    }

    /// <summary>
    /// 获取PD回合的下一个计划行动
    /// </summary>
    public PlannedAction GetNextPlannedActionPD(string diceName)
    {
        if (PlannedActionsPD.ContainsKey(diceName))
        {
            return PlannedActionsPD[diceName].GetAndRemoveFirstAction();
        }
        return null;
    }

    /// <summary>
    /// 清空所有计划行动
    /// </summary>
    public void ClearAllPlannedActions()
    {
        PlannedActionsAD.Clear();
        PlannedActionsPD.Clear();
    }

    /// <summary>
    /// 是否有装备"预见"饰品
    /// </summary>
    public bool HasForesightAccessory()
    {
        return GetEquippedAccessories().Any(a => a is ForesightAccessory);
    }

}
