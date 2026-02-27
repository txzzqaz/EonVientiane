using System.Collections.Generic;
using EonVientiane.Shared;
using EonVientiane;

namespace EonVientianeServer;

/// ═══════════════════════════════════════════════════════════════════════════════
/// ████████████████████  道具系统服务器侧初始化  ████████████████████
/// ═══════════════════════════════════════════════════════════════════════════════
/// 
/// 【用途】
/// 本类负责在服务器侧管理所有道具的信息和初始化逻辑
/// 
/// 【三个关键方法】
/// 
/// 1️⃣  GetAllItems() - 道具总注册表
///     ├─ 返回游戏中所有道具的ID和名称列表
///     ├─ 新增道具必须在此注册！
///     └─ 用于下拉列表、初始化等场景
/// 
/// 2️⃣  GetInitialInventory() - 新用户初始物品
///     ├─ 返回新用户创建时获得的初始物品
///     └─ 用于用户注册后的首次物品发放
/// 
/// 3️⃣  CreateItemFromStackData() - 物品创建工厂
///     ├─ 从堆叠数据创建装备实例
///     ├─ 用于从数据库加载已保存的道具
///     └─ 所有装备类道具都需要在此注册创建逻辑
/// 
/// 【新增道具检查清单】
/// ✓ GetAllItems() - 添加("item_id", "Item Name")
/// ✓ CreateItemFromStackData() - 添加 case "item_id" => new ItemClass()
/// ✓ InventoryManager.cs 的 RegisterAllItems() - 添加注册
/// ✓ 根据需要在 GetInitialInventory() 中设置初始发放
/// ✓ 根据需要在 AchievementSystem.cs 中设置成就奖励
/// 
/// 【完整指南】docs/ITEM_CREATION_GUIDE.md
/// 
/// ═══════════════════════════════════════════════════════════════════════════════
/// 
/// <summary>
/// 物品初始化器 - 为新用户初始化起始物品
/// </summary>
public class ItemInitializer
{
    /// <summary>
    /// 【★ 道具总注册表 ★】获取游戏中所有道具的ID和名称列表（包括未来可能添加的）
    /// 
    /// 每个新道具都必须在这里添加！
    /// 格式: ("item_id", "Item Display Name")
    /// 
    /// 💡 提示：这个列表用于：
    ///    - 道具下拉列表
    ///    - 成就系统
    ///    - 初始化检查
    /// 
    /// 【添加新道具】
    /// 1. 在下方的适当分类中添加新行
    /// 2. 在 CreateItemFromStackData() 中添加创建逻辑（装备类）
    /// 3. 在 InventoryManager.ItemFactory.RegisterAllItems() 中注册
    /// </summary>
    public static List<(string ItemId, string ItemName)> GetAllItems()
    {
        return new List<(string ItemId, string ItemName)>
        {
            // ────────────────── 骰子类 ──────────────────
            ("d6_dice", "D6"),
            ("feathered_dice", "飞羽"),
            ("spring_breeze", "春风"),
            ("guasha_parquet", "刮痧师傅"),
            ("error_dice", "ERROR"),
            ("blood_trace", "血痕"),
            // 【新增骰子在这里添加】
            
            // ────────────────── 饰品类 ──────────────────
            ("self_accessory", "自我"),
            ("ascension_proof", "飞升之证"),
            ("wanderer_heart", "流浪者之心"),
            ("foresight", "预知"),
            ("concerted_effort", "齐心协力"),
            ("holy_fire", "圣火")
            // 【新增饰品在这里添加】
            
            // ────────────────── 材料类 ──────────────────
            // 【新增材料在这里添加】
        };
    }
    
    /// <summary>
    /// 为测试账号（qaz1和qaz2）初始化物品 - 包含所有道具
    /// </summary>
    public static List<InitialInventoryItem> GetTestAccountInventory()
    {
        var items = new List<InitialInventoryItem>();
        
        foreach (var (itemId, itemName) in GetAllItems())
        {
            int quantity = 10; // 所有道具给10个
            items.Add(new InitialInventoryItem 
            { 
                ItemId = itemId, 
                ItemName = itemName, 
                Quantity = quantity 
            });
        }
        
        return items;
    }
    
    /// <summary>
    /// 为用户初始化物品
    /// </summary>
    public static List<InitialInventoryItem> GetInitialInventory(string userId)
    {
        var items = new List<InitialInventoryItem>
        {
            new InitialInventoryItem { ItemId = "d6_dice", ItemName = "D6", Quantity = 1 },
            // DEBUG骰子默认不下发，只用于测试账号
            // 飞羽已改为成就奖励
            // new InitialInventoryItem { ItemId = "feathered_dice", ItemName = "飞羽", Quantity = 1 },
            new InitialInventoryItem { ItemId = "self_accessory", ItemName = "自我", Quantity = 1 }
            // 飞升之证已改为成就奖励
            // new InitialInventoryItem { ItemId = "ascension_proof", ItemName = "飞升之证", Quantity = 1 }
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
            "d6_dice",
            "self_accessory"
        };
    }
    
    /// <summary>
    /// 【★ 装备实例创建工厂 ★】从堆叠数据创建装备实例
    /// 
    /// 这个方法用于从保存的数据（例如数据库）恢复道具实例。
    /// 所有装备类道具都必须在这里添加创建逻辑！
    /// 
    /// 【何时调用】
    /// - 玩家登录时，从数据库加载已保存的道具
    /// - 背包数据同步时
    /// 
    /// 【添加新道具】
    /// 在下方的 switch 语句中添加 case 分支：
    /// "your_item_id" => new YourItemClass(),
    /// 
    /// 【对应关系】
    /// - 骰子: case "dice_id" => new DiceClass()
    /// - 饰品: case "accessory_id" => new AccessoryClass()
    /// - 消耗品/材料: 通常返回 null（非装备类）
    /// </summary>
    public static Equipment? CreateItemFromStackData(InventoryStackRecord stackData)
    {
        return stackData.ItemId switch
        {
            // ──────────────────── 骰子 ────────────────────
            "d6_dice" => new D6Dice(DiceUsageType.Both),
            "feathered_dice" => new FeatheredDice(),
            "spring_breeze" => new SpringBreezeDice(),
            "guasha_parquet" => new GuaShaParquetDice(),
            "error_dice" => new ErrorDice(),
            "blood_trace" => new BloodTraceDice(),
            // 【新增骰子在这里添加】
            
            // ──────────────────── 饰品 ────────────────────
            "self_accessory" => new SelfAccessory(),
            "ascension_proof" => new AscensionProofAccessory(),
            "wanderer_heart" => new WandererHeartAccessory(),
            "foresight" => new ForesightAccessory(),
            "concerted_effort" => new ConcertedEffortAccessory(),
            "holy_fire" => new HolyFireAccessory(),
            // 【新增饰品在这里添加】
            
            _ => null
        };
    }
    
    /// <summary>
    /// 【★ 从SignedItem创建装备实例 ★】
    /// 
    /// 从SignedItem（钱包系统）创建装备实例，并恢复metadata中的状态。
    /// 用于新的钱包系统，支持道具状态持久化。
    /// </summary>
    public static Equipment? CreateItemFromSignedItem(SignedItem signedItem)
    {
        Equipment? equipment = signedItem.ItemId switch
        {
            // ──────────────────── 骰子 ────────────────────
            "d6_dice" => new D6Dice(DiceUsageType.Both),
            "feathered_dice" => new FeatheredDice(),
            "spring_breeze" => new SpringBreezeDice(),
            "guasha_parquet" => new GuaShaParquetDice(),
            "error_dice" => new ErrorDice(),
            "blood_trace" => new BloodTraceDice(),
            
            // ──────────────────── 饰品 ────────────────────
            "self_accessory" => new SelfAccessory(),
            "ascension_proof" => new AscensionProofAccessory(),
            "wanderer_heart" => new WandererHeartAccessory(),
            "foresight" => new ForesightAccessory(),
            "concerted_effort" => new ConcertedEffortAccessory(),
            "holy_fire" => new HolyFireAccessory(),
            
            _ => null
        };
        
        // 特殊处理：飞升之证需要从metadata恢复状态
        if (equipment is AscensionProofAccessory ascensionProof && signedItem.Metadata != null)
        {
            ascensionProof.LoadFromMetadata(signedItem.Metadata);
        }
        
        return equipment;
    }
}
