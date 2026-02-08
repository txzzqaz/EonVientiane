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
    
    public string WalletAddress { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    
    public bool IsOfflineMode => _isOfflineMode;
    public LocalAccountManager LocalAccountManager => _localAccountManager;
    
    public LoginManager()
    {
        _users = new Dictionary<string, UserProfile>();
        _localAccountManager = new LocalAccountManager();
        // 客户端支持本地离线账户和服务器认证
    }
    
    /// <summary>
    /// 本地离线登录 - 使用钱包地址和私钥
    /// </summary>
    public (bool success, string message) LocalLogin(string walletAddress, string privateKey)
    {
        var (success, account, message) = _localAccountManager.Login(walletAddress, privateKey);
        
        if (success && account != null)
        {
            // 创建本地用户配置
            _currentUser = new UserProfile(
                account.Username,
                walletAddress,
                account.CreatedDate,
                account.ProfileData.ContainsKey("level") ? account.ProfileData["level"] : "1"
            );
            _isOfflineMode = true;
            System.Diagnostics.Debug.WriteLine($"本地离线登录成功: {walletAddress}");
            return (true, "本地离线登录成功");
        }
        
        return (false, message);
    }
    
    /// <summary>
    /// 本地离线注册 - 基于钱包地址和私钥
    /// </summary>
    public (bool success, string message) LocalRegister(string walletAddress, string privateKey)
    {
        return _localAccountManager.CreateAccount(walletAddress, privateKey, walletAddress);
    }
    
    /// <summary>
    /// 用户登录 - 仅用于本地输入验证，实际认证由服务器处理
    /// </summary>
    public bool Login(string walletAddress, string privateKey)
    {
        // 仅进行基本的输入验证
        if (string.IsNullOrEmpty(walletAddress) || string.IsNullOrEmpty(privateKey))
        {
            System.Diagnostics.Debug.WriteLine("Login failed: wallet address or private key is empty");
            return false;
        }
        
        // 注意：实际的认证逻辑在服务器端，客户端仅做输入验证
        System.Diagnostics.Debug.WriteLine($"Login validation passed for wallet {walletAddress}, awaiting server response");
        return true;
    }
    
    /// <summary>
    /// 用户注册 - 仅用于本地输入验证，实际注册由服务器处理
    /// </summary>
    public bool Register(string walletAddress, string privateKey)
    {
        // 仅进行基本的输入验证
        if (string.IsNullOrEmpty(walletAddress) || string.IsNullOrEmpty(privateKey))
        {
            System.Diagnostics.Debug.WriteLine("Register failed: wallet address or private key is empty");
            return false;
        }
        
        // 注意：实际的注册逻辑在服务器端，客户端仅做输入验证
        System.Diagnostics.Debug.WriteLine($"Register validation passed for wallet {walletAddress}, awaiting server response");
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
        WalletAddress = "";
        PrivateKey = "";
    }
    
    /// <summary>
    /// 验证用户是否已登录
    /// </summary>
    public bool IsLoggedIn()
    {
        return _currentUser != null;
    }
}

