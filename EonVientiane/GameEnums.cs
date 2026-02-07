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
    Button1 = 1,      // 联机大厅
    Button2 = 2,      // 背包
    Button3 = 3,      // 对战历史
    Button4 = 4,      // 成就
    Button5 = 5,      // 图鉴
    Battle = 6,       // 战斗
    Settings = 7
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
