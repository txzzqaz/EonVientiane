using Microsoft.Xna.Framework;
using System;

namespace EonVientiane;

/// <summary>
/// 饰品：漫游者之心
/// 若回合内最慢的一步选择的时间在1秒内，最终攻击点数倍率将依据时间增加（0-1秒对应10-1倍）
/// 描述：纯粹
/// 获取方式：成就"秒了"（己方总行动时间在5秒内的情况下胜利）
/// </summary>
public class WandererHeartAccessory : Accessory
{
    public WandererHeartAccessory()
        : base("wanderer_heart", "漫游者之心", "纯粹")
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
        
        // 0-1秒对应10-1倍
        // 0秒 = 10倍，1秒 = 1倍
        // 公式: 10 - (timeInSeconds * 9)
        double timeInSeconds = slowestActionTime.TotalSeconds;
        return 10.0 - (timeInSeconds * 9.0);
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
