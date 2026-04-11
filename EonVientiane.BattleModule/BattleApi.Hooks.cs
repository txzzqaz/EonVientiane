namespace EonVientiane.BattleModule;

public static partial class BattleApi
{
    private sealed record HookGateDecision(bool ShouldCancel, bool ShouldForcePass, string? Message);
    private sealed record HookInvocationDecision(
        bool ShouldCancel,
        bool ShouldForcePass,
        bool SkipOriginal,
        bool HasResultOverride,
        object? ResultOverride,
        string? Message);

    private static object? InvokeItemFunctionWithHooks(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        BattleUnit target,
        BattleItemDescriptor source,
        string methodName,
        params object?[] args)
    {
        var initialResult = default(object);
        var beforeDecision = RunFunctionHooks(
            state,
            session,
            actor,
            target,
            source,
            methodName,
            "before",
            args,
            initialResult,
            null,
            useGlobalSources: true);

        var resolvedResult = beforeDecision.HasResultOverride
            ? beforeDecision.ResultOverride
            : null;

        if (!beforeDecision.SkipOriginal)
        {
            resolvedResult = InvokeOptional(source.RuntimeType, methodName, args);
        }

        var afterDecision = RunFunctionHooks(
            state,
            session,
            actor,
            target,
            source,
            methodName,
            "after",
            args,
            resolvedResult,
            null,
            useGlobalSources: true);

        return afterDecision.HasResultOverride ? afterDecision.ResultOverride : resolvedResult;
    }

    private static HookGateDecision RunSyntheticFunctionGateHooks(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        string functionName,
        object[] args,
        Dictionary<string, object>? extra)
    {
        var decision = RunFunctionHooks(
            state,
            session,
            actor,
            actor,
            source: null,
            methodName: functionName,
            stage: "before",
            args: args,
            currentResult: null,
            extra: extra,
            useGlobalSources: true);

        return new HookGateDecision(decision.ShouldCancel, decision.ShouldForcePass, decision.Message);
    }

    private static object? InvokeAssumedFunctionWithHooks(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        string assumedItemId,
        string assumedItemName,
        string assumedKind,
        string methodName,
        params object?[] args)
    {
        var assumedTarget = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["itemId"] = assumedItemId,
            ["name"] = assumedItemName,
            ["kind"] = assumedKind,
            ["isAssumed"] = true,
        };

        var before = RunFunctionHooks(
            state,
            session,
            actor,
            actor,
            source: null,
            methodName,
            "before",
            args,
            currentResult: null,
            extra: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["assumedTarget"] = assumedTarget,
            },
            useGlobalSources: true);

        var resolvedResult = before.HasResultOverride ? before.ResultOverride : null;
        var after = RunFunctionHooks(
            state,
            session,
            actor,
            actor,
            source: null,
            methodName,
            "after",
            args,
            resolvedResult,
            extra: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["assumedTarget"] = assumedTarget,
            },
            useGlobalSources: true);

        return after.HasResultOverride ? after.ResultOverride : resolvedResult;
    }

    private static HookInvocationDecision RunFunctionHooks(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        BattleUnit target,
        BattleItemDescriptor? source,
        string methodName,
        string stage,
        object?[] args,
        object? currentResult,
        Dictionary<string, object>? extra,
        bool useGlobalSources)
    {
        var hookSources = CollectHookSources(session, actor, useGlobalSources);
        var shouldCancel = false;
        var shouldForcePass = false;
        var skipOriginal = false;
        var hasResultOverride = false;
        object? resultOverride = null;
        var messages = new List<string>();

        foreach (var hookSource in hookSources)
        {
            var context = CreateHookContext(
                state,
                session,
                actor,
                target,
                source,
                methodName,
                stage,
                args,
                currentResult,
                hookSource,
                extra);

            var hookResult = InvokeOptional(hookSource.Item.RuntimeType, "OnBattleHook", context)
                ?? InvokeOptional(hookSource.Item.RuntimeType, "OnHook", context);
            if (hookResult is null)
            {
                continue;
            }

            var resultDict = ToDictionary(hookResult);
            if (resultDict.Count == 0)
            {
                continue;
            }

            if (TryGetString(resultDict, "message", out var message) && !string.IsNullOrWhiteSpace(message))
            {
                var trimmed = message.Trim();
                messages.Add(trimmed);
                Log(session, $"{hookSource.Item.DisplayName}: {trimmed}");
            }

            if (TryGetBool(resultDict, "cancel", out var cancel) && cancel)
            {
                shouldCancel = true;
            }

            if (TryGetBool(resultDict, "forcePass", out var forcePass) && forcePass)
            {
                shouldForcePass = true;
            }

            if ((TryGetBool(resultDict, "skipOriginal", out var skipFlag) && skipFlag)
                || (TryGetBool(resultDict, "skip", out skipFlag) && skipFlag))
            {
                skipOriginal = true;
            }

            if (resultDict.TryGetValue("result", out var resultObj))
            {
                resultOverride = resultObj;
                hasResultOverride = true;
            }

            if (resultDict.TryGetValue("overrideResult", out var overrideResultObj))
            {
                resultOverride = overrideResultObj;
                hasResultOverride = true;
            }

            ApplyHookEffects(state, hookSource.Owner, hookSource.Item, resultDict);
        }

        if (shouldForcePass)
        {
            shouldCancel = false;
        }

        var joinedMessage = messages.Count == 0 ? null : string.Join(" | ", messages);
        return new HookInvocationDecision(
            shouldCancel,
            shouldForcePass,
            skipOriginal,
            hasResultOverride,
            resultOverride,
            joinedMessage);
    }

    private static Dictionary<string, object> CreateHookContext(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        BattleUnit target,
        BattleItemDescriptor? source,
        string methodName,
        string stage,
        object?[] args,
        object? currentResult,
        HookSource hookSource,
        Dictionary<string, object>? extra)
    {
        var elapsedMs = Math.Max(0, (int)(DateTime.UtcNow - session.TurnStartedAtUtc).TotalMilliseconds);
        var targetCall = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["methodName"] = methodName,
            ["stage"] = stage,
            ["argumentCount"] = args.Length,
            ["result"] = currentResult!,
        };

        if (source is not null)
        {
            targetCall["itemId"] = source.ItemId;
            targetCall["name"] = source.DisplayName;
            targetCall["kind"] = source.Kind;
            targetCall["runtimeType"] = source.RuntimeType.FullName ?? source.RuntimeType.Name;
            targetCall["assembly"] = source.RuntimeType.Assembly.GetName().Name ?? string.Empty;
        }

        var context = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["hook"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["eventName"] = "function.invoke",
                ["stage"] = stage,
                ["elapsedMs"] = elapsedMs,
            },
            ["targetCall"] = targetCall,
            ["arguments"] = args,
            ["sharedState"] = state,
            ["battle"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["battleId"] = session.BattleId,
                ["turnNumber"] = session.TurnNumber,
                ["currentActorId"] = session.CurrentActorId,
                ["turnStartedAtUtc"] = session.TurnStartedAtUtc,
            },
            ["actor"] = BuildUnitContext(actor, includeLoadout: true),
            ["target"] = BuildUnitContext(target, includeLoadout: false),
            ["hookSource"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["itemId"] = hookSource.Item.ItemId,
                ["name"] = hookSource.Item.DisplayName,
                ["kind"] = hookSource.Item.Kind,
                ["ownerId"] = hookSource.Owner.UnitId,
                ["ownerName"] = hookSource.Owner.DisplayName,
            },
        };

        if (extra is not null && extra.Count > 0)
        {
            context["extra"] = new Dictionary<string, object>(extra, StringComparer.Ordinal);
        }

        return context;
    }

    private sealed record HookSource(BattleUnit Owner, BattleItemDescriptor Item);

    private static List<HookSource> CollectHookSources(BattleSession session, BattleUnit actor, bool useGlobalSources)
    {
        var dedupe = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<HookSource>();

        IEnumerable<BattleUnit> sourceUnits = useGlobalSources
            ? session.Units.Values
            : new[] { actor };

        foreach (var unit in sourceUnits)
        {
            foreach (var item in unit.Loadout)
            {
                var key = $"{unit.UnitId}|{item.ItemId}|{item.RuntimeType.FullName ?? item.RuntimeType.Name}";
                if (!dedupe.Add(key))
                {
                    continue;
                }

                list.Add(new HookSource(unit, item));
            }
        }

        return list;
    }

    private static HookGateDecision RunCommandGateHooks(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        string command,
        string[] args)
    {
        return RunSyntheticFunctionGateHooks(
            state,
            session,
            actor,
            $"BattleCommand.{command}",
            args.Cast<object>().ToArray(),
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["command"] = command,
                ["args"] = args,
            });
    }

    private static void ApplyHookEffects(
        IDictionary<string, object> state,
        BattleUnit actor,
        BattleItemDescriptor source,
        Dictionary<string, object?> dict)
    {
        if (dict.TryGetValue("effects", out var effectsObj) && effectsObj is IEnumerable<object?> effects)
        {
            foreach (var effectItem in effects)
            {
                var effectDict = ToDictionary(effectItem);
                if (!TryGetString(effectDict, "scope", out var scope) || !TryGetString(effectDict, "key", out var effectKey))
                {
                    continue;
                }

                var effectOwnerId = GetStringOrDefault(effectDict, "ownerId", actor.UnitId);
                var sourceItemId = GetStringOrDefault(effectDict, "sourceItemId", source.ItemId);
                var effectValue = effectDict.TryGetValue("value", out var v) ? v : null;
                WriteEffect(state, scope, effectOwnerId, sourceItemId, effectKey, effectValue);
            }
        }
    }

    private static bool TryGetBool(Dictionary<string, object?> dict, string key, out bool value)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
        {
            value = false;
            return false;
        }

        switch (obj)
        {
            case bool b:
                value = b;
                return true;
            default:
                return bool.TryParse(obj.ToString(), out value);
        }
    }
}