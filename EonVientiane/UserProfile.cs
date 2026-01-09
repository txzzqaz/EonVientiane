using System;

namespace EonVientiane;

/// <summary>
/// 用户配置文件
/// </summary>
public class UserProfile
{
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string UserLevel { get; set; }
    
    public UserProfile(string username, string email, DateTime registrationDate, string userLevel)
    {
        Username = username;
        Email = email;
        RegistrationDate = registrationDate;
        UserLevel = userLevel;
    }
}
