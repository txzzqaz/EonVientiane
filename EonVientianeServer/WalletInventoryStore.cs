using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 钱包存储包装器 - 提供与InventoryStore兼容的接口，内部使用WalletManager
/// 
/// 这个类作为桥接层，允许现有代码无缝迁移到新的钱包系统
/// </summary>
public class WalletInventoryStore
{
    private readonly WalletManager _walletManager;
    private readonly UserManager? _userManager;
    
    public WalletInventoryStore(WalletManager walletManager, UserManager? userManager = null)
    {
        _walletManager = walletManager;
        _userManager = userManager;
    }
    
    /// <summary>
    /// 加载或创建用户背包（兼容旧接口）
    /// </summary>
    public UserInventoryStateData LoadOrCreate(string userId, Func<List<InitialInventoryItem>> initialFactory)
    {
        var initialItems = initialFactory();
        var wallet = _walletManager.LoadOrCreateWallet(userId, initialItems);
        
        // 转换为旧格式
        return ConvertWalletToInventoryState(wallet);
    }
    
    /// <summary>
    /// 保存背包（兼容旧接口）
    /// </summary>
    public UserInventoryStateData Save(UserInventoryStateData state)
    {
        var wallet = ConvertInventoryStateToWallet(state);
        _walletManager.SaveWallet(wallet);
        return state;
    }
    
    /// <summary>
    /// 转换为DTO（兼容旧接口）
    /// </summary>
    public InventoryState ToDto(UserInventoryStateData state)
    {
        return new InventoryState
        {
            Items = state.Items
                .Select(item => new InventoryItemDto
                {
                    StackId = item.StackId,
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    IsEquipped = item.IsEquipped
                })
                .ToList()
        };
    }
    
    /// <summary>
    /// 获取底层的钱包管理器（用于直接访问钱包功能）
    /// </summary>
    public WalletManager GetWalletManager() => _walletManager;
    
    /// <summary>
    /// 将钱包转换为旧的库存状态格式
    /// </summary>
    private UserInventoryStateData ConvertWalletToInventoryState(PlayerWallet wallet)
    {
        var state = new UserInventoryStateData
        {
            UserId = wallet.UserId,
            Items = new List<InventoryStackRecord>()
        };
        
        foreach (var item in wallet.Items)
        {
            state.Items.Add(new InventoryStackRecord
            {
                StackId = item.InstanceId,
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                IsEquipped = item.IsEquipped
            });
        }
        
        return state;
    }
    
    /// <summary>
    /// 将旧的库存状态转换为钱包（注意：会重新签名所有道具）
    /// </summary>
    private PlayerWallet ConvertInventoryStateToWallet(UserInventoryStateData state)
    {
        var wallet = _walletManager.LoadOrCreateWallet(state.UserId);
        
        // 清空现有道具
        wallet.Items.Clear();
        
        // 重新签发所有道具
        foreach (var item in state.Items)
        {
            var signedItem = _walletManager.IssueItem(
                state.UserId,
                item.ItemId,
                item.ItemName,
                item.Quantity
            );
            signedItem.IsEquipped = item.IsEquipped;
            wallet.Items.Add(signedItem);
        }
        
        return wallet;
    }
}

/// <summary>
/// 钱包迁移工具 - 将旧的InventoryStore数据迁移到新的WalletManager
/// </summary>
public class WalletMigrationTool
{
    private readonly InventoryStore _oldStore;
    private readonly WalletManager _walletManager;
    
    public WalletMigrationTool(InventoryStore oldStore, WalletManager walletManager)
    {
        _oldStore = oldStore;
        _walletManager = walletManager;
    }
    
    /// <summary>
    /// 迁移单个用户的数据
    /// </summary>
    public PlayerWallet MigrateUser(string userId, Func<List<InitialInventoryItem>> initialFactory)
    {
        Console.WriteLine($"[Migration] Migrating user {userId}...");
        
        // 从旧存储加载数据
        var oldData = _oldStore.LoadOrCreate(userId, initialFactory);
        
        // 迁移到新钱包
        var wallet = _walletManager.MigrateFromInventoryStore(oldData);
        
        Console.WriteLine($"[Migration] Successfully migrated {wallet.Items.Count} items for user {userId}");
        
        return wallet;
    }
    
    /// <summary>
    /// 批量迁移多个用户
    /// </summary>
    public Dictionary<string, PlayerWallet> MigrateUsers(List<string> userIds, Func<string, Func<List<InitialInventoryItem>>> initialFactoryProvider)
    {
        var results = new Dictionary<string, PlayerWallet>();
        
        Console.WriteLine($"[Migration] Starting batch migration for {userIds.Count} users...");
        
        foreach (var userId in userIds)
        {
            try
            {
                var factory = initialFactoryProvider(userId);
                var wallet = MigrateUser(userId, factory);
                results[userId] = wallet;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error migrating user {userId}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"[Migration] Batch migration complete: {results.Count}/{userIds.Count} successful");
        
        return results;
    }
}
