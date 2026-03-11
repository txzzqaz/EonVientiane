namespace EonVientiane.PlayerModule;

using EonVientiane.Core.Models;
using System.Text.Json;

public sealed partial class PlayerRuntime
{
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
}