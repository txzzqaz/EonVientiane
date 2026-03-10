namespace EonVientiane.PlayerModule;

using EonVientiane.Core.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public sealed class PlayerRuntime : IRemoteGameRuntime
{
    private readonly Dictionary<string, object> sharedState = new(StringComparer.Ordinal);

    private string playerName = "玩家";
    private int playerLevel = 1;
    private int experience = 0;
    private bool firstStatusAchievementTriggered;

    public string RuntimeId => "module.player.core";
    public string RuntimeVersion => "1.0.0";

    public void Initialize(string playerName)
    {
        this.playerName = playerName;
        playerLevel = 1;
        experience = 0;
        firstStatusAchievementTriggered = false;
        sharedState["player.name"] = playerName;
        sharedState["level.current"] = string.Empty;

        InvokeOptional("EonVientiane.BattleModule", "EonVientiane.BattleModule.BattleApi", "Initialize", sharedState);
        InvokeOptional("EonVientiane.InventoryModule", "EonVientiane.InventoryModule.InventoryApi", "Initialize", sharedState);
        InvokeOptional("EonVientiane.AchievementModule", "EonVientiane.AchievementModule.AchievementRuntime", "Initialize", sharedState);
        InvokeOptional("EonVientiane.AchievementConnectionModule", "EonVientiane.AchievementConnectionModule.ConnectionAchievementRuntime", "Initialize", sharedState);
        InvokeOptional("EonVientiane.AchievementStatusModule", "EonVientiane.AchievementStatusModule.StatusFirstAchievementRuntime", "Initialize", sharedState);
        RefreshUnlockedFromLocalPackages();
    }

    public string GetPrompt()
    {
        var currentLevelJson = GetCurrentLevelJson();
        if (string.IsNullOrWhiteSpace(currentLevelJson))
        {
            return "等待中";
        }

        try
        {
            using var doc = JsonDocument.Parse(currentLevelJson);
            if (doc.RootElement.TryGetProperty("Name", out var nameProp))
            {
                return nameProp.GetString() ?? "等待中";
            }
        }
        catch
        {
        }

        return "等待中";
    }

    public RuntimeCommandResult Execute(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return new RuntimeCommandResult { Handled = true, Output = string.Empty };
        }

        var parts = commandLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        return cmd switch
        {
            "help" => Ok(GetHelp()),
            "status" => Ok(GetStatus()),
            _ when TryExecuteModuleCommand(cmd, args, out var output) => Ok(output),
            _ => new RuntimeCommandResult { Handled = false },
        };
    }

    private static RuntimeCommandResult Ok(string output) => new() { Handled = true, Output = output };

    private string GetHelp()
    {
        var moduleHelp = BuildModuleHelp();
        return $"""
=== 游戏命令（远程模块）===
status

=== 模块命令 ===
{moduleHelp}
""";
    }

    private string GetStatus()
    {
        var triggerNotice = TriggerFirstStatusAchievementIfNeeded();

        var currentLevelJson = GetCurrentLevelJson();
        var sb = new StringBuilder();
        sb.AppendLine("=== 玩家信息 ===");
        sb.AppendLine($"名字: {playerName}");
        sb.AppendLine($"等级: {playerLevel}");
        sb.AppendLine($"经验: {experience}");
        sb.AppendLine($"状态: {(string.IsNullOrWhiteSpace(currentLevelJson) ? "Idle" : "InLevel")}");
        if (!string.IsNullOrWhiteSpace(currentLevelJson))
        {
            sb.AppendLine();
            var levelText = InvokeOptional(
                "EonVientiane.LevelModule",
                "EonVientiane.LevelModule.LevelApi",
                "BuildCurrentLevelText",
                currentLevelJson) as string;
            if (!string.IsNullOrWhiteSpace(levelText))
            {
                sb.AppendLine(levelText.TrimEnd());
            }
        }

        var inventoryAddon = InvokeOptional(
            "EonVientiane.InventoryModule",
            "EonVientiane.InventoryModule.InventoryApi",
            "GetStatusAddon",
            sharedState) as string;
        if (!string.IsNullOrWhiteSpace(inventoryAddon))
        {
            sb.AppendLine();
            sb.AppendLine(inventoryAddon);
        }

        var achievementAddon = InvokeOptional(
            "EonVientiane.AchievementModule",
            "EonVientiane.AchievementModule.AchievementRuntime",
            "GetStatusAddon",
            sharedState) as string;
        if (!string.IsNullOrWhiteSpace(achievementAddon))
        {
            sb.AppendLine(achievementAddon);
        }

        if (!string.IsNullOrWhiteSpace(triggerNotice))
        {
            sb.AppendLine(triggerNotice);
        }

        return sb.ToString();
    }

    private string TriggerFirstStatusAchievementIfNeeded()
    {
        if (firstStatusAchievementTriggered)
        {
            return string.Empty;
        }

        firstStatusAchievementTriggered = true;

        var shouldRequest = InvokeOptional(
            "EonVientiane.AchievementStatusModule",
            "EonVientiane.AchievementStatusModule.StatusFirstAchievementRuntime",
            "ShouldRequestForFirstStatus",
            sharedState) as bool?;

        if (shouldRequest == false)
        {
            return string.Empty;
        }

        try
        {
            var requestResult = RequestAchievementVerification("player.status.first");
            if (!requestResult.Success)
            {
                return string.IsNullOrWhiteSpace(requestResult.Message)
                    ? string.Empty
                    : $"⚠ 成就验证失败: {requestResult.Message}";
            }

            MergeUnlockedAchievements(requestResult.GrantedAchievementIds);
            RefreshUnlockedFromLocalPackages();

            if (requestResult.DownloadedCount == 0)
            {
                return "✓ 成就验证完成（无新增模块）";
            }

            var moduleList = string.Join(", ", requestResult.SyncedModuleIds.Distinct(StringComparer.Ordinal));
            return $"✓ 首次 status 成就验证完成，新增模块: {moduleList}";
        }
        catch (Exception ex)
        {
            return $"⚠ 成就验证失败: {ex.Message}";
        }
    }

    private bool TryExecuteModuleCommand(string command, string[] args, out string output)
    {
        var modules = new[]
        {
            (Assembly: "EonVientiane.BattleModule", Type: "EonVientiane.BattleModule.BattleApi"),
            (Assembly: "EonVientiane.LevelModule", Type: "EonVientiane.LevelModule.LevelApi"),
            (Assembly: "EonVientiane.InventoryModule", Type: "EonVientiane.InventoryModule.InventoryApi"),
            (Assembly: "EonVientiane.EquipmentModule", Type: "EonVientiane.EquipmentModule.EquipmentApi"),
            (Assembly: "EonVientiane.AchievementModule", Type: "EonVientiane.AchievementModule.AchievementRuntime"),
        };

        foreach (var module in modules)
        {
            var canHandle = InvokeOptional(module.Assembly, module.Type, "CanHandleCommand", command) as bool?;
            if (canHandle != true)
            {
                continue;
            }

            var result = InvokeOptional(module.Assembly, module.Type, "ExecuteCommand", sharedState, command, args) as string;
            output = result ?? string.Empty;
            return true;
        }

        output = string.Empty;
        return false;
    }

    private string BuildModuleHelp()
    {
        var moduleHelpTexts = new List<string>();
        var modules = new[]
        {
            (Assembly: "EonVientiane.BattleModule", Type: "EonVientiane.BattleModule.BattleApi"),
            (Assembly: "EonVientiane.LevelModule", Type: "EonVientiane.LevelModule.LevelApi"),
            (Assembly: "EonVientiane.InventoryModule", Type: "EonVientiane.InventoryModule.InventoryApi"),
            (Assembly: "EonVientiane.EquipmentModule", Type: "EonVientiane.EquipmentModule.EquipmentApi"),
            (Assembly: "EonVientiane.AchievementModule", Type: "EonVientiane.AchievementModule.AchievementRuntime"),
        };

        foreach (var module in modules)
        {
            var text = InvokeOptional(module.Assembly, module.Type, "GetHelpText") as string;
            if (!string.IsNullOrWhiteSpace(text))
            {
                moduleHelpTexts.Add(text.TrimEnd());
            }
        }

        if (moduleHelpTexts.Count == 0)
        {
            return "(无可用模块命令)";
        }

        return string.Join(Environment.NewLine, moduleHelpTexts);
    }

    private string GetCurrentLevelJson()
    {
        if (sharedState.TryGetValue("level.current", out var levelObj) && levelObj is string levelJson)
        {
            return levelJson;
        }

        return string.Empty;
    }

    private VerifyAchievementResult RequestAchievementVerification(string trigger)
    {
        var userId = Environment.GetEnvironmentVariable("EV_USER_ID");
        var userPublicKeyBase64 = Environment.GetEnvironmentVariable("EV_USER_PUBLIC_KEY_PEM_BASE64");
        var packageDir = Environment.GetEnvironmentVariable("EV_USER_PACKAGE_DIR");
        var serverBaseUrl = Environment.GetEnvironmentVariable("EV_SERVER_URL") ?? "http://127.0.0.1:5000";

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userPublicKeyBase64) || string.IsNullOrWhiteSpace(packageDir))
        {
            return new VerifyAchievementResult { Success = false, Message = "缺少运行时成就验证上下文" };
        }

        var userPublicKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(userPublicKeyBase64));
        Directory.CreateDirectory(packageDir);

        var (achievementIds, moduleIds, moduleVersions) = ScanLocalPackageState(packageDir);
        var payload = new AchievementVerifyRequest
        {
            UserId = userId,
            UserPublicKeyPem = userPublicKeyPem,
            Trigger = trigger,
            ExistingAchievementIds = achievementIds.ToList(),
            ExistingModuleIds = moduleIds.ToList(),
            ModuleVersions = new Dictionary<string, string>(moduleVersions, StringComparer.Ordinal),
        };

        using var client = new HttpClient
        {
            BaseAddress = new Uri(serverBaseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(15),
        };

        using var response = client.PostAsJsonAsync("/api/logic/achievement/verify", payload).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var verifyResponse = response.Content.ReadFromJsonAsync<AchievementVerifyResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("成就验证响应无效");

        var syncedModuleIds = new List<string>(verifyResponse.Manifests.Count);
        foreach (var item in verifyResponse.Manifests)
        {
            if (string.IsNullOrWhiteSpace(item.PackageBase64))
            {
                continue;
            }

            var outputFilePath = Path.Combine(packageDir, item.FileName);
            var packageBytes = Convert.FromBase64String(item.PackageBase64);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
            File.WriteAllBytes(outputFilePath, packageBytes);
            syncedModuleIds.Add(item.ModuleId);
        }

        return new VerifyAchievementResult
        {
            Success = true,
            DownloadedCount = verifyResponse.Manifests.Count,
            SyncedModuleIds = syncedModuleIds,
            GrantedAchievementIds = verifyResponse.GrantedAchievementIds,
        };
    }

    private static (HashSet<string> AchievementIds, HashSet<string> ModuleIds, Dictionary<string, string> ModuleVersions) ScanLocalPackageState(string packageDir)
    {
        var achievementIds = new HashSet<string>(StringComparer.Ordinal);
        var moduleIds = new HashSet<string>(StringComparer.Ordinal);
        var moduleVersions = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!Directory.Exists(packageDir))
        {
            return (achievementIds, moduleIds, moduleVersions);
        }

        foreach (var file in Directory.GetFiles(packageDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var json = File.ReadAllText(file);
                var envelope = JsonSerializer.Deserialize<LogicPackageEnvelope>(json);
                if (envelope is null || string.IsNullOrWhiteSpace(envelope.ModuleId))
                {
                    continue;
                }

                moduleIds.Add(envelope.ModuleId);
                if (!string.IsNullOrWhiteSpace(envelope.Version))
                {
                    moduleVersions[envelope.ModuleId] = envelope.Version.Trim();
                }

                if (IsAchievementModuleId(envelope.ModuleId))
                {
                    achievementIds.Add(envelope.ModuleId);
                }
            }
            catch
            {
            }
        }

        return (achievementIds, moduleIds, moduleVersions);
    }

    private static bool IsAchievementModuleId(string moduleId)
    {
        return moduleId.StartsWith("achievement.", StringComparison.Ordinal)
            || (moduleId.StartsWith("module.achievement.", StringComparison.Ordinal)
                && !string.Equals(moduleId, "module.achievement.core", StringComparison.Ordinal));
    }

    private void RefreshUnlockedFromLocalPackages()
    {
        if (!sharedState.TryGetValue("achievement.unlocked", out var unlockedObj) || unlockedObj is not List<string> unlocked)
        {
            unlocked = new List<string>();
            sharedState["achievement.unlocked"] = unlocked;
        }

        var packageDir = Environment.GetEnvironmentVariable("EV_USER_PACKAGE_DIR");
        if (string.IsNullOrWhiteSpace(packageDir) || !Directory.Exists(packageDir))
        {
            return;
        }

        var (achievementIds, _, _) = ScanLocalPackageState(packageDir);
        foreach (var achievementId in achievementIds)
        {
            if (!unlocked.Contains(achievementId, StringComparer.Ordinal))
            {
                unlocked.Add(achievementId);
            }
        }
    }

    private void MergeUnlockedAchievements(IEnumerable<string> achievementIds)
    {
        if (!sharedState.TryGetValue("achievement.unlocked", out var unlockedObj) || unlockedObj is not List<string> unlocked)
        {
            unlocked = new List<string>();
            sharedState["achievement.unlocked"] = unlocked;
        }

        foreach (var achievementId in achievementIds)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                continue;
            }

            if (!unlocked.Contains(achievementId, StringComparer.Ordinal))
            {
                unlocked.Add(achievementId);
            }
        }
    }

    private static object? InvokeOptional(string assemblyName, string typeName, string methodName, params object[] args)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (type is null)
        {
            return null;
        }

        var methods = type.GetMethods().Where(m => m.Name == methodName).ToList();
        var target = methods.FirstOrDefault(m => m.GetParameters().Length == args.Length) ?? methods.FirstOrDefault();
        if (target is null)
        {
            return null;
        }

        return target.Invoke(null, args);
    }

    private sealed class AchievementVerifyRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string UserPublicKeyPem { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public List<string> ExistingAchievementIds { get; set; } = new();
        public List<string> ExistingModuleIds { get; set; } = new();
        public Dictionary<string, string> ModuleVersions { get; set; } = new();
    }

    private sealed class AchievementVerifyResponse
    {
        public List<string> IssuedFiles { get; set; } = new();
        public List<string> GrantedAchievementIds { get; set; } = new();
        public List<PackageManifestItemDto> Manifests { get; set; } = new();
    }

    private sealed class PackageManifestItemDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ModuleId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
        public long SizeBytes { get; set; }
        public string PackageBase64 { get; set; } = string.Empty;
    }

    private sealed class VerifyAchievementResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DownloadedCount { get; set; }
        public List<string> SyncedModuleIds { get; set; } = new();
        public List<string> GrantedAchievementIds { get; set; } = new();
    }
}
