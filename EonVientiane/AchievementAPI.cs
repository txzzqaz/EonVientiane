using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 成就系统API扩展 - 提供成就系统的扩展功能
/// </summary>
public static class AchievementAPI
{
    /// <summary>
    /// 成就触发器字典 - 用于自定义成就条件
    /// </summary>
    private static readonly Dictionary<string, Func<AchievementSystem.Achievement, bool>> _customTriggers = new();
    
    /// <summary>
    /// 成就奖励生成器
    /// </summary>
    private static readonly List<IAchievementRewardGenerator> _rewardGenerators = new();
    
    /// <summary>
    /// 注册自定义成就触发器
    /// </summary>
    public static void RegisterTrigger(string achievementId, Func<AchievementSystem.Achievement, bool> trigger)
    {
        _customTriggers[achievementId] = trigger;
    }
    
    /// <summary>
    /// 检查成就是否满足自定义触发条件
    /// </summary>
    public static bool CheckCustomTrigger(AchievementSystem.Achievement achievement)
    {
        if (_customTriggers.TryGetValue(achievement.Id, out var trigger))
        {
            return trigger(achievement);
        }
        return false;
    }
    
    /// <summary>
    /// 添加奖励生成器
    /// </summary>
    public static void AddRewardGenerator(IAchievementRewardGenerator generator)
    {
        if (generator != null && !_rewardGenerators.Contains(generator))
        {
            _rewardGenerators.Add(generator);
        }
    }
    
    /// <summary>
    /// 生成成就奖励
    /// </summary>
    public static List<AchievementSystem.Reward> GenerateRewards(AchievementSystem.Achievement achievement)
    {
        var rewards = new List<AchievementSystem.Reward>();
        foreach (var generator in _rewardGenerators)
        {
            var generatedRewards = generator.GenerateRewards(achievement);
            if (generatedRewards != null)
            {
                rewards.AddRange(generatedRewards);
            }
        }
        return rewards;
    }
    
    /*
    /// <summary>
    /// 创建自定义成就 - 需要根据AchievementSystem的实际结构调整
    /// </summary>
    public static AchievementSystem.Achievement CreateCustomAchievement(
        string id,
        string name,
        string description,
        AchievementSystem.AchievementType type,
        int requiredProgress,
        params AchievementSystem.Reward[] rewards)
    {
        var achievement = new AchievementSystem.Achievement
        {
            Id = id,
            Name = name,
            Description = description,
            Type = type,
            RequiredProgress = requiredProgress,
            Progress = 0,
            IsCompleted = false,
            UnlockTime = null
        };
        
        achievement.Rewards.AddRange(rewards);
        return achievement;
    }
    */
    
    /*
    /// <summary>
    /// 创建成就链（一系列相关成就） - 需要CreateCustomAchievement方法
    /// </summary>
    public static List<AchievementSystem.Achievement> CreateAchievementChain(
        string chainId,
        string baseName,
        int[] progressMilestones,
        AchievementSystem.AchievementType type)
    {
        var chain = new List<AchievementSystem.Achievement>();
        
        for (int i = 0; i < progressMilestones.Length; i++)
        {
            var achievement = CreateCustomAchievement(
                $"{chainId}_{i + 1}",
                $"{baseName} {i + 1}",
                $"达到 {progressMilestones[i]} 进度",
                type,
                progressMilestones[i]
            );
            chain.Add(achievement);
        }
        
        return chain;
    }
    */
}

/// <summary>
/// 成就奖励生成器接口
/// </summary>
public interface IAchievementRewardGenerator
{
    /// <summary>
    /// 生成奖励
    /// </summary>
    List<AchievementSystem.Reward> GenerateRewards(AchievementSystem.Achievement achievement);
}

/// <summary>
/// 标准奖励生成器 - 根据成就难度生成奖励
/// </summary>
public class StandardRewardGenerator : IAchievementRewardGenerator
{
    public List<AchievementSystem.Reward> GenerateRewards(AchievementSystem.Achievement achievement)
    {
        var rewards = new List<AchievementSystem.Reward>();
        
        // 根据所需进度确定难度
        int difficulty = achievement.RequiredProgress switch
        {
            < 10 => 1,
            < 50 => 2,
            < 100 => 3,
            _ => 4
        };
        
        return rewards;
    }
}

/// <summary>
/// 成就进度跟踪器 - 辅助跟踪复杂成就进度
/// </summary>
public class AchievementProgressTracker
{
    private readonly Dictionary<string, Dictionary<string, int>> _customProgress = new();
    
    /// <summary>
    /// 增加自定义进度值
    /// </summary>
    public void IncrementCustomProgress(string achievementId, string key, int amount = 1)
    {
        if (!_customProgress.ContainsKey(achievementId))
        {
            _customProgress[achievementId] = new Dictionary<string, int>();
        }
        
        if (!_customProgress[achievementId].ContainsKey(key))
        {
            _customProgress[achievementId][key] = 0;
        }
        
        _customProgress[achievementId][key] += amount;
    }
    
    /// <summary>
    /// 获取自定义进度值
    /// </summary>
    public int GetCustomProgress(string achievementId, string key)
    {
        if (_customProgress.TryGetValue(achievementId, out var progress))
        {
            return progress.TryGetValue(key, out var value) ? value : 0;
        }
        return 0;
    }
    
    /// <summary>
    /// 重置进度
    /// </summary>
    public void ResetProgress(string achievementId)
    {
        _customProgress.Remove(achievementId);
    }
}
