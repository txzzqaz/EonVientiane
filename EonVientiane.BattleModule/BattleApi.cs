namespace EonVientiane.BattleModule;

using System.Collections;

public static partial class BattleApi
{
    private const string SessionStateKey = "battle.session";
    private const string LastCompletedRecordStateKey = "battle.lastCompletedRecord";
    private const string BattleCommand = "battle";

    public static bool CanHandleCommand(string command)
    {
        return command.Equals(BattleCommand, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetHelpText()
    {
        return "battle status\n  查看当前战斗状态\nbattle active ...\n  主动回合: battle active <目标> <主动骰子名>\n  被动回合: battle active <被动骰子名>\nbattle pass\n  主动回合: 跳过当前回合\n  被动回合: 不使用被动骰，直接将 ATKP 转化为伤害\nbattle end\n  结束当前战斗\n\n说明: 战斗开始应由业务模块调用 BattleApi.StartSession(...)，例如 Level 模块在进入关卡时发起。";
    }

    public static string StartSession(IDictionary<string, object> state, string mode, string? formation = null)
    {
        var resolvedMode = string.IsNullOrWhiteSpace(mode) ? "level" : mode.Trim();
        var resolvedFormation = string.IsNullOrWhiteSpace(formation) ? "1v1" : formation.Trim();
        return StartBattle(state, new[] { resolvedMode, resolvedFormation });
    }

    public static void Initialize(IDictionary<string, object> state)
    {
        EnsureEffectStore(state);
        state[SessionStateKey] = null!;
        state[LastCompletedRecordStateKey] = null!;
    }

    public static string? ConsumeLastCompletedRecord(IDictionary<string, object> state)
    {
        if (!state.TryGetValue(LastCompletedRecordStateKey, out var value) || value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        state[LastCompletedRecordStateKey] = null!;
        return text;
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
            "start" => "❌ battle start 已禁用。请由业务模块调用 BattleApi.StartSession(...) 发起战斗。",
            "status" => DescribeSession(GetSession(state)),
            "active" => ExecuteActiveCommand(state, args.Skip(1).ToArray()),
            "pass" => PassTurn(state),
            "end" => EndBattle(state),
            "help" => GetHelpText(),
            _ => "❌ 未知 battle 子命令。使用 'battle help' 查看帮助。",
        };
    }

    public static object? InvokeAssumedItemFunctionHook(
        IDictionary<string, object> state,
        string assumedItemId,
        string methodName,
        object?[] args,
        string? assumedItemName = null,
        string? assumedKind = null)
    {
        var session = GetSession(state);
        if (session is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(assumedItemId) || string.IsNullOrWhiteSpace(methodName))
        {
            return null;
        }

        var actor = session.GetCurrentActor();
        return InvokeAssumedFunctionWithHooks(
            state,
            session,
            actor,
            assumedItemId,
            string.IsNullOrWhiteSpace(assumedItemName) ? assumedItemId : assumedItemName,
            string.IsNullOrWhiteSpace(assumedKind) ? "Assumed" : assumedKind,
            methodName,
            args ?? Array.Empty<object?>());
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
