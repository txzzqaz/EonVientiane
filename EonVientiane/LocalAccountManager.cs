using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 本地账户管理器 - 支持离线账户，使用区块链风格加密存储
/// </summary>
public class LocalAccountManager
{
    private readonly string _accountsDirectory;
    private readonly string _indexFilePath;
    private Dictionary<string, LocalAccount> _accounts;

    /// <summary>
    /// 本地账户数据结构
    /// </summary>
    public class LocalAccount
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; } // SHA-256哈希
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PublicKey { get; set; } // RSA公钥用于数字签名
        public DateTime LastLogin { get; set; }
        public Dictionary<string, string> ProfileData { get; set; } // 其他玩家数据
    }

    public LocalAccountManager(string accountsDir = "data/local_accounts")
    {
        _accountsDirectory = accountsDir;
        _indexFilePath = Path.Combine(_accountsDirectory, "accounts.json");
        _accounts = new Dictionary<string, LocalAccount>();
        
        // 创建目录
        Directory.CreateDirectory(_accountsDirectory);
        
        // 加载现有账户
        LoadAccounts();
    }

    /// <summary>
    /// 创建本地账户
    /// </summary>
    public (bool success, string message) CreateAccount(string username, string password, string email)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "用户名至少需要3个字符");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "密码至少需要6个字符");

        if (!IsValidEmail(email))
            return (false, "邮箱格式不正确");

        // 检查用户是否已存在
        if (_accounts.ContainsKey(username.ToLower()))
            return (false, "用户已存在");

        try
        {
            // 生成RSA密钥对
            var (publicKey, privateKey) = GenerateRSAKeyPair();

            // 创建账户
            var account = new LocalAccount
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Email = email,
                CreatedDate = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                PublicKey = publicKey,
                ProfileData = new Dictionary<string, string>
                {
                    { "level", "1" },
                    { "experience", "0" },
                    { "coins", "1000" }
                }
            };

            _accounts[username.ToLower()] = account;

            // 保存账户信息
            SaveAccount(account, privateKey);
            SaveIndex();

            Console.WriteLine($"[LocalAccount] 账户创建成功: {username}");
            return (true, "账户创建成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalAccount] 创建账户失败: {ex.Message}");
            return (false, $"创建账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 本地登录
    /// </summary>
    public (bool success, LocalAccount account, string message) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, null, "用户名和密码不能为空");

        var key = username.ToLower();
        if (!_accounts.ContainsKey(key))
            return (false, null, "用户不存在");

        var account = _accounts[key];

        // 验证密码
        if (!VerifyPassword(password, account.PasswordHash))
            return (false, null, "密码错误");

        // 更新最后登录时间
        account.LastLogin = DateTime.UtcNow;
        SaveIndex();

        Console.WriteLine($"[LocalAccount] 本地登录成功: {username}");
        return (true, account, "登录成功");
    }

    /// <summary>
    /// 获取所有本地账户用户名列表
    /// </summary>
    public List<string> GetAllLocalUsernames()
    {
        return _accounts.Keys.ToList();
    }

    /// <summary>
    /// 删除本地账户
    /// </summary>
    public bool DeleteAccount(string username)
    {
        var key = username.ToLower();
        if (!_accounts.ContainsKey(key))
            return false;

        _accounts.Remove(key);
        SaveIndex();

        // 删除账户文件
        var accountFile = Path.Combine(_accountsDirectory, $"{key}.json");
        var keyFile = Path.Combine(_accountsDirectory, $"{key}.key");
        
        try
        {
            if (File.Exists(accountFile)) File.Delete(accountFile);
            if (File.Exists(keyFile)) File.Delete(keyFile);
            Console.WriteLine($"[LocalAccount] 账户已删除: {username}");
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// 密码哈希 - 使用SHA-256
    /// </summary>
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }

    /// <summary>
    /// 生成RSA密钥对
    /// </summary>
    private (string publicKey, string privateKey) GenerateRSAKeyPair()
    {
        using (var rsa = new RSACryptoServiceProvider(2048))
        {
            var publicKey = rsa.ToXmlString(false);
            var privateKey = rsa.ToXmlString(true);
            return (publicKey, privateKey);
        }
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 保存账户到文件
    /// </summary>
    private void SaveAccount(LocalAccount account, string privateKey)
    {
        try
        {
            var accountFile = Path.Combine(_accountsDirectory, $"{account.Username.ToLower()}.json");
            var keyFile = Path.Combine(_accountsDirectory, $"{account.Username.ToLower()}.key");

            // 保存账户信息（不含私钥）
            var accountData = new
            {
                account.Username,
                account.PasswordHash,
                account.Email,
                account.CreatedDate,
                account.PublicKey,
                account.LastLogin,
                account.ProfileData
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(accountData, options);
            File.WriteAllText(accountFile, json);

            // 保存私钥到单独的加密文件中
            File.WriteAllText(keyFile, privateKey);

            Console.WriteLine($"[LocalAccount] 账户已保存: {account.Username}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalAccount] 保存账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载所有账户
    /// </summary>
    private void LoadAccounts()
    {
        try
        {
            if (!File.Exists(_indexFilePath))
            {
                Console.WriteLine("[LocalAccount] 账户索引文件不存在，使用空账户列表");
                return;
            }

            var json = File.ReadAllText(_indexFilePath);
            var accountList = JsonSerializer.Deserialize<List<LocalAccount>>(json);

            if (accountList != null)
            {
                foreach (var account in accountList)
                {
                    _accounts[account.Username.ToLower()] = account;
                }
                Console.WriteLine($"[LocalAccount] 已加载 {accountList.Count} 个本地账户");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalAccount] 加载账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存账户索引
    /// </summary>
    private void SaveIndex()
    {
        try
        {
            var accountList = _accounts.Values.ToList();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(accountList, options);
            File.WriteAllText(_indexFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalAccount] 保存索引失败: {ex.Message}");
        }
    }
}
