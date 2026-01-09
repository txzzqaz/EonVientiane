using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 登录管理器 - 处理用户认证逻辑
/// </summary>
public class LoginManager
{
    private Dictionary<string, UserProfile> _users;
    private UserProfile _currentUser;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Email { get; set; } = "";
    
    public LoginManager()
    {
        _users = new Dictionary<string, UserProfile>();
        // 客户端不再存储内置用户，所有认证由服务器处理
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
