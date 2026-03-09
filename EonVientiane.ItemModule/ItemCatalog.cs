namespace EonVientiane.ItemModule;

using System.Text.Json;

public static class ItemApi
{
    public static string GetStarterItemsJson()
    {
        var items = new List<ItemEntry>
        {
            new("potion_hp", "生命药水", 3),
            new("potion_mp", "魔法药水", 2),
            new("coin", "金币", 100),
        };

        return JsonSerializer.Serialize(items);
    }
}

public sealed record ItemEntry(string Id, string Name, int Quantity);
