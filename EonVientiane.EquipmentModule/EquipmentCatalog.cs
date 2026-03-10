namespace EonVientiane.EquipmentModule;

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
        return JsonSerializer.Serialize(Array.Empty<EquipmentEntry>());
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

public sealed record EquipmentEntry(string Id, string Name, string Slot, int ArmorValue, int AttackBonus);
