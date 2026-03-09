namespace EonVientiane.CLI;

using EonVientiane.Core.Models;
using EonVientiane.Core.Services;

/// <summary>
/// 游戏引擎 - 处理命令执行
/// </summary>
public class GameEngine
{
    private readonly AccountService accountService;
    private readonly IRemoteGameRuntime remoteRuntime;
    private readonly Func<string>? manualSyncAction;
    private bool isRunning;

    public GameEngine(AccountService accountService, IRemoteGameRuntime remoteRuntime, Func<string>? manualSyncAction = null)
    {
        this.accountService = accountService;
        this.remoteRuntime = remoteRuntime;
        this.manualSyncAction = manualSyncAction;
        this.isRunning = true;
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    public void ExecuteCommand(GameCommand command)
    {
        if (command == null)
            return;

        switch (command.Name)
        {
            case "logout":
                HandleLogout();
                break;

            case "account":
                HandleAccountInfo();
                break;

            case "users":
                HandleUsers(command.Args);
                break;

            case "changepwd":
                HandleChangePassword();
                break;

            case "sync":
                HandleManualSync();
                break;

            case "delaccount":
                HandleDeleteAccount();
                break;

            case "resetaccount":
                HandleResetAccount();
                break;

            case "help":
            case "?":
                HandleHelp();
                break;

            case "clear":
                Console.Clear();
                Console.WriteLine("游戏已加载...\n");
                break;

            case "exit":
            case "quit":
                HandleQuit();
                break;

            default:
                HandleRemoteCommand(command);
                break;
        }
    }

    // ========== 账户命令处理 ==========

    private void HandleLogout()
    {
        if (!accountService.IsLoggedIn())
        {
            Console.WriteLine("❌ 您还未登录");
            return;
        }

        var user = accountService.GetCurrentUser();
        accountService.Logout();
        Console.WriteLine($"✓ {user?.Username} 已登出");
    }

    private void HandleAccountInfo()
    {
        if (!accountService.IsLoggedIn())
        {
            Console.WriteLine("❌ 请先登录");
            return;
        }

        Console.WriteLine(accountService.GetAccountInfo());
    }

    private void HandleUsers(List<string> args)
    {
        if (args.Count > 0 && args[0] == "list")
        {
            Console.WriteLine(accountService.GetAccountStats());
            return;
        }

        var usernames = accountService.GetAllUsernames();
        if (usernames.Count == 0)
        {
            Console.WriteLine("还没有任何账户");
            return;
        }

        Console.WriteLine("=== 现有用户 ===");
        for (int i = 0; i < usernames.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {usernames[i]}");
        }
    }

    private void HandleHelp()
    {
        Console.WriteLine(CommandParser.GetHelpInfo());
        var remoteHelp = remoteRuntime.Execute("help");
        if (remoteHelp.Handled && !string.IsNullOrWhiteSpace(remoteHelp.Output))
        {
            Console.WriteLine(remoteHelp.Output);
        }
    }

    private void HandleManualSync()
    {
        if (manualSyncAction == null)
        {
            Console.WriteLine("❌ 当前运行时未配置手动同步");
            return;
        }

        Console.WriteLine(manualSyncAction());
    }

    private void HandleRemoteCommand(GameCommand command)
    {
        var raw = command.ToString().Trim();
        var result = remoteRuntime.Execute(raw);
        if (!result.Handled)
        {
            Console.WriteLine($"❌ 未知命令: '{command.Name}'. 输入 'help' 查看帮助.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            Console.WriteLine(result.Output);
        }

        if (result.ShouldExit)
        {
            isRunning = false;
        }
    }

    private void HandleChangePassword()
    {
        if (!accountService.IsLoggedIn())
        {
            Console.WriteLine("❌ 请先登录");
            return;
        }

        Console.Write("请输入旧密码: ");
        string oldPassword = ReadPasswordFromConsole();

        Console.Write("请输入新密码: ");
        string newPassword = ReadPasswordFromConsole();

        Console.Write("请确认新密码: ");
        string confirmPassword = ReadPasswordFromConsole();

        if (newPassword != confirmPassword)
        {
            Console.WriteLine("❌ 两次新密码输入不一致");
            return;
        }

        if (accountService.ChangePassword(oldPassword, newPassword))
        {
            Console.WriteLine("✓ 密码已更改");
        }
        else
        {
            Console.WriteLine("❌ 密码更改失败。请检查旧密码是否正确。");
        }
    }

    private void HandleQuit()
    {
        Console.WriteLine("感謝遊玩! 再見!");
        isRunning = false;
    }

    private void HandleDeleteAccount()
    {
        if (!accountService.IsLoggedIn())
        {
            Console.WriteLine("❌ 请先登录");
            return;
        }

        var user = accountService.GetCurrentUser();
        Console.Write($"请输入密码以确认删除账户 {user?.Username}: ");
        string password = ReadPasswordFromConsole();

        Console.Write("请输入 DELETE 确认删除: ");
        var confirmation = Console.ReadLine();

        if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal))
        {
            Console.WriteLine("已取消删除");
            return;
        }

        if (accountService.DeleteAccount(password))
        {
            Console.WriteLine("✓ 账户已删除（本地账户文件已移除）");
            isRunning = false;
        }
        else
        {
            Console.WriteLine("❌ 删除失败。请检查密码是否正确。");
        }
    }

    private void HandleResetAccount()
    {
        if (!accountService.IsLoggedIn())
        {
            Console.WriteLine("❌ 请先登录");
            return;
        }

        var user = accountService.GetCurrentUser();
        Console.Write($"请输入密码以确认重置账户 {user?.Username}: ");
        string password = ReadPasswordFromConsole();

        Console.Write("请输入 RESET 确认重置: ");
        var confirmation = Console.ReadLine();

        if (!string.Equals(confirmation, "RESET", StringComparison.Ordinal))
        {
            Console.WriteLine("已取消重置");
            return;
        }

        if (accountService.ResetCurrentAccount(password))
        {
            Console.WriteLine("✓ 账户已重置（等效于删除并重建同账号），请重新连接登录");
            isRunning = false;
        }
        else
        {
            Console.WriteLine("❌ 重置失败。请检查密码是否正确。\n❌ 若账户文件已被删除，请重新创建账户后再登录。");
        }
    }

    /// <summary>
    /// 检查游戏是否仍在运行
    /// </summary>
    public bool IsRunning => isRunning;

    /// <summary>
    /// 显示欢迎信息
    /// </summary>
    public void ShowWelcome()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════════╗
║          欢迎来到 Eon Vientiane 游戏                         ║
    ║               远程逻辑加载版本                              ║
╚════════════════════════════════════════════════════════════╝
        ");
        Console.ResetColor();
        Console.WriteLine($"玩家: {accountService.GetCurrentUser()?.Username}");
        Console.WriteLine($"已加载远程模块: {remoteRuntime.RuntimeId}@{remoteRuntime.RuntimeVersion}");
        Console.WriteLine("输入 'help' 查看所有可用命令\n");
    }

    /// <summary>
    /// 获取命令提示符
    /// </summary>
    public string GetPrompt()
    {
        var status = remoteRuntime.GetPrompt();
        var loginStatus = accountService.IsLoggedIn() ? accountService.GetCurrentUser()?.Username : "游客";
        return $"[{status}][{loginStatus}]> ";
    }

    private string ReadPasswordFromConsole()
    {
        var password = new System.Text.StringBuilder();
        ConsoleKey key;
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                }
            }
            else if (key == ConsoleKey.Enter)
            {
                break;
            }
            else
            {
                password.Append(keyInfo.KeyChar);
            }
        }
        while (key != ConsoleKey.Enter);

        Console.WriteLine();
        return password.ToString();
    }
}
