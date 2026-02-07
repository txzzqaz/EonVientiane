using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// PVE 挑战系统管理器
/// </summary>
public class PVEChallengeManager
{
    private List<PVEChallenge> _challenges;
    private List<PVEChallenge> _completedChallenges;

    public List<PVEChallenge> Challenges => _challenges;
    public List<PVEChallenge> CompletedChallenges => _completedChallenges;

    public PVEChallengeManager()
    {
        _challenges = new List<PVEChallenge>();
        _completedChallenges = new List<PVEChallenge>();
        InitializeDefaultChallenges();
    }

    /// <summary>
    /// 初始化默认挑战
    /// </summary>
    private void InitializeDefaultChallenges()
    {
        // 示例挑战：对手只有 d6 和自我
        var challenge1 = new PVEChallenge
        {
            Id = "pve_beginner_01",
            Name = "初级挑战 - 自我对阵",
            Description = "与一个只使用d6和自我的对手对战。这是一个很好的练习",
            Difficulty = 1,
            OpponentName = "新手对手",
            OpponentDiceNames = new List<string> { "d6_dice", "self_accessory" },
            RewardGold = 100
        };

        _challenges.Add(challenge1);
    }

    /// <summary>
    /// 获取挑战列表
    /// </summary>
    public List<PVEChallenge> GetAllChallenges()
    {
        return _challenges;
    }

    /// <summary>
    /// 获取未完成的挑战
    /// </summary>
    public List<PVEChallenge> GetIncompleteChallenges()
    {
        return _challenges.Where(c => !c.IsCompleted).ToList();
    }

    /// <summary>
    /// 标记挑战为已完成
    /// </summary>
    public void CompleteChallenge(string challengeId)
    {
        var challenge = _challenges.FirstOrDefault(c => c.Id == challengeId);
        if (challenge != null)
        {
            challenge.IsCompleted = true;
            _completedChallenges.Add(challenge);
        }
    }

    /// <summary>
    /// 获取总完成数
    /// </summary>
    public int GetCompletionCount()
    {
        return _completedChallenges.Count;
    }

    /// <summary>
    /// 添加自定义挑战
    /// </summary>
    public void AddChallenge(PVEChallenge challenge)
    {
        if (!_challenges.Any(c => c.Id == challenge.Id))
        {
            _challenges.Add(challenge);
        }
    }

    /// <summary>
    /// 获取总奖励金
    /// </summary>
    public int GetTotalReward()
    {
        return _completedChallenges.Sum(c => c.RewardGold);
    }
}
