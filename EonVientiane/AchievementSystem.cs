using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// 成就系统 - 管理游戏内的成就和奖励
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
    private string _userId = string.Empty;
    private InventoryManager _inventoryManager;

    // 事件：成就完成时触发
    public event Action<Achievement>? AchievementCompleted;
    public event Action<Reward>? RewardGiven;

    public IReadOnlyDictionary<string, Achievement> Achievements => _achievements.AsReadOnly();

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
    }

    /// <summary>
    /// 初始化默认成就
    /// </summary>
    private void InitializeDefaultAchievements()
    {
        // 成就1: 首次胜利
        // _achievements["first_victory"] = new Achievement
        // {
        //     Id = "first_victory",
        //     Name = "初露锋芒",
        //     Description = "赢得第一场战斗",
        //     Icon = "achievement_first_win",
        //     RequiredProgress = 1,
        //     Rewards = new List<Reward>
        //     {
        //         new Reward { Type = "Item", ItemId = "item_reward_1", Quantity = 1 },
        //         new Reward { Type = "Gold", ItemId = "", Quantity = 100 }
        //     }
        // };

        // 成就2: 战斗好手
        // _achievements["battle_master"] = new Achievement
        // {
        //     Id = "battle_master",
        //     Name = "战斗好手",
        //     Description = "赢得10场战斗",
        //     Icon = "achievement_battle_master",
        //     RequiredProgress = 10,
        //     Rewards = new List<Reward>
        //     {
        //         new Reward { Type = "Item", ItemId = "item_reward_2", Quantity = 1 },
        //         new Reward { Type = "Gold", ItemId = "", Quantity = 500 }
        //     }
        // };

        // 成就3: 装备收集家
        // _achievements["item_collector"] = new Achievement
        // {
        //     Id = "item_collector",
        //     Name = "装备收集家",
        //     Description = "收集20件装备",
        //     Icon = "achievement_collector",
        //     RequiredProgress = 20,
        //     Rewards = new List<Reward>
        //     {
        //         new Reward { Type = "Item", ItemId = "item_reward_3", Quantity = 1 },
        //         new Reward { Type = "Gold", ItemId = "", Quantity = 300 }
        //     }
        // };

        // 成就4: 无敌战士
        // _achievements["no_death_warrior"] = new Achievement
        // {
        //     Id = "no_death_warrior",
        //     Name = "无敌战士",
        //     Description = "完成5场无死亡战斗",
        //     Icon = "achievement_warrior",
        //     RequiredProgress = 5,
        //     Rewards = new List<Reward>
        //     {
        //         new Reward { Type = "Item", ItemId = "item_reward_4", Quantity = 1 },
        //         new Reward { Type = "Gold", ItemId = "", Quantity = 200 }
        //     }
        // };

        // 成就5: 时间旅者
        // _achievements["time_traveler"] = new Achievement
        // {
        //     Id = "time_traveler",
        //     Name = "时间旅者",
        //     Description = "游戏时间累计10小时",
        //     Icon = "achievement_traveler",
        //     RequiredProgress = 10,
        //     Rewards = new List<Reward>
        //     {
        //         new Reward { Type = "Item", ItemId = "item_reward_5", Quantity = 1 },
        //         new Reward { Type = "Gold", ItemId = "", Quantity = 400 }
        //     }
        // };
    }

    /// <summary>
    /// 更新成就进度
    /// </summary>
    public void UpdateProgress(string achievementId, int progressDelta)
    {
        if (!_achievements.TryGetValue(achievementId, out var achievement))
            return;

        if (achievement.IsCompleted)
            return;

        achievement.Progress += progressDelta;
        achievement.Progress = Math.Min(achievement.Progress, achievement.RequiredProgress);

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
    public void LoadFromServer(List<AchievementData> serverData)
    {
        foreach (var data in serverData)
        {
            if (_achievements.TryGetValue(data.Id, out var achievement))
            {
                achievement.Progress = data.Progress;
                achievement.IsCompleted = data.IsCompleted;
                achievement.CompletedTime = data.CompletedTime;
            }
        }
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
