# 测试账号说明文档

## 概述

为了方便测试，系统提供了两个常驻测试账号：**qaz1** 和 **qaz2**。这两个账号的背包中会自动填充游戏中所有已有的道具，并且会随着游戏道具的更新而自动保持最新状态。

## 测试账号信息

| 用户名 | 密码 | 邮箱 |
|--------|------|------|
| qaz1 | qaz1 | qaz1@test.com |
| qaz2 | qaz2 | qaz2@test.com |

## 特性

### 1. 自动填充所有道具

测试账号在首次创建或登录时，会自动获得游戏中所有已实现的道具，包括：

#### 骰子类
- **D6** (`d6_dice`) - 普通六面骰
- **飞羽** (`feathered_dice`) - 成就奖励骰子
- **春风** (`spring_breeze`) - 特殊骰子
- **刮痧师傅** (`guasha_parquet`) - 成就奖励骰子

#### 饰品类
- **自我** (`self_accessory`) - 基础饰品
- **飞升之证** (`ascension_proof`) - 成就奖励饰品
- **流浪者之心** (`wanderer_heart`) - 成就奖励饰品
- **预知** (`foresight`) - 特殊饰品
- **齐心协力** (`concerted_effort`) - 特殊饰品
- **圣火** (`holy_fire`) - 特殊饰品

#### 材料类
- **金币** (`gold_coin`) - 货币道具（数量：9999）

### 2. 自动同步新道具

每次测试账号登录或请求背包时，系统会自动检查是否有新增的道具：
- 如果发现新道具，会自动添加到背包中
- 装备类道具默认数量为 10 个
- 金币固定为 9999 个
- 控制台会输出添加新道具的日志信息

### 3. 与普通账号的区别

| 特性 | 测试账号 (qaz1/qaz2) | 普通账号 |
|------|---------------------|----------|
| 初始道具 | 所有道具 | 基础道具（D6、自我、金币200） |
| 道具数量 | 装备10个，金币9999 | 基础数量 |
| 自动更新 | ✅ 自动获取新道具 | ❌ 需要通过成就或其他途径获取 |
| 用途 | 测试和开发 | 正常游戏 |

## 使用方法

### 登录测试账号

1. 启动游戏客户端
2. 在登录界面输入：
   - 用户名：`qaz1` 或 `qaz2`
   - 密码：`qaz1` 或 `qaz2`
3. 点击登录

### 查看背包

登录后，打开背包界面即可看到所有道具已自动填充。

## 技术实现

### 服务端实现

#### 1. UserManager - 测试账号标记

```csharp
private class UserAccount
{
    public bool IsTestAccount { get; set; } = false;  // 标记是否为测试账号
}

private void InitializeTestUsers()
{
    // 创建常驻测试账号
    CreateTestAccountInternal("qaz1", "qaz1", "qaz1@test.com");
    CreateTestAccountInternal("qaz2", "qaz2", "qaz2@test.com");
}

public bool IsTestAccount(string userId)
{
    // 检查用户是否为测试账号
}
```

#### 2. ItemInitializer - 道具列表管理

```csharp
public static List<(string ItemId, string ItemName)> GetAllItems()
{
    // 返回游戏中所有道具的ID和名称
}

public static List<InitialInventoryItem> GetTestAccountInventory()
{
    // 为测试账号生成包含所有道具的初始背包
}
```

#### 3. InventoryStore - 自动同步机制

```csharp
private UserInventoryStateData SyncTestAccountInventory(UserInventoryStateData state)
{
    var allItems = ItemInitializer.GetAllItems();
    
    // 检查并添加新道具
    foreach (var (itemId, itemName) in allItems)
    {
        if (!existingItemIds.Contains(itemId))
        {
            // 添加新道具到背包
        }
    }
}
```

#### 4. GameServer - 条件初始化

```csharp
var initialFactory = _userManager.IsTestAccount(client.UserId)
    ? (Func<List<InitialInventoryItem>>)ItemInitializer.GetTestAccountInventory
    : () => ItemInitializer.GetInitialInventory(client.UserId);
    
var state = _inventoryStore.LoadOrCreate(client.UserId, initialFactory);
```

## 添加新道具

当开发人员添加新道具时，只需要在 `ItemInitializer.GetAllItems()` 方法中添加新的条目：

```csharp
public static List<(string ItemId, string ItemName)> GetAllItems()
{
    return new List<(string ItemId, string ItemName)>
    {
        // 现有道具...
        
        // 新增道具
        ("new_item_id", "新道具名称"),
    };
}
```

测试账号会在下次登录时自动获得新道具。

同时，还需要在 `CreateItemFromStackData` 方法中添加新道具的创建逻辑：

```csharp
public static Equipment? CreateItemFromStackData(InventoryStackRecord stackData)
{
    return stackData.ItemId switch
    {
        // 现有道具...
        "new_item_id" => new NewItemClass(),
        _ => null
    };
}
```

## 注意事项

1. **仅用于测试**：测试账号仅供开发和测试使用，不应在正式环境中使用
2. **数据持久化**：测试账号的背包数据会保存在 `data/users/` 目录下
3. **手动清理**：如果需要重置测试账号，删除对应的 JSON 文件即可
4. **并发登录**：两个测试账号可以同时登录，方便进行多人对战测试

## 日志示例

当测试账号登录并同步新道具时，服务端会输出如下日志：

```
[Server] Test accounts initialized: qaz1, qaz2
[Server] User 'qaz1' logged in successfully
[InventoryStore] Added new item '圣火' to test account <userId>
[InventoryStore] Added new item '刮痧师傅' to test account <userId>
```

## 相关文件

- `/EonVientianeServer/UserManager.cs` - 用户管理和测试账号标记
- `/EonVientianeServer/ItemInitializer.cs` - 道具初始化和列表管理
- `/EonVientianeServer/InventoryStore.cs` - 背包存储和自动同步
- `/EonVientianeServer/GameServer.cs` - 服务端主逻辑

## 更新历史

- **2026-01-22**：创建测试账号系统，支持 qaz1 和 qaz2
- 自动填充所有道具（包括骰子、饰品、材料）
- 实现自动同步新道具功能
