namespace EonVientiane.Level.First;

public static class FirstLevelRuntime
{
    public static Dictionary<string, object> GetLevelMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "first",
            ["Name"] = "初见",
            ["Description"] = "第一个关卡。敌人刚刚现身，但已经带着自己的骰子与自我。",
            ["Difficulty"] = 1,
        };
    }

    public static Dictionary<string, object> GetLevel()
    {
        return GetLevelMetadata();
    }

    public static Dictionary<string, object> GetBattleOpponent()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "level.enemy.first",
            ["Name"] = "初始敌人",
            ["Loadout"] = new[] { "D6", "自我" },
        };
    }

    public static Dictionary<string, object> DecideBattleAction(Dictionary<string, object> context)
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["action"] = "active",
            ["requestedDiceName"] = "D6",
        };
    }

    public static string SelectEnemyActiveDice(Dictionary<string, object> context)
    {
        var decision = DecideBattleAction(context);
        return decision.TryGetValue("requestedDiceName", out var requested) && requested is not null
            ? requested.ToString() ?? "D6"
            : "D6";
    }
}
