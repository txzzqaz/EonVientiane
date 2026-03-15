namespace EonVientiane.Rank.Galaxy;

public static class GalaxyRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "galaxy",
            ["Name"] = "Galaxy / 天河",
            ["Tier"] = 4,
        };
    }
}
