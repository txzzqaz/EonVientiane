using Microsoft.Xna.Framework;

namespace EonVientiane;

/// <summary>
/// 饰品：自我 (参考实现)
/// 对局开始时提供10HP（生命值）
/// 
/// 【新饰品创建参考】
/// 这个类是饰品的最简实现示例，展示了如何使用事件回调。
/// 详见: docs/ITEM_CREATION_GUIDE.md
/// 
/// 【关键步骤】
/// 1. 继承 Accessory 类
/// 2. 在构造函数中设置属性值 (Attack, Defense, Health 等)
/// 3. 覆写需要的事件方法 (OnBattleStart, OnHit 等)
/// 4. 实现 Clone() 方法复制所有属性
/// 5. 在 InventoryManager.ItemFactory.RegisterAllItems() 中注册
/// 6. 设置 AccessorySlotsCost (消耗槽位数)
/// </summary>
public class SelfAccessory : Accessory
{
    public SelfAccessory()
        : base(
            id: "self_accessory",
            name: "自我",
            description: "这就是你自己",
            function: "对局开始时提供10点生命值（HP）。若当前不能获得HP则无效"
        )
    {
        Health = 10;
        DisplayColor = Color.LightGreen;
        AccessorySlotsCost = 2;
    }
    
    public override void OnBattleStart(BattleContext context)
    {
        if (context.CanGainHP)
        {
            context.PlayerHP += Health;
        }
    }
    
    public override Item Clone()
    {
        return new SelfAccessory()
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
