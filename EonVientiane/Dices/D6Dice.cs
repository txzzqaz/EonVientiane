using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// D6 - 六面骰子 (参考实现)
/// 主动使用时，roll出点数作为ATKP（攻击点数）
/// 被动使用时，roll出点数获得DEFP（防御点数）
/// ATKP <= DEFP 则完全防御，不受伤
/// ATKP > DEFP 则受到 ATKP - DEFP 点伤害
/// 
/// 【新骰子创建参考】
/// 这个类是骰子的最简实现示例，作为新骰子创建的参考模板。
/// 详见: docs/ITEM_CREATION_GUIDE.md
/// 
/// 【关键步骤】
/// 1. 继承 Dice 类，在构造函数中设置 DiceUsageType
/// 2. 实现 Roll() 方法返回骰子面数
/// 3. 实现 ExecuteActiveAction() 和 ExecutePassiveAction()
/// 4. 在 InventoryManager.ItemFactory.RegisterAllItems() 中注册
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
