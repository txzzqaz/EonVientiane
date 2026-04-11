namespace EonVientiane.Core.Services;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 加密服务 - 提供数据加密和密码哈希功能
/// </summary>
public class EncryptionService
{
    private readonly byte[] encryptionKey;
    private readonly byte[] iv;
    private const int PasswordDeriveIterations = 100_000;

    /// <summary>
    /// 初始化加密服务
    /// 注意：生产环境应该从安全的配置中加载密钥
    /// </summary>
    public EncryptionService(string? key = null)
    {
        // 如果没有提供密钥，使用默认密钥（仅用于开发）
        // 生产环境必须使用强密钥
        if (string.IsNullOrEmpty(key))
        {
            // 生成一个基于默认值的256位密钥
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes("EonVientiane-Default-Key-2026"));
                encryptionKey = hash;
            }
        }
        else
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                encryptionKey = hash;
            }
        }

        // 初始化向量（IV）
        iv = new byte[16];
        Array.Copy(encryptionKey, 0, iv, 0, 16);
    }

    /// <summary>
    /// 对密码进行加密哈希（单向）
    /// 使用 PBKDF2 算法
    /// </summary>
    public string HashPassword(string password)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] salt = new byte[16];
            rng.GetBytes(salt);

#pragma warning disable SYSLIB0060
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                // 将盐和哈希值组合在一起存储
                byte[] hashWithSalt = new byte[48];
                Array.Copy(salt, 0, hashWithSalt, 0, 16);
                Array.Copy(hash, 0, hashWithSalt, 16, 32);

                return Convert.ToBase64String(hashWithSalt);
            }
#pragma warning restore SYSLIB0060
        }
    }

    /// <summary>
    /// 验证密码是否正确
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            byte[] hashWithSalt = Convert.FromBase64String(hash);

            // 提取盐
            byte[] salt = new byte[16];
            Array.Copy(hashWithSalt, 0, salt, 0, 16);

#pragma warning disable SYSLIB0060
            // 使用相同的盐对提供的密码进行哈希
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] newHash = pbkdf2.GetBytes(32);
                byte[] storedHash = hashWithSalt[16..48];
                return CryptographicOperations.FixedTimeEquals(storedHash, newHash);
            }
#pragma warning restore SYSLIB0060
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 加密字符串数据
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        byte[] encryptedData = ms.ToArray();
                        return Convert.ToBase64String(encryptedData);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("加密过程中出错", ex);
        }
    }

    /// <summary>
    /// 解密字符串数据
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream(buffer))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("解密过程中出错", ex);
        }
    }

    /// <summary>
    /// 生成加密的GUID
    /// </summary>
    public string GenerateEncryptedId()
    {
        return Encrypt(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// 计算数据的校验和（用于完整性验证）
    /// </summary>
    public string ComputeChecksum(string data)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// 验证数据完整性
    /// </summary>
    public bool VerifyChecksum(string data, string checksum)
    {
        string computed = ComputeChecksum(data);
        return computed == checksum;
    }

    public (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize);
        var publicPem = rsa.ExportRSAPublicKeyPem();
        var privatePem = rsa.ExportRSAPrivateKeyPem();
        return (publicPem, privatePem);
    }

    public byte[] EncryptKeyWithPublicKey(byte[] key, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] DecryptKeyWithPrivateKey(byte[] encryptedKey, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] SignData(byte[] data, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public bool VerifySignature(byte[] data, byte[] signature, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public string ProtectWithPassword(string plainText, string password)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
        Span<byte> salt = stackalloc byte[16];
        Span<byte> nonce = stackalloc byte[12];
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);

#pragma warning disable SYSLIB0060
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt.ToArray(), PasswordDeriveIterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(32);
#pragma warning restore SYSLIB0060

        var cipher = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plaintextBytes, cipher, tag);
        }

        var packed = new byte[1 + salt.Length + nonce.Length + tag.Length + cipher.Length];
        packed[0] = 1;
        salt.CopyTo(packed.AsSpan(1, salt.Length));
        nonce.CopyTo(packed.AsSpan(1 + salt.Length, nonce.Length));
        tag.CopyTo(packed.AsSpan(1 + salt.Length + nonce.Length, tag.Length));
        cipher.CopyTo(packed.AsSpan(1 + salt.Length + nonce.Length + tag.Length));
        return Convert.ToBase64String(packed);
    }

    public string UnprotectWithPassword(string protectedData, string password)
    {
        var packed = Convert.FromBase64String(protectedData);
        if (packed.Length < 45 || packed[0] != 1)
        {
            throw new InvalidOperationException("无效的受保护数据格式");
        }

        var salt = packed.AsSpan(1, 16).ToArray();
        var nonce = packed.AsSpan(17, 12).ToArray();
        var tag = packed.AsSpan(29, 16).ToArray();
        var cipher = packed.AsSpan(45).ToArray();

#pragma warning disable SYSLIB0060
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PasswordDeriveIterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(32);
#pragma warning restore SYSLIB0060

        var plaintext = new byte[cipher.Length];
        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Decrypt(nonce, cipher, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }
}
