using Microsoft.Xna.Framework;
using System;

namespace EonVientiane;

/// <summary>
/// 飞羽 - 被动骰子(PD)
/// 为一个 (计数器 + ATKP) 面的骰子
/// roll出点数获得AVOP（闪避点数）
/// ATKP > AVOP 则闪避成功，不受伤
/// ATKP <= AVOP 则闪避失败，受到全部ATKP点伤害
/// 每次使用时计数器临时+1，游戏结束后清空
/// </summary>
public class FeatheredDice : Dice, ICounterDice
{
    private Random _random;
    public int Counter { get; set; } = 0; // 计数器，游戏结束后清空
    
    public FeatheredDice()
        : base(
            id: "feathered_dice",
            name: "飞羽",
            description: "一小步.",
            usageType: DiceUsageType.Passive,
            function: "掷(计数器+ATKP×2)面骰获得AVOP（闪避点数）。ATKP > AVOP则闪避成功无伤；ATKP ≤ AVOP则闪避失败受全部伤害。每次使用后计数器临时+1（游戏结束清空）"
        )
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
        int dicefaces = Math.Max(1, Counter + (atkp * 2));
        int result = _random.Next(1, dicefaces + 1);
        
        // 使用后计数器临时+1
        Counter++;
        
        return result;
    }
    
    public override int Roll()
    {
        // 对于基础Roll，仅使用计数器
        int dicefaces = Math.Max(1, Counter + 1);
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
        
        if (attackDamage >= avop)
        {
            // 闪避成功
            actualDamage = 0;
            message = $"飞羽掷出{avop}点，闪避成功！";
        }
        else
        {
            // 闪避失败
            actualDamage = attackDamage;
            message = $"飞羽掷出{avop}点，闪避失败！受到全部{attackDamage}点伤害";
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
