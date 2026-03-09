namespace EonVientiane.CLI;

using System.Net.Http.Json;
using System.Text.Json;
using EonVientiane.Core.Models;

public sealed class ServerBootstrapClient
{
    private readonly HttpClient httpClient;
    private readonly string baseUrl;

    public ServerBootstrapClient(string baseUrl)
    {
        this.baseUrl = baseUrl.TrimEnd('/');
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(this.baseUrl),
            Timeout = TimeSpan.FromSeconds(20),
        };
    }

    public async Task<string> FetchServerPublicKeyAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("/api/logic/public-key", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<List<PackageManifestItemDto>> ConnectAndIssueAsync(
        string userId,
        string userPublicKeyPem,
        IReadOnlyCollection<string> existingAchievementIds,
        IReadOnlyCollection<string> existingModuleIds,
        CancellationToken cancellationToken = default)
    {
        var payload = new ConnectBootstrapRequestDto
        {
            UserId = userId,
            UserPublicKeyPem = userPublicKeyPem,
            ExistingAchievementIds = existingAchievementIds.ToList(),
            ExistingModuleIds = existingModuleIds.ToList(),
        };

        using var response = await httpClient.PostAsJsonAsync("/api/logic/connect", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var connectResponse = await response.Content.ReadFromJsonAsync<ConnectBootstrapResponseDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("服务端连接响应无效");

        return connectResponse.Manifests;
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

    public static (HashSet<string> AchievementIds, HashSet<string> ModuleIds) ScanLocalPackageState(string userPackageDirectory)
    {
        var achievements = new HashSet<string>(StringComparer.Ordinal);
        var modules = new HashSet<string>(StringComparer.Ordinal);

        if (!Directory.Exists(userPackageDirectory))
        {
            return (achievements, modules);
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
                if (IsAchievementModuleId(envelope.ModuleId))
                {
                    achievements.Add(envelope.ModuleId);
                }
            }
            catch
            {
            }
        }

        return (achievements, modules);
    }

    private static bool IsAchievementModuleId(string moduleId)
    {
        return moduleId.StartsWith("achievement.", StringComparison.Ordinal)
            || (moduleId.StartsWith("module.achievement.", StringComparison.Ordinal)
                && !string.Equals(moduleId, "module.achievement.core", StringComparison.Ordinal));
    }

    private sealed class ConnectBootstrapRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserPublicKeyPem { get; set; } = string.Empty;
        public List<string> ExistingAchievementIds { get; set; } = new();
        public List<string> ExistingModuleIds { get; set; } = new();
    }

    private sealed class ConnectBootstrapResponseDto
    {
        public List<string> IssuedFiles { get; set; } = new();
        public List<PackageManifestItemDto> Manifests { get; set; } = new();
    }
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
