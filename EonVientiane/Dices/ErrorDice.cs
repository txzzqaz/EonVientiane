using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// ERROR - 主被动骰子
/// 每次使用前可手动输入点数（无上限），否则按D6规则随机
/// 描述：Debug
/// </summary>
public class ErrorDice : Dice, IManualRollDice
{
    private readonly Random _random;
    private int? _pendingManualRoll;

    public ErrorDice()
        : base("error_dice", "ERROR", "Debug", DiceUsageType.Both)
    {
        _random = new Random();
        DisplayColor = Color.LightGray;
    }

    public bool RequiresManualInput => true;

    public void SetManualRoll(int? value)
    {
        if (value.HasValue)
        {
            // 仅允许非负整数，超过int上限时钳制
            int clamped = value.Value < 0 ? 0 : value.Value;
            _pendingManualRoll = clamped;
        }
        else
        {
            _pendingManualRoll = null;
        }
    }

    private int ConsumeRollValue()
    {
        int roll = _pendingManualRoll ?? _random.Next(1, 7); // 默认回落到D6掷法
        _pendingManualRoll = null;
        return Math.Max(0, roll);
    }

    public override int Roll()
    {
        return ConsumeRollValue();
    }

    /// <summary>
    /// 作为主动骰子执行攻击，逻辑与D6一致
    /// </summary>
    public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        if (defenders == null || defenders.Count == 0)
            return new ActionResult(false, "没有可攻击的目标");

        Player target = defenders[new Random().Next(defenders.Count)];
        int atkp = Roll();

        return new ActionResult(true, $"ERROR掷出{atkp}点攻击", target, atkp);
    }

    /// <summary>
    /// 作为被动骰子执行防御，逻辑与D6一致
    /// </summary>
    public override DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
    {
        int defp = Roll();
        int actualDamage = Math.Max(0, attackDamage - defp);

        string message = actualDamage == 0
            ? $"ERROR掷出{defp}点完全防御！"
            : $"ERROR掷出{defp}点，仍受到{actualDamage}点伤害";

        return new DefenseResult(defp, actualDamage, message);
    }

    public override Item Clone()
    {
        return new ErrorDice()
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
