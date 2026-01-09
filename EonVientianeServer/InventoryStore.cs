using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 简单的文件持久化背包存储
/// </summary>
public class InventoryStore
{
    private readonly string _rootDir;
    private readonly Dictionary<string, UserInventoryStateData> _cache = new();
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public InventoryStore(string rootDir = "data/users")
    {
        _rootDir = rootDir;
        Directory.CreateDirectory(_rootDir);
    }

    public UserInventoryStateData LoadOrCreate(string userId, Func<List<InitialInventoryItem>> initialFactory)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(userId, out var cached))
            {
                return CloneState(cached);
            }

            var path = GetPath(userId);
            UserInventoryStateData state;

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                state = JsonSerializer.Deserialize<UserInventoryStateData>(text, _jsonOptions) ?? new UserInventoryStateData { UserId = userId };
            }
            else
            {
                state = CreateFromInitial(userId, initialFactory());
                SaveInternal(state);
            }

            _cache[userId] = state;
            return CloneState(state);
        }
    }

    public UserInventoryStateData Save(UserInventoryStateData state)
    {
        lock (_lock)
        {
            _cache[state.UserId] = CloneState(state);
            SaveInternal(state);
            return CloneState(state);
        }
    }

    public InventoryState ToDto(UserInventoryStateData state)
    {
        return new InventoryState
        {
            Items = state.Items
                .Select(item => new InventoryItemDto
                {
                    StackId = item.StackId,
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    IsEquipped = item.IsEquipped
                })
                .ToList()
        };
    }

    public UserInventoryStateData CloneState(UserInventoryStateData state)
    {
        return new UserInventoryStateData
        {
            UserId = state.UserId,
            Items = state.Items
                .Select(i => new InventoryStackRecord
                {
                    StackId = i.StackId,
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    IsEquipped = i.IsEquipped
                })
                .ToList()
        };
    }

    private UserInventoryStateData CreateFromInitial(string userId, List<InitialInventoryItem> initial)
    {
        var items = new List<InventoryStackRecord>();
        foreach (var item in initial)
        {
            items.Add(new InventoryStackRecord
            {
                StackId = Guid.NewGuid().ToString("N"),
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Quantity = Math.Max(1, item.Quantity),
                IsEquipped = false
            });
        }

        return new UserInventoryStateData
        {
            UserId = userId,
            Items = items
        };
    }

    private void SaveInternal(UserInventoryStateData state)
    {
        var path = GetPath(state.UserId);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _rootDir);
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(path, json);
    }

    private string GetPath(string userId)
    {
        return Path.Combine(_rootDir, $"{userId}_inventory.json");
    }
}

public class InventoryStackRecord
{
    public string StackId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsEquipped { get; set; }
}

public class UserInventoryStateData
{
    public string UserId { get; set; } = string.Empty;
    public List<InventoryStackRecord> Items { get; set; } = new();
}
