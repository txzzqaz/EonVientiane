namespace EonVientiane.Rank.Stardust;

public static class StardustRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "stardust",
            ["Name"] = "Stardust / 星尘",
            ["Tier"] = 1,
        };
    }
}
