using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 钱包管理器 - 服务器端管理所有玩家钱包
/// 
/// 职责：
/// 1. 管理服务器密钥对
/// 2. 为玩家签发道具
/// 3. 验证道具签名
/// 4. 持久化钱包数据
/// 5. 从旧的InventoryStore迁移数据
/// </summary>
public class WalletManager
{
    private readonly string _walletsDir;
    private readonly string _keysFile;
    private readonly WalletCrypto _crypto;
    private readonly Dictionary<string, PlayerWallet> _cache = new();
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    
    public WalletManager(string dataDir = "data/wallets")
    {
        _walletsDir = Path.Combine(dataDir, "wallets");
        _keysFile = Path.Combine(dataDir, "server_keys.xml");
        
        Directory.CreateDirectory(_walletsDir);
        Directory.CreateDirectory(dataDir);
        
        // 初始化或加载服务器密钥
        _crypto = InitializeKeys();
        
        Console.WriteLine("[WalletManager] Initialized with RSA-2048 encryption");
    }
    
    /// <summary>
    /// 获取公钥（用于分发给客户端）
    /// </summary>
    public string GetPublicKey()
    {
        return _crypto.ExportPublicKey();
    }
    
    /// <summary>
    /// 为玩家签发新道具
    /// </summary>
    public SignedItem IssueItem(string userId, string itemId, string itemName, int quantity = 1, Dictionary<string, string>? metadata = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("数量必须大于0", nameof(quantity));
            
        var item = new SignedItem
        {
            ItemId = itemId,
            ItemName = itemName,
            Quantity = quantity,
            InstanceId = Guid.NewGuid().ToString("N"),
            IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsEquipped = false,
            Metadata = metadata
        };
        
        // 生成签名
        var signableData = item.GetSignableData();
        item.Signature = _crypto.SignItemData(signableData);
        
        Console.WriteLine($"[WalletManager] Issued item '{itemName}' (ID: {itemId}) to user {userId}");
        
        return item;
    }
    
    /// <summary>
    /// 批量签发道具
    /// </summary>
    public List<SignedItem> IssueItems(string userId, List<IssueItemRequest> requests)
    {
        var items = new List<SignedItem>();
        
        foreach (var request in requests)
        {
            var item = IssueItem(userId, request.ItemId, request.ItemName, request.Quantity, request.Metadata);
            items.Add(item);
        }
        
        return items;
    }
    
    /// <summary>
    /// 验证道具签名
    /// </summary>
    public bool VerifyItem(SignedItem item)
    {
        if (string.IsNullOrEmpty(item.Signature))
            return false;
            
        var signableData = item.GetSignableData();
        return _crypto.VerifyItemSignature(signableData, item.Signature);
    }
    
    /// <summary>
    /// 加载或创建玩家钱包
    /// </summary>
    public PlayerWallet LoadOrCreateWallet(string userId, List<InitialInventoryItem>? initialItems = null)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(userId, out var cached))
            {
                return CloneWallet(cached);
            }
            
            var path = GetWalletPath(userId);
            PlayerWallet wallet;
            
            if (File.Exists(path))
            {
                // 加载现有钱包
                var json = File.ReadAllText(path);
                wallet = JsonSerializer.Deserialize<PlayerWallet>(json, _jsonOptions) ?? new PlayerWallet { UserId = userId };
                
                // 验证所有道具
                var invalidItems = wallet.Items.Where(item => !VerifyItem(item)).ToList();
                if (invalidItems.Any())
                {
                    Console.WriteLine($"[WalletManager] WARNING: User {userId} has {invalidItems.Count} invalid items!");
                    // 移除无效道具
                    wallet.Items.RemoveAll(item => !VerifyItem(item));
                }
            }
            else
            {
                // 创建新钱包
                wallet = new PlayerWallet
                {
                    UserId = userId,
                    Items = new List<SignedItem>(),
                    Version = 1,
                    LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                
                // 如果有初始道具，签发它们
                if (initialItems != null)
                {
                    foreach (var initial in initialItems)
                    {
                        var item = IssueItem(userId, initial.ItemId, initial.ItemName, initial.Quantity);
                        wallet.Items.Add(item);
                    }
                }
                
                SaveWalletInternal(wallet);
            }
            
            _cache[userId] = wallet;
            return CloneWallet(wallet);
        }
    }
    
    /// <summary>
    /// 保存钱包
    /// </summary>
    public void SaveWallet(PlayerWallet wallet)
    {
        lock (_lock)
        {
            wallet.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _cache[wallet.UserId] = CloneWallet(wallet);
            SaveWalletInternal(wallet);
        }
    }
    
    /// <summary>
    /// 向钱包添加道具
    /// </summary>
    public PlayerWallet AddItemToWallet(string userId, string itemId, string itemName, int quantity = 1)
    {
        lock (_lock)
        {
            var wallet = LoadOrCreateWallet(userId);
            var newItem = IssueItem(userId, itemId, itemName, quantity);
            wallet.Items.Add(newItem);
            SaveWallet(wallet);
            return wallet;
        }
    }
    
    /// <summary>
    /// 验证整个钱包
    /// </summary>
    public WalletValidationResult ValidateWallet(PlayerWallet wallet)
    {
        var result = new WalletValidationResult
        {
            TotalItems = wallet.Items.Count
        };
        
        foreach (var item in wallet.Items)
        {
            if (VerifyItem(item))
            {
                result.ValidItems++;
            }
            else
            {
                result.InvalidItems.Add($"{item.ItemName} ({item.InstanceId})");
                result.Errors.Add($"道具 '{item.ItemName}' 签名无效或已被篡改");
            }
        }
        
        result.IsValid = result.ValidItems == result.TotalItems;
        return result;
    }
    
    /// <summary>
    /// 从旧的InventoryStore迁移数据
    /// </summary>
    public PlayerWallet MigrateFromInventoryStore(UserInventoryStateData oldData)
    {
        var wallet = new PlayerWallet
        {
            UserId = oldData.UserId,
            Items = new List<SignedItem>(),
            Version = 1,
            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        foreach (var oldItem in oldData.Items)
        {
            var signedItem = IssueItem(
                oldData.UserId,
                oldItem.ItemId,
                oldItem.ItemName,
                oldItem.Quantity
            );
            signedItem.IsEquipped = oldItem.IsEquipped;
            wallet.Items.Add(signedItem);
        }
        
        SaveWallet(wallet);
        Console.WriteLine($"[WalletManager] Migrated {wallet.Items.Count} items for user {oldData.UserId}");
        
        return wallet;
    }
    
    private WalletCrypto InitializeKeys()
    {
        if (File.Exists(_keysFile))
        {
            // 加载现有密钥
            var keyXml = File.ReadAllText(_keysFile);
            Console.WriteLine("[WalletManager] Loaded existing server keys");
            return WalletCrypto.CreateServerInstance(keyXml);
        }
        else
        {
            // 生成新密钥对
            var crypto = WalletCrypto.CreateServerInstance();
            var privateKey = crypto.ExportPrivateKey();
            
            // 保存私钥（需要妥善保管）
            File.WriteAllText(_keysFile, privateKey);
            
            // 同时保存公钥副本，方便分发
            var publicKey = crypto.ExportPublicKey();
            var publicKeyFile = Path.Combine(Path.GetDirectoryName(_keysFile) ?? ".", "public_key.xml");
            File.WriteAllText(publicKeyFile, publicKey);
            
            Console.WriteLine("[WalletManager] Generated new RSA key pair");
            Console.WriteLine($"[WalletManager] Public key saved to: {publicKeyFile}");
            
            return crypto;
        }
    }
    
    private void SaveWalletInternal(PlayerWallet wallet)
    {
        var path = GetWalletPath(wallet.UserId);
        var json = JsonSerializer.Serialize(wallet, _jsonOptions);
        File.WriteAllText(path, json);
    }
    
    private string GetWalletPath(string userId)
    {
        return Path.Combine(_walletsDir, $"{userId}_wallet.json");
    }
    
    private PlayerWallet CloneWallet(PlayerWallet wallet)
    {
        var json = JsonSerializer.Serialize(wallet, _jsonOptions);
        return JsonSerializer.Deserialize<PlayerWallet>(json, _jsonOptions) ?? new PlayerWallet { UserId = wallet.UserId };
    }
}
