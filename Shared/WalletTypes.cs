using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EonVientiane.Shared;

/// <summary>
/// 签名道具 - 每个道具实例都包含加密签名以确保完整性
/// 
/// 这个结构类似于区块链中的NFT，确保道具的所有权和完整性：
/// - ItemId: 道具的类型标识
/// - ItemName: 道具名称
/// - Quantity: 数量（对于可堆叠道具）
/// - InstanceId: 唯一实例ID（类似NFT的Token ID）
/// - IssuedAt: 签发时间戳
/// - Signature: 服务器的数字签名
/// 
/// 签名覆盖所有字段（除了Signature本身），任何修改都会导致签名失效
/// </summary>
public class SignedItem
{
    /// <summary>
    /// 道具类型ID
    /// </summary>
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;
    
    /// <summary>
    /// 道具名称
    /// </summary>
    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;
    
    /// <summary>
    /// 数量
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    /// <summary>
    /// 唯一实例ID（用于区分同类道具的不同实例）
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = string.Empty;
    
    /// <summary>
    /// 签发时间戳（Unix时间戳）
    /// </summary>
    [JsonPropertyName("issuedAt")]
    public long IssuedAt { get; set; }
    
    /// <summary>
    /// 是否已装备
    /// </summary>
    [JsonPropertyName("isEquipped")]
    public bool IsEquipped { get; set; }
    
    /// <summary>
    /// 服务器数字签名（Base64编码）
    /// 这个签名确保道具的真实性和完整性
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
    
    /// <summary>
    /// 扩展数据（用于未来功能，如道具属性、强化等级等）
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// 获取用于签名的数据字符串
    /// 注意：这个方法必须保持向后兼容，不能改变字段顺序或格式
    /// </summary>
    public string GetSignableData()
    {
        // 使用固定格式，确保签名的稳定性
        var parts = new List<string>
        {
            $"ItemId:{ItemId}",
            $"ItemName:{ItemName}",
            $"Quantity:{Quantity}",
            $"InstanceId:{InstanceId}",
            $"IssuedAt:{IssuedAt}",
            $"IsEquipped:{IsEquipped}"
        };
        
        // 如果有扩展数据，按键排序后添加
        if (Metadata != null && Metadata.Count > 0)
        {
            var sortedKeys = new List<string>(Metadata.Keys);
            sortedKeys.Sort();
            
            foreach (var key in sortedKeys)
            {
                parts.Add($"Meta:{key}={Metadata[key]}");
            }
        }
        
        return string.Join("|", parts);
    }
}

/// <summary>
/// 玩家钱包 - 存储玩家所有的签名道具
/// 
/// 类似区块链钱包，但是：
/// - 不支持玩家间转账（因为我们不需要玩家交易）
/// - 所有道具都由服务器签发
/// - 客户端可以离线验证道具的真实性
/// </summary>
public class PlayerWallet
{
    /// <summary>
    /// 钱包所有者的用户ID
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 钱包中的所有道具
    /// </summary>
    [JsonPropertyName("items")]
    public List<SignedItem> Items { get; set; } = new();
    
    /// <summary>
    /// 钱包版本号（用于未来升级时的兼容性）
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public long LastUpdated { get; set; }
}

/// <summary>
/// 道具签发请求
/// </summary>
public class IssueItemRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;
    
    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// 钱包验证结果
/// </summary>
public class WalletValidationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }
    
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }
    
    [JsonPropertyName("validItems")]
    public int ValidItems { get; set; }
    
    [JsonPropertyName("invalidItems")]
    public List<string> InvalidItems { get; set; } = new();
    
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}
