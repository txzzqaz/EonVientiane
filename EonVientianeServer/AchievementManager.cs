using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 服务端成就管理器 - 管理用户成就进度和数据保存
/// </summary>
public class AchievementManager
{
    private class UserAchievements
    {
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, AchievementProgress> Achievements { get; set; } = new();
    }

    private class AchievementProgress
    {
        public string Id { get; set; } = string.Empty;
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedTime { get; set; }
    }

    private readonly Dictionary<string, UserAchievements> _userAchievements = new();
    private readonly object _lock = new();

    public AchievementManager()
    {
        InitializeDefaultAchievements();
    }

    /// <summary>
    /// 初始化默认成就数据
    /// </summary>
    private void InitializeDefaultAchievements()
    {
        var defaultAchievementIds = new[]
        {
            "first_defense",
            "perfect_victory",
            "long_thinking",
            "blitz_victory",
            "where_am_i",
            "guasha_master",
            "absolute_luck",
            // "first_victory",
            // "battle_master",
            // "item_collector",
            // "no_death_warrior",
            // "time_traveler"
        };

        // 预初始化测试用户的成就
        var testUsers = new[] { "admin", "user", "test" };
        foreach (var userId in testUsers)
        {
            var userAchievements = new UserAchievements { UserId = userId };
            foreach (var achievementId in defaultAchievementIds)
            {
                userAchievements.Achievements[achievementId] = new AchievementProgress
                {
                    Id = achievementId,
                    Progress = 0,
                    IsCompleted = false,
                    CompletedTime = null
                };
            }
            _userAchievements[userId] = userAchievements;
        }
    }

    /// <summary>
    /// 获取用户成就列表
    /// </summary>
    public List<AchievementDto> GetUserAchievements(string userId)
    {
        lock (_lock)
        {
            if (!_userAchievements.TryGetValue(userId, out var userAchievements))
            {
                // 如果用户不存在，为其创建默认成就
                userAchievements = CreateDefaultAchievementsForUser(userId);
                Console.WriteLine($"[Server] Created default achievements for new user '{userId}'");
            }

            var achievements = userAchievements.Achievements.Values.Select(a => new AchievementDto
            {
                Id = a.Id,
                Name = GetAchievementName(a.Id),
                Description = GetAchievementDescription(a.Id),
                Icon = GetAchievementIcon(a.Id),
                Progress = a.Progress,
                RequiredProgress = GetAchievementRequirement(a.Id),
                IsCompleted = a.IsCompleted,
                CompletedTime = a.CompletedTime,
                Rewards = GetCompletionRewards(a.Id)
            }).ToList();
            
            Console.WriteLine($"[Server] Retrieved {achievements.Count} achievements for user '{userId}'");
            return achievements;
        }
    }

    /// <summary>
    /// 更新用户成就进度
    /// </summary>
    public (bool success, bool isCompleted, int currentProgress, string? error) 
        UpdateAchievementProgress(string userId, string achievementId, int progressDelta)
    {
        lock (_lock)
        {
            if (!_userAchievements.TryGetValue(userId, out var userAchievements))
            {
                userAchievements = CreateDefaultAchievementsForUser(userId);
                Console.WriteLine($"[Server] Created default achievements for user '{userId}' during update");
            }

            if (!userAchievements.Achievements.TryGetValue(achievementId, out var progress))
            {
                Console.WriteLine($"[Server] Achievement '{achievementId}' not found for user '{userId}'");
                return (false, false, 0, $"成就'{achievementId}'不存在");
            }

            if (progress.IsCompleted)
            {
                Console.WriteLine($"[Server] Achievement '{achievementId}' already completed for user '{userId}'");
                return (true, true, progress.Progress, null);
            }

            int requiredProgress = GetAchievementRequirement(achievementId);
            int previousProgress = progress.Progress;
            progress.Progress = Math.Max(0, Math.Min(progress.Progress + progressDelta, requiredProgress));

            bool isNowCompleted = false;
            if (progress.Progress >= requiredProgress && !progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedTime = DateTime.UtcNow;
                isNowCompleted = true;
                Console.WriteLine($"[Server] User '{userId}' completed achievement '{achievementId}' ({GetAchievementName(achievementId)})");
            }
            else
            {
                Console.WriteLine($"[Server] User '{userId}' progressed achievement '{achievementId}' from {previousProgress} to {progress.Progress}/{requiredProgress}");
            }

            return (true, isNowCompleted, progress.Progress, null);
        }
    }

    /// <summary>
    /// 为新用户创建默认成就
    /// </summary>
    private UserAchievements CreateDefaultAchievementsForUser(string userId)
    {
        var userAchievements = new UserAchievements { UserId = userId };
        var defaultAchievementIds = new[]
        {
            "first_defense",
            "perfect_victory",
            "long_thinking",
            "blitz_victory",
            "where_am_i",
            "guasha_master",
            "miracle",
            "absolute_luck",
            // "first_victory",
            // "battle_master",
            // "item_collector",
            // "no_death_warrior",
            // "time_traveler"
        };

        foreach (var achievementId in defaultAchievementIds)
        {
            userAchievements.Achievements[achievementId] = new AchievementProgress
            {
                Id = achievementId,
                Progress = 0,
                IsCompleted = false,
                CompletedTime = null
            };
        }

        _userAchievements[userId] = userAchievements;
        return userAchievements;
    }

    /// <summary>
    /// 获取成就名称
    /// </summary>
    private string GetAchievementName(string achievementId) => achievementId switch
    {
        "first_defense" => "第一次防御",
        "perfect_victory" => "绝对碾压",
        "long_thinking" => "长考",
        "blitz_victory" => "秒了",
        "where_am_i" => "我在哪？",
        "guasha_master" => "刮痧",
        "miracle" => "奇迹",
        "absolute_luck" => "绝对幸运",
        // "first_victory" => "初露锋芒",
        // "battle_master" => "战斗好手",
        // "item_collector" => "装备收集家",
        // "no_death_warrior" => "无敌战士",
        // "time_traveler" => "时间旅者",
        _ => "未知成就"
    };

    /// <summary>
    /// 获取成就描述
    /// </summary>
    private string GetAchievementDescription(string achievementId) => achievementId switch
    {
        "first_defense" => "这是攻，这是防",
        "perfect_victory" => "这是攻，这是防",
        "long_thinking" => "一局游戏中敌方的总行动时间达到10分钟",
        "blitz_victory" => "己方总行动时间在5秒内的情况下胜利",
        "where_am_i" => "携带饰品'漫游者之心'而一整局都没有触发过增益",
        "guasha_master" => "一局游戏内连续10回合造成了并且只造成1点伤害",
        "miracle" => "一局内使用飞羽骰子进行闪避连续成功5次",
        "absolute_luck" => "连胜6局并期间所有掷出骰子点数均相同",
        // "first_victory" => "赢得第一场战斗",
        // "battle_master" => "赢得10场战斗",
        // "item_collector" => "收集20件装备",
        // "no_death_warrior" => "完成5场无死亡战斗",
        // "time_traveler" => "游戏时间累计10小时",
        _ => "无描述"
    };

    /// <summary>
    /// 获取成就要求
    /// </summary>
    private int GetAchievementRequirement(string achievementId) => achievementId switch
    {
        "first_defense" => 1,
        "perfect_victory" => 1,
        "long_thinking" => 600,  // 10分钟 = 600秒
        "blitz_victory" => 1,    // 完成1次5秒内胜利
        "where_am_i" => 1,       // 完成1次满足条件的战斗
        "guasha_master" => 1,    // 完成1次满足条件的战斗
        "miracle" => 1,          // 完成1次飞羽连续闪避5次成功
        "absolute_luck" => 6,    // 连续6场满足条件的胜利
        // "first_victory" => 1,
        // "battle_master" => 10,
        // "item_collector" => 20,
        // "no_death_warrior" => 5,
        // "time_traveler" => 10,
        _ => 0
    };

    /// <summary>
    /// 获取成就图标
    /// </summary>
    private string GetAchievementIcon(string achievementId) => achievementId switch
    {
        "first_defense" => "achievement_first_defense",
        "perfect_victory" => "achievement_perfect_victory",
        "long_thinking" => "achievement_long_thinking",
        "blitz_victory" => "achievement_blitz_victory",
        "where_am_i" => "achievement_where_am_i",
        "guasha_master" => "achievement_guasha_master",
        "miracle" => "achievement_miracle",
        "absolute_luck" => "achievement_absolute_luck",
        // "first_victory" => "achievement_first_victory",
        // "battle_master" => "achievement_battle_master",
        // "item_collector" => "achievement_item_collector",
        // "no_death_warrior" => "achievement_no_death_warrior",
        // "time_traveler" => "achievement_time_traveler",
        _ => "achievement_unknown"
    };

    /// <summary>
    /// 检查并返回完成的成就奖励
    /// </summary>
    public List<RewardDto> GetCompletionRewards(string achievementId)
    {
        return achievementId switch
        {
            "first_defense" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "feathered_dice", Quantity = 1 }
            },
            "perfect_victory" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "ascension_proof", Quantity = 1 }
            },
            "long_thinking" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "holy_fire", Quantity = 1 }
            },
            "blitz_victory" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "wanderer_heart", Quantity = 1 }
            },
            "where_am_i" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "foresight", Quantity = 1 }
            },
            "guasha_master" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "guasha_parquet", Quantity = 1 }
            },
            "miracle" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "spring_breeze", Quantity = 1 }
            },
            "absolute_luck" => new List<RewardDto>
            {
                new RewardDto { Type = "Item", ItemId = "concerted_effort", Quantity = 1 }
            },
            // "first_victory" => new List<RewardDto>
            // {
            //     new RewardDto { Type = "Item", ItemId = "item_reward_1", Quantity = 1 },
            //     new RewardDto { Type = "Gold", ItemId = "", Quantity = 100 }
            // },
            // "battle_master" => new List<RewardDto>
            // {
            //     new RewardDto { Type = "Item", ItemId = "item_reward_2", Quantity = 1 },
            //     new RewardDto { Type = "Gold", ItemId = "", Quantity = 500 }
            // },
            // "item_collector" => new List<RewardDto>
            // {
            //     new RewardDto { Type = "Item", ItemId = "item_reward_3", Quantity = 1 },
            //     new RewardDto { Type = "Gold", ItemId = "", Quantity = 300 }
            // },
            // "no_death_warrior" => new List<RewardDto>
            // {
            //     new RewardDto { Type = "Item", ItemId = "item_reward_4", Quantity = 1 },
            //     new RewardDto { Type = "Gold", ItemId = "", Quantity = 200 }
            // },
            // "time_traveler" => new List<RewardDto>
            // {
            //     new RewardDto { Type = "Item", ItemId = "item_reward_5", Quantity = 1 },
            //     new RewardDto { Type = "Gold", ItemId = "", Quantity = 400 }
            // },
            _ => new List<RewardDto>()
        };
    }

    /// <summary>
    /// 获取用户的成就完成统计
    /// </summary>
    public (int completed, int total, float percentage) GetCompletionStats(string userId)
    {
        lock (_lock)
        {
            if (!_userAchievements.TryGetValue(userId, out var userAchievements))
            {
                return (0, 5, 0);
            }

            int total = userAchievements.Achievements.Count;
            int completed = userAchievements.Achievements.Count(kvp => kvp.Value.IsCompleted);
            float percentage = total > 0 ? (completed * 100f) / total : 0;

            return (completed, total, percentage);
        }
    }
}
