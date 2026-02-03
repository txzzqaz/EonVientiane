using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

#nullable enable

namespace EonVientiane;

/// <summary>
/// 成就系统 - 管理游戏内的成就和奖励，支持客户端与服务器同步
/// </summary>
public class AchievementSystem
{
    /// <summary>
    /// 成就数据模型
    /// </summary>
    public class Achievement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Progress { get; set; } // 0-100
        public int RequiredProgress { get; set; } // 完成条件的目标值
        public bool IsCompleted { get; set; }
        public DateTime? CompletedTime { get; set; }
        public List<Reward> Rewards { get; set; } = new();
        
        public Achievement Clone()
        {
            return new Achievement
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Icon = Icon,
                Progress = Progress,
                RequiredProgress = RequiredProgress,
                IsCompleted = IsCompleted,
                CompletedTime = CompletedTime,
                Rewards = new List<Reward>(Rewards)
            };
        }
    }

    /// <summary>
    /// 奖励数据模型
    /// </summary>
    public class Reward
    {
        public string Type { get; set; } = string.Empty; // "Item", "Gold", "Experience" 等
        public string ItemId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 成就类型枚举
    /// </summary>
    public enum AchievementType
    {
        BattleVictories,        // 战斗胜利数
        ItemsCollected,         // 收集物品数
        LevelReached,           // 达到等级
        EquipmentUpgraded,      // 装备升级
        PlaytimeHours,          // 游戏时数
        FirstVictory,           // 首次胜利
        NoDeathBattle,          // 无死亡战斗
        CustomEvent             // 自定义事件
    }

    private Dictionary<string, Achievement> _achievements = new();
    private Dictionary<string, Achievement> _serverAchievements = new(); // 缓存服务器数据
    private string _userId = string.Empty;
    private InventoryManager _inventoryManager;
    private DateTime _lastServerSync = DateTime.MinValue;

    // 事件：成就完成时触发
    public event Action<Achievement>? AchievementCompleted;
    public event Action<Reward>? RewardGiven;
    public event Action<string>? SyncStarted;
    public event Action<string>? SyncCompleted;
    public event Action<string>? SyncFailed;

    public IReadOnlyDictionary<string, Achievement> Achievements => _achievements.AsReadOnly();
    public IReadOnlyDictionary<string, Achievement> ServerAchievements => _serverAchievements.AsReadOnly();
    public DateTime LastServerSync => _lastServerSync;

    public AchievementSystem(InventoryManager inventoryManager)
    {
        _inventoryManager = inventoryManager;
        InitializeDefaultAchievements();
    }

    /// <summary>
    /// 设置用户ID
    /// </summary>
    public void SetUserId(string userId)
    {
        _userId = userId;
        Console.WriteLine($"[Client] AchievementSystem userId set to '{userId}'");
    }

    /// <summary>
    /// 初始化默认成就
    /// </summary>
    private void InitializeDefaultAchievements()
    {
        // 初始化为空，由服务器下发数据填充
        Console.WriteLine("[Client] AchievementSystem initialized, waiting for server data");
    }

    /// <summary>
    /// 更新成就进度
    /// </summary>
    public void UpdateProgress(string achievementId, int progressDelta)
    {
        if (!_achievements.TryGetValue(achievementId, out var achievement))
        {
            Console.WriteLine($"[Client] Achievement '{achievementId}' not found locally");
            return;
        }

        if (achievement.IsCompleted)
        {
            Console.WriteLine($"[Client] Achievement '{achievementId}' already completed");
            return;
        }

        int previousProgress = achievement.Progress;
        achievement.Progress += progressDelta;
        achievement.Progress = Math.Min(achievement.Progress, achievement.RequiredProgress);

        Console.WriteLine($"[Client] Updated achievement '{achievementId}': {previousProgress} -> {achievement.Progress}/{achievement.RequiredProgress}");

        if (achievement.Progress >= achievement.RequiredProgress)
        {
            CompleteAchievement(achievementId);
        }
    }

    /// <summary>
    /// 完成成就
    /// </summary>
    private void CompleteAchievement(string achievementId)
    {
        if (!_achievements.TryGetValue(achievementId, out var achievement))
            return;

        achievement.IsCompleted = true;
        achievement.CompletedTime = DateTime.UtcNow;

        Console.WriteLine($"[Client] Achievement completed: {achievement.Name} ({achievementId})");

        // 发放奖励
        foreach (var reward in achievement.Rewards)
        {
            GiveReward(reward);
        }

        AchievementCompleted?.Invoke(achievement);
    }

    /// <summary>
    /// 发放奖励
    /// </summary>
    private void GiveReward(Reward reward)
    {
        if (reward.Type == "Item" && !string.IsNullOrEmpty(reward.ItemId))
        {
            // 创建奖励物品
            var rewardItem = CreateRewardItem(reward.ItemId);
            if (rewardItem != null)
            {
                _inventoryManager.AddItem(rewardItem, reward.Quantity);
                Console.WriteLine($"[Client] Added reward item: {reward.ItemId} x{reward.Quantity}");
            }
        }

        RewardGiven?.Invoke(reward);
    }

    /// <summary>
    /// 创建奖励物品
    /// </summary>
    private Item? CreateRewardItem(string itemId)
    {
        return itemId switch
        {
            "item_reward_1" => new Equipment("item_reward_1", "初心者之剑", "首次胜利的纪念品", EquipmentType.Dice)
            {
                MaxStackSize = 1,
                Attack = 5
            },
            "item_reward_2" => new Equipment("item_reward_2", "战神之盾", "十场战斗的证明", EquipmentType.Accessory)
            {
                MaxStackSize = 1,
                Defense = 10
            },
            "item_reward_3" => new Equipment("item_reward_3", "收集家之冠", "装备收集的奖励", EquipmentType.Accessory)
            {
                MaxStackSize = 1,
                Attack = 3,
                Defense = 3
            },
            "item_reward_4" => new Equipment("item_reward_4", "无敌甲胄", "无死亡战斗的荣誉", EquipmentType.Accessory)
            {
                MaxStackSize = 1,
                Defense = 15
            },
            "item_reward_5" => new Equipment("item_reward_5", "时间护符", "游戏时间的纪念", EquipmentType.Accessory)
            {
                MaxStackSize = 1,
                Attack = 2,
                Defense = 2
            },
            "feathered_dice" => new Equipment("feathered_dice", "羽毛骰", "成就奖励", EquipmentType.Dice)
            {
                MaxStackSize = 1,
                Attack = 8,
                Defense = 2
            },
            "ascension_proof" => new Equipment("ascension_proof", "晋升凭证", "成就奖励", EquipmentType.Accessory)
            {
                MaxStackSize = 1,
                Attack = 5,
                Defense = 5
            },
            "holy_fire" => new HolyFireAccessory()
            {
                MaxStackSize = 1
            },
            "wanderer_heart" => new WandererHeartAccessory()
            {
                MaxStackSize = 1
            },
            "foresight" => new ForesightAccessory()
            {
                MaxStackSize = 1
            },
            "guasha_parquet" => new GuaShaParquetDice()
            {
                MaxStackSize = 1
            },
            "concerted_effort" => new ConcertedEffortAccessory()
            {
                MaxStackSize = 1
            },
            _ => null
        };
    }

    /// <summary>
    /// 获取成就
    /// </summary>
    public Achievement? GetAchievement(string achievementId)
    {
        return _achievements.TryGetValue(achievementId, out var achievement) ? achievement.Clone() : null;
    }

    /// <summary>
    /// 获取所有成就
    /// </summary>
    public List<Achievement> GetAllAchievements()
    {
        return _achievements.Values.Select(a => a.Clone()).ToList();
    }

    /// <summary>
    /// 获取已完成的成就
    /// </summary>
    public List<Achievement> GetCompletedAchievements()
    {
        return _achievements.Values
            .Where(a => a.IsCompleted)
            .Select(a => a.Clone())
            .ToList();
    }

    /// <summary>
    /// 获取成就完成度统计
    /// </summary>
    public (int completed, int total, float percentage) GetCompletionStats()
    {
        int total = _achievements.Count;
        int completed = _achievements.Count(kvp => kvp.Value.IsCompleted);
        float percentage = total > 0 ? (completed * 100f) / total : 0;
        return (completed, total, percentage);
    }

    /// <summary>
    /// 从服务端数据加载成就状态
    /// </summary>
    public void SyncWithServer(List<AchievementDto> serverData)
    {
        try
        {
            SyncStarted?.Invoke("正在与服务器同步成就...");
            Console.WriteLine($"[Client] Syncing {serverData.Count} achievements from server");

            _serverAchievements.Clear();
            _achievements.Clear();

            foreach (var dto in serverData)
            {
                var achievement = new Achievement
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Description = dto.Description,
                    Icon = dto.Icon,
                    Progress = dto.Progress,
                    RequiredProgress = dto.RequiredProgress,
                    IsCompleted = dto.IsCompleted,
                    CompletedTime = dto.CompletedTime,
                    Rewards = dto.Rewards?.Select(r => new Reward
                    {
                        Type = r.Type,
                        ItemId = r.ItemId,
                        Quantity = r.Quantity
                    }).ToList() ?? new()
                };

                _achievements[dto.Id] = achievement;
                _serverAchievements[dto.Id] = achievement.Clone();

                Console.WriteLine($"[Client] Loaded achievement: {achievement.Name} (Progress: {achievement.Progress}/{achievement.RequiredProgress})");
            }

            _lastServerSync = DateTime.UtcNow;
            SyncCompleted?.Invoke($"成功同步 {_achievements.Count} 个成就");
            Console.WriteLine($"[Client] Achievement sync completed, {_achievements.Count} achievements loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client] Achievement sync failed: {ex.Message}");
            SyncFailed?.Invoke($"同步失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查本地成就与服务器数据的差异
    /// </summary>
    public List<string> GetModifiedAchievements()
    {
        var modified = new List<string>();

        foreach (var (id, local) in _achievements)
        {
            if (_serverAchievements.TryGetValue(id, out var server))
            {
                if (local.Progress != server.Progress || 
                    local.IsCompleted != server.IsCompleted)
                {
                    modified.Add(id);
                }
            }
        }

        return modified;
    }

    /// <summary>
    /// 验证成就状态一致性
    /// </summary>
    public bool ValidateSyncState()
    {
        int inconsistencies = 0;

        foreach (var (id, local) in _achievements)
        {
            if (!_serverAchievements.TryGetValue(id, out var server))
            {
                Console.WriteLine($"[Client] Achievement '{id}' missing in server cache");
                inconsistencies++;
                continue;
            }

            if (local.Progress != server.Progress)
            {
                Console.WriteLine($"[Client] Achievement '{id}' progress mismatch: local={local.Progress}, server={server.Progress}");
                inconsistencies++;
            }

            if (local.IsCompleted != server.IsCompleted)
            {
                Console.WriteLine($"[Client] Achievement '{id}' completion status mismatch");
                inconsistencies++;
            }
        }

        if (inconsistencies == 0)
        {
            Console.WriteLine("[Client] Achievement state validation passed");
            return true;
        }

        Console.WriteLine($"[Client] Found {inconsistencies} inconsistencies in achievement state");
        return false;
    }

    /// <summary>
    /// 导出成就数据用于服务端保存
    /// </summary>
    public List<AchievementData> ExportToServer()
    {
        return _achievements.Values.Select(a => new AchievementData
        {
            Id = a.Id,
            Progress = a.Progress,
            IsCompleted = a.IsCompleted,
            CompletedTime = a.CompletedTime
        }).ToList();
    }
}

/// <summary>
/// 用于网络传输的成就数据
/// </summary>
public class AchievementData
{
    public string Id { get; set; } = string.Empty;
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedTime { get; set; }
}
