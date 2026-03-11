namespace EonVientiane.NetworkBattleModule;

public static partial class NetworkBattleApi
{
    private static void EnsureInitialized(IDictionary<string, object> state)
    {
        if (!state.ContainsKey(RoomsStateKey) || state[RoomsStateKey] is not Dictionary<string, LanRoom> rooms)
        {
            rooms = new Dictionary<string, LanRoom>(StringComparer.Ordinal);
            state[RoomsStateKey] = rooms;
        }

        if (!state.TryGetValue(LocalPlayerIdStateKey, out var playerIdObj) || string.IsNullOrWhiteSpace(playerIdObj?.ToString()))
        {
            state[LocalPlayerIdStateKey] = $"local-{Guid.NewGuid():N}";
        }

        var playerName = ResolvePlayerName(state);
        state[LocalPlayerNameStateKey] = playerName;

        if (!state.TryGetValue(LocalRoomIdStateKey, out _))
        {
            state[LocalRoomIdStateKey] = string.Empty;
        }
    }

    private static object? InvokeOptional(string assemblyName, string typeName, string methodName, params object[] args)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (type is null)
        {
            return null;
        }

        var method = type.GetMethod(methodName);
        if (method is null)
        {
            return null;
        }

        return method.Invoke(null, args);
    }

    private static Dictionary<string, LanRoom> GetRooms(IDictionary<string, object> state)
    {
        if (!state.TryGetValue(RoomsStateKey, out var roomsObj) || roomsObj is not Dictionary<string, LanRoom> rooms)
        {
            rooms = new Dictionary<string, LanRoom>(StringComparer.Ordinal);
            state[RoomsStateKey] = rooms;
        }

        return rooms;
    }

    private static LanRoom? GetCurrentRoom(IDictionary<string, object> state)
    {
        var rooms = GetRooms(state);
        if (!state.TryGetValue(LocalRoomIdStateKey, out var roomIdObj) || string.IsNullOrWhiteSpace(roomIdObj?.ToString()))
        {
            return null;
        }

        return rooms.TryGetValue(roomIdObj.ToString()!, out var room) ? room : null;
    }

    private static string ResolvePlayerName(IDictionary<string, object> state)
    {
        if (state.TryGetValue("player.name", out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return value!.ToString()!;
        }

        if (state.TryGetValue(LocalPlayerNameStateKey, out var local) && !string.IsNullOrWhiteSpace(local?.ToString()))
        {
            return local!.ToString()!;
        }

        return "玩家";
    }

    private static string GetLocalPlayerId(IDictionary<string, object> state)
    {
        if (state.TryGetValue(LocalPlayerIdStateKey, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return value!.ToString()!;
        }

        var id = $"local-{Guid.NewGuid():N}";
        state[LocalPlayerIdStateKey] = id;
        return id;
    }

    private static string ResolveDisplayName(LanRoom room, string playerId)
    {
        return room.Members.TryGetValue(playerId, out var member)
            ? member.DisplayName
            : playerId;
    }

    private static string ResolveSuggestedGroup(LanRoom room)
    {
        var usedGroups = room.Members.Values
            .Select(x => x.GroupId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!usedGroups.Contains("A"))
        {
            return "A";
        }

        if (!usedGroups.Contains("B"))
        {
            return "B";
        }

        return "C";
    }

    private static string NormalizeGroupId(string raw)
    {
        var text = raw.Trim();
        if (text.Length > 12)
        {
            text = text[..12];
        }

        return text;
    }
}