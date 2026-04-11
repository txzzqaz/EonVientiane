namespace EonVientiane.InventoryModule;

using System.Collections;
using System.Text;
using System.Text.Json;

public static class InventoryApi
{
    private const int MaxEquippedDice = 8;
    private const int MaxAccessorySlots = 12;

    public static bool CanHandleCommand(string command)
    {
        return command.Equals("inv", StringComparison.OrdinalIgnoreCase)
            || command.Equals("inventory", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExecuteCommand(IDictionary<string, object> state, string command, string[] args)
    {
        return command.ToLowerInvariant() switch
        {
            "inv" or "inventory" => ViewInventory(state),
            _ => null,
        };
    }

    public static string GetHelpText()
    {
        return "inv / inventory\n  查看背包和已穿戴装备";
    }

    public static void Initialize(IDictionary<string, object> state)
    {
        var equipmentsJson = InvokeOptional("EonVientiane.EquipmentModule", "EonVientiane.EquipmentModule.EquipmentApi", "GetStarterEquipmentsJson") as string;

        state["inventory.items"] = new List<ItemEntry>();
        state["inventory.equipments"] = DeserializeEquipments(equipmentsJson);
        state["inventory.equippedBySlot"] = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
    }

    public static string Equip(IDictionary<string, object> state, string itemName)
    {
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);

        var eq = equipments.FirstOrDefault(x => GetString(x, "Name").Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (eq is null)
        {
            return $"❌ 无法穿戴 '{itemName}'. 请检查物品是否存在且为装备.";
        }

        var equippedItems = equippedBySlot.Values.ToList();
        if (IsDice(eq))
        {
            var equippedDiceCount = equippedItems.Count(IsDice);
            if (equippedDiceCount >= MaxEquippedDice)
            {
                return $"❌ 最多只能装备 {MaxEquippedDice} 个骰子。";
            }
        }
        else if (IsAccessory(eq))
        {
            var usedAccessorySlots = equippedItems.Where(IsAccessory).Sum(GetAccessorySlotCost);
            var nextUsedAccessorySlots = usedAccessorySlots + GetAccessorySlotCost(eq);
            if (nextUsedAccessorySlots > MaxAccessorySlots)
            {
                return $"❌ 饰品槽不足。当前已用 {usedAccessorySlots}/{MaxAccessorySlots}，装备后将达到 {nextUsedAccessorySlots}/{MaxAccessorySlots}。";
            }
        }

        var equipKey = BuildEquipKey(eq, equippedBySlot);
        equippedBySlot[equipKey] = eq;
        equipments.Remove(eq);
        return $"✓ 已穿戴: {itemName}";
    }

    public static string Unequip(IDictionary<string, object> state, string itemName)
    {
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);

        var eq = equippedBySlot.Values.FirstOrDefault(x =>
            GetString(x, "Name").Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (eq is null)
        {
            return $"❌ 无法卸下 '{itemName}'. 请检查是否已穿戴此装备.";
        }

        var equippedKey = equippedBySlot.First(x => ReferenceEquals(x.Value, eq)).Key;
        equippedBySlot.Remove(equippedKey);
        equipments.Add(eq);
        return $"✓ 已卸下: {itemName}";
    }

    public static string ViewInventory(IDictionary<string, object> state)
    {
        var items = GetItems(state);
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);

        var sb = new StringBuilder();
        sb.AppendLine($"=== 背包 ({items.Count + equipments.Count}) ===");

        foreach (var item in items)
        {
            var text = item.Quantity > 1 ? $"{item.Name} x{item.Quantity}" : item.Name;
            sb.AppendLine($"  • {text}");
        }

        foreach (var eq in equipments)
        {
            AppendEquipmentDescription(sb, eq);
        }

        if (equippedBySlot.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"=== 已穿戴 ({equippedBySlot.Count}) ===");
            foreach (var e in equippedBySlot.Values)
            {
                AppendEquipmentDescription(sb, e);
            }
        }

        return sb.ToString();
    }

    public static string GetStatusAddon(IDictionary<string, object> state)
    {
        var equippedBySlot = GetEquippedBySlot(state);
        var equippedItems = equippedBySlot.Values.ToList();
        var equippedDiceCount = equippedItems.Count(IsDice);
        var usedAccessorySlots = equippedItems.Where(IsAccessory).Sum(GetAccessorySlotCost);
        return $"已装备: 骰子 {equippedDiceCount}/{MaxEquippedDice}，饰品槽 {usedAccessorySlots}/{MaxAccessorySlots}";
    }

    public static IDictionary<string, object> GetGuiContentDefinition(IDictionary<string, object> state)
    {
        var items = GetItems(state);
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);

        var sections = new List<object>
        {
            CreateGuiSection(
                $"普通物品 ({items.Count})",
                items.Select(CreateGuiItemFromItemEntry).ToList<object>()),

            CreateGuiSection(
                $"背包装备 ({equipments.Count})",
                equipments.Select(CreateGuiItemFromEquipment).ToList<object>()),

            CreateGuiSection(
                $"已穿戴 ({equippedBySlot.Count})",
                equippedBySlot.Values.Select(CreateGuiItemFromEquipment).ToList<object>())
        };

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModuleId"] = "inventory",
            ["Title"] = "背包列表",
            ["Sections"] = sections
        };
    }

    public static IDictionary<string, object> GetEquipmentGuiState(IDictionary<string, object> state)
    {
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);
        var equippedItems = equippedBySlot.Values.ToList();

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["AvailableDice"] = equipments.Where(IsDice).Select(CloneEquipment).ToList<object>(),
            ["AvailableAccessories"] = equipments.Where(IsAccessory).Select(CloneEquipment).ToList<object>(),
            ["EquippedDice"] = equippedItems.Where(IsDice).Select(CloneEquipment).ToList<object>(),
            ["EquippedAccessories"] = equippedItems.Where(IsAccessory).Select(CloneEquipment).ToList<object>(),
            ["MaxDice"] = MaxEquippedDice,
            ["MaxAccessorySlots"] = MaxAccessorySlots,
            ["UsedAccessorySlots"] = equippedItems.Where(IsAccessory).Sum(GetAccessorySlotCost)
        };
    }

    private static string BuildEquipKey(Dictionary<string, object> eq, Dictionary<string, Dictionary<string, object>> equippedBySlot)
    {
        var id = GetString(eq, "Id");
        var name = GetString(eq, "Name");
        var slot = GetString(eq, "Slot");
        var baseKey = !string.IsNullOrWhiteSpace(id)
            ? id
            : $"{name}@{slot}";

        if (!equippedBySlot.ContainsKey(baseKey))
        {
            return baseKey;
        }

        var index = 2;
        while (equippedBySlot.ContainsKey($"{baseKey}#{index}"))
        {
            index++;
        }

        return $"{baseKey}#{index}";
    }

    private static bool IsDice(Dictionary<string, object> eq)
    {
        var kind = GetString(eq, "Kind");
        var slot = GetString(eq, "Slot");
        return kind.Equals("Dice", StringComparison.OrdinalIgnoreCase)
            || slot.Equals("dice", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAccessory(Dictionary<string, object> eq)
    {
        return !IsDice(eq);
    }

    private static int GetAccessorySlotCost(Dictionary<string, object> eq)
    {
        return GetInt(eq, "AccessorySlotCost", 1);
    }

    private static object? InvokeOptional(string assemblyName, string typeName, string methodName, params object[] args)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (type is null)
        {
            return null;
        }

        var method = type.GetMethod(methodName);
        if (method is null)
        {
            return null;
        }

        return method.Invoke(null, args);
    }

    private static List<ItemEntry> GetItems(IDictionary<string, object> state)
    {
        if (!state.TryGetValue("inventory.items", out var itemsObj) || itemsObj is not List<ItemEntry> items)
        {
            items = new List<ItemEntry>();
            state["inventory.items"] = items;
        }

        return items;
    }

    private static List<Dictionary<string, object>> GetEquipments(IDictionary<string, object> state)
    {
        if (state.TryGetValue("inventory.equipments", out var eqObj) && eqObj is List<Dictionary<string, object>> typed)
        {
            return typed;
        }

        var migrated = new List<Dictionary<string, object>>();
        if (state.TryGetValue("inventory.equipments", out eqObj) && eqObj is IEnumerable enumerable)
        {
            foreach (var value in enumerable)
            {
                if (value is null)
                {
                    continue;
                }

                if (TryConvertEquipmentObject(value, out var converted))
                {
                    migrated.Add(converted);
                }
            }
        }

        state["inventory.equipments"] = migrated;
        return migrated;
    }

    private static Dictionary<string, Dictionary<string, object>> GetEquippedBySlot(IDictionary<string, object> state)
    {
        if (state.TryGetValue("inventory.equippedBySlot", out var slotObj) && slotObj is Dictionary<string, Dictionary<string, object>> typed)
        {
            return typed;
        }

        var migrated = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        if (state.TryGetValue("inventory.equippedBySlot", out slotObj) && slotObj is IDictionary raw)
        {
            foreach (DictionaryEntry entry in raw)
            {
                var key = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(key) || entry.Value is null)
                {
                    continue;
                }

                if (TryConvertEquipmentObject(entry.Value, out var converted))
                {
                    migrated[key] = converted;
                }
            }
        }

        state["inventory.equippedBySlot"] = migrated;
        return migrated;
    }

    private static string DescribeEquipment(Dictionary<string, object> eq)
    {
        var name = GetString(eq, "Name");
        var kind = GetEquipmentKindLabel(eq);
        return string.IsNullOrWhiteSpace(kind)
            ? name
            : $"{name}（{kind}）";
    }

    private static void AppendEquipmentDescription(StringBuilder sb, Dictionary<string, object> eq)
    {
        sb.AppendLine($"  • {DescribeEquipment(eq)}");

        var description = GetString(eq, "Description");
        if (!string.IsNullOrWhiteSpace(description))
        {
            sb.AppendLine($"    描述: {description}");
        }

        if (IsAccessory(eq))
        {
            sb.AppendLine($"    饰品槽位: {GetAccessorySlotCost(eq)}");
        }
    }

    private static string GetEquipmentKindLabel(Dictionary<string, object> eq)
    {
        return IsDice(eq) ? "骰子"
            : IsAccessory(eq) ? "饰品"
            : string.Empty;
    }

    private static IDictionary<string, object> CreateGuiSection(string title, List<object> items)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Title"] = title,
            ["Items"] = items
        };
    }

    private static IDictionary<string, object> CreateGuiItemFromItemEntry(ItemEntry item)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryText"] = item.Name,
            ["SecondaryText"] = item.Quantity > 1 ? $"数量: {item.Quantity}" : "数量: 1",
            ["Badge"] = "物品"
        };
    }

    private static IDictionary<string, object> CreateGuiItemFromEquipment(Dictionary<string, object> eq)
    {
        var details = new List<string>();
        var description = GetString(eq, "Description");
        if (!string.IsNullOrWhiteSpace(description))
        {
            details.Add(description);
        }

        if (IsAccessory(eq))
        {
            details.Add($"饰品槽位: {GetAccessorySlotCost(eq)}");
        }

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryText"] = GetString(eq, "Name"),
            ["SecondaryText"] = details.Count == 0 ? string.Empty : string.Join("\n", details),
            ["Badge"] = GetEquipmentKindLabel(eq)
        };
    }

    private static IDictionary<string, object> CloneEquipment(Dictionary<string, object> eq)
    {
        return new Dictionary<string, object>(eq, StringComparer.OrdinalIgnoreCase);
    }

    private static List<Dictionary<string, object>> DeserializeEquipments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Dictionary<string, object>>();
        }

        var result = new List<Dictionary<string, object>>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in element.EnumerateObject())
                {
                    map[prop.Name] = ToClrObject(prop.Value);
                }

                result.Add(map);
            }
        }
        catch
        {
            return new List<Dictionary<string, object>>();
        }

        return result;
    }

    private static object ToClrObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number when value.TryGetInt32(out var i) => i,
            JsonValueKind.Number when value.TryGetInt64(out var l) => l,
            JsonValueKind.Number when value.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => value.ToString(),
        };
    }

    private static bool TryConvertEquipmentObject(object value, out Dictionary<string, object> converted)
    {
        if (value is Dictionary<string, object> dict)
        {
            converted = new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        if (value is IDictionary raw)
        {
            converted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in raw)
            {
                var key = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                converted[key] = entry.Value ?? string.Empty;
            }

            return converted.Count > 0;
        }

        var type = value.GetType();
        converted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in type.GetProperties())
        {
            if (!property.CanRead)
            {
                continue;
            }

            converted[property.Name] = property.GetValue(value) ?? string.Empty;
        }

        return converted.Count > 0;
    }

    private static string GetString(Dictionary<string, object> eq, string key)
    {
        return eq.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static int GetInt(Dictionary<string, object> eq, string key, int defaultValue = 0)
    {
        if (!eq.TryGetValue(key, out var value) || value is null)
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

    private sealed record ItemEntry(string Id, string Name, int Quantity);
}
