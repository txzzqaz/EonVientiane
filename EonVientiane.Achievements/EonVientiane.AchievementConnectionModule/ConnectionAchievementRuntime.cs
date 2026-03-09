namespace EonVientiane.AchievementConnectionModule;

public static class ConnectionAchievementRuntime
{
    public const string AchievementId = "module.achievement.connection";
    public const string AchievementName = "连接？";
    public const string AchievementDescription = "你总会获得这个成就的，不是么？";
    public const string TriggerLoginFirst = "login.first";

    public static void Initialize(IDictionary<string, object> state)
    {
        if (state is null)
        {
            return;
        }

        RegisterAchievementMetadata(state);
    }

    public static bool ShouldRequestForLogin(IDictionary<string, object> state)
    {
        if (state.TryGetValue("achievement.requested.connection.login", out var requestedObj) && requestedObj is true)
        {
            return false;
        }

        state["achievement.requested.connection.login"] = true;
        return true;
    }

    public static bool VerifyOnServer(string trigger, IReadOnlyCollection<string> unlockedAchievementIds)
    {
        if (!string.Equals(trigger, TriggerLoginFirst, StringComparison.Ordinal))
        {
            return false;
        }

        return !unlockedAchievementIds.Contains(AchievementId, StringComparer.Ordinal);
    }

    public static IReadOnlyList<string> GetModulesToIssueOnUnlock()
    {
        return new[]
        {
            "module.player.core",
            AchievementId,
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
