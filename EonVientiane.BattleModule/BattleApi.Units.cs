namespace EonVientiane.BattleModule;

using System.Collections;
using System.Reflection;

public static partial class BattleApi
{
    private sealed record OpponentAccountSpec(
        string UnitId,
        string DisplayName,
        int Hp,
        int Atkp,
        bool IsLocalControlled,
        bool IsLoadoutVisible,
        List<string> PreferredItemNames,
        string SourceMode,
        Type? ControllerRuntimeType);

    private static BattleUnit BuildUnitFromState(IDictionary<string, object> state, string unitId, string displayName, string sideId, string sideName)
    {
        var equippedItems = ReadEquippedItems(state);
        var discoveredItems = DiscoverItemModules();
        var loadout = new List<BattleItemDescriptor>();
        var publicValues = new Dictionary<string, int>(StringComparer.Ordinal);

        var matchedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var equipped in equippedItems)
        {
            var match = discoveredItems.FirstOrDefault(x =>
                x.ItemId.Equals(equipped.Id, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Equals(equipped.Name, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                continue;
            }

            loadout.Add(match);
            matchedNames.Add(equipped.Name);
        }

        return new BattleUnit(
            unitId,
            displayName,
            sideId,
            sideName,
            publicValues,
            loadout,
            IsLocalControlled: true,
            IsLoadoutVisible: true,
            ControllerRuntimeType: null);
    }

    private static BattleUnit BuildUnitFromSpec(OpponentAccountSpec spec, List<BattleItemDescriptor> discoveredItems, string sideId, string sideName)
    {
        var publicValues = new Dictionary<string, int>(StringComparer.Ordinal);
        if (spec.Hp > 0)
        {
            publicValues["HP"] = spec.Hp;
        }

        if (spec.Atkp > 0)
        {
            publicValues["ATKP"] = spec.Atkp;
        }

        var loadout = new List<BattleItemDescriptor>();
        foreach (var name in spec.PreferredItemNames)
        {
            var found = discoveredItems.FirstOrDefault(x =>
                x.ItemId.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                loadout.Add(found);
            }
        }

        return new BattleUnit(
            spec.UnitId,
            spec.DisplayName,
            sideId,
            sideName,
            publicValues,
            loadout,
            spec.IsLocalControlled,
            spec.IsLoadoutVisible,
            spec.ControllerRuntimeType);
    }

    private static BattleUnit CloneUnit(BattleUnit source, string newUnitId, string newDisplayName, string sideId, string sideName, bool? isLocalControlled = null, bool? isLoadoutVisible = null)
    {
        return new BattleUnit(
            newUnitId,
            newDisplayName,
            sideId,
            sideName,
            new Dictionary<string, int>(source.PublicValues, StringComparer.Ordinal),
            source.Loadout.Select(x => x with { }).ToList(),
            IsLocalControlled: isLocalControlled ?? source.IsLocalControlled,
            IsLoadoutVisible: isLoadoutVisible ?? source.IsLoadoutVisible,
            ControllerRuntimeType: source.ControllerRuntimeType);
    }

    private static OpponentAccountSpec ResolveOpponentAccountSpec(IDictionary<string, object> state, string mode)
    {
        if (mode.Equals("level", StringComparison.OrdinalIgnoreCase) &&
            TryReadLevelEnemyAccount(state, out var levelOpponent))
        {
            return levelOpponent;
        }

        if (mode.Equals("pvp", StringComparison.OrdinalIgnoreCase) &&
            TryReadRemoteOpponentAccount(state, out var remoteOpponent))
        {
            return remoteOpponent;
        }

        var fallbackName = mode.Equals("pvp", StringComparison.OrdinalIgnoreCase) ? "对手" : "镜像敌人";
        return new OpponentAccountSpec(
            UnitId: mode.Equals("pvp", StringComparison.OrdinalIgnoreCase) ? "remote" : "mirror",
            DisplayName: fallbackName,
            Hp: 12,
            Atkp: 2,
            IsLocalControlled: !mode.Equals("pvp", StringComparison.OrdinalIgnoreCase),
            IsLoadoutVisible: false,
            PreferredItemNames: new List<string>(),
            SourceMode: mode,
            ControllerRuntimeType: null);
    }

    private static bool TryReadRemoteOpponentAccount(IDictionary<string, object> state, out OpponentAccountSpec spec)
    {
        spec = default!;
        if (!state.TryGetValue("battle.remoteOpponent.public", out var raw) || raw is null)
        {
            return false;
        }

        var dict = raw switch
        {
            string text when TryParseJsonObject(text, out var parsed) => parsed,
            _ => ToDictionary(raw),
        };

        if (dict.Count == 0)
        {
            return false;
        }

        var unitId = GetStringOrDefault(dict, "unitId", "remote");
        var displayName = GetStringOrDefault(dict, "displayName", "远程对手");
        _ = TryGetInt(dict, "HP", out var hp);
        _ = TryGetInt(dict, "ATKP", out var atkp);

        spec = new OpponentAccountSpec(
            UnitId: string.IsNullOrWhiteSpace(unitId) ? "remote" : unitId,
            DisplayName: string.IsNullOrWhiteSpace(displayName) ? "远程对手" : displayName,
            Hp: Math.Max(0, hp),
            Atkp: Math.Max(0, atkp),
            IsLocalControlled: false,
            IsLoadoutVisible: false,
            PreferredItemNames: new List<string>(),
            SourceMode: "pvp",
            ControllerRuntimeType: null);
        return true;
    }

    private static bool TryReadLevelEnemyAccount(IDictionary<string, object> state, out OpponentAccountSpec spec)
    {
        spec = default!;
        if (!TryGetCurrentLevelRuntimeType(state, out var runtimeType))
        {
            return false;
        }

        var getOpponentMethod = runtimeType.GetMethod("GetBattleOpponent", BindingFlags.Public | BindingFlags.Static)
            ?? runtimeType.GetMethod("CreateBattleOpponent", BindingFlags.Public | BindingFlags.Static)
            ?? runtimeType.GetMethod("GetEnemyAccount", BindingFlags.Public | BindingFlags.Static);
        if (getOpponentMethod is null || getOpponentMethod.GetParameters().Length != 0)
        {
            return false;
        }

        var enemyAccountObj = getOpponentMethod.Invoke(null, null);
        if (enemyAccountObj is null)
        {
            return false;
        }

        var enemy = ToDictionary(enemyAccountObj);
        if (enemy.Count == 0)
        {
            return false;
        }

        var unitId = GetStringOrDefault(enemy, "Id", "level.enemy");
        var displayName = GetStringOrDefault(enemy, "Name", "关卡敌人");
        _ = TryGetInt(enemy, "HP", out var hp);
        _ = TryGetInt(enemy, "ATKP", out var atkp);

        var loadoutNames = GetStringList(enemy, "Loadout").ToList();
        if (loadoutNames.Count == 0)
        {
            loadoutNames.AddRange(GetStringList(enemy, "Dice"));
            loadoutNames.AddRange(GetStringList(enemy, "Accessories"));
        }

        spec = new OpponentAccountSpec(
            UnitId: string.IsNullOrWhiteSpace(unitId) ? "level.enemy" : unitId,
            DisplayName: string.IsNullOrWhiteSpace(displayName) ? "关卡敌人" : displayName,
            Hp: Math.Max(0, hp),
            Atkp: Math.Max(0, atkp),
            IsLocalControlled: false,
            IsLoadoutVisible: false,
            PreferredItemNames: loadoutNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SourceMode: "level",
            ControllerRuntimeType: runtimeType);
        return true;
    }

    private static bool TryGetCurrentLevelRuntimeType(IDictionary<string, object> state, out Type runtimeType)
    {
        runtimeType = default!;
        if (!state.TryGetValue("level.current", out var currentLevelObj) || currentLevelObj is not string levelJson || string.IsNullOrWhiteSpace(levelJson))
        {
            return false;
        }

        if (!TryParseJsonObject(levelJson, out var levelDict))
        {
            return false;
        }

        var runtimeAssembly = GetStringOrDefault(levelDict, "AssemblyName", string.Empty);
        var runtimeTypeName = GetStringOrDefault(levelDict, "RuntimeType", string.Empty);
        if (string.IsNullOrWhiteSpace(runtimeAssembly) || string.IsNullOrWhiteSpace(runtimeTypeName))
        {
            return false;
        }

        var type = Type.GetType($"{runtimeTypeName}, {runtimeAssembly}");
        if (type is null)
        {
            return false;
        }

        runtimeType = type;
        return true;
    }

    private static Dictionary<string, object?>? ResolveAutoTurnAction(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        BattleUnit target,
        int currentAttackValue,
        string phase)
    {
        if (actor.IsLocalControlled || actor.ControllerRuntimeType is null)
        {
            return null;
        }

        var context = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["battleId"] = session.BattleId,
            ["battleMode"] = session.BattleMode,
            ["turnNumber"] = session.TurnNumber,
            ["phase"] = phase,
            ["currentAttack"] = currentAttackValue,
            ["actor"] = CreateBattleUnitContext(actor, includeLoadout: true),
            ["target"] = CreateBattleUnitContext(target, includeLoadout: false),
            ["allUnits"] = session.Units.Values.Select(x => CreateBattleUnitContext(x, includeLoadout: false)).ToList(),
        };

        var decisionObj = InvokeVirtualPlayerDecision(actor.ControllerRuntimeType, context);
        var decision = decisionObj is null ? null : ToDictionary(decisionObj);
        return decision is null || decision.Count == 0 ? null : decision;
    }

    private static object? InvokeVirtualPlayerDecision(Type runtimeType, Dictionary<string, object> context)
    {
        var decisionMethodNames = new[]
        {
            "DecideBattleAction",
            "GetBattleAction",
            "DecideAction",
        };

        foreach (var methodName in decisionMethodNames)
        {
            var methods = runtimeType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.Name.Equals(methodName, StringComparison.Ordinal))
                .ToList();
            if (methods.Count == 0)
            {
                continue;
            }

            var withContext = methods.FirstOrDefault(x =>
            {
                var parameters = x.GetParameters();
                return parameters.Length == 1 &&
                       parameters[0].ParameterType.IsAssignableFrom(typeof(Dictionary<string, object>));
            });
            if (withContext is not null)
            {
                return withContext.Invoke(null, new object?[] { context });
            }

            var withoutContext = methods.FirstOrDefault(x => x.GetParameters().Length == 0);
            if (withoutContext is not null)
            {
                return withoutContext.Invoke(null, null);
            }
        }

        return null;
    }

    private static Dictionary<string, object> CreateBattleUnitContext(BattleUnit unit, bool includeLoadout)
    {
        var context = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["unitId"] = unit.UnitId,
            ["displayName"] = unit.DisplayName,
            ["sideId"] = unit.SideId,
            ["sideName"] = unit.SideName,
            ["isLocalControlled"] = unit.IsLocalControlled,
            ["publicValues"] = unit.PublicValues.ToDictionary(x => x.Key, x => (object)x.Value, StringComparer.Ordinal),
        };

        if (includeLoadout)
        {
            context["loadout"] = unit.Loadout
                .Select(x => new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["itemId"] = x.ItemId,
                    ["name"] = x.DisplayName,
                    ["kind"] = x.Kind,
                    ["isDice"] = x.IsDice,
                    ["supportsActive"] = x.SupportsActive,
                    ["supportsPassive"] = x.SupportsPassive,
                })
                .ToList();
        }

        return context;
    }

    private static List<InventoryEquipmentSnapshot> ReadEquippedItems(IDictionary<string, object> state)
    {
        if (!state.TryGetValue("inventory.equippedBySlot", out var equippedObj) || equippedObj is not IDictionary rawDict)
        {
            return new List<InventoryEquipmentSnapshot>();
        }

        var list = new List<InventoryEquipmentSnapshot>();
        foreach (DictionaryEntry entry in rawDict)
        {
            if (entry.Value is null)
            {
                continue;
            }

            var item = InventoryEquipmentSnapshot.FromObject(entry.Value);
            if (item is not null)
            {
                list.Add(item);
            }
        }

        return list;
    }

    private static List<BattleItemDescriptor> DiscoverItemModules()
    {
        var descriptors = new List<BattleItemDescriptor>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.StartsWith("EonVientiane", StringComparison.Ordinal))
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

                var metadataMethod = type.GetMethod("GetMetadata", BindingFlags.Public | BindingFlags.Static);
                if (metadataMethod is null)
                {
                    continue;
                }

                var metadataObject = metadataMethod.GetParameters().Length == 0
                    ? metadataMethod.Invoke(null, null)
                    : null;
                var metadata = ToDictionary(metadataObject);
                if (metadata.Count == 0)
                {
                    continue;
                }

                var kind = GetStringOrDefault(metadata, "kind", string.Empty);
                if (!kind.Equals("Accessory", StringComparison.OrdinalIgnoreCase) &&
                    !kind.Equals("Dice", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var itemId = GetStringOrDefault(metadata, "itemId", type.FullName ?? type.Name);
                var name = GetStringOrDefault(metadata, "name", itemId);
                var diceModes = GetStringList(metadata, "diceModes");
                var isDice = kind.Equals("Dice", StringComparison.OrdinalIgnoreCase);
                var isAccessory = kind.Equals("Accessory", StringComparison.OrdinalIgnoreCase);
                var supportsActive = isDice && (diceModes.Count == 0 || diceModes.Contains("AD", StringComparer.OrdinalIgnoreCase));
                var supportsPassive = isDice && diceModes.Contains("PD", StringComparer.OrdinalIgnoreCase);

                descriptors.Add(new BattleItemDescriptor(
                    ItemId: itemId,
                    DisplayName: name,
                    Kind: kind,
                    IsAccessory: isAccessory,
                    IsDice: isDice,
                    SupportsActive: supportsActive,
                    SupportsPassive: supportsPassive,
                    RuntimeType: type));
            }
        }

        return descriptors;
    }

    private static string GetPlayerName(IDictionary<string, object> state)
    {
        return state.TryGetValue("player.name", out var nameObj) && nameObj is string playerName && !string.IsNullOrWhiteSpace(playerName)
            ? playerName
            : "玩家";
    }
}