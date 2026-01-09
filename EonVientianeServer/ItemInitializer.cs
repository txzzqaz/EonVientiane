using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 物品初始化器 - 为新用户初始化起始物品
/// </summary>
public class ItemInitializer
{
    /// <summary>
    /// 为用户初始化物品
    /// </summary>
    public static List<InitialInventoryItem> GetInitialInventory(string userId)
    {
        var items = new List<InitialInventoryItem>
        {
            new InitialInventoryItem { ItemId = "d6_dice", ItemName = "D6", Quantity = 1 },
            new InitialInventoryItem { ItemId = "feathered_dice", ItemName = "飞羽骰子", Quantity = 1 },
            new InitialInventoryItem { ItemId = "self_accessory", ItemName = "自我", Quantity = 1 },
            new InitialInventoryItem { ItemId = "ascension_proof", ItemName = "飞升之证", Quantity = 1 },
            new InitialInventoryItem { ItemId = "health_potion", ItemName = "生命药水", Quantity = 10 },
            new InitialInventoryItem { ItemId = "mana_potion", ItemName = "魔法药水", Quantity = 6 },
            new InitialInventoryItem { ItemId = "gold_coin", ItemName = "金币", Quantity = 200 }
        };
        
        return items;
    }
    
    /// <summary>
    /// 获取推荐的初始装备
    /// </summary>
    public static List<string> GetRecommendedEquipment()
    {
        return new List<string>
        {
            "item_sword_starter",
            "item_armor_starter"
        };
    }
}
