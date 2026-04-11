namespace EonVientiane.EquipmentModule;

public static class EquipmentGuiMenuProvider
{
    public static IDictionary<string, object> GetGuiMenuDefinition()
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModuleId"] = "equipment",
            ["Title"] = "装备",
            ["Layout"] = "Vertical",
            ["Order"] = 15,
            ["Buttons"] = new List<object>
            {
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Text"] = "装备管理",
                    ["Command"] = string.Empty,
                    ["ActivatesContent"] = true
                }
            }
        };
    }

    public static IDictionary<string, object> GetGuiContentDefinition(IDictionary<string, object> state)
    {
        return EquipmentApi.GetGuiContentDefinition(state);
    }
}
