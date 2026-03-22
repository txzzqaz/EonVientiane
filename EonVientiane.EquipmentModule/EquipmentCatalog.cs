namespace EonVientiane.EquipmentModule;

using System.Collections;
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

    public static IDictionary<string, object> GetGuiContentDefinition(IDictionary<string, object> state)
    {
        var snapshot = InvokeInventory("GetEquipmentGuiState", state) as IDictionary<string, object>;
        if (snapshot is null)
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["ModuleId"] = "equipment",
                ["Title"] = "装备管理",
                ["Sections"] = new List<object>()
            };
        }

        var availableDice = ReadEquipmentList(snapshot, "AvailableDice");
        var availableAccessories = ReadEquipmentList(snapshot, "AvailableAccessories");
        var equippedDice = ReadEquipmentList(snapshot, "EquippedDice");
        var equippedAccessories = ReadEquipmentList(snapshot, "EquippedAccessories");
        var maxDice = ReadInt(snapshot, "MaxDice", 8);
        var maxAccessorySlots = ReadInt(snapshot, "MaxAccessorySlots", 12);
        var usedAccessorySlots = ReadInt(snapshot, "UsedAccessorySlots", 0);

        var sections = new List<object>
        {
            CreateSection($"未装备骰子 ({availableDice.Count})", availableDice.Select(CreateEquipItem).ToList<object>()),
            CreateSection($"未装备饰品 ({availableAccessories.Count})", availableAccessories.Select(CreateEquipItem).ToList<object>()),
            CreateSection($"骰子位 {equippedDice.Count}/{maxDice}", BuildDiceSlotItems(equippedDice, maxDice)),
            CreateSection($"饰品位 {usedAccessorySlots}/{maxAccessorySlots}", BuildAccessorySlotItems(usedAccessorySlots, maxAccessorySlots)),
            CreateSection($"已装备饰品 ({equippedAccessories.Count})", equippedAccessories.Select(CreateUnequipItem).ToList<object>())
        };

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModuleId"] = "equipment",
            ["Title"] = "装备管理",
            ["Sections"] = sections
        };
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
                    ["Description"] = GetString(metadata, "description"),
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

    private static List<Dictionary<string, object>> ReadEquipmentList(IDictionary<string, object> snapshot, string key)
    {
        var result = new List<Dictionary<string, object>>();
        if (!snapshot.TryGetValue(key, out var raw) || raw is not IEnumerable enumerable)
        {
            return result;
        }

        foreach (var item in enumerable)
        {
            if (item is Dictionary<string, object> dict)
            {
                result.Add(new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase));
            }
            else if (item is IDictionary<string, object> map)
            {
                result.Add(new Dictionary<string, object>(map, StringComparer.OrdinalIgnoreCase));
            }
        }

        return result;
    }

    private static int ReadInt(IDictionary<string, object> snapshot, string key, int defaultValue)
    {
        if (!snapshot.TryGetValue(key, out var value) || value is null)
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

    private static IDictionary<string, object> CreateSection(string title, List<object> items)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Title"] = title,
            ["Items"] = items
        };
    }

    private static IDictionary<string, object> CreateEquipItem(Dictionary<string, object> eq)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryText"] = GetString(eq, "Name"),
            ["SecondaryText"] = BuildEquipmentDescription(eq),
            ["Badge"] = GetKindLabel(eq),
            ["ActionText"] = "装备",
            ["ActionCommand"] = $"equip {GetString(eq, "Name")}"
        };
    }

    private static IDictionary<string, object> CreateUnequipItem(Dictionary<string, object> eq)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryText"] = GetString(eq, "Name"),
            ["SecondaryText"] = BuildEquipmentDescription(eq),
            ["Badge"] = GetKindLabel(eq),
            ["ActionText"] = "卸下",
            ["ActionCommand"] = $"unequip {GetString(eq, "Name")}"
        };
    }

    private static List<object> BuildDiceSlotItems(List<Dictionary<string, object>> equippedDice, int maxDice)
    {
        var result = new List<object>();
        for (var i = 0; i < maxDice; i++)
        {
            if (i < equippedDice.Count)
            {
                var eq = equippedDice[i];
                result.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PrimaryText"] = $"骰子位 {i + 1}",
                    ["SecondaryText"] = GetString(eq, "Name") + BuildSecondarySuffix(eq),
                    ["Badge"] = "已装备",
                    ["ActionText"] = "卸下",
                    ["ActionCommand"] = $"unequip {GetString(eq, "Name")}"
                });
            }
            else
            {
                result.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PrimaryText"] = $"骰子位 {i + 1}",
                    ["SecondaryText"] = "空",
                    ["Badge"] = "空位"
                });
            }
        }

        return result;
    }

    private static List<object> BuildAccessorySlotItems(int usedAccessorySlots, int maxAccessorySlots)
    {
        var result = new List<object>();
        for (var i = 0; i < maxAccessorySlots; i++)
        {
            result.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["PrimaryText"] = (i + 1).ToString(),
                ["Badge"] = i < usedAccessorySlots ? "占用" : "空"
            });
        }

        return result;
    }

    private static string GetKindLabel(Dictionary<string, object> eq)
    {
        var kind = GetString(eq, "Kind");
        return kind.Equals("Dice", StringComparison.OrdinalIgnoreCase) ? "骰子" : "饰品";
    }

    private static string BuildEquipmentDescription(Dictionary<string, object> eq)
    {
        var parts = new List<string>();
        var description = GetString(eq, "Description");
        if (!string.IsNullOrWhiteSpace(description))
        {
            parts.Add(description);
        }

        if (!GetKindLabel(eq).Equals("骰子", StringComparison.Ordinal))
        {
            parts.Add($"槽位消耗: {GetInt(eq, "AccessorySlotCost", 1)}");
        }

        return string.Join("\n", parts);
    }

    private static string BuildSecondarySuffix(Dictionary<string, object> eq)
    {
        var description = GetString(eq, "Description");
        return string.IsNullOrWhiteSpace(description) ? string.Empty : $"\n{description}";
    }
}
