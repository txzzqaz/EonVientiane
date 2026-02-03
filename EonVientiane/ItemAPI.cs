using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 物品系统API扩展 - 提供物品创建、查询和管理的扩展功能
/// </summary>
public static class ItemAPI
{
    /// <summary>
    /// 物品创建事件
    /// </summary>
    public static event Action<Item> ItemCreated;
    
    /// <summary>
    /// 物品使用事件
    /// </summary>
    public static event Action<Item, Player> ItemUsed;
    
    /// <summary>
    /// 物品堆叠事件
    /// </summary>
    public static event Action<Item, Item> ItemStacked;
    
    /// <summary>
    /// 自定义物品效果处理器
    /// </summary>
    private static readonly Dictionary<string, Func<Item, Player, bool>> _customItemEffects = new();
    
    /// <summary>
    /// 物品属性修改器
    /// </summary>
    private static readonly List<IItemModifier> _itemModifiers = new();
    
    /// <summary>
    /// 注册自定义物品效果
    /// </summary>
    public static void RegisterItemEffect(string itemId, Func<Item, Player, bool> effectHandler)
    {
        _customItemEffects[itemId] = effectHandler;
    }
    
    /// <summary>
    /// 执行物品效果
    /// </summary>
    public static bool ExecuteItemEffect(Item item, Player player)
    {
        if (_customItemEffects.TryGetValue(item.Id, out var handler))
        {
            return handler(item, player);
        }
        return false;
    }
    
    /// <summary>
    /// 添加物品属性修改器
    /// </summary>
    public static void AddItemModifier(IItemModifier modifier)
    {
        if (modifier != null && !_itemModifiers.Contains(modifier))
        {
            _itemModifiers.Add(modifier);
        }
    }
    
    /// <summary>
    /// 移除物品属性修改器
    /// </summary>
    public static void RemoveItemModifier(IItemModifier modifier)
    {
        _itemModifiers.Remove(modifier);
    }
    
    /// <summary>
    /// 应用所有物品修改器
    /// </summary>
    public static Item ApplyModifiers(Item item)
    {
        var modifiedItem = item;
        foreach (var modifier in _itemModifiers.OrderBy(m => m.Priority))
        {
            modifiedItem = modifier.ModifyItem(modifiedItem);
        }
        return modifiedItem;
    }
    
    /// <summary>
    /// 创建物品堆叠
    /// 注意：需要使用ItemRegistry.CreateItem或直接创建物品
    /// </summary>
    public static ItemStack CreateStack(Item item, int quantity = 1)
    {
        if (item == null) return null;
        return new ItemStack(item, quantity);
    }
    
    /// <summary>
    /// 批量创建物品
    /// </summary>
    public static List<Item> CreateItems(ItemRegistry registry, params (string itemId, int count)[] items)
    {
        var result = new List<Item>();
        foreach (var (itemId, count) in items)
        {
            for (int i = 0; i < count; i++)
            {
                var item = registry.CreateItem(itemId);
                if (item != null)
                {
                    result.Add(item);
                }
            }
        }
        return result;
    }
    
    /// <summary>
    /// 检查物品是否可以堆叠
    /// </summary>
    public static bool CanStack(Item item1, Item item2)
    {
        if (item1 == null || item2 == null) return false;
        if (item1.Id != item2.Id) return false;
        if (item1.MaxStackSize <= 1) return false;
        return true;
    }
    
    /// <summary>
    /// 触发物品创建事件
    /// </summary>
    internal static void InvokeItemCreated(Item item)
    {
        ItemCreated?.Invoke(item);
    }
    
    /// <summary>
    /// 触发物品使用事件
    /// </summary>
    internal static void InvokeItemUsed(Item item, Player player)
    {
        ItemUsed?.Invoke(item, player);
    }
    
    /// <summary>
    /// 触发物品堆叠事件
    /// </summary>
    internal static void InvokeItemStacked(Item source, Item target)
    {
        ItemStacked?.Invoke(source, target);
    }
}

/// <summary>
/// 物品修改器接口 - 用于动态修改物品属性
/// </summary>
public interface IItemModifier
{
    /// <summary>
    /// 修改器名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 优先级（数值越小越先执行）
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// 修改物品
    /// </summary>
    Item ModifyItem(Item item);
}

/// <summary>
/// 物品查询构建器 - 用于方便地查询和筛选物品
/// </summary>
public class ItemQueryBuilder
{
    private readonly List<Item> _items;
    private Func<Item, bool> _filter;
    
    public ItemQueryBuilder(IEnumerable<Item> items)
    {
        _items = items?.ToList() ?? new List<Item>();
        _filter = _ => true;
    }
    
    public ItemQueryBuilder OfType(ItemType type)
    {
        var previousFilter = _filter;
        _filter = item => previousFilter(item) && item.Type == type;
        return this;
    }
    
    public ItemQueryBuilder WithId(string itemId)
    {
        var previousFilter = _filter;
        _filter = item => previousFilter(item) && item.Id == itemId;
        return this;
    }
    
    public ItemQueryBuilder WithNameContaining(string text)
    {
        var previousFilter = _filter;
        _filter = item => previousFilter(item) && item.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
        return this;
    }
    
    public ItemQueryBuilder Stackable()
    {
        var previousFilter = _filter;
        _filter = item => previousFilter(item) && item.MaxStackSize > 1;
        return this;
    }
    
    public ItemQueryBuilder Equipments()
    {
        var previousFilter = _filter;
        _filter = item => previousFilter(item) && item is Equipment;
        return this;
    }
    
    public ItemQueryBuilder Dices()
    {
        var previousFilter = _filter;
        _filter = item => previousFilter(item) && item is Dice;
        return this;
    }
    
    public ItemQueryBuilder Accessories()
    {
        var previousFilter = _filter;
        _filter = item => previousFilter(item) && item is Accessory;
        return this;
    }
    
    public List<Item> ToList()
    {
        return _items.Where(_filter).ToList();
    }
    
    public Item FirstOrDefault()
    {
        return _items.FirstOrDefault(_filter);
    }
    
    public int Count()
    {
        return _items.Count(_filter);
    }
}

/// <summary>
/// 物品工厂扩展 - 为ItemFactory添加更多便捷方法
/// </summary>
public static class ItemFactoryExtensions
{
    /// <summary>
    /// 创建物品并应用修改器
    /// </summary>
    public static Item CreateItemWithModifiers(ItemRegistry registry, string itemId)
    {
        var item = registry.CreateItem(itemId);
        if (item != null)
        {
            item = ItemAPI.ApplyModifiers(item);
            ItemAPI.InvokeItemCreated(item);
        }
        return item;
    }
    
    /*
    /// <summary>
    /// 创建随机品质的物品 - 需要ItemQuality支持
    /// </summary>
    public static Item CreateRandomQualityItem(string itemId)
    {
        var item = ItemFactory.CreateItem(itemId);
        if (item != null)
        {
            var random = new Random();
            var qualities = Enum.GetValues<ItemQuality>();
            item.Quality = qualities[random.Next(qualities.Length)];
        }
        return item;
    }
    */
    
    /// <summary>
    /// 批量创建骰子
    /// </summary>
    public static List<Dice> CreateDiceSet(ItemRegistry registry, params string[] diceIds)
    {
        var dices = new List<Dice>();
        foreach (var id in diceIds)
        {
            var item = registry.CreateItem(id);
            if (item is Dice dice)
            {
                dices.Add(dice);
            }
        }
        return dices;
    }
    
    /// <summary>
    /// 批量创建饰品
    /// </summary>
    public static List<Accessory> CreateAccessorySet(ItemRegistry registry, params string[] accessoryIds)
    {
        var accessories = new List<Accessory>();
        foreach (var id in accessoryIds)
        {
            var item = registry.CreateItem(id);
            if (item is Accessory accessory)
            {
                accessories.Add(accessory);
            }
        }
        return accessories;
    }
}

/// <summary>
/// 物品分类器 - 提供物品分类和组织功能
/// </summary>
public class ItemCategorizer
{
    private readonly Dictionary<string, List<Item>> _categories = new();
    
    public void AddCategory(string categoryName)
    {
        if (!_categories.ContainsKey(categoryName))
        {
            _categories[categoryName] = new List<Item>();
        }
    }
    
    public void AddItemToCategory(string categoryName, Item item)
    {
        if (!_categories.ContainsKey(categoryName))
        {
            AddCategory(categoryName);
        }
        _categories[categoryName].Add(item);
    }
    
    public List<Item> GetItemsInCategory(string categoryName)
    {
        return _categories.TryGetValue(categoryName, out var items) ? items : new List<Item>();
    }
    
    public List<string> GetAllCategories()
    {
        return _categories.Keys.ToList();
    }
    
    public void CategorizeByType(IEnumerable<Item> items)
    {
        foreach (var item in items)
        {
            AddItemToCategory(item.Type.ToString(), item);
        }
    }
    
    /*
    /// <summary>
    /// 按品质分类 - 需要Quality属性
    /// </summary>
    public void CategorizeByQuality(IEnumerable<Item> items)
    {
        foreach (var item in items)
        {
            AddItemToCategory(item.Quality.ToString(), item);
        }
    }
    */
}
