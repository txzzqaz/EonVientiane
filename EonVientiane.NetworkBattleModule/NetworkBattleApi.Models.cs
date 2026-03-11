namespace EonVientiane.NetworkBattleModule;

public static partial class NetworkBattleApi
{
    private sealed record LanRoom(
        string RoomId,
        string RoomName,
        string HostPlayerId,
        string Formation,
        DateTime CreatedAtUtc,
        Dictionary<string, LanMember> Members)
    {
        public string HostPlayerId { get; set; } = HostPlayerId;
    }

    private sealed record LanMember(
        string PlayerId,
        string DisplayName,
        bool IsReady,
        string GroupId,
        DateTime JoinedAtUtc);
}