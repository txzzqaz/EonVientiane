using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// 背包管理器
/// </summary>
public class InventoryManager
{
    // 背包物品列表（暂不设上限，但预留maxCapacity接口）
    private List<ItemStack> _inventoryItems;
    
    // 已装备物品列表（不再按槽位分类，可以随意装备多个）
    private List<ItemStack> _equippedItems;
    
    // 背包容量上限（预留接口，当前为-1表示无限制）
    private int _maxCapacity = -1;
    
    // 背包滚动偏移
    private int _inventoryScrollOffset = 0;
    
    // 饰品槽系统
    private int _maxAccessorySlots = 12; // 初始12个槽位
    
    // 最多可装备骰子数量
    private int _maxEquippedDice = 8;
    
    /// <summary>
    /// 背包物品列表（只读）
    /// </summary>
    public IReadOnlyList<ItemStack> InventoryItems => _inventoryItems.AsReadOnly();
    
    /// <summary>
    /// 已装备物品列表（只读）
    /// </summary>
    public IReadOnlyList<ItemStack> EquippedStacks => _equippedItems.AsReadOnly();
    public IReadOnlyList<Equipment> EquippedItems => _equippedItems.Select(s => s.Item).OfType<Equipment>().ToList();
    
    /// <summary>
    /// 背包滚动偏移
    /// </summary>
    public int InventoryScrollOffset
    {
        get => _inventoryScrollOffset;
        set => _inventoryScrollOffset = Math.Max(0, value);
    }
    
    /// <summary>
    /// 背包容量上限（-1表示无限制）
    /// </summary>
    public int MaxCapacity
    {
        get => _maxCapacity;
        set => _maxCapacity = value;
    }
    
    /// <summary>
    /// 当前背包占用槽位数
    /// </summary>
    public int UsedSlots => _inventoryItems.Count;
    
    /// <summary>
    /// 是否已满（仅当设置了上限时有效）
    /// </summary>
    public bool IsFull => _maxCapacity > 0 && UsedSlots >= _maxCapacity;
    
    /// <summary>
    /// 最多可装备骰子数量（上限8个）
    /// </summary>
    public int MaxEquippedDice => _maxEquippedDice;
    
    /// <summary>
    /// 当前已装备骰子数量
    /// </summary>
    public int EquippedDiceCount => EquippedItems.OfType<Dice>().Count();
    
    /// <summary>
    /// 最多饰品槽位数
    /// </summary>
    public int MaxAccessorySlots
    {
        get => _maxAccessorySlots;
        set => _maxAccessorySlots = Math.Max(0, value);
    }
    
    /// <summary>
    /// 当前已使用的饰品槽位数
    /// </summary>
    public int UsedAccessorySlots
    {
        get
        {
            var equippedAccessories = EquippedItems.OfType<Accessory>().ToList();
            return equippedAccessories.Sum(a => a.AccessorySlotsCost);
        }
    }
    
    /// <summary>
    /// 当前可用饰品槽位数
    /// </summary>
    public int AvailableAccessorySlots => Math.Max(0, MaxAccessorySlots - UsedAccessorySlots);
    
    public InventoryManager()
    {
        _inventoryItems = new List<ItemStack>();
        _equippedItems = new List<ItemStack>();
    }
    
    #region 背包管理
    
    /// <summary>
    /// 添加物品到背包
    /// </summary>
    public bool AddItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return false;
        
        // 如果物品可堆叠，先尝试叠加到现有堆叠
        if (item.MaxStackSize > 1)
        {
            var existingStack = _inventoryItems.FirstOrDefault(
                stack => stack.Item.Id == item.Id && stack.CanStack
            );
            
            if (existingStack != null)
            {
                int remaining = existingStack.AddQuantity(quantity);
                if (remaining > 0)
                {
                    // 如果还有剩余，创建新堆叠
                    return AddNewStack(item, remaining);
                }
                return true;
            }
        }
        
        // 创建新堆叠
        return AddNewStack(item, quantity);
    }
    
    private bool AddNewStack(Item item, int quantity)
    {
        // 检查容量限制
        if (IsFull)
            return false;
        
        _inventoryItems.Add(new ItemStack(item, quantity));
        return true;
    }
    
    /// <summary>
    /// 从背包移除物品
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        var stacks = _inventoryItems.Where(s => s.Item.Id == itemId).ToList();
        int totalQuantity = stacks.Sum(s => s.Quantity);
        
        if (totalQuantity < quantity)
            return false;
        
        int remaining = quantity;
        foreach (var stack in stacks)
        {
            if (remaining <= 0)
                break;
            
            int toRemove = Math.Min(remaining, stack.Quantity);
            stack.RemoveQuantity(toRemove);
            remaining -= toRemove;
            
            if (stack.Quantity <= 0)
            {
                _inventoryItems.Remove(stack);
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 根据索引移除物品堆叠
    /// </summary>
    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= _inventoryItems.Count)
            return false;
        
        _inventoryItems.RemoveAt(index);
        return true;
    }
    
    /// <summary>
    /// 获取指定物品的总数量
    /// </summary>
    public int GetItemCount(string itemId)
    {
        return _inventoryItems
            .Where(s => s.Item.Id == itemId)
            .Sum(s => s.Quantity);
    }
    
    /// <summary>
    /// 检查是否拥有指定物品
    /// </summary>
    public bool HasItem(string itemId, int quantity = 1)
    {
        return GetItemCount(itemId) >= quantity;
    }
    
    /// <summary>
    /// 清空背包
    /// </summary>
    public void ClearInventory()
    {
        _inventoryItems.Clear();
    }
    
    #endregion
    
    #region 装备管理
    
    /// <summary>
    /// 装备物品（从背包）
    /// </summary>
    public bool EquipItem(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= _inventoryItems.Count)
            return false;
        
        var stack = _inventoryItems[inventoryIndex];
        if (stack.Item is not Equipment equipment)
            return false;
        
        // 检查骰子数量限制
        if (equipment is Dice && EquippedDiceCount >= MaxEquippedDice)
            return false;
        
        // 检查饰品槽位限制
        if (equipment is Accessory accessory && accessory.AccessorySlotsCost > AvailableAccessorySlots)
            return false;
        
        _inventoryItems.RemoveAt(inventoryIndex);
        _equippedItems.Add(stack);
        return true;
    }
    
    /// <summary>
    /// 卸下装备（根据索引）
    /// </summary>
    public bool UnequipItem(int equippedIndex)
    {
        if (equippedIndex < 0 || equippedIndex >= _equippedItems.Count)
            return false;
        
        var stack = _equippedItems[equippedIndex];

        if (IsFull)
        {
            return false;
        }

        _equippedItems.RemoveAt(equippedIndex);
        _inventoryItems.Add(stack);
        return true;
    }
    
    /// <summary>
    /// 卸下所有装备
    /// </summary>
    public void UnequipAll()
    {
        while (_equippedItems.Count > 0)
        {
            UnequipItem(0);
        }
    }
    
    /// <summary>
    /// 计算总属性加成
    /// </summary>
    public (int attack, int defense, int speed, int health, int mana) GetTotalStats()
    {
        int attack = 0, defense = 0, speed = 0, health = 0, mana = 0;
        
        foreach (var equipment in _equippedItems.Select(s => s.Item).OfType<Equipment>())
        {
            attack += equipment.Attack;
            defense += equipment.Defense;
            speed += equipment.Speed;
            health += equipment.Health;
            mana += equipment.Mana;
        }
        
        return (attack, defense, speed, health, mana);
    }

    public ItemStack GetInventoryStack(int index)
    {
        if (index < 0 || index >= _inventoryItems.Count)
            return null;
        return _inventoryItems[index];
    }

    public ItemStack GetEquippedStack(int index)
    {
        if (index < 0 || index >= _equippedItems.Count)
            return null;
        return _equippedItems[index];
    }

    /// <summary>
    /// 将服务端下发的背包状态应用到客户端
    /// </summary>
    public void ApplyServerState(InventoryState state)
    {
        _inventoryItems.Clear();
        _equippedItems.Clear();

        foreach (var dto in state.Items)
        {
            var item = ItemFactory.Create(dto.ItemId, dto.ItemName);
            var stack = new ItemStack(item, dto.Quantity, dto.StackId);
            if (dto.IsEquipped)
            {
                _equippedItems.Add(stack);
            }
            else
            {
                _inventoryItems.Add(stack);
            }
        }
    }
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 初始化测试数据
    /// </summary>
    public void InitializeTestData()
    {
        // 添加骰子装备
        var d6Dice = new D6Dice(); // 默认同时可作为主动/被动骰子
        var featheredDice = new FeatheredDice();
        var guashaDice = new GuaShaParquetDice();
        var errorDice = new ErrorDice();
        
        // 添加饰品装备
        var selfAccessory = new SelfAccessory();
        var ascensionProof = new AscensionProofAccessory();
        
        // 添加消耗品和材料
        var healthPotion = new Item("health_potion", "生命药水", "恢复50点生命", ItemType.Consumable)
        {
            MaxStackSize = 99,
            DisplayColor = Microsoft.Xna.Framework.Color.Red
        };
        
        var manaPotion = new Item("mana_potion", "魔力药水", "恢复50点魔力", ItemType.Consumable)
        {
            MaxStackSize = 99,
            DisplayColor = Microsoft.Xna.Framework.Color.Blue
        };
        
        // 添加到背包
        AddItem(d6Dice);
        AddItem(featheredDice);
        AddItem(guashaDice);
        AddItem(errorDice);
        AddItem(selfAccessory);
        AddItem(ascensionProof);
        AddItem(healthPotion, 15);
        AddItem(manaPotion, 10);
    }
    
    #endregion
}

/// ═══════════════════════════════════════════════════════════════════════════════
/// ████████████████████████████  道具创建核心流程  ████████████████████████████
/// ═══════════════════════════════════════════════════════════════════════════════
/// 
/// 【重要】在制作新道具时必须遵循以下步骤：
/// 
/// 1. 【创建道具类】
///    - 骰子放在: EonVientiane/Dices/ 目录，继承 Dice 类
///    - 饰品放在: EonVientiane/Accessories/ 目录，继承 Accessory 类
///    - 参考文档: docs/ITEM_CREATION_GUIDE.md
/// 
/// 2. 【本文件注册】→ RegisterAllItems() 方法中添加
///    示例: _registry.RegisterItem("item_id", () => new ItemClass());
/// 
/// 3. 【服务器初始化】→ EonVientianeServer/ItemInitializer.cs 中：
///    - GetAllItems(): 添加到道具列表
///    - CreateItemFromStackData(): 如果是装备类需要添加
/// 
/// 4. 【获取方式配置】
///    - 新用户初始化: ItemInitializer.GetInitialInventory()
///    - 成就奖励: AchievementSystem.CreateRewardItem()
/// 
/// 5. 【测试】编译 → 启动本地测试 → 验证功能
/// 
/// ✓ 完整流程详见: docs/ITEM_CREATION_GUIDE.md
/// ═══════════════════════════════════════════════════════════════════════════════
/// 
/// <summary>
/// 物品工厂 - 用于创建和管理所有道具
/// </summary>
public static class ItemFactory
{
    private static ItemRegistry _registry;
    private static bool _initialized = false;
    
    /// <summary>
    /// 初始化工厂，注册所有可用的道具
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;
        
        _registry = new ItemRegistry();
        RegisterAllItems();
        _initialized = true;
    }
    
    /// <summary>
    /// 确保已初始化
    /// </summary>
    private static void EnsureInitialized()
    {
        if (!_initialized)
            Initialize();
    }
    
    /// <summary>
    /// 【★ 道具注册中心 ★】注册所有可用的道具
    /// 
    /// 每个新道具都必须在这里注册！
    /// 格式: _registry.RegisterItem("item_id", () => new ItemClass());
    /// 
    /// 【步骤检查清单】
    /// ✓ 步骤1: 在 Dices/ 或 Accessories/ 中创建道具类
    /// ✓ 步骤2: 在下方添加注册语句 ← 【你在这里】
    /// ✓ 步骤3: 在 ItemInitializer.cs 的 GetAllItems() 中添加
    /// ✓ 步骤4: 在 ItemInitializer.cs 的 CreateItemFromStackData() 中添加(装备类)
    /// ✓ 步骤5: 根据需要在初始化方法中设置获取方式
    /// ✓ 步骤6: 编译测试
    /// 
    /// 【道具ID命名规则】snake_case 全小写，例: my_dice_name
    /// 【参考文档】docs/ITEM_CREATION_GUIDE.md
    /// </summary>
    private static void RegisterAllItems()
    {
        // ──────────────────── 骰子 ────────────────────
        // 主动骰子(AD) + 被动骰子(PD) + 双向骰子(Both)
        _registry.RegisterItem("d6_dice", () => new D6Dice());
        _registry.RegisterItem("feathered_dice", () => new FeatheredDice());
        _registry.RegisterItem("guasha_parquet", () => new GuaShaParquetDice());
        _registry.RegisterItem("spring_breeze", () => new SpringBreezeDice());
        _registry.RegisterItem("error_dice", () => new ErrorDice());
        
        // 【新增骰子请在这里添加】
        // _registry.RegisterItem("your_dice_id", () => new YourDiceClass());
        
        // ──────────────────── 饰品 ────────────────────
        // 被动属性加成、战斗事件触发、槽位消耗
        _registry.RegisterItem("self_accessory", () => new SelfAccessory());
        _registry.RegisterItem("ascension_proof", () => new AscensionProofAccessory());
        _registry.RegisterItem("holy_fire", () => new HolyFireAccessory());
        _registry.RegisterItem("wanderer_heart", () => new WandererHeartAccessory());
        _registry.RegisterItem("foresight", () => new ForesightAccessory());
        _registry.RegisterItem("concerted_effort", () => new ConcertedEffortAccessory());
        
        // 【新增饰品请在这里添加】
        // _registry.RegisterItem("your_accessory_id", () => new YourAccessoryClass());
        
        // ──────────────────── 消耗品 ────────────────────
        _registry.RegisterItem("health_potion", () => new Item("health_potion", "生命药水", "恢复50点生命", ItemType.Consumable)
        {
            MaxStackSize = 99,
            DisplayColor = Microsoft.Xna.Framework.Color.Red
        });
        _registry.RegisterItem("mana_potion", () => new Item("mana_potion", "魔力药水", "恢复50点魔力", ItemType.Consumable)
        {
            MaxStackSize = 99,
            DisplayColor = Microsoft.Xna.Framework.Color.Blue
        });
        
        // 【新增消耗品请在这里添加】
    }
    
    /// <summary>
    /// 创建物品实例
    /// </summary>
    public static Item Create(string itemId, string itemName = null)
    {
        EnsureInitialized();
        
        // 如果注册表中有该物品，使用注册的工厂创建
        if (_registry.IsItemRegistered(itemId))
        {
            return _registry.CreateItem(itemId);
        }
        
        // 否则创建通用物品
        return new Item(itemId, string.IsNullOrWhiteSpace(itemName) ? itemId : itemName, string.Empty, ItemType.Other);
    }
    
    /// <summary>
    /// 根据物品ID创建物品堆栈
    /// </summary>
    public static ItemStack CreateItemStack(string itemId, int quantity = 1)
    {
        var item = Create(itemId);
        if (item == null)
            return null;
        return new ItemStack(item, quantity);
    }
    
    /// <summary>
    /// 获取所有注册的物品ID列表
    /// </summary>
    public static IEnumerable<string> GetAllItemIds()
    {
        EnsureInitialized();
        return _registry.GetAllItemIds();
    }
    
    /// <summary>
    /// 获取所有注册的骰子ID列表
    /// </summary>
    public static IEnumerable<string> GetAllDiceIds()
    {
        return new[] { "d6_dice", "feathered_dice", "guasha_parquet", "spring_breeze", "error_dice" };
    }
    
    /// <summary>
    /// 获取所有注册的饰品ID列表
    /// </summary>
    public static IEnumerable<string> GetAllAccessoryIds()
    {
        return new[] { "self_accessory", "ascension_proof", "holy_fire", "wanderer_heart", "foresight", "concerted_effort" };
    }
    
    /// <summary>
    /// 检查物品ID是否已注册
    /// </summary>
    public static bool IsItemRegistered(string itemId)
    {
        EnsureInitialized();
        return _registry.IsItemRegistered(itemId);
    }
    
    /// <summary>
    /// 创建骰子列表（用于初始化玩家装备）
    /// </summary>
    public static List<Dice> CreateStarterDices()
    {
        return new List<Dice>
        {
            new D6Dice(DiceUsageType.Both),
            new FeatheredDice()
        };
    }
    
    /// <summary>
    /// 创建饰品列表（用于初始化玩家装备）
    /// </summary>
    public static List<Accessory> CreateStarterAccessories()
    {
        return new List<Accessory>
        {
            new SelfAccessory()
        };
    }
}
