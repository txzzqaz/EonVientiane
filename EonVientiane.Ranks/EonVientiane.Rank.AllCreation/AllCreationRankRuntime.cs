namespace EonVientiane.Rank.AllCreation;

public static class AllCreationRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "allcreation",
            ["Name"] = "AllCreation / 万物",
            ["Tier"] = 6,
        };
    }
}
