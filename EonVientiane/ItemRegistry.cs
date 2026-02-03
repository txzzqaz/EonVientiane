using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 物品注册表 - 集中管理所有物品的创建工厂方法
/// </summary>
public class ItemRegistry
{
    private Dictionary<string, Func<Item>> _itemFactories;
    
    public ItemRegistry()
    {
        _itemFactories = new Dictionary<string, Func<Item>>();
    }
    
    /// <summary>
    /// 注册物品工厂方法
    /// </summary>
    public void RegisterItem(string itemId, Func<Item> factory)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("物品ID不能为空");
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        
        _itemFactories[itemId] = factory;
    }
    
    /// <summary>
    /// 创建物品实例
    /// </summary>
    public Item CreateItem(string itemId)
    {
        if (!_itemFactories.ContainsKey(itemId))
        {
            System.Diagnostics.Debug.WriteLine($"警告: 物品ID '{itemId}' 未注册");
            return null;
        }
        
        try
        {
            return _itemFactories[itemId].Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"错误: 创建物品 '{itemId}' 失败: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 获取所有注册的物品ID
    /// </summary>
    public IEnumerable<string> GetAllItemIds()
    {
        return _itemFactories.Keys;
    }
    
    /// <summary>
    /// 检查物品ID是否已注册
    /// </summary>
    public bool IsItemRegistered(string itemId)
    {
        return _itemFactories.ContainsKey(itemId);
    }
    
    /// <summary>
    /// 注销物品
    /// </summary>
    public bool UnregisterItem(string itemId)
    {
        return _itemFactories.Remove(itemId);
    }
    
    /// <summary>
    /// 清空所有注册的物品
    /// </summary>
    public void Clear()
    {
        _itemFactories.Clear();
    }
    
    /// <summary>
    /// 获取注册的物品数量
    /// </summary>
    public int GetRegisteredItemCount()
    {
        return _itemFactories.Count;
    }
}
