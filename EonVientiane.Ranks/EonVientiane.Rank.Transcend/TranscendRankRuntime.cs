namespace EonVientiane.Rank.Transcend;

public static class TranscendRankRuntime
{
    public static Dictionary<string, object> GetRankMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Id"] = "transcend",
            ["Name"] = "Transcend / 超脱",
            ["Tier"] = 7,
        };
    }
}
