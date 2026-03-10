namespace EonVientiane.InventoryModule;

using System.Text;
using System.Text.Json;

public static class InventoryApi
{
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
        state["inventory.equipments"] = JsonSerializer.Deserialize<List<EquipmentEntry>>(equipmentsJson ?? "[]") ?? new List<EquipmentEntry>();
        state["inventory.equippedBySlot"] = new Dictionary<string, EquipmentEntry>(StringComparer.OrdinalIgnoreCase);
    }

    public static string Equip(IDictionary<string, object> state, string itemName)
    {
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);

        var eq = equipments.FirstOrDefault(x => x.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (eq is null)
        {
            return $"❌ 无法穿戴 '{itemName}'. 请检查物品是否存在且为装备.";
        }

        if (equippedBySlot.ContainsKey(eq.Slot))
        {
            var old = equippedBySlot[eq.Slot];
            equipments.Add(old);
        }

        equippedBySlot[eq.Slot] = eq;
        equipments.Remove(eq);
        return $"✓ 已穿戴: {itemName}";
    }

    public static string Unequip(IDictionary<string, object> state, string itemName)
    {
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);

        var eq = equippedBySlot.Values.FirstOrDefault(x => x.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (eq is null)
        {
            return $"❌ 无法卸下 '{itemName}'. 请检查是否已穿戴此装备.";
        }

        equippedBySlot.Remove(eq.Slot);
        equipments.Add(eq);
        return $"✓ 已卸下: {itemName}";
    }

    public static string ViewInventory(IDictionary<string, object> state)
    {
        var items = GetItems(state);
        var equipments = GetEquipments(state);
        var equippedBySlot = GetEquippedBySlot(state);

        var sb = new StringBuilder();
        sb.AppendLine($"=== 背包 ({items.Count + equipments.Count}/20) ===");

        foreach (var item in items)
        {
            var text = item.Quantity > 1 ? $"{item.Name} x{item.Quantity}" : item.Name;
            sb.AppendLine($"  • {text}");
        }

        foreach (var eq in equipments)
        {
            sb.AppendLine($"  • {DescribeEquipment(eq)}");
        }

        if (equippedBySlot.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"=== 已穿戴装备 ({equippedBySlot.Count}) ===");
            foreach (var e in equippedBySlot.Values)
            {
                sb.AppendLine($"  • [{e.Slot}] {DescribeEquipment(e)}");
            }
        }

        return sb.ToString();
    }

    public static string GetStatusAddon(IDictionary<string, object> state)
    {
        var equippedBySlot = GetEquippedBySlot(state);
        return $"已装备槽位: {equippedBySlot.Count}";
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

    private static List<EquipmentEntry> GetEquipments(IDictionary<string, object> state)
    {
        if (!state.TryGetValue("inventory.equipments", out var eqObj) || eqObj is not List<EquipmentEntry> equipments)
        {
            equipments = new List<EquipmentEntry>();
            state["inventory.equipments"] = equipments;
        }

        return equipments;
    }

    private static Dictionary<string, EquipmentEntry> GetEquippedBySlot(IDictionary<string, object> state)
    {
        if (!state.TryGetValue("inventory.equippedBySlot", out var slotObj) || slotObj is not Dictionary<string, EquipmentEntry> slots)
        {
            slots = new Dictionary<string, EquipmentEntry>(StringComparer.OrdinalIgnoreCase);
            state["inventory.equippedBySlot"] = slots;
        }

        return slots;
    }

    private static string DescribeEquipment(EquipmentEntry eq)
    {
        var s = $"{eq.Name} [{eq.Slot}]";
        if (eq.ArmorValue > 0) s += $" (防御+{eq.ArmorValue})";
        if (eq.AttackBonus > 0) s += $" (攻击+{eq.AttackBonus})";
        return s;
    }

    private sealed record ItemEntry(string Id, string Name, int Quantity);
    private sealed record EquipmentEntry(string Id, string Name, string Slot, int ArmorValue, int AttackBonus);
}
