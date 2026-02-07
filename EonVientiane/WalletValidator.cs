using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// 客户端钱包验证器
/// 
/// 用于在客户端（包括离线模式）验证道具的真实性
/// 只需要公钥，不需要连接服务器
/// </summary>
public class WalletValidator
{
    private WalletCrypto? _crypto;
    private bool _isInitialized;
    
    public WalletValidator()
    {
        _isInitialized = false;
    }
    
    /// <summary>
    /// 使用服务器公钥初始化验证器
    /// 这个方法应该在游戏启动时调用，使用内嵌或下载的公钥
    /// </summary>
    public void Initialize(string publicKeyXml)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("验证器已经初始化");
        }
        
        try
        {
            _crypto = WalletCrypto.CreateClientInstance(publicKeyXml);
            _isInitialized = true;
            
            Console.WriteLine("[WalletValidator] Initialized with server public key");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("无法初始化钱包验证器", ex);
        }
    }
    
    /// <summary>
    /// 验证单个道具
    /// </summary>
    public bool VerifyItem(SignedItem item)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("验证器未初始化，请先调用Initialize()");
        }
        
        if (string.IsNullOrEmpty(item.Signature))
        {
            Console.WriteLine($"[WalletValidator] Item {item.ItemName} has no signature");
            return false;
        }
        
        var signableData = item.GetSignableData();
        var isValid = _crypto.VerifyItemSignature(signableData, item.Signature);
        
        if (!isValid)
        {
            Console.WriteLine($"[WalletValidator] Item {item.ItemName} ({item.InstanceId}) has invalid signature!");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 验证整个钱包
    /// </summary>
    public WalletValidationResult ValidateWallet(PlayerWallet wallet)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("验证器未初始化，请先调用Initialize()");
        }
        
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
        
        if (!result.IsValid)
        {
            Console.WriteLine($"[WalletValidator] Wallet validation failed: {result.InvalidItems.Count} invalid items");
        }
        
        return result;
    }
    
    /// <summary>
    /// 过滤出有效的道具
    /// </summary>
    public List<SignedItem> GetValidItems(PlayerWallet wallet)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("验证器未初始化，请先调用Initialize()");
        }
        
        return wallet.Items.Where(item => VerifyItem(item)).ToList();
    }
    
    /// <summary>
    /// 检查玩家是否拥有特定道具（已验证）
    /// </summary>
    public bool HasValidItem(PlayerWallet wallet, string itemId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("验证器未初始化，请先调用Initialize()");
        }
        
        return wallet.Items.Any(item => item.ItemId == itemId && VerifyItem(item));
    }
    
    /// <summary>
    /// 获取特定道具的总数量（仅计算已验证的）
    /// </summary>
    public int GetValidItemQuantity(PlayerWallet wallet, string itemId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("验证器未初始化，请先调用Initialize()");
        }
        
        return wallet.Items
            .Where(item => item.ItemId == itemId && VerifyItem(item))
            .Sum(item => item.Quantity);
    }
}
