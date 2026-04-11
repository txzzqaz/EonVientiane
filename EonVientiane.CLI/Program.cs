using EonVientiane.CLI;
using EonVientiane.Core.Models;
using EonVientiane.Core.Services;
using System.Reflection;

// 创建账户服务
var accountService = new AccountService();
var encryptionService = new EncryptionService();
var logicPackageService = new LogicPackageService(encryptionService);
AutoLogicPackageRuntime? autoLogicRuntime = null;

// 显示欢迎信息
Console.Clear();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"
╔════════════════════════════════════════════════════════════╗
║          欢迎来到 Eon Vientiane 游戏                         ║
║                  基础CLI版本                               ║
╚════════════════════════════════════════════════════════════╝
        ");
Console.ResetColor();
Console.WriteLine("请先登录或创建账户以继续\n");

// 账户登录循环
bool isAuthenticated = false;
string authenticatedPassword = string.Empty;
while (!isAuthenticated)
{
    Console.WriteLine("1. 登录");
    Console.WriteLine("2. 创建账户");
    Console.WriteLine("3. 退出");
    Console.Write("\n请选择 (1-3): ");

    string? choice = Console.ReadLine();

    if (choice == "1")
    {
        Console.Write("用户名: ");
        string? username = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(username))
        {
            Console.Write("密码: ");
            var password = new System.Text.StringBuilder();
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                        password.Length--;
                }
                else if (key != ConsoleKey.Enter)
                {
                    password.Append(keyInfo.KeyChar);
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();

            if (accountService.Login(username, password.ToString()))
            {
                Console.WriteLine($"✓ 欢迎 {username}!\n");
                authenticatedPassword = password.ToString();
                isAuthenticated = true;
            }
            else
            {
                Console.WriteLine("❌ 用户名或密码错误\n");
            }
        }
    }
    else if (choice == "2")
    {
        Console.Write("新用户名: ");
        string? username = Console.ReadLine();
        Console.Write("邮箱: ");
        string? email = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(email))
        {
            Console.Write("密码: ");
            var password = new System.Text.StringBuilder();
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                        password.Length--;
                }
                else if (key != ConsoleKey.Enter)
                {
                    password.Append(keyInfo.KeyChar);
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();

            Console.Write("确认密码: ");
            var confirmPassword = new System.Text.StringBuilder();
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace)
                {
                    if (confirmPassword.Length > 0)
                        confirmPassword.Length--;
                }
                else if (key != ConsoleKey.Enter)
                {
                    confirmPassword.Append(keyInfo.KeyChar);
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();

            if (password.ToString() != confirmPassword.ToString())
            {
                Console.WriteLine("❌ 密码不一致\n");
            }
            else if (accountService.CreateAccount(username, password.ToString(), email))
            {
                Console.WriteLine($"✓ 账户创建成功! 请登录\n");
            }
            else
            {
                Console.WriteLine("❌ 创建账户失败。用户名可能已存在或邮箱格式错误\n");
            }
        }
    }
    else if (choice == "3")
    {
        Console.WriteLine("再见!");
        return;
    }
    else
    {
        Console.WriteLine("❌ 无效选择\n");
    }
}

var currentUser = accountService.GetCurrentUser()!;
if (!accountService.TryGetCurrentUserPrivateKeyPem(authenticatedPassword, out var currentUserPrivateKeyPem) ||
    string.IsNullOrWhiteSpace(currentUserPrivateKeyPem))
{
    Console.WriteLine("❌ 无法解锁当前用户私钥，程序将退出。");
    return;
}

var currentUserPublicKeyPem = accountService.GetCurrentUserPublicKeyPem();
if (string.IsNullOrWhiteSpace(currentUserPublicKeyPem))
{
    Console.WriteLine("❌ 当前用户公钥不存在，程序将退出。");
    return;
}

var appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EonVientiane");
var serverPublicKeyFilePath = Path.Combine(appDataRoot, "Keys", "server_public.pem");
var userPackageDirectory = Path.Combine(appDataRoot, "LogicPackages", currentUser.UserId);
Directory.CreateDirectory(Path.GetDirectoryName(serverPublicKeyFilePath)!);
Directory.CreateDirectory(userPackageDirectory);

var serverBaseUrl = Environment.GetEnvironmentVariable("EV_SERVER_URL") ?? "http://127.0.0.1:5000";
var moduleSyncService = new ModuleSyncService(serverBaseUrl);
var syncedFromServer = false;

try
{
    var loginAchievementResult = await moduleSyncService.VerifyAchievementAsync(
        currentUser.UserId,
        currentUserPublicKeyPem,
        trigger: "login.first",
        userPackageDirectory);

    if (loginAchievementResult.DownloadedCount > 0)
    {
        Console.WriteLine($"✓ 登录成就验证完成，新增 {loginAchievementResult.DownloadedCount} 个逻辑包");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ 登录成就验证请求失败: {ex.Message}");
}

try
{
    var syncResult = await moduleSyncService.ManualSyncAsync(
        currentUser.UserId,
        currentUserPublicKeyPem,
        serverPublicKeyFilePath,
        userPackageDirectory);

    syncedFromServer = true;
    Console.WriteLine($"✓ 已从服务端同步 {syncResult.DownloadedCount} 个逻辑包");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ 无法从服务端同步逻辑模块: {ex.Message}");
    Console.WriteLine("⚠ 将尝试加载本地永久存储的逻辑包...");
}

if (!syncedFromServer)
{
    if (!File.Exists(serverPublicKeyFilePath))
    {
        Console.WriteLine("❌ 离线启动失败：缺少 server_public.pem，无法校验本地逻辑包签名。请至少联网成功启动一次。");
        return;
    }

    var localPackageCount = Directory.GetFiles(userPackageDirectory, "*.json", SearchOption.TopDirectoryOnly).Length;
    if (localPackageCount == 0)
    {
        Console.WriteLine("❌ 离线启动失败：本地不存在任何逻辑包。请先联网同步后再离线运行。");
        return;
    }
}

autoLogicRuntime = new AutoLogicPackageRuntime(
    logicPackageService,
    currentUser,
    currentUserPrivateKeyPem,
    serverPublicKeyFilePath,
    userPackageDirectory);
autoLogicRuntime.Start();

IRemoteGameRuntime? remoteRuntime = null;
var playerAssembly = logicPackageService.GetLoadedAssembly("module.player.core");
if (playerAssembly != null)
{
    remoteRuntime = CreateRemoteRuntime(playerAssembly);
}

if (remoteRuntime == null)
{
    Console.WriteLine("❌ 未加载到远程 player 模块，程序将退出。");
    autoLogicRuntime.Dispose();
    return;
}

Environment.SetEnvironmentVariable("EV_USER_ID", currentUser.UserId);
Environment.SetEnvironmentVariable("EV_USER_PUBLIC_KEY_PEM_BASE64", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(currentUserPublicKeyPem)));
Environment.SetEnvironmentVariable("EV_USER_PACKAGE_DIR", userPackageDirectory);

remoteRuntime.Initialize(currentUser.Username);

// 创建游戏引擎
var gameEngine = new GameEngine(
    accountService,
    remoteRuntime,
    manualSyncAction: () =>
    {
        try
        {
            var result = moduleSyncService.ManualSyncAsync(
                    currentUser.UserId,
                    currentUserPublicKeyPem,
                    serverPublicKeyFilePath,
                    userPackageDirectory)
                .GetAwaiter()
                .GetResult();

            autoLogicRuntime?.ReloadExistingNow();

            if (result.DownloadedCount == 0)
            {
                return "✓ 同步完成：所有模块均为最新";
            }

            var moduleList = string.Join(", ", result.SyncedModuleIds.Distinct(StringComparer.Ordinal));
            return $"✓ 同步完成：已更新 {result.DownloadedCount} 个逻辑包，模块: {moduleList}";
        }
        catch (Exception ex)
        {
            return $"❌ 同步失败: {ex.Message}";
        }
    });

// 显示游戏欢迎信息
gameEngine.ShowWelcome();

// 主游戏循环
while (gameEngine.IsRunning)
{
    try
    {
        Console.Write(gameEngine.GetPrompt());
        var input = Console.ReadLine() ?? "";

        var command = CommandParser.ParseCommand(input);
        if (command != null)
        {
            gameEngine.ExecuteCommand(command);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 错误: {ex.Message}");
    }

    if (gameEngine.IsRunning)
    {
        Console.WriteLine();
    }
}

autoLogicRuntime.Dispose();

static IRemoteGameRuntime? CreateRemoteRuntime(Assembly assembly)
{
    var runtimeType = assembly
        .GetTypes()
        .FirstOrDefault(t =>
            typeof(IRemoteGameRuntime).IsAssignableFrom(t) &&
            !t.IsAbstract &&
            t.GetConstructor(Type.EmptyTypes) != null);

    if (runtimeType == null)
    {
        return null;
    }

    return Activator.CreateInstance(runtimeType) as IRemoteGameRuntime;
}

