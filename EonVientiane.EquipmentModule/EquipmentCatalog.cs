namespace EonVientiane.EquipmentModule;

using System.Reflection;
using System.Text.Json;

public static class EquipmentApi
{
    public static bool CanHandleCommand(string command)
    {
        return command.Equals("equip", StringComparison.OrdinalIgnoreCase)
            || command.Equals("unequip", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExecuteCommand(IDictionary<string, object> state, string command, string[] args)
    {
        switch (command.ToLowerInvariant())
        {
            case "equip":
                if (args.Length == 0)
                {
                    return "❌ 请指定要穿戴的装备名称. 如: equip D6";
                }

                return InvokeInventory("Equip", state, string.Join(' ', args)) as string
                    ?? "❌ 背包模块不可用";

            case "unequip":
                if (args.Length == 0)
                {
                    return "❌ 请指定要卸下的装备名称. 如: unequip D6";
                }

                return InvokeInventory("Unequip", state, string.Join(' ', args)) as string
                    ?? "❌ 背包模块不可用";

            default:
                return null;
        }
    }

    public static string GetHelpText()
    {
        return "equip <物品名>\n  穿戴装备\nunequip <物品名>\n  卸下装备";
    }

    public static string GetStarterEquipmentsJson()
    {
        var equipments = DiscoverStarterEquipments();
        return JsonSerializer.Serialize(equipments);
    }

    private static List<Dictionary<string, object>> DiscoverStarterEquipments()
    {
        var results = new List<Dictionary<string, object>>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.StartsWith("EonVientiane.Item.", StringComparison.Ordinal))
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
                if (metadataMethod is null || metadataMethod.GetParameters().Length != 0)
                {
                    continue;
                }

                var metadata = ToDictionary(metadataMethod.Invoke(null, null));
                if (metadata.Count == 0)
                {
                    continue;
                }

                var kind = GetString(metadata, "kind");
                if (!kind.Equals("Accessory", StringComparison.OrdinalIgnoreCase) &&
                    !kind.Equals("Dice", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var itemId = GetString(metadata, "itemId");
                var name = GetString(metadata, "name");
                if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var eq = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["Id"] = itemId,
                    ["Name"] = name,
                    ["Slot"] = kind.Equals("Dice", StringComparison.OrdinalIgnoreCase) ? "dice" : "accessory",
                    ["Kind"] = kind,
                };

                if (kind.Equals("Accessory", StringComparison.OrdinalIgnoreCase))
                {
                    eq["AccessorySlotCost"] = GetInt(metadata, "accessorySlotCost", 1);
                }

                results.Add(eq);
            }
        }

        return results
            .GroupBy(x => x["Id"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => g.First())
            .OrderBy(x => x["Name"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, object> ToDictionary(object? value)
    {
        if (value is Dictionary<string, object> dict)
        {
            return new Dictionary<string, object>(dict, StringComparer.Ordinal);
        }

        if (value is IDictionary<string, object?> dictNullable)
        {
            return dictNullable.ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.Ordinal);
        }

        return new Dictionary<string, object>(StringComparer.Ordinal);
    }

    private static string GetString(Dictionary<string, object> map, string key)
    {
        return map.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static int GetInt(Dictionary<string, object> map, string key, int defaultValue)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return value switch
        {
            int i => i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
            _ when int.TryParse(value.ToString(), out var parsed) => parsed,
            _ => defaultValue,
        };
    }

    private static object? InvokeInventory(string methodName, params object[] args)
    {
        var type = Type.GetType("EonVientiane.InventoryModule.InventoryApi, EonVientiane.InventoryModule");
        if (type is null)
        {
            return null;
        }

        var methods = type.GetMethods().Where(m => m.Name == methodName).ToList();
        var target = methods.FirstOrDefault(m => m.GetParameters().Length == args.Length) ?? methods.FirstOrDefault();
        if (target is null)
        {
            return null;
        }

        return target.Invoke(null, args);
    }
}
