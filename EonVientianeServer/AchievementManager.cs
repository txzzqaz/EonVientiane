using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;
using EonVientianeServer.Achievements;

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
        var defaultAchievementIds = AchievementCatalog.DefaultIds;

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

            var achievements = userAchievements.Achievements.Values.Select(a =>
            {
                var definition = GetDefinition(a.Id);
                return new AchievementDto
                {
                    Id = a.Id,
                    Name = definition.Name,
                    Description = definition.Description,
                    LockedHint = definition.LockedHint,
                    UnlockedHint = definition.UnlockedHint,
                    Icon = definition.Icon,
                    Progress = a.Progress,
                    RequiredProgress = definition.RequiredProgress,
                    IsCompleted = a.IsCompleted,
                    CompletedTime = a.CompletedTime,
                    Rewards = definition.Rewards.ToList()
                };
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

            int requiredProgress = GetDefinition(achievementId).RequiredProgress;
            int previousProgress = progress.Progress;
            progress.Progress = Math.Max(0, Math.Min(progress.Progress + progressDelta, requiredProgress));

            bool isNowCompleted = false;
            if (progress.Progress >= requiredProgress && !progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedTime = DateTime.UtcNow;
                isNowCompleted = true;
                Console.WriteLine($"[Server] User '{userId}' completed achievement '{achievementId}' ({GetDefinition(achievementId).Name})");
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
        var defaultAchievementIds = AchievementCatalog.DefaultIds;

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

    private IAchievementDefinition GetDefinition(string achievementId)
    {
        if (AchievementCatalog.TryGet(achievementId, out var definition))
        {
            return definition;
        }

        return new UnknownAchievementDefinition(achievementId);
    }

    /// <summary>
    /// 检查并返回完成的成就奖励
    /// </summary>
    public List<RewardDto> GetCompletionRewards(string achievementId)
    {
        return GetDefinition(achievementId).Rewards.ToList();
    }

    /// <summary>
    /// 处理战斗结束时的成就触发检查
    /// </summary>
    public List<(string PlayerId, string AchievementId)> CheckBattleEndAchievements(AchievementTriggerContext context)
    {
        var completedAchievements = new List<(string PlayerId, string AchievementId)>();

        foreach (var achievementId in AchievementCatalog.DefaultIds)
        {
            var definition = GetDefinition(achievementId);
            var trigger = definition.Trigger;

            // 只处理战斗结束类型的触发器
            if (trigger.TriggerType != AchievementTriggerType.BattleEnd)
                continue;

            // 获取符合条件的玩家
            var eligiblePlayers = trigger.GetEligiblePlayers(context);

            foreach (var playerId in eligiblePlayers)
            {
                // 计算进度
                int progress = trigger.CalculateProgress(context, playerId);

                // 更新成就
                var (success, isCompleted, _, _) = UpdateAchievementProgress(playerId, achievementId, progress);

                if (success && isCompleted)
                {
                    completedAchievements.Add((playerId, achievementId));
                }
            }
        }

        return completedAchievements;
    }

    private sealed class UnknownAchievementDefinition : IAchievementDefinition
    {
        public UnknownAchievementDefinition(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public string Name => "未知成就";
        public string Description => "无描述";
        public string LockedHint => "???";
        public string UnlockedHint => "已解锁成就";
        public string Icon => "achievement_unknown";
        public int RequiredProgress => 0;
        public IReadOnlyList<RewardDto> Rewards => new List<RewardDto>();
        public IAchievementTrigger Trigger => new UnknownAchievementTrigger();

        private sealed class UnknownAchievementTrigger : IAchievementTrigger
        {
            public AchievementTriggerType TriggerType => AchievementTriggerType.Custom;
            public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context) 
                => Enumerable.Empty<string>();
            public int CalculateProgress(AchievementTriggerContext context, string playerId) => 0;
        }
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
                return (0, AchievementCatalog.DefaultIds.Count, 0);
            }

            int total = userAchievements.Achievements.Count;
            int completed = userAchievements.Achievements.Count(kvp => kvp.Value.IsCompleted);
            float percentage = total > 0 ? (completed * 100f) / total : 0;

            return (completed, total, percentage);
        }
    }
}
