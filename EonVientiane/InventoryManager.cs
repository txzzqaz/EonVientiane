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
        if (stack.Item is not Equipment)
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
        
        var goldCoin = new Item("gold_coin", "金币", "闪闪发光的金币", ItemType.Material)
        {
            MaxStackSize = 9999,
            DisplayColor = Microsoft.Xna.Framework.Color.Gold
        };
        
        // 添加到背包
        AddItem(d6Dice);
        AddItem(featheredDice);
        AddItem(selfAccessory);
        AddItem(ascensionProof);
        AddItem(healthPotion, 15);
        AddItem(manaPotion, 10);
        AddItem(goldCoin, 250);
    }
    
    #endregion
}

public static class ItemFactory
{
    public static Item Create(string itemId, string itemName)
    {
        return itemId switch
        {
            "d6_dice" => new D6Dice(),
            "feathered_dice" => new FeatheredDice(),
            "self_accessory" => new SelfAccessory(),
            "ascension_proof" => new AscensionProofAccessory(),
            "health_potion" => new Item("health_potion", "生命药水", "恢复50点生命", ItemType.Consumable)
            {
                MaxStackSize = 99,
                DisplayColor = Microsoft.Xna.Framework.Color.Red
            },
            "mana_potion" => new Item("mana_potion", "魔力药水", "恢复50点魔力", ItemType.Consumable)
            {
                MaxStackSize = 99,
                DisplayColor = Microsoft.Xna.Framework.Color.Blue
            },
            "gold_coin" => new Item("gold_coin", "金币", "闪闪发光的金币", ItemType.Material)
            {
                MaxStackSize = 9999,
                DisplayColor = Microsoft.Xna.Framework.Color.Gold
            },
            _ => new Item(itemId, string.IsNullOrWhiteSpace(itemName) ? itemId : itemName, string.Empty, ItemType.Other)
        };
    }
}
