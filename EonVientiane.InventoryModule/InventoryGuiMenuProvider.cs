namespace EonVientiane.InventoryModule;

using System.Collections;

public static class InventoryGuiMenuProvider
{
    public static IDictionary<string, object> GetGuiMenuDefinition()
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModuleId"] = "inventory",
            ["Title"] = "背包",
            ["Layout"] = "Vertical",
            ["Order"] = 20,
            ["Buttons"] = new List<object>
            {
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Text"] = "查看背包",
                    ["Command"] = "inv",
                    ["ActivatesContent"] = true
                }
            }
        };
    }

    public static IDictionary<string, object> GetGuiContentDefinition(IDictionary<string, object> state)
    {
        return InventoryApi.GetGuiContentDefinition(state);
    }
}
