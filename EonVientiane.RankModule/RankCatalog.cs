namespace EonVientiane.RankModule;

using System.Reflection;
using System.Text;
using System.Text.Json;

public static class RankApi
{
    private const string CurrentRankStateKey = "rank.current";
    private const string OwnedRanksStateKey = "rank.owned";
    private const string RankScoresStateKey = "rank.scores";

    public static void Initialize(IDictionary<string, object> state)
    {
        var ranks = DiscoverRanksOrdered();
        if (ranks.Count == 0)
        {
            return;
        }

        EnsureState(state, ranks, out var currentRankId, out _, out _);

        if (string.IsNullOrWhiteSpace(currentRankId) || !ranks.Any(x => x.Id.Equals(currentRankId, StringComparison.OrdinalIgnoreCase)))
        {
            state[CurrentRankStateKey] = ranks[0].Id;
        }
    }

    public static bool CanHandleCommand(string command)
    {
        return command.Equals("rank", StringComparison.OrdinalIgnoreCase)
            || command.Equals("ranks", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExecuteCommand(IDictionary<string, object> state, string command, string[] args)
    {
        switch (command.ToLowerInvariant())
        {
            case "ranks":
                return BuildRankListText(state);

            case "rank":
                if (args.Length == 0 || args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildCurrentRankText(state);
                }

                if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHelpText();
                }

                if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildRankListText(state);
                }

                if (args[0].Equals("switch", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < 2)
                    {
                        return "❌ 用法: rank switch <段位ID>";
                    }

                    return SwitchRank(state, args[1]);
                }

                return "❌ 未知 rank 子命令。使用 'rank help' 查看帮助。";

            default:
                return null;
        }
    }

    public static string GetHelpText()
    {
        return "rank\n  查看当前段位\nrank list\n  查看全部段位、拥有状态与段位分\nrank switch <段位ID>\n  切换到已拥有段位";
    }

    public static string GetStatusAddon(IDictionary<string, object> state)
    {
        var ranks = DiscoverRanksOrdered();
        if (ranks.Count == 0)
        {
            return "段位: (暂无可用段位模块)";
        }

        EnsureState(state, ranks, out var currentRankId, out var owned, out var scores);
        var current = ranks.FirstOrDefault(x => x.Id.Equals(currentRankId, StringComparison.OrdinalIgnoreCase)) ?? ranks[0];
        var currentScore = scores.TryGetValue(current.Id, out var score) ? score : 0;
        var next = GetNextRank(ranks, current.Id);

        if (next is null || current.PromotionScore <= 0)
        {
            return $"段位: {current.Name} ({current.Id})\n段位分: {currentScore}\n升段目标: 已达最高段位\n已拥有段位: {owned.Count}/{ranks.Count}";
        }

        var needed = Math.Max(0, current.PromotionScore - currentScore);
        return $"段位: {current.Name} ({current.Id})\n段位分: {currentScore}/{current.PromotionScore}\n下一段位: {next.Name} ({next.Id})，还需 {needed} 分\n已拥有段位: {owned.Count}/{ranks.Count}";
    }

    public static string ApplyBattleResult(IDictionary<string, object> state, string battleRecordJson)
    {
        if (string.IsNullOrWhiteSpace(battleRecordJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(battleRecordJson);
            var root = doc.RootElement;

            var isCompleted = root.TryGetProperty("isCompleted", out var completedProp) && completedProp.ValueKind == JsonValueKind.True;
            var winnerSideId = root.TryGetProperty("winnerSideId", out var winnerSideProp)
                ? winnerSideProp.GetString() ?? string.Empty
                : string.Empty;

            var points = isCompleted
                ? (winnerSideId.Equals("side1", StringComparison.OrdinalIgnoreCase) ? 35 : 12)
                : 6;

            var reason = isCompleted
                ? (winnerSideId.Equals("side1", StringComparison.OrdinalIgnoreCase) ? "战斗胜利" : "战斗结算")
                : "未完成战斗结算";

            return AddRankScore(state, points, reason);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string SwitchRank(IDictionary<string, object> state, string targetRankId)
    {
        var ranks = DiscoverRanksOrdered();
        if (ranks.Count == 0)
        {
            return "❌ 无可用段位模块。";
        }

        EnsureState(state, ranks, out var currentRankId, out var owned, out var scores);

        var target = ranks.FirstOrDefault(x => x.Id.Equals(targetRankId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return $"❌ 未找到段位 '{targetRankId}'。使用 'rank list' 查看可用段位ID。";
        }

        if (!owned.Any(x => x.Equals(target.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return $"❌ 段位 '{target.Id}' 尚未拥有，无法切换。";
        }

        if (target.Id.Equals(currentRankId, StringComparison.OrdinalIgnoreCase))
        {
            return $"✓ 当前已是 {target.Name} ({target.Id})";
        }

        state[CurrentRankStateKey] = target.Id;
        var currentScore = scores.TryGetValue(target.Id, out var score) ? score : 0;
        return $"✓ 已切换段位为 {target.Name} ({target.Id})，当前段位分 {currentScore}";
    }

    private static string AddRankScore(IDictionary<string, object> state, int points, string reason)
    {
        var ranks = DiscoverRanksOrdered();
        if (ranks.Count == 0)
        {
            return "❌ 无可用段位模块。";
        }

        EnsureState(state, ranks, out var currentRankId, out var owned, out var scores);

        if (points <= 0)
        {
            return "❌ 段位分必须为正整数。";
        }

        var currentRank = ranks.FirstOrDefault(x => x.Id.Equals(currentRankId, StringComparison.OrdinalIgnoreCase)) ?? ranks[0];
        if (!scores.TryGetValue(currentRank.Id, out var score))
        {
            score = 0;
        }

        score += points;
        scores[currentRank.Id] = score;

        var currentScore = scores.TryGetValue(currentRank.Id, out var finalScore) ? finalScore : 0;
        var sb = new StringBuilder();
        sb.Append($"✓ {reason}: +{points} 段位分");
        sb.AppendLine();
        sb.Append($"当前段位: {currentRank.Name} ({currentRank.Id})");

        if (currentRank.PromotionScore > 0)
        {
            sb.Append($"，段位分 {currentScore}/{currentRank.PromotionScore}");
        }
        else
        {
            sb.Append($"，段位分 {currentScore}（最高段位）");
        }

        return sb.ToString();
    }

    private static string BuildCurrentRankText(IDictionary<string, object> state)
    {
        var ranks = DiscoverRanksOrdered();
        if (ranks.Count == 0)
        {
            return "=== 当前段位 ===\n(暂无可用段位模块)";
        }

        EnsureState(state, ranks, out var currentRankId, out var owned, out var scores);
        var current = ranks.FirstOrDefault(x => x.Id.Equals(currentRankId, StringComparison.OrdinalIgnoreCase)) ?? ranks[0];
        var currentScore = scores.TryGetValue(current.Id, out var score) ? score : 0;
        var next = GetNextRank(ranks, current.Id);

        var sb = new StringBuilder();
        sb.AppendLine("=== 当前段位 ===");
        sb.AppendLine($"段位: {current.Name} ({current.Id})");

        if (next is null || current.PromotionScore <= 0)
        {
            sb.AppendLine($"段位分: {currentScore}");
            sb.AppendLine("升段目标: 已达最高段位");
        }
        else
        {
            var needed = Math.Max(0, current.PromotionScore - currentScore);
            sb.AppendLine($"段位分: {currentScore}/{current.PromotionScore}");
            sb.AppendLine($"下一段位: {next.Name} ({next.Id})，还需 {needed} 分");
        }

        sb.AppendLine($"已拥有段位数: {owned.Count}/{ranks.Count}");
        return sb.ToString();
    }

    private static string BuildRankListText(IDictionary<string, object> state)
    {
        var ranks = DiscoverRanksOrdered();
        var sb = new StringBuilder();
        sb.AppendLine("=== 段位列表 ===");

        if (ranks.Count == 0)
        {
            sb.AppendLine("(暂无可用段位模块)");
            return sb.ToString();
        }

        EnsureState(state, ranks, out var currentRankId, out var owned, out var scores);

        foreach (var rank in ranks)
        {
            var ownedTag = owned.Any(x => x.Equals(rank.Id, StringComparison.OrdinalIgnoreCase)) ? "已拥有" : "未拥有";
            var currentTag = rank.Id.Equals(currentRankId, StringComparison.OrdinalIgnoreCase) ? "[当前]" : string.Empty;
            var score = scores.TryGetValue(rank.Id, out var s) ? s : 0;
            var target = rank.PromotionScore > 0 ? $"/{rank.PromotionScore}" : string.Empty;
            sb.AppendLine($"  • {rank.Id.PadRight(12)} - {rank.Name} [{ownedTag}] {currentTag} 段位分: {score}{target}");
        }

        return sb.ToString();
    }

    private static RankEntry? GetNextRank(IReadOnlyList<RankEntry> ranks, string currentRankId)
    {
        for (var i = 0; i < ranks.Count; i++)
        {
            if (!ranks[i].Id.Equals(currentRankId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return i + 1 < ranks.Count ? ranks[i + 1] : null;
        }

        return null;
    }

    private static void EnsureState(
        IDictionary<string, object> state,
        IReadOnlyList<RankEntry> ranks,
        out string currentRankId,
        out List<string> owned,
        out Dictionary<string, int> scores)
    {
        if (!state.TryGetValue(OwnedRanksStateKey, out var ownedObj) || ownedObj is not List<string> ownedList)
        {
            ownedList = new List<string>();
            state[OwnedRanksStateKey] = ownedList;
        }

        if (!ownedList.Any())
        {
            ownedList.Add(ranks[0].Id);
        }

        if (!state.TryGetValue(RankScoresStateKey, out var scoreObj) || scoreObj is not Dictionary<string, int> scoreMap)
        {
            scoreMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            state[RankScoresStateKey] = scoreMap;
        }

        foreach (var rank in ranks)
        {
            if (!scoreMap.ContainsKey(rank.Id))
            {
                scoreMap[rank.Id] = 0;
            }
        }

        if (!state.TryGetValue(CurrentRankStateKey, out var currentObj)
            || currentObj is not string current
            || string.IsNullOrWhiteSpace(current)
            || !ranks.Any(x => x.Id.Equals(current, StringComparison.OrdinalIgnoreCase)))
        {
            current = ownedList.FirstOrDefault(x => ranks.Any(r => r.Id.Equals(x, StringComparison.OrdinalIgnoreCase))) ?? ranks[0].Id;
            state[CurrentRankStateKey] = current;
        }

        currentRankId = current;
        owned = ownedList;
        scores = scoreMap;
    }

    private static List<RankEntry> DiscoverRanksOrdered()
    {
        var ranks = new Dictionary<string, RankEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.StartsWith("EonVientiane.Rank.", StringComparison.Ordinal))
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(x => x is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type is null || !type.IsClass || !type.IsAbstract)
                {
                    continue;
                }

                TryCollectRanksFromMethod(type, "GetRankMetadata", ranks);
                TryCollectRanksFromMethod(type, "GetRank", ranks);
                TryCollectRanksFromMethod(type, "GetRanks", ranks);
            }
        }

        return ranks.Values
            .OrderBy(x => x.Tier)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void TryCollectRanksFromMethod(Type type, string methodName, IDictionary<string, RankEntry> ranks)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method is null || method.GetParameters().Length != 0)
        {
            return;
        }

        var value = method.Invoke(null, null);
        foreach (var rank in ExpandRanks(value, type))
        {
            if (!string.IsNullOrWhiteSpace(rank.Id))
            {
                ranks[rank.Id] = rank;
            }
        }
    }

    private static IEnumerable<RankEntry> ExpandRanks(object? value, Type sourceType)
    {
        if (value is null)
        {
            yield break;
        }

        if (TryParseRank(value, sourceType, out var single))
        {
            yield return single;
            yield break;
        }

        if (value is System.Collections.IEnumerable sequence and not string)
        {
            foreach (var item in sequence)
            {
                if (TryParseRank(item, sourceType, out var rank))
                {
                    yield return rank;
                }
            }
        }
    }

    private static bool TryParseRank(object? value, Type sourceType, out RankEntry rank)
    {
        rank = default!;
        if (value is null)
        {
            return false;
        }

        if (value is RankEntry typed)
        {
            rank = typed;
            return true;
        }

        var map = ToDictionary(value);
        if (map.Count == 0)
        {
            return false;
        }

        var id = GetString(map, "Id");
        var name = GetString(map, "Name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        rank = new RankEntry(
            id.Trim(),
            name.Trim(),
            GetInt(map, "Tier", 1),
            Math.Max(0, GetInt(map, "PromotionScore", 0)),
            sourceType.Assembly.GetName().Name ?? string.Empty,
            sourceType.FullName ?? sourceType.Name);
        return true;
    }

    private static Dictionary<string, object> ToDictionary(object value)
    {
        if (value is Dictionary<string, object> dict)
        {
            return new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase);
        }

        if (value is IDictionary<string, object?> nullableDict)
        {
            return nullableDict.ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = property.Value;
            }

            return result;
        }

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    private static string GetString(Dictionary<string, object> map, string key, string defaultValue = "")
    {
        return map.TryGetValue(key, out var value)
            ? value?.ToString() ?? defaultValue
            : defaultValue;
    }

    private static int GetInt(Dictionary<string, object> map, string key, int defaultValue)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return value switch
        {
            int i => i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
            JsonElement e when e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var i) => i,
            JsonElement e when e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var parsed) => parsed,
            _ when int.TryParse(value.ToString(), out var parsed) => parsed,
            _ => defaultValue,
        };
    }

    private sealed record RankEntry(
        string Id,
        string Name,
        int Tier,
        int PromotionScore,
        string AssemblyName,
        string RuntimeType);
}
