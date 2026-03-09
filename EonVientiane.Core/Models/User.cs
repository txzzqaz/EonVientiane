namespace EonVientiane.Core.Models;

/// <summary>
/// 用户账户代表
/// </summary>
public class User
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
    public string PublicKeyPem { get; set; }
    public string EncryptedPrivateKeyPem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public UserStatus Status { get; set; }
    public Dictionary<string, string> EncryptedData { get; set; }

    public User(
        string userId,
        string username,
        string email,
        string passwordHash,
        string publicKeyPem,
        string encryptedPrivateKeyPem)
    {
        UserId = userId;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        PublicKeyPem = publicKeyPem;
        EncryptedPrivateKeyPem = encryptedPrivateKeyPem;
        CreatedAt = DateTime.Now;
        LastLoginAt = DateTime.Now;
        Status = UserStatus.Active;
        EncryptedData = new Dictionary<string, string>();
    }

    public override string ToString()
    {
        return $"User: {Username} ({UserId}) - {Status} - Created: {CreatedAt:yyyy-MM-dd}";
    }

    public string GetUserInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== 账户信息 ===");
        sb.AppendLine($"用户ID: {UserId}");
        sb.AppendLine($"用户名: {Username}");
        sb.AppendLine($"邮箱: {Email}");
        sb.AppendLine($"状态: {GetStatusName()}");
        sb.AppendLine($"创建时间: {CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"最后登录: {LastLoginAt:yyyy-MM-dd HH:mm:ss}");
        return sb.ToString();
    }

    private string GetStatusName()
    {
        return Status switch
        {
            UserStatus.Active => "活跃",
            UserStatus.Inactive => "不活跃",
            UserStatus.Suspended => "被冻结",
            UserStatus.Deleted => "已删除",
            _ => "未知"
        };
    }
}

/// <summary>
/// 用户状态枚举
/// </summary>
public enum UserStatus
{
    Active,
    Inactive,
    Suspended,
    Deleted
}
