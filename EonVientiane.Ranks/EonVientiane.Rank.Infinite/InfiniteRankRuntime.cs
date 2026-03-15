namespace EonVientiane.Rank.Infinite;

public static class InfiniteRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "infinite",
            ["Name"] = "Infinite / 无限",
            ["Tier"] = 8,
        };
    }
}
