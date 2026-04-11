namespace EonVientiane.Core.Services;

using EonVientiane.Core.Models;
using System.Text.Json;

/// <summary>
/// 账户管理服务 - 处理用户注册、登录、验证等
/// </summary>
public class AccountService
{
    private readonly EncryptionService encryptionService;
    private readonly string accountsDirectory;
    private Dictionary<string, User> cachedUsers;
    private User? currentUser;

    public AccountService(string? encryptionKey = null, string? accountsDirectory = null)
    {
        encryptionService = new EncryptionService(encryptionKey);
        this.accountsDirectory = accountsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EonVientiane", "Accounts");
        this.cachedUsers = new Dictionary<string, User>();
        this.currentUser = null;

        // 创建账户目录
        Directory.CreateDirectory(this.accountsDirectory);
        LoadAllAccounts();
    }

    /// <summary>
    /// 创建新账户
    /// </summary>
    public bool CreateAccount(string username, string password, string email)
    {
        // 验证用户名不重复
        if (cachedUsers.Values.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // 验证邮箱格式
        if (!IsValidEmail(email))
        {
            return false;
        }

        try
        {
            // 生成用户ID
            string userId = Guid.NewGuid().ToString();

            // 对密码进行哈希
            string passwordHash = encryptionService.HashPassword(password);

            // 为用户生成非对称密钥对，私钥使用用户密码再次保护
            var (publicKeyPem, privateKeyPem) = encryptionService.GenerateRsaKeyPair();
            string encryptedPrivateKeyPem = encryptionService.ProtectWithPassword(privateKeyPem, password);

            // 创建用户对象
            var user = new User(userId, username, email, passwordHash, publicKeyPem, encryptedPrivateKeyPem);

            // 保存用户
            cachedUsers[userId] = user;
            SaveAccount(user);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public bool Login(string username, string password)
    {
        var user = cachedUsers.Values.FirstOrDefault(u => 
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
            u.Status == UserStatus.Active);

        if (user == null)
        {
            return false;
        }

        if (!encryptionService.VerifyPassword(password, user.PasswordHash))
        {
            return false;
        }

        // 更新最后登录时间
        user.LastLoginAt = DateTime.Now;
        SaveAccount(user);

        // 设置为当前用户
        currentUser = user;

        return true;
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    public void Logout()
    {
        currentUser = null;
    }

    /// <summary>
    /// 获取当前登录用户
    /// </summary>
    public User? GetCurrentUser()
    {
        return currentUser;
    }

    /// <summary>
    /// 检查是否已登录
    /// </summary>
    public bool IsLoggedIn()
    {
        return currentUser != null;
    }

    /// <summary>
    /// 更改密码
    /// </summary>
    public bool ChangePassword(string oldPassword, string newPassword)
    {
        if (currentUser == null)
            return false;

        if (!encryptionService.VerifyPassword(oldPassword, currentUser.PasswordHash))
            return false;

        currentUser.PasswordHash = encryptionService.HashPassword(newPassword);
        SaveAccount(currentUser);

        return true;
    }

    /// <summary>
    /// 为当前用户保存加密的数据
    /// </summary>
    public bool SaveEncryptedData(string key, string data)
    {
        if (currentUser == null)
            return false;

        try
        {
            currentUser.EncryptedData[key] = encryptionService.Encrypt(data);
            SaveAccount(currentUser);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从当前用户读取加密的数据
    /// </summary>
    public string? GetEncryptedData(string key)
    {
        if (currentUser == null)
            return null;

        if (!currentUser.EncryptedData.TryGetValue(key, out var encryptedValue))
            return null;

        try
        {
            return encryptionService.Decrypt(encryptedValue);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取所有用户名列表
    /// </summary>
    public List<string> GetAllUsernames()
    {
        return cachedUsers.Values
            .Where(u => u.Status == UserStatus.Active)
            .Select(u => u.Username)
            .OrderBy(u => u)
            .ToList();
    }

    /// <summary>
    /// 删除账户
    /// </summary>
    public bool DeleteAccount(string password)
    {
        if (currentUser == null)
            return false;

        if (!encryptionService.VerifyPassword(password, currentUser.PasswordHash))
            return false;

        try
        {
            var userId = currentUser.UserId;
            var filePath = GetAccountFilePath(userId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            cachedUsers.Remove(userId);
            Logout();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 重置当前账户（删除并使用相同用户名/密码/邮箱重新创建）
    /// </summary>
    public bool ResetCurrentAccount(string password)
    {
        if (currentUser == null)
            return false;

        if (!encryptionService.VerifyPassword(password, currentUser.PasswordHash))
            return false;

        var username = currentUser.Username;
        var email = currentUser.Email;

        try
        {
            var oldUserId = currentUser.UserId;
            var filePath = GetAccountFilePath(oldUserId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            cachedUsers.Remove(oldUserId);
            Logout();

            return CreateAccount(username, password, email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取账户信息
    /// </summary>
    public string GetAccountInfo()
    {
        if (currentUser == null)
            return "未登录";

        return currentUser.GetUserInfo();
    }

    public string? GetCurrentUserPublicKeyPem()
    {
        return currentUser?.PublicKeyPem;
    }

    public bool TryGetCurrentUserPrivateKeyPem(string password, out string? privateKeyPem)
    {
        privateKeyPem = null;
        if (currentUser == null)
        {
            return false;
        }

        if (!encryptionService.VerifyPassword(password, currentUser.PasswordHash))
        {
            return false;
        }

        try
        {
            privateKeyPem = encryptionService.UnprotectWithPassword(currentUser.EncryptedPrivateKeyPem, password);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SaveAccount(User user)
    {
        try
        {
            string filePath = GetAccountFilePath(user.UserId);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(user, options);

            // 保存前加密用户数据
            string encryptedJson = encryptionService.Encrypt(json);

            File.WriteAllText(filePath, encryptedJson);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"保存账户失败: {ex.Message}", ex);
        }
    }

    private void LoadAllAccounts()
    {
        try
        {
            if (!Directory.Exists(accountsDirectory))
                return;

            foreach (var filePath in Directory.GetFiles(accountsDirectory, "*.json"))
            {
                try
                {
                    string encryptedJson = File.ReadAllText(filePath);
                    string json = encryptionService.Decrypt(encryptedJson);

                    var user = JsonSerializer.Deserialize<User>(json);
                    if (user != null)
                    {
                        cachedUsers[user.UserId] = user;
                    }
                }
                catch
                {
                    // 跳过损坏的账户文件
                    Console.WriteLine($"警告: 无法加载账户文件 {filePath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载账户时出错: {ex.Message}");
        }
    }

    private string GetAccountFilePath(string userId)
    {
        return Path.Combine(accountsDirectory, $"{userId}.json");
    }

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
    /// 获取账户统计信息
    /// </summary>
    public string GetAccountStats()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 账户统计 ===");
        sb.AppendLine($"总账户数: {cachedUsers.Count}");
        sb.AppendLine($"活跃账户: {cachedUsers.Values.Count(u => u.Status == UserStatus.Active)}");
        sb.AppendLine($"当前登录: {(currentUser != null ? currentUser.Username : "无")}");
        return sb.ToString();
    }
}
