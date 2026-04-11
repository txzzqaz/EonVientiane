namespace EonVientiane.Core.Services;

using System.Net.Http.Json;
using System.Text.Json;
using EonVientiane.Core.Models;

public sealed class ModuleSyncService
{
    private readonly HttpClient httpClient;

    public ModuleSyncService(string baseUrl)
    {
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(20),
        };
    }

    public async Task<string> FetchServerPublicKeyAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("/api/logic/public-key", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<ModuleSyncResult> ManualSyncAsync(
        string userId,
        string userPublicKeyPem,
        string serverPublicKeyFilePath,
        string userPackageDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(serverPublicKeyFilePath)!);
        Directory.CreateDirectory(userPackageDirectory);

        var serverPublicKeyPem = await FetchServerPublicKeyAsync(cancellationToken);
        File.WriteAllText(serverPublicKeyFilePath, serverPublicKeyPem);

        var localState = ScanLocalPackageState(userPackageDirectory);
        var payload = new ConnectBootstrapRequestDto
        {
            UserId = userId,
            UserPublicKeyPem = userPublicKeyPem,
            ExistingAchievementIds = localState.AchievementIds.ToList(),
            ExistingModuleIds = localState.ModuleIds.ToList(),
            ModuleVersions = new Dictionary<string, string>(localState.ModuleVersions, StringComparer.Ordinal),
        };

        using var response = await httpClient.PostAsJsonAsync("/api/logic/connect", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var connectResponse = await response.Content.ReadFromJsonAsync<ConnectBootstrapResponseDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("服务端连接响应无效");

        var syncedModuleIds = new List<string>(connectResponse.Manifests.Count);
        foreach (var item in connectResponse.Manifests)
        {
            var outputFilePath = Path.Combine(userPackageDirectory, item.FileName);
            await SavePackageToLocalAsync(item, outputFilePath, cancellationToken);
            syncedModuleIds.Add(item.ModuleId);
        }

        return new ModuleSyncResult
        {
            DownloadedCount = connectResponse.Manifests.Count,
            SyncedModuleIds = syncedModuleIds,
        };
    }

    public async Task<ModuleSyncResult> VerifyAchievementAsync(
        string userId,
        string userPublicKeyPem,
        string trigger,
        string userPackageDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(userPackageDirectory);

        var localState = ScanLocalPackageState(userPackageDirectory);
        var recentBattleRecords = LoadRecentLocalBattleRecords(userPackageDirectory, maxCount: 5);
        var payload = new AchievementVerifyRequestDto
        {
            UserId = userId,
            UserPublicKeyPem = userPublicKeyPem,
            Trigger = trigger,
            ExistingAchievementIds = localState.AchievementIds.ToList(),
            ExistingModuleIds = localState.ModuleIds.ToList(),
            ModuleVersions = new Dictionary<string, string>(localState.ModuleVersions, StringComparer.Ordinal),
            RecentBattleRecords = recentBattleRecords,
        };

        using var response = await httpClient.PostAsJsonAsync("/api/logic/achievement/verify", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var verifyResponse = await response.Content.ReadFromJsonAsync<ConnectBootstrapResponseDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("服务端成就验证响应无效");

        var syncedModuleIds = new List<string>(verifyResponse.Manifests.Count);
        foreach (var item in verifyResponse.Manifests)
        {
            var outputFilePath = Path.Combine(userPackageDirectory, item.FileName);
            await SavePackageToLocalAsync(item, outputFilePath, cancellationToken);
            syncedModuleIds.Add(item.ModuleId);
        }

        return new ModuleSyncResult
        {
            DownloadedCount = verifyResponse.Manifests.Count,
            SyncedModuleIds = syncedModuleIds,
        };
    }

    public Task SavePackageToLocalAsync(PackageManifestItemDto item, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(item.PackageBase64))
        {
            throw new InvalidOperationException($"服务端未返回逻辑包内容: {item.FileName}");
        }

        var packageBytes = Convert.FromBase64String(item.PackageBase64);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
        File.WriteAllBytes(outputFilePath, packageBytes);

        return Task.CompletedTask;
    }

    public static ModuleSyncState ScanLocalPackageState(string userPackageDirectory)
    {
        var achievements = new HashSet<string>(StringComparer.Ordinal);
        var modules = new HashSet<string>(StringComparer.Ordinal);
        var moduleVersions = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!Directory.Exists(userPackageDirectory))
        {
            return new ModuleSyncState(achievements, modules, moduleVersions);
        }

        foreach (var file in Directory.GetFiles(userPackageDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var json = File.ReadAllText(file);
                var envelope = JsonSerializer.Deserialize<LogicPackageEnvelope>(json);
                if (envelope is null || string.IsNullOrWhiteSpace(envelope.ModuleId))
                {
                    continue;
                }

                modules.Add(envelope.ModuleId);
                if (!string.IsNullOrWhiteSpace(envelope.Version))
                {
                    moduleVersions[envelope.ModuleId] = envelope.Version.Trim();
                }

                if (IsAchievementModuleId(envelope.ModuleId))
                {
                    achievements.Add(envelope.ModuleId);
                }
            }
            catch
            {
            }
        }

        return new ModuleSyncState(achievements, modules, moduleVersions);
    }

    private static bool IsAchievementModuleId(string moduleId)
    {
        return moduleId.StartsWith("achievement.", StringComparison.Ordinal)
            || (moduleId.StartsWith("module.achievement.", StringComparison.Ordinal)
                && !string.Equals(moduleId, "module.achievement.core", StringComparison.Ordinal));
    }

    private static List<ClientVerifiedBattleRecordDto> LoadRecentLocalBattleRecords(string userPackageDirectory, int maxCount)
    {
        var result = new List<ClientVerifiedBattleRecordDto>();
        if (maxCount <= 0)
        {
            return result;
        }

        var rootDir = Path.GetFullPath(Path.Combine(userPackageDirectory, "..", "battle-records"));
        if (!Directory.Exists(rootDir))
        {
            return result;
        }

        foreach (var filePath in Directory.GetFiles(rootDir, "*.signed.json", SearchOption.TopDirectoryOnly)
                     .OrderByDescending(File.GetLastWriteTimeUtc)
                     .Take(maxCount))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var record = JsonSerializer.Deserialize<ClientVerifiedBattleRecordDto>(json);
                if (record is null ||
                    string.IsNullOrWhiteSpace(record.UserId) ||
                    string.IsNullOrWhiteSpace(record.BattleId) ||
                    string.IsNullOrWhiteSpace(record.BattleRecordJson) ||
                    string.IsNullOrWhiteSpace(record.RecordHash) ||
                    string.IsNullOrWhiteSpace(record.Signature))
                {
                    continue;
                }

                result.Add(record);
            }
            catch
            {
            }
        }

        return result;
    }

    public sealed record ModuleSyncState(
        HashSet<string> AchievementIds,
        HashSet<string> ModuleIds,
        Dictionary<string, string> ModuleVersions);

    public sealed class ModuleSyncResult
    {
        public int DownloadedCount { get; set; }
        public List<string> SyncedModuleIds { get; set; } = new();
    }

    private sealed class ConnectBootstrapRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserPublicKeyPem { get; set; } = string.Empty;
        public List<string> ExistingAchievementIds { get; set; } = new();
        public List<string> ExistingModuleIds { get; set; } = new();
        public Dictionary<string, string> ModuleVersions { get; set; } = new();
    }

    private sealed class ConnectBootstrapResponseDto
    {
        public List<string> IssuedFiles { get; set; } = new();
        public List<PackageManifestItemDto> Manifests { get; set; } = new();
    }

    private sealed class AchievementVerifyRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserPublicKeyPem { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public List<string> ExistingAchievementIds { get; set; } = new();
        public List<string> ExistingModuleIds { get; set; } = new();
        public Dictionary<string, string> ModuleVersions { get; set; } = new();
        public List<ClientVerifiedBattleRecordDto> RecentBattleRecords { get; set; } = new();
    }

    private sealed class ClientVerifiedBattleRecordDto
    {
        public string UserId { get; set; } = string.Empty;
        public string BattleId { get; set; } = string.Empty;
        public string BattleRecordJson { get; set; } = string.Empty;
        public string RecordHash { get; set; } = string.Empty;
        public DateTime VerifiedAtUtc { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public sealed class PackageManifestItemDto
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
}