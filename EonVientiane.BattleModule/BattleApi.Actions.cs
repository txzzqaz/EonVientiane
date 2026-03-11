namespace EonVientiane.BattleModule;

using System.Collections;

public static partial class BattleApi
{
    private static int MergeActionResult(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit owner,
        BattleUnit target,
        BattleItemDescriptor source,
        int pendingAttack,
        int currentValue,
        object? result,
        bool isDamageResult)
    {
        if (result is null)
        {
            return currentValue;
        }

        if (result is string text)
        {
            if (TryParseJsonObject(text, out var jsonDict))
            {
                return MergeActionResult(state, session, owner, target, source, pendingAttack, currentValue, jsonDict, isDamageResult);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                Log(session, $"{source.DisplayName}: {text.Trim()}");
            }

            return currentValue;
        }

        var dict = ToDictionary(result);
        if (dict.Count == 0)
        {
            return currentValue;
        }

        if (TryGetString(dict, "message", out var message) && !string.IsNullOrWhiteSpace(message))
        {
            Log(session, $"{source.DisplayName}: {message}");
        }

        ApplyPublicValuePatch(owner.PublicValues, dict, prefix: "owner.");
        ApplyPublicValuePatch(target.PublicValues, dict, prefix: "target.");

        if (dict.TryGetValue("effects", out var effectsObj) && effectsObj is IEnumerable effectItems)
        {
            foreach (var effectItem in effectItems)
            {
                var effectDict = ToDictionary(effectItem);
                if (!TryGetString(effectDict, "scope", out var scope) ||
                    !TryGetString(effectDict, "key", out var effectKey))
                {
                    continue;
                }

                var effectOwnerId = GetStringOrDefault(effectDict, "ownerId", owner.UnitId);
                var sourceItemId = GetStringOrDefault(effectDict, "sourceItemId", source.ItemId);
                var effectValue = effectDict.TryGetValue("value", out var v) ? v : null;
                WriteEffect(state, scope, effectOwnerId, sourceItemId, effectKey, effectValue);
            }
        }

        var overrideKeys = isDamageResult
            ? new[] { "resolvedDamage", "damage", "value" }
            : new[] { "pendingAttack", "attack", "value", "ATKP" };

        foreach (var key in overrideKeys)
        {
            if (TryGetInt(dict, key, out var value))
            {
                return Math.Max(0, value);
            }
        }

        return currentValue;
    }

    private static void ApplyPublicValuePatch(Dictionary<string, int> publicValues, Dictionary<string, object?> dict, string prefix)
    {
        foreach (var key in new[] { "HP", "ATKP" })
        {
            if (TryGetInt(dict, prefix + key, out var value) || TryGetInt(dict, prefix + key.ToLowerInvariant(), out value))
            {
                publicValues[key] = value;
            }
        }
    }

    private static Dictionary<string, object> CreateActionContext(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit owner,
        BattleUnit target,
        BattleItemDescriptor source,
        string phase,
        int pendingAttack,
        int resolvedDamage = 0)
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["phase"] = phase,
            ["sharedState"] = state,
            ["battle"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["battleId"] = session.BattleId,
                ["turnNumber"] = session.TurnNumber,
                ["currentActorId"] = session.CurrentActorId,
            },
            ["owner"] = BuildUnitContext(owner, includeLoadout: true),
            ["target"] = BuildUnitContext(target, includeLoadout: false),
            ["source"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["itemId"] = source.ItemId,
                ["name"] = source.DisplayName,
                ["kind"] = source.Kind,
            },
            ["pendingAttack"] = pendingAttack,
            ["resolvedDamage"] = resolvedDamage,
        };
    }

    private static Dictionary<string, object> BuildUnitContext(BattleUnit unit, bool includeLoadout)
    {
        var context = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["unitId"] = unit.UnitId,
            ["displayName"] = unit.DisplayName,
            ["sideId"] = unit.SideId,
            ["sideName"] = unit.SideName,
            ["publicValues"] = new Dictionary<string, object>(unit.PublicValues.ToDictionary(x => x.Key, x => (object)x.Value), StringComparer.Ordinal),
            ["loadoutVisible"] = includeLoadout,
        };

        if (includeLoadout)
        {
            context["loadout"] = unit.Loadout
                .Select(x => new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["itemId"] = x.ItemId,
                    ["name"] = x.DisplayName,
                    ["kind"] = x.Kind,
                    ["supportsActive"] = x.SupportsActive,
                    ["supportsPassive"] = x.SupportsPassive,
                })
                .ToList();
        }

        return context;
    }

    private static BattleItemDescriptor? SelectActiveDice(BattleUnit actor, string? requestedDiceName)
    {
        var candidates = actor.Loadout.Where(x => x.IsDice && x.SupportsActive).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(requestedDiceName))
        {
            return null;
        }

        return candidates.FirstOrDefault(x =>
            x.DisplayName.Equals(requestedDiceName, StringComparison.OrdinalIgnoreCase) ||
            x.ItemId.Equals(requestedDiceName, StringComparison.OrdinalIgnoreCase));
    }

    private static BattleItemDescriptor? SelectPassiveDice(BattleUnit actor, string? requestedDiceName)
    {
        var candidates = actor.Loadout.Where(x => x.IsDice && x.SupportsPassive).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(requestedDiceName))
        {
            return null;
        }

        return candidates.FirstOrDefault(x =>
            x.DisplayName.Equals(requestedDiceName, StringComparison.OrdinalIgnoreCase) ||
            x.ItemId.Equals(requestedDiceName, StringComparison.OrdinalIgnoreCase));
    }
}