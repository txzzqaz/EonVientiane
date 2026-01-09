namespace EonVientiane;

/// <summary>
/// 游戏UI状态枚举
/// </summary>
public enum GameUIState
{
    Game,
    Login,
    UserProfile
}

/// <summary>
/// 右侧内容视图
/// </summary>
public enum ContentView
{
    None = 0,
    Button1 = 1,
    Button2 = 2,
    Button3 = 3,
    Button4 = 4,
    Button5 = 5,
    Settings = 6,
    Battle = 7
}

/// <summary>
/// 输入框枚举
/// </summary>
public enum InputField
{
    None,
    Username,
    Password,
    Email
}

/// <summary>
/// 联机大厅输入框枚举
/// </summary>
public enum LobbyInputField
{
    None,
    RoomName
}
