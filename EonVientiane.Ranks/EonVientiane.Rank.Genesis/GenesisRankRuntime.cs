namespace EonVientiane.Rank.Genesis;

public static class GenesisRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "genesis",
            ["Name"] = "Genesis / 创生",
            ["Tier"] = 5,
        };
    }
}
