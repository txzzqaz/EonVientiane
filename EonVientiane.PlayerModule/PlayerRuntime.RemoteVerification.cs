namespace EonVientiane.PlayerModule;

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public sealed partial class PlayerRuntime
{
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
        var recentBattleRecords = LoadRecentLocalBattleRecords(packageDir, maxCount: 5);
        var payload = new AchievementVerifyRequest
        {
            UserId = userId,
            UserPublicKeyPem = userPublicKeyPem,
            Trigger = trigger,
            ExistingAchievementIds = achievementIds.ToList(),
            ExistingModuleIds = moduleIds.ToList(),
            ModuleVersions = new Dictionary<string, string>(moduleVersions, StringComparer.Ordinal),
            RecentBattleRecords = recentBattleRecords,
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

    private static List<ClientVerifiedBattleRecord> LoadRecentLocalBattleRecords(string packageDir, int maxCount)
    {
        var result = new List<ClientVerifiedBattleRecord>();
        if (maxCount <= 0)
        {
            return result;
        }

        var rootDir = Path.GetFullPath(Path.Combine(packageDir, "..", "battle-records"));
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
                var record = JsonSerializer.Deserialize<ClientVerifiedBattleRecord>(json);
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

    private BattleVerifyResponse RequestBattleVerification(string battleRecordJson)
    {
        var userId = Environment.GetEnvironmentVariable("EV_USER_ID");
        var serverBaseUrl = Environment.GetEnvironmentVariable("EV_SERVER_URL") ?? "http://127.0.0.1:5000";

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("缺少运行时用户上下文");
        }

        var payload = new BattleVerifyRequest
        {
            UserId = userId,
            BattleRecordJson = battleRecordJson,
        };

        using var client = new HttpClient
        {
            BaseAddress = new Uri(serverBaseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(15),
        };

        using var response = client.PostAsJsonAsync("/api/logic/battle/verify", payload).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        return response.Content.ReadFromJsonAsync<BattleVerifyResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("战斗签验响应无效");
    }

    private void SaveVerifiedBattleRecordToLocal(BattleVerifyResponse response)
    {
        var packageDir = Environment.GetEnvironmentVariable("EV_USER_PACKAGE_DIR");
        if (string.IsNullOrWhiteSpace(packageDir))
        {
            throw new InvalidOperationException("缺少运行时本地包目录上下文");
        }

        var safeBattleId = SanitizePathPart(response.BattleId);
        var rootDir = Path.GetFullPath(Path.Combine(packageDir, "..", "battle-records"));
        Directory.CreateDirectory(rootDir);

        var outputPath = Path.Combine(rootDir, $"{safeBattleId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.signed.json");
        var content = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        File.WriteAllText(outputPath, content);
    }

    private static string SanitizePathPart(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "unknown";
        }

        var safe = string.Concat(raw.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
    }
}