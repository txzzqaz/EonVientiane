namespace EonVientiane.Rank.Sunblaze;

public static class SunblazeRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "sunblaze",
            ["Name"] = "Sunblaze / 日曜",
            ["Tier"] = 3,
        };
    }
}
