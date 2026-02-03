using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 刮痧师傅 - 六面骰子
/// 主动使用时：
/// 1. 掷六面骰，获得ATKP点攻击力
/// 2. 施加给敌方
/// 3. 若对方被动回合伤害未被完全格挡（造成了伤害）：
///    - 根据造成伤害获得MITP（倍数点数）
///    - 立即再进行MITP次(6-MITP)面骰的投掷
///    - 得到的总数为再次获得的ATKP
///    - 略过对方PD回合，直接施加给敌方造成伤害
/// 描述：驽马十驾，功在不舍
/// 获取方式：成就"刮痧"（一局游戏内连续10回合造成了并且只造成1点伤害）
/// </summary>
public class GuaShaParquetDice : Dice
{
    private Random _random;
    
    public GuaShaParquetDice()
        : base("guasha_parquet", "刮痧师傅", "驽马十驾，功在不舍", DiceUsageType.Active, "yyzh")
    {
        _random = new Random();
        DisplayColor = Color.Orange;
    }
    
    public override int Roll()
    {
        return _random.Next(1, 7); // 返回1-6
    }
    
    /// <summary>
    /// 进行(6-turns)面的额外骰子投掷
    /// </summary>
    private int RollAdditionalDice(int turns)
    {
        int faces = Math.Max(1, 6 - turns);
        return _random.Next(1, faces + 1);
    }
    
    /// <summary>
    /// 执行再次掷骰效果
    /// 当防御未能完全格挡伤害时触发
    /// </summary>
    public int ExecuteRepeatedRoll(int actualDamage)
    {
        if (actualDamage <= 0)
            return 0;
        
        // 进行 actualDamage 次 (6 - actualDamage) 面骰的投掷
        int totalAdditionalDamage = 0;
        for (int i = 0; i < actualDamage; i++)
        {
            totalAdditionalDamage += RollAdditionalDice(actualDamage);
        }
        
        return totalAdditionalDamage;
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
        
        return new ActionResult(true, $"刮痧师傅掷出{atkp}点攻击", target, atkp);
    }
    
    /// <summary>
    /// 克隆对象
    /// </summary>
    public override Item Clone()
    {
        return new GuaShaParquetDice()
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
