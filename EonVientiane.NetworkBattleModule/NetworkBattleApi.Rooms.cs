namespace EonVientiane.NetworkBattleModule;

using System.Text;

public static partial class NetworkBattleApi
{
    private static string BuildLanSummary(IDictionary<string, object> state)
    {
        var rooms = GetRooms(state);
        var room = GetCurrentRoom(state);
        var localName = ResolvePlayerName(state);

        var sb = new StringBuilder();
        sb.AppendLine("=== 局域网对战 ===");
        sb.AppendLine($"玩家: {localName}");
        sb.AppendLine($"房间总数: {rooms.Count}");

        if (room is null)
        {
            sb.AppendLine("当前未在房间中");
            sb.AppendLine("提示: 使用 'lan create' 或 'lan join <房间ID>'");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine($"当前房间: {room.RoomName} ({room.RoomId})");
        sb.AppendLine($"房主: {ResolveDisplayName(room, room.HostPlayerId)}");
        sb.AppendLine($"阵型: {room.Formation}");
        sb.AppendLine("成员:");

        foreach (var member in room.Members.Values.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var hostMark = member.PlayerId.Equals(room.HostPlayerId, StringComparison.Ordinal) ? "房主" : "成员";
            var readyMark = member.IsReady ? "已准备" : "未准备";
            sb.AppendLine($"  • {member.DisplayName} | 组 {member.GroupId} | {readyMark} | {hostMark}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string ListRooms(IDictionary<string, object> state)
    {
        var rooms = GetRooms(state);
        var sb = new StringBuilder();
        sb.AppendLine("=== 局域网房间列表 ===");

        if (rooms.Count == 0)
        {
            sb.AppendLine("(当前无房间)");
            return sb.ToString().TrimEnd();
        }

        foreach (var room in rooms.Values.OrderBy(x => x.CreatedAtUtc))
        {
            var readyCount = room.Members.Values.Count(x => x.IsReady);
            var groupCount = room.Members.Values
                .Select(x => x.GroupId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            sb.AppendLine($"  • {room.RoomId} | {room.RoomName} | 房主 {ResolveDisplayName(room, room.HostPlayerId)} | 人数 {room.Members.Count} | 准备 {readyCount}/{room.Members.Count} | 分组 {groupCount}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string CreateRoom(IDictionary<string, object> state, string[] args)
    {
        var roomName = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0].Trim()
            : "局域网房间";
        var formation = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1].Trim()
            : "1v1";

        if (formation.Length > 16)
        {
            return "❌ 阵型文本过长。";
        }

        _ = LeaveRoomInternal(state, silentWhenNotInRoom: true);

        var rooms = GetRooms(state);
        var localPlayerId = GetLocalPlayerId(state);
        var localPlayerName = ResolvePlayerName(state);
        var roomId = $"lan-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..26];

        var room = new LanRoom(
            RoomId: roomId,
            RoomName: roomName,
            HostPlayerId: localPlayerId,
            Formation: formation,
            CreatedAtUtc: DateTime.UtcNow,
            Members: new Dictionary<string, LanMember>(StringComparer.Ordinal));

        room.Members[localPlayerId] = new LanMember(localPlayerId, localPlayerName, IsReady: false, GroupId: "A", JoinedAtUtc: DateTime.UtcNow);
        rooms[roomId] = room;
        state[LocalRoomIdStateKey] = roomId;

        return $"✓ 已创建房间: {room.RoomName} ({room.RoomId})\n你已加入房间，当前分组: A，准备状态: 未准备";
    }

    private static string JoinRoom(IDictionary<string, object> state, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            return "❌ 请指定房间ID。示例: lan join <房间ID>";
        }

        var roomId = args[0].Trim();
        var rooms = GetRooms(state);
        if (!rooms.TryGetValue(roomId, out var room))
        {
            return $"❌ 房间不存在: {roomId}";
        }

        var localPlayerId = GetLocalPlayerId(state);
        var localPlayerName = ResolvePlayerName(state);

        _ = LeaveRoomInternal(state, silentWhenNotInRoom: true);

        room.Members[localPlayerId] = new LanMember(localPlayerId, localPlayerName, IsReady: false, GroupId: ResolveSuggestedGroup(room), JoinedAtUtc: DateTime.UtcNow);
        state[LocalRoomIdStateKey] = room.RoomId;

        return $"✓ 已进入房间: {room.RoomName} ({room.RoomId})\n当前分组: {room.Members[localPlayerId].GroupId}，准备状态: 未准备";
    }

    private static string LeaveRoom(IDictionary<string, object> state)
    {
        return LeaveRoomInternal(state, silentWhenNotInRoom: false);
    }

    private static string LeaveRoomInternal(IDictionary<string, object> state, bool silentWhenNotInRoom)
    {
        var room = GetCurrentRoom(state);
        if (room is null)
        {
            return silentWhenNotInRoom ? string.Empty : "❌ 当前不在任何房间中。";
        }

        var localPlayerId = GetLocalPlayerId(state);
        room.Members.Remove(localPlayerId);
        state[LocalRoomIdStateKey] = string.Empty;

        if (room.Members.Count == 0)
        {
            GetRooms(state).Remove(room.RoomId);
            return $"✓ 已离开房间，房间 {room.RoomName} 已解散。";
        }

        if (room.HostPlayerId.Equals(localPlayerId, StringComparison.Ordinal))
        {
            room.HostPlayerId = room.Members.Values.OrderBy(x => x.JoinedAtUtc).First().PlayerId;
        }

        return $"✓ 已离开房间: {room.RoomName}";
    }

    private static string SetReady(IDictionary<string, object> state, string[] args)
    {
        var room = GetCurrentRoom(state);
        if (room is null)
        {
            return "❌ 当前不在任何房间中。";
        }

        var localPlayerId = GetLocalPlayerId(state);
        if (!room.Members.TryGetValue(localPlayerId, out var member))
        {
            return "❌ 你不在当前房间成员列表中。";
        }

        var nextReady = args.Length == 0
            ? !member.IsReady
            : args[0].Equals("on", StringComparison.OrdinalIgnoreCase)
              || args[0].Equals("ready", StringComparison.OrdinalIgnoreCase)
              || args[0].Equals("true", StringComparison.OrdinalIgnoreCase);

        room.Members[localPlayerId] = member with { IsReady = nextReady };
        return $"✓ 准备状态已更新: {(nextReady ? "已准备" : "未准备")}";
    }

    private static string SetGroup(IDictionary<string, object> state, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            return "❌ 请指定组号。示例: lan group A";
        }

        var room = GetCurrentRoom(state);
        if (room is null)
        {
            return "❌ 当前不在任何房间中。";
        }

        var group = NormalizeGroupId(args[0]);
        if (string.IsNullOrWhiteSpace(group))
        {
            return "❌ 分组不能为空。";
        }

        var localPlayerId = GetLocalPlayerId(state);
        if (!room.Members.TryGetValue(localPlayerId, out var member))
        {
            return "❌ 你不在当前房间成员列表中。";
        }

        room.Members[localPlayerId] = member with { GroupId = group, IsReady = false };
        return $"✓ 分组已更新为: {group}（已自动取消准备，请重新 ready）";
    }
}