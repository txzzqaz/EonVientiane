using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EonVientiane;

/// <summary>
/// 成就系统测试示例
/// 演示如何使用成就系统
/// </summary>
public class AchievementSystemExample
{
    /// <summary>
    /// 示例：初始化和基本使用
    /// </summary>
    public static void BasicUsageExample()
    {
        // 创建库存管理器
        var inventoryManager = new InventoryManager();
        
        // 创建成就系统
        var achievementSystem = new AchievementSystem(inventoryManager);
        
        // 设置用户ID（从服务端获取）
        achievementSystem.SetUserId("user123");
        
        // 订阅成就完成事件
        achievementSystem.AchievementCompleted += (achievement) =>
        {
            Console.WriteLine($"🎉 成就完成: {achievement.Name}");
            Console.WriteLine($"   描述: {achievement.Description}");
            Console.WriteLine($"   获得奖励数: {achievement.Rewards.Count}");
        };
        
        // 订阅奖励发放事件
        achievementSystem.RewardGiven += (reward) =>
        {
            Console.WriteLine($"📦 获得奖励: {reward.Type} x{reward.Quantity}");
        };
        
        // 获取所有成就
        var allAchievements = achievementSystem.GetAllAchievements();
        Console.WriteLine($"\n总共有 {allAchievements.Count} 个成就");
        
        // 打印成就列表
        foreach (var achievement in allAchievements)
        {
            Console.WriteLine($"\n- {achievement.Name}");
            Console.WriteLine($"  描述: {achievement.Description}");
            Console.WriteLine($"  进度: {achievement.Progress}/{achievement.RequiredProgress}");
            Console.WriteLine($"  完成: {(achievement.IsCompleted ? "是" : "否")}");
        }
    }

    /// <summary>
    /// 示例：更新成就进度
    /// </summary>
    public static void UpdateProgressExample()
    {
        var inventoryManager = new InventoryManager();
        var achievementSystem = new AchievementSystem(inventoryManager);
        achievementSystem.SetUserId("user123");
        
        // 订阅完成事件
        achievementSystem.AchievementCompleted += (achievement) =>
        {
            Console.WriteLine($"✅ 成就完成: {achievement.Name}");
        };
        
        // 更新 "首次胜利" 成就进度
        Console.WriteLine("更新首次胜利成就...");
        achievementSystem.UpdateProgress("first_victory", 1);
        
        // 更新 "战斗好手" 成就进度
        Console.WriteLine("更新战斗好手成就...");
        for (int i = 0; i < 10; i++)
        {
            achievementSystem.UpdateProgress("battle_master", 1);
        }
        
        // 获取完成统计
        var stats = achievementSystem.GetCompletionStats();
        Console.WriteLine($"\n完成进度: {stats.completed}/{stats.total} ({stats.percentage:F1}%)");
    }

    /// <summary>
    /// 示例：检查成就状态
    /// </summary>
    public static void CheckAchievementStatusExample()
    {
        var inventoryManager = new InventoryManager();
        var achievementSystem = new AchievementSystem(inventoryManager);
        achievementSystem.SetUserId("user123");
        
        // 更新一些成就
        achievementSystem.UpdateProgress("first_victory", 1);
        
        // 获取特定成就
        var firstVictory = achievementSystem.GetAchievement("first_victory");
        
        if (firstVictory != null)
        {
            Console.WriteLine($"成就: {firstVictory.Name}");
            Console.WriteLine($"进度: {firstVictory.Progress}/{firstVictory.RequiredProgress}");
            Console.WriteLine($"已完成: {firstVictory.IsCompleted}");
            
            if (firstVictory.IsCompleted && firstVictory.CompletedTime.HasValue)
            {
                Console.WriteLine($"完成时间: {firstVictory.CompletedTime:yyyy-MM-dd HH:mm:ss}");
            }
            
            Console.WriteLine($"奖励数: {firstVictory.Rewards.Count}");
            foreach (var reward in firstVictory.Rewards)
            {
                Console.WriteLine($"  - {reward.Type}: {reward.Quantity} x {reward.ItemId}");
            }
        }
    }

    /// <summary>
    /// 示例：获取已完成的成就
    /// </summary>
    public static void GetCompletedAchievementsExample()
    {
        var inventoryManager = new InventoryManager();
        var achievementSystem = new AchievementSystem(inventoryManager);
        achievementSystem.SetUserId("user123");
        
        // 完成一些成就
        achievementSystem.UpdateProgress("first_victory", 1);
        achievementSystem.UpdateProgress("battle_master", 10);
        
        // 获取已完成的成就
        var completedAchievements = achievementSystem.GetCompletedAchievements();
        
        Console.WriteLine($"已完成的成就: {completedAchievements.Count}");
        foreach (var achievement in completedAchievements)
        {
            Console.WriteLine($"- {achievement.Name}");
            Console.WriteLine($"  完成时间: {achievement.CompletedTime:yyyy-MM-dd HH:mm:ss}");
        }
    }

    /// <summary>
    /// 示例：与服务端同步成就
    /// </summary>
    public static void SyncWithServerExample()
    {
        var inventoryManager = new InventoryManager();
        var achievementSystem = new AchievementSystem(inventoryManager);
        achievementSystem.SetUserId("user123");
        
        // 更新本地成就
        achievementSystem.UpdateProgress("first_victory", 1);
        
        // 导出成就数据用于服务端保存
        var achievementData = achievementSystem.ExportToServer();
        
        Console.WriteLine("导出的成就数据:");
        foreach (var data in achievementData)
        {
            Console.WriteLine($"- 成就ID: {data.Id}");
            Console.WriteLine($"  进度: {data.Progress}");
            Console.WriteLine($"  已完成: {data.IsCompleted}");
            if (data.CompletedTime.HasValue)
            {
                Console.WriteLine($"  完成时间: {data.CompletedTime}");
            }
        }
        
        // 模拟从服务端加载成就
        Console.WriteLine("\n加载服务端成就数据...");
        achievementSystem.LoadFromServer(achievementData);
        
        var stats = achievementSystem.GetCompletionStats();
        Console.WriteLine($"同步后的成就进度: {stats.completed}/{stats.total}");
    }

    /// <summary>
    /// 在实际游戏中的使用场景
    /// </summary>
    public static void GameIntegrationExample()
    {
        // 游戏中的使用场景
        Console.WriteLine("=== 游戏集成示例 ===\n");
        
        var inventoryManager = new InventoryManager();
        var achievementSystem = new AchievementSystem(inventoryManager);
        achievementSystem.SetUserId("player1");
        
        // 1. 玩家赢得战斗
        Console.WriteLine("场景1: 玩家赢得第一场战斗");
        achievementSystem.UpdateProgress("first_victory", 1);
        Console.WriteLine();
        
        // 2. 玩家收集物品
        Console.WriteLine("场景2: 玩家收集装备");
        for (int i = 0; i < 5; i++)
        {
            achievementSystem.UpdateProgress("item_collector", 1);
            Console.WriteLine($"已收集 {i + 1} 件装备...");
        }
        Console.WriteLine();
        
        // 3. 显示成就进度
        Console.WriteLine("当前成就进度:");
        var achievements = achievementSystem.GetAllAchievements();
        foreach (var achievement in achievements)
        {
            string bar = new string('█', achievement.Progress) + 
                        new string('░', achievement.RequiredProgress - achievement.Progress);
            Console.WriteLine($"{achievement.Name:15} [{bar}] {achievement.Progress}/{achievement.RequiredProgress}");
        }
        
        // 4. 获得的奖励在背包中
        Console.WriteLine("\n背包中的物品:");
        foreach (var item in inventoryManager.InventoryItems)
        {
            Console.WriteLine($"- {item.Item.Name} x{item.Quantity}");
        }
    }
}
