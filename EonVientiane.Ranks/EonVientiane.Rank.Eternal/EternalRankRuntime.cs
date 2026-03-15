namespace EonVientiane.Rank.Eternal;

public static class EternalRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "eternal",
            ["Name"] = "Eternal / 永恒",
            ["Tier"] = 9,
        };
    }
}
