namespace EonVientiane.NetworkBattleModule;

public static partial class NetworkBattleApi
{
    private const string LanCommand = "lan";
    private const string RoomsStateKey = "network.rooms";
    private const string LocalPlayerIdStateKey = "network.localPlayerId";
    private const string LocalPlayerNameStateKey = "network.localPlayerName";
    private const string LocalRoomIdStateKey = "network.localRoomId";

    public static bool CanHandleCommand(string command)
    {
        return command.Equals(LanCommand, StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExecuteCommand(IDictionary<string, object> state, string command, string[] args)
    {
        if (!CanHandleCommand(command))
        {
            return null;
        }

        EnsureInitialized(state);

        if (args.Length == 0)
        {
            return BuildLanSummary(state);
        }

        var sub = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        return sub switch
        {
            "help" => GetHelpText(),
            "status" => BuildLanSummary(state),
            "rooms" => ListRooms(state),
            "create" => CreateRoom(state, rest),
            "join" => JoinRoom(state, rest),
            "leave" => LeaveRoom(state),
            "ready" => SetReady(state, rest),
            "group" => SetGroup(state, rest),
            "start" => StartPvp(state),
            _ => "❌ 未知 lan 子命令。使用 'lan help' 查看帮助。",
        };
    }

    public static string GetHelpText()
    {
        return "lan status\n  查看当前局域网对战状态\nlan rooms\n  查看局域网房间列表\nlan create [房间名] [阵型]\n  创建房间（默认阵型 1v1）\nlan join <房间ID>\n  进入房间\nlan leave\n  离开当前房间\nlan ready [on|off]\n  设置或切换准备状态\nlan group <组号>\n  设置当前玩家分组（如 A/B/红/蓝）\nlan start\n  房主在全员准备后启动 PVP 对战";
    }

    public static void Initialize(IDictionary<string, object> state)
    {
        EnsureInitialized(state);
    }
}
