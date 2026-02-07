using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 登录管理器 - 支持本地离线登录和服务器认证
/// </summary>
public class LoginManager
{
    private Dictionary<string, UserProfile> _users;
    private UserProfile _currentUser;
    private LocalAccountManager _localAccountManager;
    private bool _isOfflineMode = false;
    
    public string Username { get; set; } = "";  // 登录时为钱包地址
    public string Password { get; set; } = "";  // 登录时为私钥/密钥
    public string Email { get; set; } = "";     // 已弃用
    public string WalletAddress { get; set; } = "";  // 注册时的钱包地址
    public string PrivateKey { get; set; } = "";      // 注册时的私钥
    
    public bool IsOfflineMode => _isOfflineMode;
    public LocalAccountManager LocalAccountManager => _localAccountManager;
    
    public LoginManager()
    {
        _users = new Dictionary<string, UserProfile>();
        _localAccountManager = new LocalAccountManager();
        // 客户端支持本地离线账户和服务器认证
    }
    
    /// <summary>
    /// 本地离线登录
    /// </summary>
    public (bool success, string message) LocalLogin(string username, string password)
    {
        var (success, account, message) = _localAccountManager.Login(username, password);
        
        if (success && account != null)
        {
            // 创建本地用户配置
            _currentUser = new UserProfile(
                account.Username,
                account.Email,
                account.CreatedDate,
                account.ProfileData.ContainsKey("level") ? account.ProfileData["level"] : "1"
            );
            _isOfflineMode = true;
            System.Diagnostics.Debug.WriteLine($"本地离线登录成功: {username}");
            return (true, "本地离线登录成功");
        }
        
        return (false, message);
    }
    
    /// <summary>
    /// 本地离线注册
    /// </summary>
    public (bool success, string message) LocalRegister(string username, string password, string email)
    {
        return _localAccountManager.CreateAccount(username, password, email);
    }
    
    /// <summary>
    /// 用户登录 - 仅用于本地输入验证，实际认证由服务器处理
    /// </summary>
    public bool Login(string username, string password)
    {
        // 仅进行基本的输入验证
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            System.Diagnostics.Debug.WriteLine("Login failed: username or password is empty");
            return false;
        }
        
        // 注意：实际的认证逻辑在服务器端，客户端仅做输入验证
        System.Diagnostics.Debug.WriteLine($"Login validation passed for user {username}, awaiting server response");
        return true;
    }
    
    /// <summary>
    /// 用户注册 - 仅用于本地输入验证，实际注册由服务器处理
    /// </summary>
    public bool Register(string username, string password, string email)
    {
        // 仅进行基本的输入验证
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
        {
            System.Diagnostics.Debug.WriteLine("Register failed: one or more fields are empty");
            return false;
        }
        
        // 注意：实际的注册逻辑在服务器端，客户端仅做输入验证
        System.Diagnostics.Debug.WriteLine($"Register validation passed for user {username}, awaiting server response");
        return true;
    }
    
    /// <summary>
    /// 设置当前登录用户（由服务器认证成功后调用）
    /// </summary>
    public void SetCurrentUser(UserProfile user)
    {
        _currentUser = user;
        _isOfflineMode = false;
        System.Diagnostics.Debug.WriteLine($"User {user.Username} logged in successfully!");
    }
    
    /// <summary>
    /// 用户注销
    /// </summary>
    public void Logout()
    {
        if (_currentUser != null)
        {
            System.Diagnostics.Debug.WriteLine($"User {_currentUser.Username} logged out!");
            _currentUser = null;
        }
        _isOfflineMode = false;
        ClearInput();
    }
    
    /// <summary>
    /// 获取当前登录的用户
    /// </summary>
    public UserProfile GetCurrentUser()
    {
        return _currentUser;
    }
    
    /// <summary>
    /// 清空输入
    /// </summary>
    public void ClearInput()
    {
        Username = "";
        Password = "";
        Email = "";
    }
    
    /// <summary>
    /// 验证用户是否已登录
    /// </summary>
    public bool IsLoggedIn()
    {
        return _currentUser != null;
    }
}

