using Microsoft.Xna.Framework;

namespace EonVientiane;

/// <summary>
/// 饰品：预见
/// 允许在对方行动完之前提前规划好后续主动回合的行动，不占用行动时间
/// 描述：指向唯一的胜利
/// 获取方式：成就"我在哪？"（携带饰品"漫游者之心"而一整局都没有触发过增益）
/// </summary>
public class ForesightAccessory : Accessory
{
    public ForesightAccessory()
        : base(
            id: "foresight",
            name: "预见",
            description: "指向唯一的胜利",
            function: "允许在对方行动完成前提前规划后续主动回合的行动，不占用行动时间。启用提前规划功能"
        )
    {
        DisplayColor = Color.Magenta;
        AccessorySlotsCost = 3;
    }
    
    /// <summary>
    /// 是否可以进行提前规划（不占用行动时间）
    /// </summary>
    public bool CanPlannedAction => true;
    
    public override Item Clone()
    {
        return new ForesightAccessory()
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
