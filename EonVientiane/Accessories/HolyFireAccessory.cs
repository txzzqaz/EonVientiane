using Microsoft.Xna.Framework;
using System;

namespace EonVientiane;

/// <summary>
/// 饰品：圣火
/// 对局内对方每步选择若大于半秒，自动选择跳过
/// 描述：沧海桑田，然后永恒
/// </summary>
public class HolyFireAccessory : Accessory
{
    public HolyFireAccessory()
        : base(
            id: "holy_fire",
            name: "圣火",
            description: "沧海桑田，然后永恒",
            function: "对局内对手每步选择若超过0.5秒，自动选择跳过。"
        )
    {
        DisplayColor = Color.OrangeRed;
        AccessorySlotsCost = 5;
    }
    
    /// <summary>
    /// 是否应该强制对手跳过（基于时间）
    /// </summary>
    public bool ShouldForceOpponentSkip(TimeSpan actionTime)
    {
        return actionTime.TotalSeconds > 0.5;
    }
    
    public override Item Clone()
    {
        return new HolyFireAccessory()
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
