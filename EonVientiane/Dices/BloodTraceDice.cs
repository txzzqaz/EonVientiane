using Microsoft.Xna.Framework;
using System;

namespace EonVientiane;

/// <summary>
/// 血痕 - 被动骰子(PD)
/// 3面骰
/// 不进行防御，直接承受全额伤害
/// roll出点数获得BLOP，玩家下一次使用AD时ATKP增加(ATKP × BLOP)
/// 描述：预言石上血痕现
/// 获取方式：不使用PD获得胜利
/// </summary>
public class BloodTraceDice : Dice
{
    private Random _random;

    /// <summary>
    /// 最近一次掷出的BLOP点数
    /// </summary>
    public int LastBloodTraceRoll { get; private set; }

    /// <summary>
    /// 待应用到下一次AD攻击的BLOP倍率
    /// </summary>
    public int PendingAttackBonusMultiplier { get; private set; }

    public BloodTraceDice()
        : base(
            id: "blood_trace",
            name: "血痕",
            description: "预言石上血痕现",
            usageType: DiceUsageType.Passive,
            function: "掷三面骰获得BLOP。放弃防御，直接受到全部伤害。下一次AD攻击ATKP增加(ATKP×BLOP)"
        )
    {
        _random = new Random();
        DisplayColor = Color.DarkRed;
    }

    public override int Roll()
    {
        return _random.Next(1, 4); // 返回1-3
    }

    /// <summary>
    /// 作为被动骰子执行防御
    /// 血痕的特殊机制：不进行防御，直接承受全额伤害，并获得下一次AD增益
    /// </summary>
    public override DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        int blop = Roll();
        LastBloodTraceRoll = blop;
        PendingAttackBonusMultiplier = blop;

        string message = $"血痕掷出{blop}点，放弃防御受到全部{attackDamage}点伤害；下次AD攻击增加(ATKP×{blop})";
        return new DefenseResult(0, attackDamage, message);
    }

    public override void OnAttackPowerCalculation(DiceAttackContext context)
    {
        if (context == null)
            return;

        if (PendingAttackBonusMultiplier <= 0 || context.AttackPower <= 0)
            return;

        int baseAttackPower = context.AttackPower;
        long bonus = (long)baseAttackPower * PendingAttackBonusMultiplier;
        long enhanced = (long)baseAttackPower + bonus;
        if (enhanced > int.MaxValue)
        {
            enhanced = int.MaxValue;
        }

        context.AttackPower = (int)enhanced;
        context.AddLog($"{context.Attacker?.PlayerName}血痕触发！下一次攻击额外增加({baseAttackPower}×{PendingAttackBonusMultiplier})，提升至{context.AttackPower}");
        PendingAttackBonusMultiplier = 0;
    }

    public void ClearPendingAttackBonus()
    {
        PendingAttackBonusMultiplier = 0;
    }

    /// <summary>
    /// 静态便捷方法：应用血痕的下一次攻击增益
    /// </summary>
    public static bool TryApplyNextAttackBonus(int baseAttackPower, int blopMultiplier, out int enhancedAttackPower, out string bonusMessage)
    {
        enhancedAttackPower = baseAttackPower;
        bonusMessage = null;

        if (blopMultiplier <= 0 || baseAttackPower <= 0)
            return false;

        long bonus = (long)baseAttackPower * blopMultiplier;
        long enhanced = (long)baseAttackPower + bonus;
        if (enhanced > int.MaxValue)
        {
            enhanced = int.MaxValue;
        }

        enhancedAttackPower = (int)enhanced;
        bonusMessage = $"血痕触发！下一次攻击额外增加({baseAttackPower}×{blopMultiplier})，提升至{enhancedAttackPower}";
        return true;
    }

    public override Item Clone()
    {
        return new BloodTraceDice()
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
