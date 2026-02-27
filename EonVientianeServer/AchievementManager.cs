using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EonVientiane.Shared;
using EonVientianeServer.Achievements;

namespace EonVientianeServer;

/// <summary>
/// 服务端成就管理器 - 管理用户成就进度和数据保存
/// 支持RSA签名和基于文件的持久化，确保成就数据与玩家账号绑定
/// </summary>
public class AchievementManager
{
    private class UserAchievements
    {
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, AchievementProgress> Achievements { get; set; } = new();
        public long LastUpdated { get; set; }
    }

    private class AchievementProgress
    {
        public string Id { get; set; } = string.Empty;
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedTime { get; set; }
    }

    private readonly Dictionary<string, UserAchievements> _cache = new();
    private readonly object _lock = new();
    private readonly string _achievementsDir;
    private readonly string _keysFile;
    private readonly WalletCrypto _crypto;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public AchievementManager(string dataDir = "data/achievements")
    {
        _achievementsDir = Path.Combine(dataDir, "achievements");
        _keysFile = Path.Combine(dataDir, "achievement_keys.xml");
        
        Directory.CreateDirectory(_achievementsDir);
        Directory.CreateDirectory(dataDir);
        
        // 复用服务器的加密密钥系统
        _crypto = InitializeKeys();
        
        Console.WriteLine("[AchievementManager] Initialized with RSA-2048 encryption and file persistence");
    }
    
    /// <summary>
    /// 获取公钥（用于客户端验证）
    /// </summary>
    public string GetPublicKey()
    {
        return _crypto.ExportPublicKey();
    }

    /// <summary>
    /// 为成就数据生成签名
    /// </summary>
    private void SignAchievement(AchievementDto achievement)
    {
        achievement.IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signableData = achievement.GetSignableData();
        achievement.Signature = _crypto.SignItemData(signableData);
    }
    
    /// <summary>
    /// 验证成就签名
    /// </summary>
    public bool VerifyAchievement(AchievementDto achievement)
    {
        if (string.IsNullOrEmpty(achievement.Signature))
            return false;
            
        var signableData = achievement.GetSignableData();
        return _crypto.VerifyItemSignature(signableData, achievement.Signature);
    }

    /// <summary>
    /// 初始化默认成就数据（仅用于测试用户）
    /// </summary>
    private void InitializeDefaultAchievements()
    {
        // 不再预初始化，改为按需加载
        Console.WriteLine("[AchievementManager] Achievement data will be loaded on-demand from files");
    }

    /// <summary>
    /// 获取用户成就列表
    /// </summary>
    public List<AchievementDto> GetUserAchievements(string userId)
    {
        lock (_lock)
        {
            // 从文件加载或创建用户成就
            var userAchievements = LoadOrCreateUserAchievements(userId);

            var achievements = userAchievements.Achievements.Values.Select(a =>
            {
                var definition = GetDefinition(a.Id);
                var dto = new AchievementDto
                {
                    Id = a.Id,
                    UserId = userId,
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
                
                // 为每个成就生成签名
                SignAchievement(dto);
                return dto;
            }).ToList();
            
            Console.WriteLine($"[AchievementManager] Retrieved {achievements.Count} signed achievements for user '{userId}'");
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
            var userAchievements = LoadOrCreateUserAchievements(userId);

            if (!userAchievements.Achievements.TryGetValue(achievementId, out var progress))
            {
                Console.WriteLine($"[AchievementManager] Achievement '{achievementId}' not found for user '{userId}'");
                return (false, false, 0, $"成就'{achievementId}'不存在");
            }

            if (progress.IsCompleted)
            {
                Console.WriteLine($"[AchievementManager] Achievement '{achievementId}' already completed for user '{userId}'");
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
                Console.WriteLine($"[AchievementManager] User '{userId}' completed achievement '{achievementId}' ({GetDefinition(achievementId).Name})");
            }
            else
            {
                Console.WriteLine($"[AchievementManager] User '{userId}' progressed achievement '{achievementId}' from {previousProgress} to {progress.Progress}/{requiredProgress}");
            }

            // 保存到文件
            SaveUserAchievements(userAchievements);

            return (true, isNowCompleted, progress.Progress, null);
        }
    }

    /// <summary>
    /// 为新用户创建默认成就
    /// </summary>
    private UserAchievements CreateDefaultAchievementsForUser(string userId)
    {
        var userAchievements = new UserAchievements 
        { 
            UserId = userId,
            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
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

        return userAchievements;
    }
    
    /// <summary>
    /// 加载或创建用户成就数据
    /// </summary>
    private UserAchievements LoadOrCreateUserAchievements(string userId)
    {
        // 检查缓存
        if (_cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }
        
        var path = GetAchievementsPath(userId);
        UserAchievements userAchievements;
        
        if (File.Exists(path))
        {
            // 从文件加载
            var json = File.ReadAllText(path);
            userAchievements = JsonSerializer.Deserialize<UserAchievements>(json, _jsonOptions) 
                ?? CreateDefaultAchievementsForUser(userId);
            Console.WriteLine($"[AchievementManager] Loaded achievements from file for user '{userId}'");
        }
        else
        {
            // 创建新的成就数据
            userAchievements = CreateDefaultAchievementsForUser(userId);
            SaveUserAchievements(userAchievements);
            Console.WriteLine($"[AchievementManager] Created new achievements file for user '{userId}'");
        }
        
        _cache[userId] = userAchievements;
        return userAchievements;
    }
    
    /// <summary>
    /// 保存用户成就数据到文件
    /// </summary>
    private void SaveUserAchievements(UserAchievements userAchievements)
    {
        userAchievements.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var path = GetAchievementsPath(userAchievements.UserId);
        var json = JsonSerializer.Serialize(userAchievements, _jsonOptions);
        File.WriteAllText(path, json);
        
        // 更新缓存
        _cache[userAchievements.UserId] = userAchievements;
    }
    
    /// <summary>
    /// 获取用户成就文件路径
    /// </summary>
    private string GetAchievementsPath(string userId)
    {
        return Path.Combine(_achievementsDir, $"{userId}_achievements.json");
    }
    
    /// <summary>
    /// 初始化或加载加密密钥
    /// </summary>
    private WalletCrypto InitializeKeys()
    {
        if (File.Exists(_keysFile))
        {
            var keyXml = File.ReadAllText(_keysFile);
            Console.WriteLine("[AchievementManager] Loaded existing encryption keys");
            return WalletCrypto.CreateServerInstance(keyXml);
        }
        else
        {
            var crypto = WalletCrypto.CreateServerInstance();
            var privateKey = crypto.ExportPrivateKey();
            File.WriteAllText(_keysFile, privateKey);
            
            var publicKey = crypto.ExportPublicKey();
            var publicKeyFile = Path.Combine(Path.GetDirectoryName(_keysFile) ?? ".", "achievement_public_key.xml");
            File.WriteAllText(publicKeyFile, publicKey);
            
            Console.WriteLine("[AchievementManager] Generated new RSA key pair for achievements");
            Console.WriteLine($"[AchievementManager] Public key saved to: {publicKeyFile}");
            
            return crypto;
        }
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
        Console.WriteLine("[AchievementManager] CheckBattleEndAchievements started");
        var completedAchievements = new List<(string PlayerId, string AchievementId)>();

        foreach (var achievementId in AchievementCatalog.DefaultIds)
        {
            var definition = GetDefinition(achievementId);
            var trigger = definition.Trigger;

            // 只处理战斗结束类型的触发器
            if (trigger.TriggerType != AchievementTriggerType.BattleEnd)
                continue;

            Console.WriteLine($"[AchievementManager] Checking achievement: {achievementId}");

            // 获取符合条件的玩家
            var eligiblePlayers = trigger.GetEligiblePlayers(context);
            var playerList = eligiblePlayers.ToList();
            Console.WriteLine($"[AchievementManager] Achievement '{achievementId}' has {playerList.Count} eligible players");

            foreach (var playerId in playerList)
            {
                // 计算进度
                int progress = trigger.CalculateProgress(context, playerId);
                Console.WriteLine($"[AchievementManager] Player {playerId} progress for {achievementId}: {progress}");

                // 更新成就
                var (success, isCompleted, _, _) = UpdateAchievementProgress(playerId, achievementId, progress);
                Console.WriteLine($"[AchievementManager] Updated achievement {achievementId} for player {playerId}: success={success}, completed={isCompleted}");

                if (success && isCompleted)
                {
                    completedAchievements.Add((playerId, achievementId));
                    Console.WriteLine($"[AchievementManager] Achievement {achievementId} completed for player {playerId}!");
                }
            }
        }

        Console.WriteLine($"[AchievementManager] Total completed achievements: {completedAchievements.Count}");
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
            var userAchievements = LoadOrCreateUserAchievements(userId);

            int total = userAchievements.Achievements.Count;
            int completed = userAchievements.Achievements.Count(kvp => kvp.Value.IsCompleted);
            float percentage = total > 0 ? (completed * 100f) / total : 0;

            return (completed, total, percentage);
        }
    }
}
