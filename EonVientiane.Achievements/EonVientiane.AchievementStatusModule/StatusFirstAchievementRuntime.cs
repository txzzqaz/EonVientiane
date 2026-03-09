namespace EonVientiane.AchievementStatusModule;

public static class StatusFirstAchievementRuntime
{
    public const string AchievementId = "module.achievement.status.first";
    public const string AchievementName = "你当然会这么做";
    public const string AchievementDescription = "或许你也永远不会这么做";
    public const string TriggerStatusFirst = "player.status.first";

    public static void Initialize(IDictionary<string, object> state)
    {
        if (state is null)
        {
            return;
        }

        RegisterAchievementMetadata(state);
    }

    public static bool ShouldRequestForFirstStatus(IDictionary<string, object> state)
    {
        if (state.TryGetValue("achievement.requested.status.first", out var requestedObj) && requestedObj is true)
        {
            return false;
        }

        state["achievement.requested.status.first"] = true;
        return true;
    }

    public static bool VerifyOnServer(string trigger, IReadOnlyCollection<string> unlockedAchievementIds)
    {
        if (!string.Equals(trigger, TriggerStatusFirst, StringComparison.Ordinal))
        {
            return false;
        }

        return !unlockedAchievementIds.Contains(AchievementId, StringComparer.Ordinal);
    }

    public static IReadOnlyList<string> GetModulesToIssueOnUnlock()
    {
        return new[]
        {
            "module.achievement.core",
            "module.achievement.status",
        };
    }

    private static void RegisterAchievementMetadata(IDictionary<string, object> state)
    {
        var type = Type.GetType("EonVientiane.AchievementModule.AchievementRuntime, EonVientiane.AchievementModule");
        var method = type?.GetMethod("RegisterAchievement", new[]
        {
            typeof(IDictionary<string, object>),
            typeof(string),
            typeof(string),
            typeof(string),
        });

        method?.Invoke(null, new object[]
        {
            state,
            AchievementId,
            AchievementName,
            AchievementDescription,
        });
    }
}