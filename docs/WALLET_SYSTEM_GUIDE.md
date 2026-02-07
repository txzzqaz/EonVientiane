# 区块链风格钱包系统 - 完整指南

## 📋 目录

1. [系统概述](#系统概述)
2. [核心概念](#核心概念)
3. [架构设计](#架构设计)
4. [快速开始](#快速开始)
5. [服务器端使用](#服务器端使用)
6. [客户端使用](#客户端使用)
7. [迁移指南](#迁移指南)
8. [安全性说明](#安全性说明)
9. [常见问题](#常见问题)

---

## 系统概述

这是一个受区块链和NFT启发的道具所有权验证系统，专为**离线/局域网游戏环境**设计，确保即使在没有中央服务器的情况下，玩家也无法伪造或篡改道具。

### 核心特性

- ✅ **加密签名验证** - 使用RSA-2048非对称加密
- ✅ **离线防作弊** - 客户端可以独立验证道具真实性
- ✅ **通用且可扩展** - 支持任何类型的道具，未来添加新道具无需修改系统
- ✅ **向后兼容** - 提供迁移工具，可从现有的InventoryStore无缝升级
- ✅ **零交易设计** - 不支持玩家间转账，只有服务器可以签发道具

### 与区块链的类比

| 区块链概念 | 本系统对应 | 说明 |
|----------|----------|------|
| 钱包 | PlayerWallet | 存储玩家的所有道具 |
| NFT | SignedItem | 每个道具都有唯一签名 |
| 私钥 | 服务器私钥 | 只有服务器能签发道具 |
| 公钥 | 客户端公钥 | 任何人都可验证道具 |
| 矿工 | 不存在 | 只有服务器能创建道具 |
| 转账 | 不支持 | 道具不能在玩家间转移 |

---

## 核心概念

### 1. SignedItem（签名道具）

每个道具都是一个独立的加密实体：

```csharp
public class SignedItem
{
    public string ItemId { get; set; }        // 道具类型（如 "d6_dice"）
    public string ItemName { get; set; }      // 道具名称（如 "六面骰"）
    public int Quantity { get; set; }         // 数量
    public string InstanceId { get; set; }    // 唯一实例ID（类似NFT的Token ID）
    public long IssuedAt { get; set; }        // 签发时间戳
    public bool IsEquipped { get; set; }      // 是否已装备
    public string Signature { get; set; }     // 服务器数字签名 ⭐
    public Dictionary<string, string>? Metadata { get; set; }  // 扩展属性
}
```

**签名覆盖内容**：除了`Signature`字段外的所有数据，确保任何修改都会导致签名失效。

### 2. PlayerWallet（玩家钱包）

玩家的道具容器：

```csharp
public class PlayerWallet
{
    public string UserId { get; set; }           // 钱包所有者
    public List<SignedItem> Items { get; set; }  // 所有道具
    public int Version { get; set; }             // 钱包格式版本
    public long LastUpdated { get; set; }        // 最后更新时间
}
```

### 3. 密钥系统

```
服务器端：
  ├─ 私钥（server_keys.xml）       ⚠️ 机密，用于签发道具
  └─ 公钥（public_key.xml）        ✓ 公开，用于验证签名

客户端：
  └─ 公钥（内嵌到客户端代码）      ✓ 用于离线验证
```

---

## 架构设计

### 文件结构

```
Shared/
  ├─ WalletCrypto.cs           # RSA加密核心
  └─ WalletTypes.cs            # 数据结构定义

EonVientianeServer/
  ├─ WalletManager.cs          # 服务器端钱包管理
  ├─ WalletInventoryStore.cs   # 兼容层（可选）
  └─ data/
      ├─ server_keys.xml       # 服务器私钥 ⚠️
      ├─ public_key.xml        # 服务器公钥 ✓
      └─ wallets/
          ├─ user1_wallet.json
          └─ user2_wallet.json

EonVientiane/
  └─ WalletValidator.cs        # 客户端验证器
```

### 数据流

```
道具签发流程：
1. 服务器接收到"给玩家道具"的请求
2. WalletManager.IssueItem() 创建SignedItem
3. 使用私钥对道具数据签名
4. 保存到PlayerWallet
5. 发送给客户端

客户端验证流程：
1. 客户端收到道具数据
2. WalletValidator.VerifyItem() 检查签名
3. 使用公钥验证签名
4. 只接受验证通过的道具
```

---

## 快速开始

### 第一步：服务器初始化

```csharp
// 在GameServer构造函数中
public class GameServer
{
    private WalletManager _walletManager;
    
    public GameServer(int port = 7777)
    {
        // 初始化钱包管理器（会自动生成密钥对）
        _walletManager = new WalletManager("data/wallets");
        
        // 获取公钥用于分发给客户端
        string publicKey = _walletManager.GetPublicKey();
        Console.WriteLine($"[Server] Public Key:\n{publicKey}");
    }
}
```

**首次运行后**，服务器会生成：
- `data/wallets/server_keys.xml` - **私钥（务必备份并保密！）**
- `data/wallets/public_key.xml` - 公钥（可以公开）

### 第二步：客户端初始化

```csharp
// 在Game1.LoadContent()或启动时
public class Game1 : Game
{
    private WalletValidator _walletValidator;
    
    protected override void LoadContent()
    {
        _walletValidator = new WalletValidator();
        
        // 使用服务器的公钥初始化（应该内嵌到客户端代码中）
        string publicKey = GetEmbeddedPublicKey(); // 从配置或资源文件读取
        _walletValidator.Initialize(publicKey);
    }
    
    private string GetEmbeddedPublicKey()
    {
        // TODO: 从 data/wallets/public_key.xml 复制内容到这里
        return @"<RSAKeyValue>...</RSAKeyValue>";
    }
}
```

---

## 服务器端使用

### 签发道具

```csharp
// 方式1：直接签发单个道具
var item = _walletManager.IssueItem(
    userId: "player123",
    itemId: "d6_dice",
    itemName: "六面骰",
    quantity: 1
);

// 方式2：签发带扩展属性的道具
var metadata = new Dictionary<string, string>
{
    { "quality", "legendary" },
    { "level", "10" }
};
var item = _walletManager.IssueItem(
    userId: "player123",
    itemId: "magic_sword",
    itemName: "魔法剑",
    quantity: 1,
    metadata: metadata
);

// 方式3：批量签发
var requests = new List<IssueItemRequest>
{
    new() { ItemId = "d6_dice", ItemName = "六面骰", Quantity = 3 },
    new() { ItemId = "health_potion", ItemName = "生命药水", Quantity = 10 }
};
var items = _walletManager.IssueItems("player123", requests);
```

### 加载和保存钱包

```csharp
// 加载或创建钱包
var initialItems = new List<InitialInventoryItem>
{
    new() { ItemId = "d6_dice", ItemName = "六面骰", Quantity = 1 }
};
var wallet = _walletManager.LoadOrCreateWallet("player123", initialItems);

// 修改钱包
wallet.Items.Add(newItem);

// 保存钱包
_walletManager.SaveWallet(wallet);
```

### 验证道具

```csharp
// 验证单个道具
bool isValid = _walletManager.VerifyItem(item);

// 验证整个钱包
var result = _walletManager.ValidateWallet(wallet);
if (!result.IsValid)
{
    Console.WriteLine($"发现{result.InvalidItems.Count}个无效道具:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### 使用兼容层（推荐用于迁移）

```csharp
// 如果想保持现有代码不变，使用包装器
var walletStore = new WalletInventoryStore(_walletManager, _userManager);

// 完全兼容旧的InventoryStore接口
var state = walletStore.LoadOrCreate(userId, () => ItemInitializer.GetInitialInventory(userId));
var dto = walletStore.ToDto(state);

// 需要直接访问钱包功能时
var realWalletManager = walletStore.GetWalletManager();
var wallet = realWalletManager.LoadOrCreateWallet(userId);
```

---

## 客户端使用

### 验证收到的道具

```csharp
// 收到服务器发来的钱包数据
PlayerWallet receivedWallet = DeserializeFromServer(message);

// 验证整个钱包
var result = _walletValidator.ValidateWallet(receivedWallet);

if (result.IsValid)
{
    // 所有道具都是合法的，可以使用
    LoadInventoryFromWallet(receivedWallet);
}
else
{
    // 发现伪造或篡改的道具
    Console.WriteLine("警告：检测到无效道具！");
    
    // 只使用验证通过的道具
    var validItems = _walletValidator.GetValidItems(receivedWallet);
    LoadInventoryFromItems(validItems);
}
```

### 离线/局域网模式

```csharp
// 在局域网对战或离线PVE中
public void StartOfflineBattle(PlayerWallet localWallet, PlayerWallet opponentWallet)
{
    // 验证双方的道具
    var localValid = _walletValidator.ValidateWallet(localWallet);
    var opponentValid = _walletValidator.ValidateWallet(opponentWallet);
    
    if (!localValid.IsValid || !opponentValid.IsValid)
    {
        ShowError("检测到无效道具，无法开始对战！");
        return;
    }
    
    // 验证通过，开始对战
    StartBattle(localWallet, opponentWallet);
}
```

### 检查特定道具

```csharp
// 检查是否拥有某个道具
bool hasDice = _walletValidator.HasValidItem(wallet, "d6_dice");

// 获取道具数量
int diceCount = _walletValidator.GetValidItemQuantity(wallet, "d6_dice");
```

---

## 迁移指南

### 从旧的InventoryStore迁移

```csharp
// 创建迁移工具
var oldStore = new InventoryStore("data/users", _userManager);
var walletManager = new WalletManager("data/wallets");
var migrationTool = new WalletMigrationTool(oldStore, walletManager);

// 方式1：迁移单个用户
var wallet = migrationTool.MigrateUser(
    "player123",
    () => ItemInitializer.GetInitialInventory("player123")
);

// 方式2：批量迁移
var userIds = new List<string> { "player1", "player2", "player3" };
var results = migrationTool.MigrateUsers(
    userIds,
    userId => () => ItemInitializer.GetInitialInventory(userId)
);

Console.WriteLine($"成功迁移 {results.Count} 个用户");
```

### 渐进式迁移策略

```csharp
// 在GameServer中同时保持两个系统
private InventoryStore _inventoryStore;  // 旧系统
private WalletManager _walletManager;    // 新系统

// 根据用户选择使用不同系统
private bool UseWalletSystem(string userId)
{
    // 可以基于用户设置、AB测试等决定
    return _userConfig.GetBool(userId, "use_wallet_system", false);
}

private void HandleRequestInventory(string userId)
{
    if (UseWalletSystem(userId))
    {
        // 使用新钱包系统
        var wallet = _walletManager.LoadOrCreateWallet(userId);
        SendWalletToClient(wallet);
    }
    else
    {
        // 使用旧库存系统
        var inventory = _inventoryStore.LoadOrCreate(userId, ...);
        SendInventoryToClient(inventory);
    }
}
```

---

## 安全性说明

### ⚠️ 私钥保护

**服务器私钥是整个系统的安全基石！**

1. **备份私钥**：
   ```bash
   # 立即备份
   cp data/wallets/server_keys.xml /secure/backup/location/
   
   # 定期自动备份
   0 0 * * * cp data/wallets/server_keys.xml /backup/server_keys_$(date +\%Y\%m\%d).xml
   ```

2. **限制访问权限**：
   ```bash
   chmod 600 data/wallets/server_keys.xml
   chown gameserver:gameserver data/wallets/server_keys.xml
   ```

3. **环境隔离**：
   - 开发环境使用不同的密钥对
   - 生产环境的私钥不要提交到版本控制

4. **密钥轮换**（高级）：
   如果怀疑私钥泄露：
   ```csharp
   // 生成新密钥对
   var newWalletManager = new WalletManager("data/wallets_v2");
   
   // 重新签发所有道具
   foreach (var userId in allUsers)
   {
       var oldWallet = oldWalletManager.LoadOrCreateWallet(userId);
       var newWallet = newWalletManager.LoadOrCreateWallet(userId);
       
       foreach (var item in oldWallet.Items)
       {
           var newItem = newWalletManager.IssueItem(
               userId, item.ItemId, item.ItemName, item.Quantity
           );
           newWallet.Items.Add(newItem);
       }
       
       newWalletManager.SaveWallet(newWallet);
   }
   ```

### ✓ 客户端公钥分发

公钥是公开的，有多种分发方式：

**方式1：内嵌到代码**（推荐）
```csharp
public static class ServerPublicKey
{
    public const string Key = @"<RSAKeyValue>
        <Modulus>...</Modulus>
        <Exponent>AQAB</Exponent>
    </RSAKeyValue>";
}
```

**方式2：配置文件**
```csharp
// 在客户端目录创建 public_key.xml
var publicKey = File.ReadAllText("public_key.xml");
_walletValidator.Initialize(publicKey);
```

**方式3：首次连接时从服务器获取**
```csharp
// 客户端首次连接时
var publicKey = await _networkClient.RequestPublicKey();
SavePublicKeyToCache(publicKey);
_walletValidator.Initialize(publicKey);
```

### 🔒 防篡改机制

1. **签名覆盖所有关键数据**：
   - 道具ID、名称、数量、实例ID、时间戳
   - 任何修改都会导致签名失效

2. **时间戳防重放**：
   ```csharp
   // 可选：检查道具签发时间是否合理
   var issuedTime = DateTimeOffset.FromUnixTimeSeconds(item.IssuedAt);
   if (issuedTime > DateTimeOffset.UtcNow.AddHours(1))
   {
       // 时间戳异常，可能是伪造的
       return false;
   }
   ```

3. **实例ID防复制**：
   - 每个道具都有唯一的InstanceId
   - 可以在服务器端维护已签发ID的记录，防止克隆

---

## 常见问题

### Q1: 如果私钥丢失怎么办？

**A**: 这是灾难性的！所有现有道具的签名都无法验证。必须：
1. 生成新的密钥对
2. 重新签发所有玩家的所有道具
3. 更新所有客户端的公钥

**预防措施**：定期备份私钥到多个安全位置。

### Q2: 玩家可以修改自己的钱包文件吗？

**A**: 可以修改，但无法伪造签名！
- 玩家修改道具数量：签名失效 ❌
- 玩家添加新道具：没有服务器签名 ❌
- 玩家复制道具：签名验证会通过，但服务器可以通过InstanceId检测重复 ⚠️

### Q3: 这个系统的性能如何？

**A**: RSA签名和验证的性能：
- 签发道具（签名）：~2-5ms per item
- 验证道具（验签）：~0.5-1ms per item
- 对于大部分游戏场景完全够用

优化建议：
```csharp
// 批量签发时可以并行化
var items = requests.AsParallel().Select(req => 
    _walletManager.IssueItem(userId, req.ItemId, req.ItemName, req.Quantity)
).ToList();
```

### Q4: 如何添加新道具类型？

**A**: 完全不需要修改钱包系统！只需：
```csharp
// 1. 在ItemInitializer中注册新道具
("new_item_id", "新道具名称")

// 2. 签发道具
var item = _walletManager.IssueItem(userId, "new_item_id", "新道具名称", 1);

// 3. 客户端验证（自动支持）
var isValid = _walletValidator.VerifyItem(item);  // ✓ 自动验证
```

### Q5: 可以给道具添加自定义属性吗？

**A**: 可以！使用Metadata字段：
```csharp
var metadata = new Dictionary<string, string>
{
    { "level", "50" },
    { "quality", "legendary" },
    { "durability", "100/100" },
    { "enchantments", "fire,ice,lightning" }
};

var item = _walletManager.IssueItem(
    userId, "magic_sword", "传说之剑", 1, metadata
);

// Metadata也会被签名保护，无法篡改
```

### Q6: 局域网对战时如何验证对手？

**A**: 完整流程：
```csharp
// 双方交换钱包数据
public void OnOpponentConnected(PlayerWallet opponentWallet)
{
    // 1. 验证对手的钱包
    var result = _walletValidator.ValidateWallet(opponentWallet);
    
    if (!result.IsValid)
    {
        // 拒绝对战
        DisconnectOpponent("检测到无效道具");
        return;
    }
    
    // 2. 检查道具是否在允许范围内
    foreach (var item in opponentWallet.Items)
    {
        if (!IsItemAllowedInPvP(item.ItemId))
        {
            DisconnectOpponent($"道具 {item.ItemName} 不允许用于PvP");
            return;
        }
    }
    
    // 3. 验证通过，开始对战
    StartBattle(myWallet, opponentWallet);
}
```

### Q7: 能否实现道具交易？

**A**: 当前设计**不支持**玩家间交易。如果需要：

```csharp
// 需要服务器介入的交易系统
public void TradeItem(string fromUserId, string toUserId, string itemInstanceId)
{
    var fromWallet = _walletManager.LoadOrCreateWallet(fromUserId);
    var toWallet = _walletManager.LoadOrCreateWallet(toUserId);
    
    // 1. 找到要交易的道具
    var item = fromWallet.Items.Find(i => i.InstanceId == itemInstanceId);
    if (item == null) return;
    
    // 2. 从发送方移除
    fromWallet.Items.Remove(item);
    
    // 3. 为接收方重新签发（新的InstanceId）
    var newItem = _walletManager.IssueItem(
        toUserId, item.ItemId, item.ItemName, item.Quantity, item.Metadata
    );
    toWallet.Items.Add(newItem);
    
    // 4. 保存两个钱包
    _walletManager.SaveWallet(fromWallet);
    _walletManager.SaveWallet(toWallet);
}
```

---

## 总结

这个钱包系统提供了：

✅ **离线防作弊** - 无需中央服务器也能验证道具  
✅ **通用可扩展** - 支持任何道具类型和未来功能  
✅ **加密安全** - RSA-2048强加密保护  
✅ **易于集成** - 兼容现有系统，提供迁移工具  

核心原则：
- 🔐 **私钥 = 道具铸造权** - 只有服务器能签发道具
- 🔓 **公钥 = 验证能力** - 任何人都能验证道具真伪
- 📝 **签名 = 所有权证明** - 道具的"数字出生证"

**下一步**：
1. 在测试环境部署钱包系统
2. 使用迁移工具转换现有数据
3. 在客户端集成验证器
4. 进行充分测试后上线生产环境
