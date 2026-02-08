using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EonVientianeServer;

/// <summary>
/// 用户管理器 - 处理服务端用户认证逻辑
/// </summary>
public class UserManager
{
    private class UserAccount
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
    }
    
    private static readonly HashSet<string> TestUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "qaz1",
        "qaz2"
    };

    private readonly Dictionary<string, UserAccount> _users = new();
    private readonly Dictionary<string, string> _tokens = new(); // token -> userId
    private readonly object _lock = new();
    private readonly string _dataDir;
    private readonly string _usersFile;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    
    public UserManager()
    {
        _dataDir = Path.Combine("data", "users");
        _usersFile = Path.Combine(_dataDir, "users.json");

        Directory.CreateDirectory(_dataDir);
        LoadUsers();
        EnsureDefaultUsers();
    }
    
    /// <summary>
    /// 初始化测试用户
    /// </summary>
    private void EnsureDefaultUsers()
    {
        lock (_lock)
        {
            CreateUserIfMissing("admin", "admin", "admin@example.com");
            CreateUserIfMissing("user", "user", "user@example.com");
            CreateUserIfMissing("test", "test", "test@example.com");

            CreateUserIfMissing("qaz1", "qaz1", "qaz1@example.com");
            CreateUserIfMissing("qaz2", "qaz2", "qaz2@example.com");

            SaveUsers();
        }
    }

    public bool IsTestAccount(string userId)
    {
        var username = GetUsername(userId);
        return username != null && TestUsernames.Contains(username);
    }
    
    /// <summary>
    /// 用户登录
    /// </summary>
    public (bool success, string? userId, string? token, string? error) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, null, null, "用户名或密码不能为空");
        }
        
        lock (_lock)
        {
            if (!_users.TryGetValue(username, out var user))
            {
                return (false, null, null, "用户名或密码错误");
            }
            
            // 验证密码
            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return (false, null, null, "用户名或密码错误");
            }
            
            // 生成token
            var token = GenerateToken(user.UserId);
            _tokens[token] = user.UserId;
            
            Console.WriteLine($"[Server] User '{username}' logged in successfully");
            return (true, user.UserId, token, null);
        }
    }
    
    /// <summary>
    /// 用户注册
    /// </summary>
    public (bool success, string? userId, string? error) Register(string username, string password, string email)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
        {
            return (false, null, "用户名、密码和邮箱都不能为空");
        }
        
        if (username.Length < 3 || username.Length > 20)
        {
            return (false, null, "用户名长度必须在3-20个字符之间");
        }
        
        if (password.Length < 6)
        {
            return (false, null, "密码长度至少6个字符");
        }
        
        lock (_lock)
        {
            if (_users.ContainsKey(username))
            {
                return (false, null, "用户名已存在");
            }
            
            var user = CreateUserInternal(username, password, email, persist: true);
            
            Console.WriteLine($"[Server] User '{username}' registered successfully");
            return (true, user.UserId, null);
        }
    }
    
    /// <summary>
    /// 验证token
    /// </summary>
    public (bool valid, string? userId) ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, null);
        }
        
        lock (_lock)
        {
            if (_tokens.TryGetValue(token, out var userId))
            {
                return (true, userId);
            }
        }
        
        return (false, null);
    }
    
    /// <summary>
    /// 获取用户名
    /// </summary>
    public string? GetUsername(string userId)
    {
        lock (_lock)
        {
            foreach (var user in _users.Values)
            {
                if (user.UserId == userId)
                {
                    return user.Username;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// 登出用户
    /// </summary>
    public void Logout(string token)
    {
        lock (_lock)
        {
            _tokens.Remove(token);
        }
    }
    
    /// <summary>
    /// 内部创建用户
    /// </summary>
    private UserAccount CreateUserInternal(string username, string password, string email, bool persist = false)
    {
        var (hash, salt) = HashPassword(password);
        var user = new UserAccount
        {
            UserId = Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedTime = DateTime.UtcNow
        };
        
        _users[username] = user;
        if (persist)
        {
            SaveUsers();
        }
        return user;
    }

    private void CreateUserIfMissing(string username, string password, string email)
    {
        if (_users.ContainsKey(username))
        {
            return;
        }

        CreateUserInternal(username, password, email, persist: false);
    }

    private void LoadUsers()
    {
        lock (_lock)
        {
            if (!File.Exists(_usersFile))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(_usersFile);
                var users = JsonSerializer.Deserialize<List<UserAccount>>(json, _jsonOptions) ?? new List<UserAccount>();
                _users.Clear();

                foreach (var user in users)
                {
                    if (!string.IsNullOrWhiteSpace(user.Username))
                    {
                        _users[user.Username] = user;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserManager] Failed to load users: {ex.Message}");
            }
        }
    }

    private void SaveUsers()
    {
        lock (_lock)
        {
            try
            {
                var users = _users.Values.ToList();
                var json = JsonSerializer.Serialize(users, _jsonOptions);
                File.WriteAllText(_usersFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserManager] Failed to save users: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 密码哈希
    /// </summary>
    private (string hash, string salt) HashPassword(string password)
    {
        var salt = Guid.NewGuid().ToString();
        var input = password + salt;
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        return (hash, salt);
    }
    
    /// <summary>
    /// 验证密码
    /// </summary>
    private bool VerifyPassword(string password, string hash, string salt)
    {
        var input = password + salt;
        var computedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        return computedHash == hash;
    }
    
    /// <summary>
    /// 生成token
    /// </summary>
    private string GenerateToken(string userId)
    {
        var token = Guid.NewGuid().ToString() + "_" + userId;
        return token;
    }
}
