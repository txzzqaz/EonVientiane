using Microsoft.Xna.Framework;
using System;

namespace EonVientiane;

/// <summary>
/// 饰品：戮力同心
/// 若掷出的点数与上一次相同，本回合行动效果提升为 n×n
/// 描述：运，赢！
/// </summary>
public class ConcertedEffortAccessory : Accessory
{
    private int? _lastRoll;

    public ConcertedEffortAccessory()
        : base("concerted_effort", "戮力同心", "运，赢！", "yyzh")
    {
        DisplayColor = Color.Goldenrod;
        AccessorySlotsCost = 1;
    }

    public override void OnBattleStart(BattleContext context)
    {
        _lastRoll = null;
    }

    /// <summary>
    /// 处理一次掷骰并根据连号返回放大后的效果值。
    /// </summary>
    /// <param name="currentRoll">本次掷出的点数</param>
    /// <param name="baseEffect">本次行动的基础效果值（通常等于骰面）</param>
    /// <param name="triggered">是否触发连号放大</param>
    /// <returns>放大后的效果值</returns>
    public int ApplyRollBonus(int currentRoll, int baseEffect, out bool triggered)
    {
        triggered = _lastRoll.HasValue && _lastRoll.Value == currentRoll;
        _lastRoll = currentRoll;

        if (!triggered)
            return baseEffect;

        long boosted = (long)baseEffect * currentRoll;
        if (boosted > int.MaxValue)
        {
            boosted = int.MaxValue;
        }
        return (int)boosted;
    }

    public override Item Clone()
    {
        return new ConcertedEffortAccessory()
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
