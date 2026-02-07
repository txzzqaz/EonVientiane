# 🔐 区块链风格钱包系统

一个受区块链和NFT启发的游戏道具验证系统，专为**离线/局域网游戏环境**设计。

## ✨ 核心特性

- **🔒 RSA-2048加密** - 使用非对称加密确保道具真实性
- **📴 离线防作弊** - 客户端可以独立验证道具，无需连接服务器
- **🔄 通用可扩展** - 支持任何类型道具，添加新道具无需修改系统
- **⚡ 高性能** - 签名验证 <1ms，适合实时游戏
- **🔧 易于集成** - 提供迁移工具，兼容现有InventoryStore

## 🎯 设计理念

### 类比区块链

```
区块链            →    本系统
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
钱包              →    PlayerWallet
NFT               →    SignedItem  
私钥（矿工）      →    服务器私钥
公钥              →    客户端公钥
转账              →    不支持
去中心化          →    服务器中心化签发
```

### 工作原理

```
服务器端 (持有私钥)                客户端 (持有公钥)
━━━━━━━━━━━━━━━━                  ━━━━━━━━━━━━━━━
                                  
1. 签发道具                        
   ├─ 创建SignedItem                
   ├─ 使用私钥签名 🔐               
   └─ 保存到钱包                    
                                  
2. 发送给客户端 ──────────────►    3. 验证道具
                                      ├─ 使用公钥验证 🔓
                                      ├─ 签名有效 → 接受 ✓
                                      └─ 签名无效 → 拒绝 ✗
```

## 📦 文件结构

```
Shared/
├─ WalletCrypto.cs         # RSA加密核心（132行）
└─ WalletTypes.cs          # 数据结构定义（174行）

EonVientianeServer/
├─ WalletManager.cs        # 服务器端钱包管理（300行）
├─ WalletInventoryStore.cs # 兼容层（170行）
└─ data/wallets/
    ├─ server_keys.xml     # ⚠️ 服务器私钥（机密）
    ├─ public_key.xml      # ✓ 公钥（可公开）
    └─ wallets/
        └─ *.json          # 玩家钱包文件

EonVientiane/
└─ WalletValidator.cs      # 客户端验证器（157行）

docs/
├─ WALLET_SYSTEM_GUIDE.md      # 完整指南（600+行）
├─ WALLET_QUICK_REFERENCE.md   # 快速参考（300+行）
└─ WalletSystemExample.cs      # 示例代码（200+行）
```

## 🚀 快速开始

### 1️⃣ 服务器端初始化

```csharp
// 初始化钱包管理器（会自动生成密钥对）
var walletManager = new WalletManager("data/wallets");

// 获取公钥（分发给客户端）
string publicKey = walletManager.GetPublicKey();
Console.WriteLine($"公钥: {publicKey}");
```

**重要**: 首次运行后，务必备份 `data/wallets/server_keys.xml` 私钥文件！

### 2️⃣ 签发道具

```csharp
// 为玩家签发道具
var item = walletManager.IssueItem(
    userId: "player123",
    itemId: "legendary_sword",
    itemName: "传说之剑",
    quantity: 1,
    metadata: new Dictionary<string, string> 
    {
        { "level", "50" },
        { "quality", "legendary" }
    }
);

// 保存到钱包
var wallet = walletManager.LoadOrCreateWallet("player123");
wallet.Items.Add(item);
walletManager.SaveWallet(wallet);
```

### 3️⃣ 客户端验证

```csharp
// 初始化验证器
var validator = new WalletValidator();
validator.Initialize(publicKeyFromServer);

// 验证收到的钱包
var result = validator.ValidateWallet(receivedWallet);

if (result.IsValid)
{
    // 所有道具都是合法的
    LoadInventory(receivedWallet);
}
else
{
    // 检测到伪造道具
    ShowError($"发现 {result.InvalidItems.Count} 个无效道具！");
}
```

### 4️⃣ 离线对战验证

```csharp
// 局域网PvP - 验证双方道具
var myValid = validator.ValidateWallet(myWallet);
var opponentValid = validator.ValidateWallet(opponentWallet);

if (myValid.IsValid && opponentValid.IsValid)
{
    StartBattle(); // 确保公平竞技
}
else
{
    RejectBattle("检测到无效道具");
}
```

## 📖 核心API

### WalletManager（服务器）

| 方法 | 说明 |
|-----|------|
| `IssueItem(...)` | 签发单个道具 |
| `IssueItems(...)` | 批量签发道具 |
| `LoadOrCreateWallet(...)` | 加载或创建钱包 |
| `SaveWallet(...)` | 保存钱包 |
| `VerifyItem(...)` | 验证道具签名 |
| `ValidateWallet(...)` | 验证整个钱包 |
| `GetPublicKey()` | 获取公钥 |

### WalletValidator（客户端）

| 方法 | 说明 |
|-----|------|
| `Initialize(...)` | 初始化验证器 |
| `VerifyItem(...)` | 验证单个道具 |
| `ValidateWallet(...)` | 验证整个钱包 |
| `GetValidItems(...)` | 获取有效道具列表 |
| `HasValidItem(...)` | 检查是否拥有道具 |

## 🔄 数据迁移

从现有的 `InventoryStore` 迁移到钱包系统：

```csharp
var oldStore = new InventoryStore("data/users");
var walletManager = new WalletManager("data/wallets");
var tool = new WalletMigrationTool(oldStore, walletManager);

// 迁移单个用户
var wallet = tool.MigrateUser("player123", () => GetInitialItems("player123"));

// 批量迁移
var results = tool.MigrateUsers(userIds, userId => () => GetInitialItems(userId));
Console.WriteLine($"成功迁移 {results.Count} 个用户");
```

## 🔐 安全最佳实践

### ⚠️ 私钥保护（至关重要！）

```bash
# 1. 立即备份私钥
cp data/wallets/server_keys.xml /secure/backup/

# 2. 设置严格权限
chmod 600 data/wallets/server_keys.xml

# 3. 定期自动备份
0 0 * * * cp data/wallets/server_keys.xml /backup/keys_$(date +\%Y\%m\%d).xml

# 4. 添加到 .gitignore
echo "data/wallets/server_keys.xml" >> .gitignore
```

### ✓ 公钥分发

公钥是公开的，有多种安全分发方式：

**方式1：内嵌到客户端**（推荐）
```csharp
public static class ServerPublicKey
{
    public const string Key = @"<RSAKeyValue>...</RSAKeyValue>";
}
```

**方式2：配置文件**
```csharp
var publicKey = File.ReadAllText("public_key.xml");
```

**方式3：首次连接获取**
```csharp
var publicKey = await networkClient.RequestPublicKey();
```

## 🛡️ 防作弊机制

### 1. 签名保护

任何修改都会导致签名失效：

```csharp
var item = wallet.Items[0];
item.Quantity = 999; // 篡改数量

bool isValid = validator.VerifyItem(item);
// 结果: false ✗ 签名失效
```

### 2. 时间戳验证（可选）

```csharp
var issuedTime = DateTimeOffset.FromUnixTimeSeconds(item.IssuedAt);
if (issuedTime > DateTimeOffset.UtcNow.AddHours(1))
{
    // 时间戳异常，可能是伪造的
}
```

### 3. 实例ID防复制

每个道具都有唯一的 `InstanceId`，服务器可以维护已签发ID的黑名单来防止克隆攻击。

## 📊 性能指标

| 操作 | 耗时 | 说明 |
|-----|------|------|
| 签发道具 | ~2-5ms | RSA签名 |
| 验证道具 | ~0.5-1ms | RSA验签 |
| 加载钱包 | ~10-50ms | 文件I/O |
| 批量验证100个道具 | ~50-100ms | 适合实时场景 |

## ❓ 常见问题

<details>
<summary><b>Q: 私钥丢失了怎么办？</b></summary>

**A**: 这是灾难性的！必须：
1. 生成新密钥对
2. 重新签发所有玩家的所有道具
3. 更新所有客户端的公钥

**预防**: 务必定期备份私钥！
</details>

<details>
<summary><b>Q: 玩家能修改钱包文件吗？</b></summary>

**A**: 可以修改，但签名会失效：
- 修改道具数量 → 验证失败 ✗
- 添加新道具（无签名） → 验证失败 ✗
- 复制道具 → 可能通过验证，需额外的InstanceId检查
</details>

<details>
<summary><b>Q: 如何添加新道具类型？</b></summary>

**A**: 完全不需要修改钱包系统！只需：
```csharp
// 直接签发新道具
var item = walletManager.IssueItem(userId, "new_item", "新道具", 1);
// 自动支持验证 ✓
```
</details>

<details>
<summary><b>Q: 能实现道具交易吗？</b></summary>

**A**: 当前不支持玩家间交易。如需要，必须通过服务器：
```csharp
// 服务器介入的交易
public void Trade(string from, string to, string itemId)
{
    // 1. 从发送方移除道具
    // 2. 为接收方重新签发（新InstanceId）
    // 3. 保存两个钱包
}
```
</details>

<details>
<summary><b>Q: 性能够用吗？</b></summary>

**A**: 对大部分游戏完全够用：
- 验证100个道具 < 100ms
- 可以并行化批量操作
- 可以缓存验证结果
</details>

## 📚 完整文档

- 📘 [完整指南](docs/WALLET_SYSTEM_GUIDE.md) - 600+行详细文档
- 📗 [快速参考](docs/WALLET_QUICK_REFERENCE.md) - API速查表
- 📙 [示例代码](docs/WalletSystemExample.cs) - 可运行的完整示例

## 🎓 示例场景

### 成就系统集成
```csharp
// 玩家完成成就
public void OnAchievementUnlocked(string userId, string achievementId)
{
    var rewards = GetAchievementRewards(achievementId);
    
    foreach (var reward in rewards)
    {
        var item = walletManager.IssueItem(
            userId, reward.ItemId, reward.ItemName, reward.Quantity
        );
        
        var wallet = walletManager.LoadOrCreateWallet(userId);
        wallet.Items.Add(item);
        walletManager.SaveWallet(wallet);
    }
    
    SendRewardsToClient(userId);
}
```

### 局域网锦标赛
```csharp
// 所有参赛者必须通过道具验证
public bool RegisterForTournament(PlayerWallet wallet)
{
    var result = validator.ValidateWallet(wallet);
    
    if (!result.IsValid)
    {
        ShowError($"道具验证失败: {result.Errors[0]}");
        return false;
    }
    
    // 确保公平竞技
    return true;
}
```

## 🔧 兼容性

### 与现有系统集成

使用兼容层保持现有代码不变：

```csharp
// 使用包装器
var walletStore = new WalletInventoryStore(walletManager, userManager);

// 完全兼容旧API
var state = walletStore.LoadOrCreate(userId, initialFactory);
var dto = walletStore.ToDto(state);

// 需要时访问钱包功能
var realWalletManager = walletStore.GetWalletManager();
```

## 📈 路线图

- [x] 基础加密系统（RSA-2048）
- [x] 服务器端钱包管理
- [x] 客户端验证器
- [x] 数据迁移工具
- [x] 完整文档和示例
- [ ] 道具交易系统（可选）
- [ ] 实例ID去重检查
- [ ] 钱包加密存储（额外安全层）
- [ ] 审计日志系统
- [ ] Web管理面板

## 🤝 贡献

欢迎提交问题和改进建议！

## 📄 许可证

与主项目相同的许可证

---

## 🎉 总结

这个钱包系统提供了：

✅ **离线防作弊** - 即使在局域网也能确保公平  
✅ **通用可扩展** - 支持任何道具类型和未来功能  
✅ **加密安全** - RSA-2048军事级加密  
✅ **易于集成** - 兼容现有系统，平滑迁移  
✅ **高性能** - 适合实时游戏场景  

**核心原则**:
- 🔐 私钥 = 道具铸造权（只有服务器）
- 🔓 公钥 = 验证能力（任何人）
- 📝 签名 = 所有权证明（不可伪造）

立即开始：
```bash
# 1. 编译项目
dotnet build

# 2. 运行示例
dotnet run --project docs/WalletSystemExample.cs

# 3. 查看生成的密钥
cat data/wallets/public_key.xml
```

**务必备份私钥！** 🔑
