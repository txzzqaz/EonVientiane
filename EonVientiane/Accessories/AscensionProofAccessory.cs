using Microsoft.Xna.Framework;

namespace EonVientiane;

/// <summary>
/// 饰品：飞升之证
/// 无视所有其他道具提供的HP，强制玩家在对局开始时HP为0并且无法获得任何HP
/// 携带飞升之证每连续赢得5场胜利，本道具计数器永久+1
/// 在战斗开始时，获得计数器对应数量的护盾层数
/// 每层护盾可以抵挡一次没有被闪避/完美防御的攻击
/// </summary>
public class AscensionProofAccessory : Accessory
{
    public int Counter { get; set; } = 0; // 永久计数器
    public int ConsecutiveWins { get; set; } = 0; // 连续胜利次数
    
    public AscensionProofAccessory()
        : base("ascension_proof", "飞升之证", "终局？")
    {
        Health = 0;
        DisplayColor = Color.Gold;
        AccessorySlotsCost = 11; // 占用11个槽位
    }
    
    public override void OnBattleStart(BattleContext context)
    {
        // 强制HP为0且无法获得HP
        context.PlayerHP = 0;
        context.CanGainHP = false;
        
        // 获得护盾层数等于计数器数量
        context.ShieldLayers = Counter;
    }
    
    /// <summary>
    /// 记录胜利，每连续5场胜利增加计数器
    /// </summary>
    public void OnWin()
    {
        ConsecutiveWins++;
        if (ConsecutiveWins >= 5)
        {
            Counter++;
            ConsecutiveWins = 0; // 重置连续胜利计数
        }
    }
    
    /// <summary>
    /// 失败时重置连续胜利计数
    /// </summary>
    public void OnLoss()
    {
        ConsecutiveWins = 0;
    }
    
    public override int GetProvidedHP() => 0; // 不提供HP
    
    public override Item Clone()
    {
        return new AscensionProofAccessory()
        {
            Counter = Counter,
            ConsecutiveWins = ConsecutiveWins,
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Health = Health,
            Mana = Mana,
            DisplayColor = DisplayColor
        };
    }
    
    /// <summary>
    /// 获取状态描述
    /// </summary>
    public string GetStatusDescription()
    {
        return $"计数器: {Counter} | 连续胜利: {ConsecutiveWins}/5";
    }
}
