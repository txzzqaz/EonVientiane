namespace EonVientiane.NetworkBattleModule;

public static partial class NetworkBattleApi
{
    private static string StartPvp(IDictionary<string, object> state)
    {
        var room = GetCurrentRoom(state);
        if (room is null)
        {
            return "❌ 当前不在任何房间中。";
        }

        var localPlayerId = GetLocalPlayerId(state);
        if (!room.HostPlayerId.Equals(localPlayerId, StringComparison.Ordinal))
        {
            return "❌ 仅房主可以启动对战。";
        }

        if (room.Members.Count < 2)
        {
            return "❌ 房间人数不足，至少需要 2 名玩家。";
        }

        var unready = room.Members.Values.Where(x => !x.IsReady).ToList();
        if (unready.Count > 0)
        {
            return $"❌ 仍有玩家未准备: {string.Join(", ", unready.Select(x => x.DisplayName))}";
        }

        var groups = room.Members.Values
            .GroupBy(x => x.GroupId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.JoinedAtUtc).ToList(), StringComparer.OrdinalIgnoreCase);
        if (groups.Count < 2)
        {
            return "❌ 当前仅有一个分组，至少需要两个分组才可开始对战。";
        }

        if (!room.Members.TryGetValue(localPlayerId, out var localMember))
        {
            return "❌ 当前房主不在房间成员中。";
        }

        var localGroupId = localMember.GroupId;
        if (!groups.TryGetValue(localGroupId, out var localGroupMembers) || localGroupMembers.Count == 0)
        {
            return "❌ 本地玩家分组无效。";
        }

        var enemyGroup = groups
            .Where(x => !x.Key.Equals(localGroupId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Value.Count)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Value)
            .FirstOrDefault();
        if (enemyGroup is null || enemyGroup.Count == 0)
        {
            return "❌ 未找到可对战的敌方分组。";
        }

        var opponent = enemyGroup[0];
        state["battle.remoteOpponent.public"] = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["unitId"] = opponent.PlayerId,
            ["displayName"] = opponent.DisplayName,
            ["HP"] = 12,
            ["ATKP"] = 2,
        };

        var side1Count = Math.Clamp(localGroupMembers.Count, 1, 9);
        var side2Count = Math.Clamp(enemyGroup.Count, 1, 9);
        var formation = $"{side1Count}v{side2Count}";

        var startResult = InvokeOptional(
            "EonVientiane.BattleModule",
            "EonVientiane.BattleModule.BattleApi",
            "StartSession",
            state,
            "pvp",
            formation) as string;

        if (string.IsNullOrWhiteSpace(startResult))
        {
            return "❌ 战斗模块不可用，无法开始网络对战。";
        }

        foreach (var key in room.Members.Keys.ToList())
        {
            var current = room.Members[key];
            room.Members[key] = current with { IsReady = false };
        }

        return $"✓ 房主已启动局域网对战：{localGroupId} vs {opponent.GroupId}（阵型 {formation}）\n{startResult}";
    }
}