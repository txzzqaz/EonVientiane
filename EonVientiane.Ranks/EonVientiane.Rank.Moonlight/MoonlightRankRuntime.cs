namespace EonVientiane.Rank.Moonlight;

public static class MoonlightRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "moonlight",
            ["Name"] = "Moonlight / 月辉",
            ["Tier"] = 2,
        };
    }
}
