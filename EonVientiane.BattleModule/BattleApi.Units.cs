namespace EonVientiane.BattleModule;

using System.Collections;
using System.Reflection;

public static partial class BattleApi
{
    private static BattleUnit BuildUnitFromState(IDictionary<string, object> state, string unitId, string displayName, bool allowLegacyFallback)
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

        if (allowLegacyFallback)
        {
            var hp = equippedItems.Sum(x => Math.Max(0, x.ArmorValue));
            var atkp = equippedItems.Sum(x => Math.Max(0, x.AttackBonus));
            if (hp > 0)
            {
                publicValues["HP"] = hp;
            }

            if (atkp > 0)
            {
                publicValues["ATKP"] = atkp;
            }
        }

        return new BattleUnit(unitId, displayName, publicValues, loadout);
    }

    private static BattleUnit CloneUnit(BattleUnit source, string newUnitId, string newDisplayName)
    {
        return new BattleUnit(
            newUnitId,
            newDisplayName,
            new Dictionary<string, int>(source.PublicValues, StringComparer.Ordinal),
            source.Loadout.Select(x => x with { }).ToList());
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