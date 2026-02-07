using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// PVE 挑战定义
/// </summary>
public class PVEChallenge
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Difficulty { get; set; } // 1-5 难度等级
    public List<string> OpponentDiceNames { get; set; } // 对手的骰子名称列表
    public string OpponentName { get; set; }
    public int RewardGold { get; set; }
    public bool IsCompleted { get; set; }

    public PVEChallenge()
    {
        OpponentDiceNames = new List<string>();
        RewardGold = 100;
        IsCompleted = false;
    }
}
