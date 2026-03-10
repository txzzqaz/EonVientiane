namespace EonVientiane.BattleModule;

using System.Collections;

public static partial class BattleApi
{
    private const string SessionStateKey = "battle.session";
    private const string BattleCommand = "battle";

    public static bool CanHandleCommand(string command)
    {
        return command.Equals(BattleCommand, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetHelpText()
    {
        return "battle start [mirror]\n  开始一场镜像战斗\nbattle status\n  查看当前战斗状态\nbattle active <目标> [骰子名]\n  执行主动骰子动作\nbattle pass\n  跳过当前回合\nbattle end\n  结束当前战斗";
    }

    public static void Initialize(IDictionary<string, object> state)
    {
        EnsureEffectStore(state);
        state[SessionStateKey] = null!;
    }

    public static string? ExecuteCommand(IDictionary<string, object> state, string command, string[] args)
    {
        if (!CanHandleCommand(command))
        {
            return null;
        }

        if (args.Length == 0)
        {
            return DescribeSession(GetSession(state));
        }

        var subCommand = args[0].ToLowerInvariant();
        return subCommand switch
        {
            "start" => StartBattle(state, args.Skip(1).ToArray()),
            "status" => DescribeSession(GetSession(state)),
            "active" => ExecuteActiveCommand(state, args.Skip(1).ToArray()),
            "pass" => PassTurn(state),
            "end" => EndBattle(state),
            "help" => GetHelpText(),
            _ => "❌ 未知 battle 子命令。使用 'battle help' 查看帮助。",
        };
    }

    public static object? ReadEffect(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId, string key)
    {
        return InvokeOptional(
            "EonVientiane.EffectModule",
            "EonVientiane.EffectModule.EffectApi",
            "Read",
            state,
            scope,
            ownerId,
            sourceItemId,
            key);
    }

    public static void WriteEffect(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId, string key, object? value)
    {
        _ = InvokeOptional(
            "EonVientiane.EffectModule",
            "EonVientiane.EffectModule.EffectApi",
            "Write",
            state,
            scope,
            ownerId,
            sourceItemId,
            key,
            value);
    }

    public static void DeleteEffect(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId, string key)
    {
        _ = InvokeOptional(
            "EonVientiane.EffectModule",
            "EonVientiane.EffectModule.EffectApi",
            "Delete",
            state,
            scope,
            ownerId,
            sourceItemId,
            key);
    }

    public static string[] ListEffectKeys(IDictionary<string, object> state, string scope, string ownerId, string sourceItemId)
    {
        var result = InvokeOptional(
            "EonVientiane.EffectModule",
            "EonVientiane.EffectModule.EffectApi",
            "ListKeys",
            state,
            scope,
            ownerId,
            sourceItemId);

        return result as string[] ?? Array.Empty<string>();
    }


    private static void EnsureEffectStore(IDictionary<string, object> state)
    {
        _ = InvokeOptional(
            "EonVientiane.EffectModule",
            "EonVientiane.EffectModule.EffectApi",
            "Initialize",
            state);
    }

    private static void ClearEffects(IDictionary<string, object> state)
    {
        _ = InvokeOptional(
            "EonVientiane.EffectModule",
            "EonVientiane.EffectModule.EffectApi",
            "Clear",
            state);
    }
}
