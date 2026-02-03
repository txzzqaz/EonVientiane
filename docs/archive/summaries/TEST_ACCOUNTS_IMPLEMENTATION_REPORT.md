# 测试账号系统实现报告

## 实现摘要

成功创建了两个常驻测试账号 `qaz1` 和 `qaz2`，实现了自动填充所有游戏道具的功能，并确保未来添加新道具时能自动同步到这两个账号。

---

## 实现的功能

### ✅ 1. 测试账号创建
- 创建了 `qaz1` 和 `qaz2` 两个常驻测试账号
- 账号在服务器启动时自动初始化
- 添加了 `IsTestAccount` 标记用于识别测试账号

### ✅ 2. 自动填充所有道具
- 测试账号首次创建时自动获得所有已实现的道具
- 装备类道具（骰子、饰品）默认数量：10个
- 金币固定数量：9999个

### ✅ 3. 自动同步新道具
- 每次测试账号登录时自动检查是否有新道具
- 发现新道具时自动添加到背包
- 控制台输出新道具添加的日志信息

### ✅ 4. 集中管理道具列表
- 在 `ItemInitializer` 中统一维护道具列表
- 新增道具时只需更新 `GetAllItems()` 方法
- 支持自动同步到测试账号

---

## 技术实现

### 修改的文件

#### 1. `EonVientianeServer/UserManager.cs`
**修改内容：**
- 添加 `IsTestAccount` 属性到 `UserAccount` 类
- 创建 `CreateTestAccountInternal()` 方法用于创建测试账号
- 在 `InitializeTestUsers()` 中初始化 qaz1 和 qaz2
- 添加 `IsTestAccount(string userId)` 公共方法用于检查账号类型

**关键代码：**
```csharp
private class UserAccount
{
    public bool IsTestAccount { get; set; } = false;
}

private void InitializeTestUsers()
{
    CreateTestAccountInternal("qaz1", "qaz1", "qaz1@test.com");
    CreateTestAccountInternal("qaz2", "qaz2", "qaz2@test.com");
}

public bool IsTestAccount(string userId) { ... }
```

#### 2. `EonVientianeServer/ItemInitializer.cs`
**修改内容：**
- 添加 `GetAllItems()` 方法返回所有道具列表
- 添加 `GetTestAccountInventory()` 方法生成测试账号的完整道具列表
- 更新 `CreateItemFromStackData()` 支持新道具（圣火、刮痧师傅）

**关键代码：**
```csharp
public static List<(string ItemId, string ItemName)> GetAllItems()
{
    return new List<(string ItemId, string ItemName)>
    {
        // 骰子类
        ("d6_dice", "D6"),
        ("feathered_dice", "飞羽"),
        ("spring_breeze", "春风"),
        ("guasha_parquet", "刮痧师傅"),
        
        // 饰品类
        ("self_accessory", "自我"),
        ("ascension_proof", "飞升之证"),
        ("wanderer_heart", "流浪者之心"),
        ("foresight", "预知"),
        ("concerted_effort", "齐心协力"),
        ("holy_fire", "圣火"),
        
        // 材料类
        ("gold_coin", "金币")
    };
}

public static List<InitialInventoryItem> GetTestAccountInventory()
{
    var items = new List<InitialInventoryItem>();
    foreach (var (itemId, itemName) in GetAllItems())
    {
        int quantity = itemId == "gold_coin" ? 9999 : 10;
        items.Add(new InitialInventoryItem { ... });
    }
    return items;
}
```

#### 3. `EonVientianeServer/InventoryStore.cs`
**修改内容：**
- 构造函数添加 `UserManager` 参数
- 在 `LoadOrCreate()` 中检查是否为测试账号
- 添加 `SyncTestAccountInventory()` 方法实现自动同步
- 自动同步时输出日志信息

**关键代码：**
```csharp
private readonly UserManager? _userManager;

public InventoryStore(string rootDir = "data/users", UserManager? userManager = null)
{
    _userManager = userManager;
}

public UserInventoryStateData LoadOrCreate(string userId, Func<...> initialFactory)
{
    bool isTestAccount = _userManager?.IsTestAccount(userId) ?? false;
    
    if (isTestAccount)
    {
        state = SyncTestAccountInventory(state);
        SaveInternal(state);
    }
}

private UserInventoryStateData SyncTestAccountInventory(UserInventoryStateData state)
{
    var allItems = ItemInitializer.GetAllItems();
    // 检查并添加新道具
}
```

#### 4. `EonVientianeServer/GameServer.cs`
**修改内容：**
- 修改 `InventoryStore` 初始化，传入 `UserManager`
- 更新所有 `LoadOrCreate()` 调用，根据账号类型选择初始化方法
- 影响的方法：
  - `HandleRequestInventoryAsync()`
  - `HandleEquipItemAsync()`
  - `HandleUnequipItemAsync()`
  - `InitializeServerBattleAsync()`

**关键代码：**
```csharp
public GameServer(int port = 7777)
{
    _inventoryStore = new InventoryStore("data/users", _userManager);
}

var initialFactory = _userManager.IsTestAccount(client.UserId)
    ? (Func<List<InitialInventoryItem>>)ItemInitializer.GetTestAccountInventory
    : () => ItemInitializer.GetInitialInventory(client.UserId);
```

---

## 当前道具列表

系统已配置以下 **11 种道具**：

### 骰子类（4种）
1. **D6** (`d6_dice`) - 普通六面骰
2. **飞羽** (`feathered_dice`) - 成就奖励
3. **春风** (`spring_breeze`) - 特殊骰子
4. **刮痧师傅** (`guasha_parquet`) - 成就奖励

### 饰品类（6种）
1. **自我** (`self_accessory`) - 基础饰品
2. **飞升之证** (`ascension_proof`) - 成就奖励
3. **流浪者之心** (`wanderer_heart`) - 成就奖励
4. **预知** (`foresight`) - 特殊饰品
5. **齐心协力** (`concerted_effort`) - 特殊饰品
6. **圣火** (`holy_fire`) - 特殊饰品

### 材料类（1种）
1. **金币** (`gold_coin`) - 货币道具

---

## 使用方法

### 登录测试账号
```
用户名: qaz1    密码: qaz1
用户名: qaz2    密码: qaz2
```

### 查看效果
1. 启动服务器
2. 登录测试账号
3. 打开背包界面
4. 确认所有道具已自动填充

### 添加新道具
开发人员在添加新道具时，只需：

1. 在 `ItemInitializer.GetAllItems()` 中添加新道具条目：
```csharp
("new_item_id", "新道具名称"),
```

2. 在 `CreateItemFromStackData()` 中添加创建逻辑：
```csharp
"new_item_id" => new NewItemClass(),
```

3. 测试账号下次登录时自动获得新道具

---

## 验证结果

### 编译测试
```
✓ 所有文件编译成功
✓ 无错误，仅有6个预期警告
```

### 功能验证
```
✓ UserManager: qaz1 账号初始化
✓ UserManager: qaz2 账号初始化
✓ UserManager: IsTestAccount 方法
✓ ItemInitializer: GetAllItems 方法
✓ ItemInitializer: GetTestAccountInventory 方法
✓ InventoryStore: SyncTestAccountInventory 方法
✓ GameServer: InventoryStore 初始化传递 UserManager
```

### 道具列表验证
```
✓ 11种道具配置完成
✓ 装备类道具数量: 10
✓ 金币数量: 9999
```

---

## 创建的文档

1. **[docs/TEST_ACCOUNTS.md](TEST_ACCOUNTS.md)**
   - 完整的测试账号说明文档
   - 包含技术实现细节
   - 使用方法和注意事项

2. **[docs/TEST_ACCOUNTS_QUICK_REFERENCE.md](TEST_ACCOUNTS_QUICK_REFERENCE.md)**
   - 快速参考文档
   - 登录信息和道具列表

3. **[test_accounts_verify.sh](../test_accounts_verify.sh)**
   - 自动验证脚本
   - 检查实现完整性

4. **更新 [docs/INDEX.md](INDEX.md)**
   - 添加测试账号文档链接

---

## 日志输出示例

当测试账号登录时，服务器会输出：
```
[Server] Test accounts initialized: qaz1, qaz2
[Server] User 'qaz1' logged in successfully
```

当检测到新道具时：
```
[InventoryStore] Added new item '圣火' to test account <userId>
[InventoryStore] Added new item '刮痧师傅' to test account <userId>
```

---

## 优势特性

### 1. 开发便利性
- 无需手动添加道具进行测试
- 新道具自动同步，无需额外操作
- 两个账号可同时登录进行多人测试

### 2. 可维护性
- 集中管理道具列表
- 添加新道具流程简单明确
- 自动同步机制确保一致性

### 3. 可扩展性
- 支持未来无限添加新道具
- 不影响普通用户的游戏体验
- 易于扩展测试账号数量

---

## 注意事项

1. **仅供测试使用**：测试账号仅用于开发和测试，不应在生产环境使用
2. **数据持久化**：账号数据保存在 `data/users/` 目录
3. **手动重置**：如需重置，删除对应的 JSON 文件即可
4. **并发支持**：两个测试账号支持同时登录

---

## 后续建议

### 可选优化
1. 添加更多测试账号（如需要）
2. 支持通过配置文件定义测试账号
3. 添加管理界面查看测试账号状态
4. 实现测试账号的一键重置功能

### 维护提醒
- 每次添加新道具时，记得更新 `GetAllItems()` 方法
- 确保新道具在 `CreateItemFromStackData()` 中有对应的创建逻辑
- 定期检查测试账号的道具列表是否完整

---

## 总结

✅ **成功实现**：两个常驻测试账号 qaz1 和 qaz2  
✅ **自动填充**：包含所有 11 种已实现的道具  
✅ **自动同步**：未来新道具会自动添加到测试账号  
✅ **文档完善**：提供完整的使用和技术文档  
✅ **验证通过**：编译成功，功能验证完整  

测试账号系统已完全就绪，可立即投入使用！

---

**实现日期**：2026年1月22日  
**实现者**：GitHub Copilot  
**验证状态**：✅ 完成并通过验证
