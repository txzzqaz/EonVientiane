using EonVientiane.CLI;
using EonVientiane.Core.Models;
using EonVientiane.Core.Services;
using EonVientiane.GUI.GuiMenus;
using System.Reflection;

namespace EonVientiane.GUI.Services;

public sealed class CliProcessBridge : IAsyncDisposable
{
    private readonly string workspaceRoot;
    private readonly AccountService accountService;
    private readonly EncryptionService encryptionService;
    private readonly LogicPackageService logicPackageService;

    private ModuleSyncService? moduleSyncService;
    private AutoLogicPackageRuntime? autoLogicRuntime;
    private IRemoteGameRuntime? remoteRuntime;
    private User? currentUser;
    private string? currentUserPublicKeyPem;
    private string? serverPublicKeyFilePath;
    private string? userPackageDirectory;

    public event Action<string>? OutputReceived;
    public bool IsLoggedIn => accountService.IsLoggedIn() && remoteRuntime != null;
    public string? CurrentUsername => accountService.GetCurrentUser()?.Username;

    public CliProcessBridge(string workspaceRoot)
    {
        this.workspaceRoot = workspaceRoot;
        accountService = new AccountService();
        encryptionService = new EncryptionService();
        logicPackageService = new LogicPackageService(encryptionService);
    }

    public string CreateAccount(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return "❌ 创建账户失败：用户名、邮箱、密码不能为空";
        }

        var created = accountService.CreateAccount(username.Trim(), password, email.Trim());
        return created
            ? "✓ 账户创建成功，请使用上方登录"
            : "❌ 创建账户失败。用户名可能已存在或邮箱格式错误";
    }

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        if (!accountService.Login(username, password))
        {
            return (false, "❌ 用户名或密码错误");
        }

        currentUser = accountService.GetCurrentUser();
        if (currentUser == null)
        {
            return (false, "❌ 登录失败：无法读取当前用户");
        }

        if (!accountService.TryGetCurrentUserPrivateKeyPem(password, out var currentUserPrivateKeyPem) ||
            string.IsNullOrWhiteSpace(currentUserPrivateKeyPem))
        {
            await LogoutAsync();
            return (false, "❌ 无法解锁用户私钥");
        }

        currentUserPublicKeyPem = accountService.GetCurrentUserPublicKeyPem();
        if (string.IsNullOrWhiteSpace(currentUserPublicKeyPem))
        {
            await LogoutAsync();
            return (false, "❌ 当前用户公钥不存在");
        }

        var appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EonVientiane");
        serverPublicKeyFilePath = Path.Combine(appDataRoot, "Keys", "server_public.pem");
        userPackageDirectory = Path.Combine(appDataRoot, "LogicPackages", currentUser.UserId);
        Directory.CreateDirectory(Path.GetDirectoryName(serverPublicKeyFilePath)!);
        Directory.CreateDirectory(userPackageDirectory);

        var serverBaseUrl = Environment.GetEnvironmentVariable("EV_SERVER_URL") ?? "http://127.0.0.1:5000";
        moduleSyncService = new ModuleSyncService(serverBaseUrl);

        try
        {
            var loginAchievementResult = await moduleSyncService.VerifyAchievementAsync(
                currentUser.UserId,
                currentUserPublicKeyPem,
                trigger: "login.first",
                userPackageDirectory);

            if (loginAchievementResult.DownloadedCount > 0)
            {
                OutputReceived?.Invoke($"✓ 登录成就验证完成，新增 {loginAchievementResult.DownloadedCount} 个逻辑包");
            }
        }
        catch (Exception ex)
        {
            OutputReceived?.Invoke($"⚠ 登录成就验证请求失败: {ex.Message}");
        }

        var syncedFromServer = false;
        try
        {
            var syncResult = await moduleSyncService.ManualSyncAsync(
                currentUser.UserId,
                currentUserPublicKeyPem,
                serverPublicKeyFilePath,
                userPackageDirectory);

            syncedFromServer = true;
            OutputReceived?.Invoke($"✓ 已从服务端同步 {syncResult.DownloadedCount} 个逻辑包");
        }
        catch (Exception ex)
        {
            OutputReceived?.Invoke($"⚠ 无法从服务端同步逻辑模块: {ex.Message}");
            OutputReceived?.Invoke("⚠ 将尝试加载本地永久存储的逻辑包...");
        }

        if (!syncedFromServer)
        {
            if (!File.Exists(serverPublicKeyFilePath))
            {
                await LogoutAsync();
                return (false, "❌ 离线启动失败：缺少 server_public.pem，请至少联网成功启动一次");
            }

            var localPackageCount = Directory.GetFiles(userPackageDirectory, "*.json", SearchOption.TopDirectoryOnly).Length;
            if (localPackageCount == 0)
            {
                await LogoutAsync();
                return (false, "❌ 离线启动失败：本地不存在任何逻辑包，请先联网同步");
            }
        }

        autoLogicRuntime?.Dispose();
        autoLogicRuntime = new AutoLogicPackageRuntime(
            logicPackageService,
            currentUser,
            currentUserPrivateKeyPem,
            serverPublicKeyFilePath,
            userPackageDirectory);
        autoLogicRuntime.Start();

        var playerAssembly = logicPackageService.GetLoadedAssembly("module.player.core");
        if (playerAssembly == null)
        {
            await LogoutAsync();
            return (false, "❌ 未加载到远程 player 模块");
        }

        remoteRuntime = CreateRemoteRuntime(playerAssembly);
        if (remoteRuntime == null)
        {
            await LogoutAsync();
            return (false, "❌ 远程运行时实例化失败");
        }

        Environment.SetEnvironmentVariable("EV_USER_ID", currentUser.UserId);
        Environment.SetEnvironmentVariable("EV_USER_PUBLIC_KEY_PEM_BASE64", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(currentUserPublicKeyPem)));
        Environment.SetEnvironmentVariable("EV_USER_PACKAGE_DIR", userPackageDirectory);

        remoteRuntime.Initialize(currentUser.Username);
        return (true, $"✓ 欢迎 {currentUser.Username}，已进入游戏");
    }

    public async Task LogoutAsync()
    {
        remoteRuntime = null;
        moduleSyncService = null;

        if (autoLogicRuntime != null)
        {
            autoLogicRuntime.Dispose();
            autoLogicRuntime = null;
        }

        accountService.Logout();
        currentUser = null;
        currentUserPublicKeyPem = null;
        serverPublicKeyFilePath = null;
        userPackageDirectory = null;
        await Task.CompletedTask;
    }

    public async Task SendInputAsync(string input)
    {
        var command = CommandParser.ParseCommand(input);
        if (command == null)
        {
            return;
        }

        switch (command.Name)
        {
            case "help":
            case "?":
                OutputReceived?.Invoke(CommandParser.GetHelpInfo());
                if (remoteRuntime != null)
                {
                    var remoteHelp = remoteRuntime.Execute("help");
                    if (remoteHelp.Handled && !string.IsNullOrWhiteSpace(remoteHelp.Output))
                    {
                        OutputReceived?.Invoke(remoteHelp.Output);
                    }
                }
                return;

            case "account":
                OutputReceived?.Invoke(accountService.GetAccountInfo());
                return;

            case "users":
                if (command.Args.Count > 0 && command.Args[0] == "list")
                {
                    OutputReceived?.Invoke(accountService.GetAccountStats());
                    return;
                }

                var usernames = accountService.GetAllUsernames();
                if (usernames.Count == 0)
                {
                    OutputReceived?.Invoke("还没有任何账户");
                }
                else
                {
                    OutputReceived?.Invoke("=== 现有用户 ===");
                    for (var i = 0; i < usernames.Count; i++)
                    {
                        OutputReceived?.Invoke($"  {i + 1}. {usernames[i]}");
                    }
                }
                return;

            case "sync":
                await HandleSyncAsync();
                return;

            case "logout":
            case "quit":
            case "exit":
                await LogoutAsync();
                OutputReceived?.Invoke("✓ 已退出当前账号");
                return;

            case "clear":
                return;
        }

        if (!IsLoggedIn || remoteRuntime == null)
        {
            OutputReceived?.Invoke("❌ 请先登录");
            return;
        }

        var result = remoteRuntime.Execute(input.Trim());
        if (!result.Handled)
        {
            OutputReceived?.Invoke($"❌ 未知命令: '{command.Name}'. 输入 'help' 查看帮助.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            OutputReceived?.Invoke(result.Output);
        }
    }

    public bool TryGetStructuredContent(
        GuiContentProviderDefinition provider,
        out GuiStructuredContentDefinition content,
        out string errorMessage)
    {
        content = default!;
        errorMessage = string.Empty;

        if (!IsLoggedIn || remoteRuntime == null)
        {
            errorMessage = "❌ 请先登录";
            return false;
        }

        if (TryGetSharedState(remoteRuntime) is not IDictionary<string, object> sharedState)
        {
            errorMessage = "❌ 无法读取当前运行时共享状态";
            return false;
        }

        try
        {
            var raw = InvokeGuiContentMethod(provider.ProviderType, sharedState);
            if (!TryConvertStructuredContent(raw, out content))
            {
                errorMessage = "❌ 模块未返回有效的 GUI 列表内容";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"❌ 读取模块 GUI 内容失败: {ex.Message}";
            return false;
        }
    }

    private async Task HandleSyncAsync()
    {
        if (moduleSyncService == null || currentUser == null || string.IsNullOrWhiteSpace(currentUserPublicKeyPem) ||
            string.IsNullOrWhiteSpace(serverPublicKeyFilePath) || string.IsNullOrWhiteSpace(userPackageDirectory))
        {
            OutputReceived?.Invoke("❌ 当前会话未配置手动同步");
            return;
        }

        try
        {
            var result = await moduleSyncService.ManualSyncAsync(
                currentUser.UserId,
                currentUserPublicKeyPem,
                serverPublicKeyFilePath,
                userPackageDirectory);

            autoLogicRuntime?.ReloadExistingNow();

            if (result.DownloadedCount == 0)
            {
                OutputReceived?.Invoke("✓ 同步完成：所有模块均为最新");
                return;
            }

            var moduleList = string.Join(", ", result.SyncedModuleIds.Distinct(StringComparer.Ordinal));
            OutputReceived?.Invoke($"✓ 同步完成：已更新 {result.DownloadedCount} 个逻辑包，模块: {moduleList}");
        }
        catch (Exception ex)
        {
            OutputReceived?.Invoke($"❌ 同步失败: {ex.Message}");
        }
    }

    private static IRemoteGameRuntime? CreateRemoteRuntime(Assembly assembly)
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

    private static IDictionary<string, object>? TryGetSharedState(IRemoteGameRuntime runtime)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        var type = runtime.GetType();
        while (type != null)
        {
            var field = type.GetField("sharedState", flags);
            if (field?.GetValue(runtime) is IDictionary<string, object> typed)
            {
                return typed;
            }

            type = type.BaseType;
        }

        return null;
    }

    private static object? InvokeGuiContentMethod(Type providerType, IDictionary<string, object> sharedState)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        var methods = providerType
            .GetMethods(flags)
            .Where(x => x.Name == "GetGuiContentDefinition")
            .ToList();

        var method = methods.FirstOrDefault(x =>
                         x.GetParameters().Length == 1 &&
                         typeof(IDictionary<string, object>).IsAssignableFrom(x.GetParameters()[0].ParameterType))
                     ?? methods.FirstOrDefault(x =>
                         x.GetParameters().Length == 1 &&
                         typeof(System.Collections.IDictionary).IsAssignableFrom(x.GetParameters()[0].ParameterType))
                     ?? methods.FirstOrDefault(x => x.GetParameters().Length == 0);

        if (method == null)
        {
            throw new InvalidOperationException("未找到 GetGuiContentDefinition 方法");
        }

        object? instance = null;
        if (!method.IsStatic)
        {
            var ctor = providerType.GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException("GUI 内容提供者缺少无参构造函数");
            instance = ctor.Invoke(null);
        }

        var parameters = method.GetParameters().Length switch
        {
            0 => null,
            1 => new object[] { sharedState },
            _ => throw new InvalidOperationException("GetGuiContentDefinition 参数数量不受支持")
        };

        return method.Invoke(instance, parameters);
    }

    private static bool TryConvertStructuredContent(object? raw, out GuiStructuredContentDefinition content)
    {
        content = default!;

        if (raw is GuiStructuredContentDefinition direct)
        {
            content = direct;
            return true;
        }

        if (raw is not IDictionary<string, object> map)
        {
            return false;
        }

        var moduleId = map.TryGetValue("ModuleId", out var moduleIdObj) ? moduleIdObj?.ToString() : null;
        var title = map.TryGetValue("Title", out var titleObj) ? titleObj?.ToString() : null;
        if (string.IsNullOrWhiteSpace(moduleId) || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var sections = new List<GuiStructuredContentSection>();
        if (map.TryGetValue("Sections", out var sectionsObj) && sectionsObj is IEnumerable<object> rawSections)
        {
            foreach (var sectionEntry in rawSections)
            {
                if (sectionEntry is not IDictionary<string, object> sectionMap ||
                    !sectionMap.TryGetValue("Title", out var sectionTitleObj))
                {
                    continue;
                }

                var sectionTitle = sectionTitleObj?.ToString();
                if (string.IsNullOrWhiteSpace(sectionTitle))
                {
                    continue;
                }

                var items = new List<GuiStructuredContentItem>();
                if (sectionMap.TryGetValue("Items", out var itemsObj) && itemsObj is IEnumerable<object> rawItems)
                {
                    foreach (var itemEntry in rawItems)
                    {
                        if (itemEntry is not IDictionary<string, object> itemMap ||
                            !itemMap.TryGetValue("PrimaryText", out var primaryObj))
                        {
                            continue;
                        }

                        var primaryText = primaryObj?.ToString();
                        if (string.IsNullOrWhiteSpace(primaryText))
                        {
                            continue;
                        }

                        var secondaryText = itemMap.TryGetValue("SecondaryText", out var secondaryObj)
                            ? secondaryObj?.ToString()
                            : null;
                        var badge = itemMap.TryGetValue("Badge", out var badgeObj)
                            ? badgeObj?.ToString()
                            : null;
                        var actionText = itemMap.TryGetValue("ActionText", out var actionTextObj)
                            ? actionTextObj?.ToString()
                            : null;
                        var actionCommand = itemMap.TryGetValue("ActionCommand", out var actionCommandObj)
                            ? actionCommandObj?.ToString()
                            : null;

                        items.Add(new GuiStructuredContentItem(primaryText, secondaryText, badge, actionText, actionCommand));
                    }
                }

                sections.Add(new GuiStructuredContentSection(sectionTitle, items));
            }
        }

        content = new GuiStructuredContentDefinition(moduleId!, title!, sections);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        await LogoutAsync();
    }
}