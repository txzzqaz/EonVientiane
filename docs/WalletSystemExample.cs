using System;
using System.Collections.Generic;
using EonVientiane.Shared;
using EonVientianeServer;

namespace Examples;

/// <summary>
/// 钱包系统使用示例
/// 
/// 演示如何在实际项目中使用钱包系统
/// </summary>
public class WalletSystemExample
{
    public static void ServerExample()
    {
        Console.WriteLine("=== 服务器端示例 ===\n");
        
        // 1. 初始化钱包管理器
        var walletManager = new WalletManager("data/wallets_example");
        Console.WriteLine($"✓ 钱包管理器已初始化");
        
        // 获取公钥（用于分发给客户端）
        var publicKey = walletManager.GetPublicKey();
        Console.WriteLine($"✓ 公钥已生成（长度: {publicKey.Length} 字符）\n");
        
        // 2. 为新玩家创建钱包并签发初始道具
        var userId = "player_demo_001";
        var initialItems = new List<InitialInventoryItem>
        {
            new() { ItemId = "d6_dice", ItemName = "六面骰", Quantity = 1 },
            new() { ItemId = "feathered_dice", ItemName = "羽化", Quantity = 1 },
            new() { ItemId = "health_potion", ItemName = "生命药水", Quantity = 5 }
        };
        
        var wallet = walletManager.LoadOrCreateWallet(userId, initialItems);
        Console.WriteLine($"✓ 为用户 {userId} 创建钱包");
        Console.WriteLine($"  包含 {wallet.Items.Count} 个道具\n");
        
        // 3. 给玩家签发成就奖励
        Console.WriteLine("签发成就奖励...");
        var rewardItem = walletManager.IssueItem(
            userId, 
            "legendary_sword", 
            "传说之剑", 
            1,
            new Dictionary<string, string>
            {
                { "quality", "legendary" },
                { "level", "50" },
                { "attack", "999" }
            }
        );
        
        wallet = walletManager.LoadOrCreateWallet(userId);
        wallet.Items.Add(rewardItem);
        walletManager.SaveWallet(wallet);
        
        Console.WriteLine($"✓ 签发道具: {rewardItem.ItemName}");
        Console.WriteLine($"  实例ID: {rewardItem.InstanceId}");
        Console.WriteLine($"  签名: {rewardItem.Signature.Substring(0, 40)}...");
        Console.WriteLine($"  扩展属性: quality={rewardItem.Metadata?["quality"]}, level={rewardItem.Metadata?["level"]}\n");
        
        // 4. 验证钱包完整性
        var validationResult = walletManager.ValidateWallet(wallet);
        Console.WriteLine($"✓ 钱包验证结果:");
        Console.WriteLine($"  是否有效: {validationResult.IsValid}");
        Console.WriteLine($"  总道具数: {validationResult.TotalItems}");
        Console.WriteLine($"  有效道具: {validationResult.ValidItems}");
        Console.WriteLine($"  无效道具: {validationResult.InvalidItems.Count}\n");
        
        // 5. 模拟篡改检测
        Console.WriteLine("模拟道具篡改...");
        var tamperedItem = wallet.Items[0];
        var originalQuantity = tamperedItem.Quantity;
        tamperedItem.Quantity = 999; // 篡改数量
        
        bool isTamperedValid = walletManager.VerifyItem(tamperedItem);
        Console.WriteLine($"✗ 篡改后的道具验证: {isTamperedValid} (预期: false)");
        
        tamperedItem.Quantity = originalQuantity; // 恢复
        bool isRestoredValid = walletManager.VerifyItem(tamperedItem);
        Console.WriteLine($"✓ 恢复后的道具验证: {isRestoredValid} (预期: true)\n");
        
        Console.WriteLine("=== 服务器端示例完成 ===\n");
    }
    
    public static void ClientExample(string publicKey)
    {
        Console.WriteLine("=== 客户端示例 ===\n");
        
        // 1. 初始化验证器
        var validator = new EonVientiane.WalletValidator();
        validator.Initialize(publicKey);
        Console.WriteLine("✓ 客户端验证器已初始化\n");
        
        // 2. 模拟从服务器接收钱包数据
        // （实际应用中，这个钱包是从网络接收的）
        var walletManager = new WalletManager("data/wallets_example");
        var userId = "player_demo_001";
        var wallet = walletManager.LoadOrCreateWallet(userId);
        
        Console.WriteLine($"收到服务器钱包数据 (用户: {userId})");
        Console.WriteLine($"道具数量: {wallet.Items.Count}\n");
        
        // 3. 验证钱包
        Console.WriteLine("验证钱包完整性...");
        var result = validator.ValidateWallet(wallet);
        
        if (result.IsValid)
        {
            Console.WriteLine($"✓ 钱包验证通过!");
            Console.WriteLine($"  所有 {result.TotalItems} 个道具均有效\n");
            
            // 4. 显示有效道具列表
            Console.WriteLine("有效道具列表:");
            foreach (var item in wallet.Items)
            {
                Console.WriteLine($"  • {item.ItemName} x{item.Quantity}");
                if (item.Metadata != null && item.Metadata.Count > 0)
                {
                    Console.WriteLine($"    属性: {string.Join(", ", item.Metadata.Select(kv => $"{kv.Key}={kv.Value}"))}");
                }
            }
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"✗ 钱包验证失败!");
            Console.WriteLine($"  有效道具: {result.ValidItems}/{result.TotalItems}");
            Console.WriteLine($"  错误:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"    - {error}");
            }
            Console.WriteLine();
        }
        
        // 5. 检查特定道具
        var hasDice = validator.HasValidItem(wallet, "d6_dice");
        var diceCount = validator.GetValidItemQuantity(wallet, "d6_dice");
        Console.WriteLine($"是否拥有六面骰: {hasDice}");
        Console.WriteLine($"六面骰数量: {diceCount}\n");
        
        Console.WriteLine("=== 客户端示例完成 ===\n");
    }
    
    public static void OfflineBattleExample(string publicKey)
    {
        Console.WriteLine("=== 离线对战示例 ===\n");
        
        // 初始化验证器
        var validator = new EonVientiane.WalletValidator();
        validator.Initialize(publicKey);
        
        // 模拟两个玩家的钱包
        var walletManager = new WalletManager("data/wallets_example");
        
        var player1Wallet = walletManager.LoadOrCreateWallet("player1");
        var player2Wallet = walletManager.LoadOrCreateWallet("player2");
        
        Console.WriteLine("局域网对战准备...");
        Console.WriteLine($"玩家1 道具数: {player1Wallet.Items.Count}");
        Console.WriteLine($"玩家2 道具数: {player2Wallet.Items.Count}\n");
        
        // 验证双方钱包
        var p1Valid = validator.ValidateWallet(player1Wallet);
        var p2Valid = validator.ValidateWallet(player2Wallet);
        
        if (p1Valid.IsValid && p2Valid.IsValid)
        {
            Console.WriteLine("✓ 双方道具验证通过，可以开始对战!");
            Console.WriteLine("  即使在离线状态下，也能确保公平竞技\n");
        }
        else
        {
            Console.WriteLine("✗ 检测到无效道具，拒绝对战!");
            if (!p1Valid.IsValid)
            {
                Console.WriteLine($"  玩家1 有 {p1Valid.InvalidItems.Count} 个无效道具");
            }
            if (!p2Valid.IsValid)
            {
                Console.WriteLine($"  玩家2 有 {p2Valid.InvalidItems.Count} 个无效道具");
            }
            Console.WriteLine();
        }
        
        Console.WriteLine("=== 离线对战示例完成 ===\n");
    }
    
    public static void MigrationExample()
    {
        Console.WriteLine("=== 数据迁移示例 ===\n");
        
        // 1. 创建旧的InventoryStore
        var oldStore = new InventoryStore("data/users_old");
        
        // 2. 创建新的WalletManager
        var walletManager = new WalletManager("data/wallets_migration");
        
        // 3. 创建迁移工具
        var migrationTool = new WalletMigrationTool(oldStore, walletManager);
        
        // 4. 迁移用户
        var userId = "migration_test_user";
        var initialItems = new List<InitialInventoryItem>
        {
            new() { ItemId = "d6_dice", ItemName = "六面骰", Quantity = 2 },
            new() { ItemId = "health_potion", ItemName = "生命药水", Quantity = 10 }
        };
        
        Console.WriteLine($"迁移用户: {userId}");
        var migratedWallet = migrationTool.MigrateUser(userId, () => initialItems);
        
        Console.WriteLine($"✓ 迁移完成");
        Console.WriteLine($"  迁移道具数: {migratedWallet.Items.Count}");
        Console.WriteLine($"  所有道具已重新签名并验证\n");
        
        // 5. 验证迁移后的钱包
        var result = walletManager.ValidateWallet(migratedWallet);
        Console.WriteLine($"迁移后验证: {(result.IsValid ? "✓ 通过" : "✗ 失败")}");
        
        Console.WriteLine("\n=== 数据迁移示例完成 ===\n");
    }
    
    public static void Main(string[] args)
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║   钱包系统完整示例                     ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");
        
        try
        {
            // 1. 服务器端示例
            ServerExample();
            
            // 获取公钥用于客户端
            var walletManager = new WalletManager("data/wallets_example");
            var publicKey = walletManager.GetPublicKey();
            
            // 2. 客户端示例
            ClientExample(publicKey);
            
            // 3. 离线对战示例
            OfflineBattleExample(publicKey);
            
            // 4. 数据迁移示例
            MigrationExample();
            
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   所有示例运行完成！                   ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");
            
            Console.WriteLine("提示:");
            Console.WriteLine("  • 服务器私钥已保存到: data/wallets_example/server_keys.xml");
            Console.WriteLine("  • 公钥已保存到: data/wallets_example/public_key.xml");
            Console.WriteLine("  • 务必备份私钥文件！\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 错误: {ex.Message}");
            Console.WriteLine($"堆栈: {ex.StackTrace}");
        }
    }
}
