using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EonVientiane.Shared;

/// <summary>
/// 钱包加密系统 - 使用RSA非对称加密确保道具所有权的完整性
/// 
/// 设计理念：
/// - 服务器持有私钥，用于签署所有合法道具
/// - 客户端和离线模式使用公钥验证道具签名
/// - 每个道具都有唯一的签名，包含道具数据和时间戳
/// - 任何未经服务器签名的道具都无法通过验证
/// 
/// 这确保了即使在离线/局域网环境下，玩家也无法自行创建或修改道具
/// </summary>
public class WalletCrypto
{
    private const int KeySize = 2048; // RSA密钥大小
    private RSA? _privateKey;         // 服务器私钥（仅服务器持有）
    private RSA? _publicKey;          // 公钥（客户端和服务器都持有）
    
    /// <summary>
    /// 初始化为服务器模式（持有私钥）
    /// </summary>
    public static WalletCrypto CreateServerInstance(string? privateKeyXml = null)
    {
        var crypto = new WalletCrypto();
        
        if (string.IsNullOrEmpty(privateKeyXml))
        {
            // 生成新的密钥对
            crypto._privateKey = RSA.Create(KeySize);
        }
        else
        {
            // 从XML加载现有私钥
            crypto._privateKey = RSA.Create();
            crypto._privateKey.FromXmlString(privateKeyXml);
        }
        
        // 从私钥提取公钥
        crypto._publicKey = RSA.Create();
        crypto._publicKey.ImportParameters(crypto._privateKey.ExportParameters(false));
        
        return crypto;
    }
    
    /// <summary>
    /// 初始化为客户端模式（仅持有公钥）
    /// </summary>
    public static WalletCrypto CreateClientInstance(string publicKeyXml)
    {
        var crypto = new WalletCrypto
        {
            _publicKey = RSA.Create()
        };
        crypto._publicKey.FromXmlString(publicKeyXml);
        
        return crypto;
    }
    
    /// <summary>
    /// 导出私钥（仅服务器端使用，需要妥善保管）
    /// </summary>
    public string ExportPrivateKey()
    {
        if (_privateKey == null)
            throw new InvalidOperationException("此实例没有私钥");
            
        return _privateKey.ToXmlString(true);
    }
    
    /// <summary>
    /// 导出公钥（可以公开分发给客户端）
    /// </summary>
    public string ExportPublicKey()
    {
        if (_publicKey == null)
            throw new InvalidOperationException("未初始化");
            
        return _publicKey.ToXmlString(false);
    }
    
    /// <summary>
    /// 对道具数据进行签名（仅服务器端）
    /// </summary>
    /// <param name="itemData">道具的完整数据（JSON格式）</param>
    /// <returns>Base64编码的签名</returns>
    public string SignItemData(string itemData)
    {
        if (_privateKey == null)
            throw new InvalidOperationException("只有服务器端可以签名道具");
            
        var dataBytes = Encoding.UTF8.GetBytes(itemData);
        var signature = _privateKey.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        return Convert.ToBase64String(signature);
    }
    
    /// <summary>
    /// 验证道具签名（服务器端和客户端都可以使用）
    /// </summary>
    /// <param name="itemData">道具的完整数据（JSON格式）</param>
    /// <param name="signature">Base64编码的签名</param>
    /// <returns>签名是否有效</returns>
    public bool VerifyItemSignature(string itemData, string signature)
    {
        if (_publicKey == null)
            throw new InvalidOperationException("未初始化");
            
        try
        {
            var dataBytes = Encoding.UTF8.GetBytes(itemData);
            var signatureBytes = Convert.FromBase64String(signature);
            
            return _publicKey.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 为道具数据生成指纹（用于快速比对，不能用于验证完整性）
    /// </summary>
    public static string GenerateFingerprint(string itemData)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(itemData));
        return Convert.ToBase64String(hash);
    }
}
