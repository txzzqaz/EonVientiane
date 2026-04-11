namespace EonVientiane.CLI;

/// <summary>
/// 游戏命令
/// </summary>
public class GameCommand
{
    public string Name { get; set; }
    public List<string> Args { get; set; }

    public GameCommand(string name, List<string> args = null!)
    {
        Name = name.ToLower();
        Args = args ?? new List<string>();
    }

    public override string ToString()
    {
        return $"{Name} {string.Join(" ", Args)}";
    }
}

/// <summary>
/// 命令解析器
/// </summary>
public class CommandParser
{
    /// <summary>
    /// 解析用户输入的命令
    /// </summary>
    public static GameCommand? ParseCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var parts = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        var commandName = parts[0];
        var args = parts.Skip(1).ToList();

        return new GameCommand(commandName, args);
    }

    /// <summary>
    /// 获取帮助信息
    /// </summary>
    public static string GetHelpInfo()
    {
        return @"
=== 游戏命令帮助 ===

账户命令:
  logout                    - 登出账户
  account                   - 查看账户信息
  users                     - 查看所有用户列表
  changepwd                 - 更改密码
  delaccount                - 删除当前账户（删除本地账户文件）
  resetaccount              - 重置当前账户（删除并重建同用户名/密码/邮箱）后退出
  users list                - 查看账户统计

游戏命令:
  其余命令由远程加密模块提供（如 loadlevel / inv / status 等）
    sync                 - 手动从服务器同步各模块更新

  help                 - 显示此帮助信息
  clear                - 清屏
  exit / quit          - 退出游戏
";
    }
}
