using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 饰品：漫游者之心
/// 若回合内最慢的一步选择的时间在1秒内，最终攻击点数倍率将依据时间增加（0-1秒对应5-1倍）
/// 描述：纯粹
/// 获取方式：成就"秒了"（己方总行动时间在5秒内的情况下胜利）
/// </summary>
public class WandererHeartAccessory : Accessory
{
    public WandererHeartAccessory()
        : base(
            id: "wanderer_heart",
            name: "漫游者之心",
            description: "纯粹",
            function: "若最慢一步选择时间在1秒内，最终攻击点数倍率根据时间增加：0秒=5倍，1秒=1倍。超过1秒无加成。"
        )
    {
        DisplayColor = Color.Cyan;
        AccessorySlotsCost = 3;
    }
    
    /// <summary>
    /// 计算基于最慢一步时间的攻击倍率
    /// </summary>
    public double GetAttackMultiplier(TimeSpan slowestActionTime)
    {
        // 如果最慢的步骤超过1秒，返回1倍（无加成）
        if (slowestActionTime.TotalSeconds > 1.0)
            return 1.0;
        
        // 0-1秒对应5-1倍
        // 0秒 = 5倍，1秒 = 1倍
        // 公式: 5 - (timeInSeconds * 4)
        double timeInSeconds = slowestActionTime.TotalSeconds;
        return 5.0 - (timeInSeconds * 4.0);
    }

    public override void OnAttackPowerCalculation(AccessoryAttackContext context)
    {
        if (context == null || context.Phase != AccessoryAttackTriggerPhase.PostBloodTraceBonus)
            return;

        double multiplier = GetAttackMultiplier(context.SlowestActionTime);
        int finalAttackPower = (int)Math.Round(context.AttackPower * multiplier);
        context.AttackPower = finalAttackPower;

        if (multiplier > 1.0)
        {
            context.WandererHeartTriggered = true;
            context.AddLog($"漫游者之心触发！根据回合内最慢一步({context.SlowestActionTime.TotalSeconds:F2}秒)，攻击力调整为{finalAttackPower}");
        }
    }

    /// <summary>
    /// 静态便捷方法：尝试为玩家应用漫游者之心倍率
    /// </summary>
    public static int TryApplyAttackMultiplier(Player attacker, TimeSpan slowestActionTime, int baseAttackPower, out bool triggered)
    {
        triggered = false;

        if (attacker == null)
            return baseAttackPower;

        var accessory = attacker.GetEquippedAccessories()
            .OfType<WandererHeartAccessory>()
            .FirstOrDefault();

        if (accessory == null)
            return baseAttackPower;

        double multiplier = accessory.GetAttackMultiplier(slowestActionTime);
        int finalAttackPower = (int)Math.Round(baseAttackPower * multiplier);

        if (multiplier > 1.0)
        {
            triggered = true;
        }

        return finalAttackPower;
    }
    
    public override Item Clone()
    {
        return new WandererHeartAccessory()
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
