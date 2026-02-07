# 钱包系统快速参考

## 🚀 30秒快速上手

### 服务器端
```csharp
// 1. 初始化（只需一次）
var walletManager = new WalletManager("data/wallets");

// 2. 签发道具
var item = walletManager.IssueItem("user123", "d6_dice", "六面骰", 1);

// 3. 保存到钱包
var wallet = walletManager.LoadOrCreateWallet("user123");
wallet.Items.Add(item);
walletManager.SaveWallet(wallet);
```

### 客户端
```csharp
// 1. 初始化（只需一次）
var validator = new WalletValidator();
validator.Initialize(publicKeyFromServer);

// 2. 验证道具
bool isValid = validator.VerifyItem(item);

// 3. 验证整个钱包
var result = validator.ValidateWallet(wallet);
if (result.IsValid) { /* 可以使用 */ }
```

---

## 📚 常用API

### WalletManager（服务器端）

| 方法 | 说明 | 示例 |
|-----|------|------|
| `IssueItem(...)` | 签发单个道具 | `IssueItem("user1", "dice", "骰子", 1)` |
| `IssueItems(...)` | 批量签发 | `IssueItems("user1", requests)` |
| `LoadOrCreateWallet(...)` | 加载/创建钱包 | `LoadOrCreateWallet("user1", initItems)` |
| `SaveWallet(...)` | 保存钱包 | `SaveWallet(wallet)` |
| `VerifyItem(...)` | 验证道具签名 | `VerifyItem(item)` |
| `ValidateWallet(...)` | 验证整个钱包 | `ValidateWallet(wallet)` |
| `GetPublicKey()` | 获取公钥 | `GetPublicKey()` |

### WalletValidator（客户端）

| 方法 | 说明 | 示例 |
|-----|------|------|
| `Initialize(...)` | 初始化验证器 | `Initialize(publicKey)` |
| `VerifyItem(...)` | 验证单个道具 | `VerifyItem(item)` |
| `ValidateWallet(...)` | 验证整个钱包 | `ValidateWallet(wallet)` |
| `GetValidItems(...)` | 获取有效道具列表 | `GetValidItems(wallet)` |
| `HasValidItem(...)` | 检查是否拥有道具 | `HasValidItem(wallet, "dice")` |
| `GetValidItemQuantity(...)` | 获取道具数量 | `GetValidItemQuantity(wallet, "dice")` |

---

## 🔑 关键文件

```
data/wallets/
├── server_keys.xml       ⚠️  服务器私钥（机密！务必备份）
├── public_key.xml        ✓  公钥（可公开分发）
└── wallets/
    ├── user1_wallet.json     用户钱包数据
    └── user2_wallet.json
```

**私钥备份命令**：
```bash
cp data/wallets/server_keys.xml /backup/server_keys_$(date +%Y%m%d).xml
```

---

## 💡 常见场景

### 场景1：玩家获得成就奖励
```csharp
// 服务器端
var item = walletManager.IssueItem(userId, "legendary_sword", "传说之剑", 1);
var wallet = walletManager.LoadOrCreateWallet(userId);
wallet.Items.Add(item);
walletManager.SaveWallet(wallet);

// 发送给客户端
SendToClient(wallet);
```

### 场景2：局域网对战验证
```csharp
// 客户端
public bool CanStartBattle(PlayerWallet myWallet, PlayerWallet opponentWallet)
{
    var myValid = validator.ValidateWallet(myWallet);
    var oppValid = validator.ValidateWallet(opponentWallet);
    
    return myValid.IsValid && oppValid.IsValid;
}
```

### 场景3：检查玩家道具
```csharp
// 服务器端
var wallet = walletManager.LoadOrCreateWallet(userId);
var hasDice = wallet.Items.Any(i => i.ItemId == "d6_dice" && walletManager.VerifyItem(i));

// 客户端
var hasDice = validator.HasValidItem(wallet, "d6_dice");
```

### 场景4：道具带属性
```csharp
var metadata = new Dictionary<string, string>
{
    { "level", "10" },
    { "quality", "legendary" }
};
var item = walletManager.IssueItem(userId, "sword", "魔剑", 1, metadata);
```

---

## ⚠️ 安全检查清单

- [ ] 私钥已备份到安全位置
- [ ] 私钥文件权限设置为600（仅所有者可读写）
- [ ] 开发/测试/生产环境使用不同的密钥对
- [ ] 私钥未提交到版本控制系统（添加到.gitignore）
- [ ] 客户端已内嵌或安全获取公钥
- [ ] 所有道具签发都通过WalletManager
- [ ] 客户端在使用道具前验证签名

---

## 🐛 故障排查

### 问题：签名验证失败
```csharp
// 检查1：公钥是否正确
Console.WriteLine(walletManager.GetPublicKey());

// 检查2：道具签名是否存在
if (string.IsNullOrEmpty(item.Signature))
    Console.WriteLine("道具未签名！");

// 检查3：手动验证
var data = item.GetSignableData();
var valid = walletManager.VerifyItem(item);
Console.WriteLine($"验证结果: {valid}");
```

### 问题：钱包加载失败
```csharp
// 检查钱包文件是否存在
var path = Path.Combine("data/wallets/wallets", $"{userId}_wallet.json");
Console.WriteLine($"钱包文件存在: {File.Exists(path)}");

// 检查JSON格式
try {
    var json = File.ReadAllText(path);
    var wallet = JsonSerializer.Deserialize<PlayerWallet>(json);
} catch (Exception ex) {
    Console.WriteLine($"JSON解析错误: {ex.Message}");
}
```

### 问题：私钥丢失
```csharp
// ⚠️ 如果私钥丢失，唯一的办法是：
// 1. 生成新密钥对
var newManager = new WalletManager("data/wallets_new");

// 2. 重新签发所有道具（需要从其他数据源恢复道具列表）
// 这会导致所有旧签名失效！
```

---

## 📊 性能参考

| 操作 | 耗时 | 说明 |
|-----|------|------|
| 签发道具 | ~2-5ms | RSA签名操作 |
| 验证道具 | ~0.5-1ms | RSA验签操作 |
| 加载钱包 | ~10-50ms | 包含文件I/O |
| 保存钱包 | ~10-50ms | 包含文件I/O |

**优化建议**：
- 批量操作使用并行化
- 缓存验证结果（道具内容不变时）
- 定期清理内存缓存

---

## 🔄 迁移工具

```csharp
// 从旧InventoryStore迁移
var oldStore = new InventoryStore("data/users");
var walletManager = new WalletManager("data/wallets");
var tool = new WalletMigrationTool(oldStore, walletManager);

// 单个用户
var wallet = tool.MigrateUser("user1", () => GetInitialItems("user1"));

// 批量迁移
var results = tool.MigrateUsers(userIds, userId => () => GetInitialItems(userId));
```

---

## 🆚 新旧系统对比

| 特性 | InventoryStore | WalletManager |
|-----|---------------|---------------|
| 防篡改 | ❌ | ✅ RSA签名 |
| 离线验证 | ❌ | ✅ |
| 扩展性 | ✓ | ✓✓ |
| 性能 | 快 | 稍慢（签名开销） |
| 安全性 | 低 | 高 |
| 复杂度 | 简单 | 中等 |

---

## 📞 快速问答

**Q**: 公钥可以公开吗？  
**A**: ✅ 可以，公钥就是设计用来公开的。

**Q**: 私钥可以重新生成吗？  
**A**: ⚠️ 可以，但会导致所有旧签名失效，需要重新签发所有道具。

**Q**: 玩家可以修改钱包文件吗？  
**A**: 可以修改，但签名会失效，验证不通过。

**Q**: 如何添加新道具？  
**A**: 直接签发即可，无需修改钱包系统代码。

**Q**: 性能够用吗？  
**A**: 对于大部分游戏场景完全够用（每个道具验证<1ms）。

**Q**: 可以交易道具吗？  
**A**: 当前不支持，需要服务器介入重新签发。

---

## 🎯 最佳实践

1. **总是验证道具** - 在使用道具前先验证签名
2. **定期备份私钥** - 至少每天备份一次
3. **使用环境隔离** - 开发/测试/生产使用不同密钥
4. **记录关键操作** - 记录所有签发/验证失败事件
5. **监控异常** - 如果发现大量验证失败，可能有安全问题

---

## 📖 完整文档

详细文档请参考：[docs/WALLET_SYSTEM_GUIDE.md](./WALLET_SYSTEM_GUIDE.md)
