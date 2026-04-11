namespace EonVientiane.AchievementModule;

using System.Text;

public static class AchievementRuntime
{
    public static void Initialize(IDictionary<string, object> state)
    {
        GetRegistry(state);
    }

    public static bool CanHandleCommand(string command)
    {
        return command.Equals("ach", StringComparison.OrdinalIgnoreCase)
            || command.Equals("achievement", StringComparison.OrdinalIgnoreCase)
            || command.Equals("achievements", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExecuteCommand(IDictionary<string, object> state, string command, string[] args)
    {
        return command.ToLowerInvariant() switch
        {
            "ach" or "achievement" or "achievements" => ExecuteAchievementCommand(state, args),
            _ => null,
        };
    }

    public static string GetHelpText()
    {
        return "ach / achievement / achievements\n  查看成就列表\nach <成就名>\n  查看成就详情";
    }

    public static string GetStatusAddon(IDictionary<string, object> state)
    {
        var unlocked = GetUnlocked(state);
        return $"已解锁成就: {unlocked.Count}";
    }

    public static void RegisterAchievement(IDictionary<string, object> state, string achievementId, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(achievementId) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var registry = GetRegistry(state);
        registry[achievementId] = new AchievementDefinition(achievementId.Trim(), name.Trim(), description?.Trim() ?? string.Empty);
    }

    private static string ExecuteAchievementCommand(IDictionary<string, object> state, string[] args)
    {
        if (args.Length == 0)
        {
            return ListAchievements(state);
        }

        var targetNameOrId = string.Join(' ', args).Trim();
        return ShowAchievementDetail(state, targetNameOrId);
    }

    private static string ListAchievements(IDictionary<string, object> state)
    {
        var registry = GetRegistry(state);
        if (registry.Count == 0)
        {
            return "=== 成就列表 ===\n(暂无可用成就)";
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== 成就列表 ===");
        foreach (var item in registry.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            sb.AppendLine($"  • {item.Name}");
        }

        return sb.ToString();
    }

    private static string ShowAchievementDetail(IDictionary<string, object> state, string targetNameOrId)
    {
        var registry = GetRegistry(state);
        var item = registry.Values.FirstOrDefault(x =>
            x.Name.Equals(targetNameOrId, StringComparison.OrdinalIgnoreCase) ||
            x.Id.Equals(targetNameOrId, StringComparison.OrdinalIgnoreCase));

        if (item is null)
        {
            return $"❌ 未找到成就: {targetNameOrId}";
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== 成就详情 ===");
        sb.AppendLine($"成就名: {item.Name}");
        sb.AppendLine($"成就ID: {item.Id}");
        sb.AppendLine($"成就描述: {item.Description}");

        return sb.ToString();
    }

    private static List<string> GetUnlocked(IDictionary<string, object> state)
    {
        if (!state.TryGetValue("achievement.unlocked", out var unlockedObj) || unlockedObj is not List<string> unlocked)
        {
            unlocked = new List<string>();
            state["achievement.unlocked"] = unlocked;
        }

        return unlocked;
    }

    private static Dictionary<string, AchievementDefinition> GetRegistry(IDictionary<string, object> state)
    {
        if (!state.TryGetValue("achievement.registry", out var registryObj) ||
            registryObj is not Dictionary<string, AchievementDefinition> registry)
        {
            registry = new Dictionary<string, AchievementDefinition>(StringComparer.Ordinal);
            state["achievement.registry"] = registry;
        }

        return registry;
    }

    private sealed record AchievementDefinition(string Id, string Name, string Description);
}
