using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 春风 - 主动骰子(AD)
/// 为一个4面骰
/// roll出点数获得SPRP（春风点数）
/// 后一栏位骰子临时计数器=原计数器数值-SPRP（允许为负）
/// 描述：生生不息
/// 获取方式：成就"奇迹"（一局内使用飞羽骰子进行闪避连续成功5次）
/// </summary>
public class SpringBreezeDice : Dice
{
    private Random _random;
    
    public SpringBreezeDice()
        : base(
            id: "spring_breeze",
            name: "春风",
            description: "生生不息",
            usageType: DiceUsageType.Active,
            function: "掷四面骰获得SPRP（春风点数）。将下一栏位骰子的计数器修改为（原值-SPRP），允许为负数。仅对支持计数器的骰子生效"
        )
    {
        _random = new Random();
        DisplayColor = Color.LightGreen;
    }
    
    public override int Roll()
    {
        return _random.Next(1, 5); // 返回1-4
    }
    
    /// <summary>
    /// 作为主动骰子执行攻击
    /// 春风的特殊机制：roll出点数获得SPRP，后一栏位骰子临时计数器=原计数器数值-SPRP
    /// </summary>
    public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
    {
        int sprp = Roll();
        
        // 获取下一栏位的骰子
        var diceList = attacker.GetEquippedDice();
        int currentIndex = diceList.FindIndex(d => d.Id == this.Id);
        
        string effectMessage = "";
        if (currentIndex >= 0 && currentIndex < diceList.Count - 1)
        {
            var nextDice = diceList[currentIndex + 1];
            
            // 根据骰子类型应用临时计数器效果（允许为负）
            if (nextDice is ICounterDice counterDice)
            {
                int originalCounter = counterDice.Counter;
                counterDice.Counter = originalCounter - sprp;
                effectMessage = $"\n春风效果：{nextDice.Name}计数器从{originalCounter}变为{counterDice.Counter}";
            }
        }

        var message = string.IsNullOrEmpty(effectMessage)
            ? $"春风掷出{sprp}点，未找到可作用的计数器骰子"
            : $"春风掷出{sprp}点{effectMessage}";
        
        return new ActionResult(true, message, null, 0, false);
    }
    
    public override Item Clone()
    {
        return new SpringBreezeDice()
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
