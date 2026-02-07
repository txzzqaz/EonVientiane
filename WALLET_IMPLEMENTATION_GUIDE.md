# 钱包系统实施指南

## 🎯 实施概览

您现在拥有一个完整的、可用于生产环境的区块链风格钱包系统，专为保证离线/局域网对战公平性而设计。

## 📦 已创建的文件

### 核心代码（949行）

```
Shared/
├─ WalletCrypto.cs (136行)          # RSA加密核心
└─ WalletTypes.cs (174行)           # 数据结构

EonVientianeServer/
├─ WalletManager.cs (300行)         # 服务器钱包管理
└─ WalletInventoryStore.cs (182行)  # 兼容层

EonVientiane/
└─ WalletValidator.cs (157行)       # 客户端验证器
```

### 文档和示例（1500+行）

```
docs/
├─ WALLET_SYSTEM_GUIDE.md (600+行)      # 完整指南
├─ WALLET_QUICK_REFERENCE.md (300+行)   # 快速参考
└─ WalletSystemExample.cs (200+行)      # 示例代码

WALLET_SYSTEM_README.md (400+行)        # 系统总览
```

## 🚀 立即开始使用

### 选项A：快速测试（推荐先做）

```bash
# 1. 编译项目
cd /home/qazokmwsxijn/Documents/EonVientiane/EonVientiane
dotnet build

# 2. 创建测试脚本
cat > test_wallet.sh << 'EOF'
#!/bin/bash
cd "$(dirname "$0")"

# 测试钱包系统
cat > /tmp/wallet_test.cs << 'CSHARP'
using System;
using EonVientiane.Shared;
using EonVientianeServer;

class WalletTest {
    static void Main() {
        Console.WriteLine("=== 钱包系统测试 ===\n");
        
        // 初始化
        var wm = new WalletManager("data/wallets_test");
        Console.WriteLine("✓ 钱包管理器初始化");
        
        // 签发道具
        var item = wm.IssueItem("test_user", "test_item", "测试道具", 1);
        Console.WriteLine($"✓ 签发道具: {item.ItemName}");
        Console.WriteLine($"  签名: {item.Signature.Substring(0, 40)}...");
        
        // 验证
        bool valid = wm.VerifyItem(item);
        Console.WriteLine($"✓ 验证结果: {valid}");
        
        // 篡改测试
        item.Quantity = 999;
        bool tampered = wm.VerifyItem(item);
        Console.WriteLine($"✓ 篡改后验证: {tampered} (应为false)");
        
        Console.WriteLine("\n=== 测试完成 ===");
    }
}
CSHARP

# 编译并运行
dotnet build > /dev/null 2>&1
csc /tmp/wallet_test.cs \
    /r:Shared/bin/Debug/net8.0/Shared.dll \
    /r:EonVientianeServer/bin/Debug/net8.0/EonVientianeServer.dll \
    /out:/tmp/wallet_test.exe 2>/dev/null

if [ -f /tmp/wallet_test.exe ]; then
    mono /tmp/wallet_test.exe
else
    echo "编译失败，请检查项目"
fi
EOF

chmod +x test_wallet.sh
./test_wallet.sh
```

### 选项B：集成到现有系统

#### 步骤1：在GameServer中初始化

在 `EonVientianeServer/GameServer.cs` 中添加：

```csharp
public class GameServer
{
    private WalletManager _walletManager;
    
    public GameServer(int port = 7777)
    {
        // ... 现有代码 ...
        
        // 初始化钱包系统
        _walletManager = new WalletManager("data/wallets");
        Console.WriteLine($"[WalletManager] 公钥: {_walletManager.GetPublicKey().Substring(0, 50)}...");
    }
}
```

#### 步骤2：修改道具签发逻辑

将现有的道具分发改为使用钱包系统：

```csharp
// 旧代码（InventoryStore）
var state = _inventoryStore.LoadOrCreate(userId, initialFactory);

// 新代码（WalletManager）
var wallet = _walletManager.LoadOrCreateWallet(userId, initialItems);

// 或使用兼容层（无需修改现有代码）
var walletStore = new WalletInventoryStore(_walletManager, _userManager);
var state = walletStore.LoadOrCreate(userId, initialFactory); // 兼容旧接口
```

#### 步骤3：在成就系统中签发奖励

在 `EonVientianeServer/AchievementManager.cs` 中：

```csharp
public void GrantAchievementReward(string userId, string itemId, string itemName, int quantity)
{
    var item = _walletManager.IssueItem(userId, itemId, itemName, quantity);
    
    var wallet = _walletManager.LoadOrCreateWallet(userId);
    wallet.Items.Add(item);
    _walletManager.SaveWallet(wallet);
    
    Console.WriteLine($"[Achievement] Granted {itemName} to {userId}");
}
```

#### 步骤4：客户端集成

在 `EonVientiane/Game1.cs` 的 LoadContent 中：

```csharp
public class Game1 : Game
{
    private WalletValidator _walletValidator;
    
    protected override void LoadContent()
    {
        // ... 现有代码 ...
        
        // 初始化钱包验证器
        _walletValidator = new WalletValidator();
        
        // TODO: 从服务器获取公钥或使用内嵌的公钥
        // 临时方案：连接服务器后获取
        // string publicKey = await GetPublicKeyFromServer();
        // _walletValidator.Initialize(publicKey);
    }
    
    // 处理收到的钱包数据
    private void OnWalletReceived(PlayerWallet wallet)
    {
        var result = _walletValidator.ValidateWallet(wallet);
        
        if (result.IsValid)
        {
            LoadInventoryFromWallet(wallet);
        }
        else
        {
            ShowError($"检测到 {result.InvalidItems.Count} 个无效道具！");
        }
    }
}
```

### 选项C：数据迁移

如果您已有现有玩家数据：

```csharp
// 创建迁移脚本
public void MigrateAllUsers()
{
    var oldStore = new InventoryStore("data/users", _userManager);
    var walletManager = new WalletManager("data/wallets");
    var tool = new WalletMigrationTool(oldStore, walletManager);
    
    // 获取所有用户ID
    var userIds = Directory.GetFiles("data/users", "*_inventory.json")
        .Select(f => Path.GetFileNameWithoutExtension(f).Replace("_inventory", ""))
        .ToList();
    
    Console.WriteLine($"开始迁移 {userIds.Count} 个用户...");
    
    var results = tool.MigrateUsers(
        userIds,
        userId => () => ItemInitializer.GetInitialInventory(userId)
    );
    
    Console.WriteLine($"迁移完成: {results.Count}/{userIds.Count} 成功");
}
```

## 🔐 安全配置清单

在部署到生产环境前，请确保：

- [ ] **私钥已备份** 到多个安全位置
  ```bash
  cp data/wallets/server_keys.xml /backup/$(date +%Y%m%d)_server_keys.xml
  ```

- [ ] **私钥权限已设置**
  ```bash
  chmod 600 data/wallets/server_keys.xml
  chown youruser:yourgroup data/wallets/server_keys.xml
  ```

- [ ] **私钥已添加到 .gitignore**
  ```bash
  echo "data/wallets/server_keys.xml" >> .gitignore
  ```

- [ ] **公钥已分发** 给客户端
  - 内嵌到客户端代码（最安全）
  - 或在首次连接时获取并缓存

- [ ] **测试环境使用独立密钥**
  - 开发环境：`data/wallets_dev/`
  - 测试环境：`data/wallets_test/`
  - 生产环境：`data/wallets/`

## 📝 配置建议

### 服务器端配置

在服务器启动时记录关键信息：

```csharp
public GameServer(int port = 7777)
{
    _walletManager = new WalletManager("data/wallets");
    
    var publicKey = _walletManager.GetPublicKey();
    var publicKeyHash = WalletCrypto.GenerateFingerprint(publicKey);
    
    Console.WriteLine($"[WalletManager] 已初始化");
    Console.WriteLine($"[WalletManager] 公钥指纹: {publicKeyHash.Substring(0, 16)}...");
    Console.WriteLine($"[WalletManager] 确保客户端使用相同的公钥！");
}
```

### 客户端配置

创建配置类存储公钥：

```csharp
// EonVientiane/WalletConfig.cs
public static class WalletConfig
{
    // TODO: 首次部署后，将 data/wallets/public_key.xml 的内容粘贴到这里
    public const string ServerPublicKey = @"<RSAKeyValue>
        <Modulus>...</Modulus>
        <Exponent>AQAB</Exponent>
    </RSAKeyValue>";
    
    // 公钥指纹，用于验证是否使用了正确的公钥
    public const string PublicKeyFingerprint = "...";
}
```

## 🧪 测试计划

### 1. 单元测试

```csharp
[Test]
public void TestItemSigning()
{
    var wm = new WalletManager("data/test");
    var item = wm.IssueItem("user1", "item1", "测试", 1);
    
    Assert.IsTrue(wm.VerifyItem(item));
    
    item.Quantity = 999; // 篡改
    Assert.IsFalse(wm.VerifyItem(item));
}
```

### 2. 集成测试

- [ ] 服务器签发道具
- [ ] 客户端验证收到的道具
- [ ] 离线模式验证对手道具
- [ ] 篡改检测测试
- [ ] 迁移测试

### 3. 性能测试

```csharp
// 测试批量签发性能
var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    wm.IssueItem($"user{i}", "item", "道具", 1);
}
stopwatch.Stop();
Console.WriteLine($"签发1000个道具耗时: {stopwatch.ElapsedMilliseconds}ms");
```

## 📊 监控建议

在生产环境中监控以下指标：

```csharp
// 添加日志记录
public class WalletManager
{
    private static int _totalItemsIssued = 0;
    private static int _validationFailures = 0;
    
    public SignedItem IssueItem(...)
    {
        var item = /* ... */;
        _totalItemsIssued++;
        
        if (_totalItemsIssued % 1000 == 0)
        {
            Console.WriteLine($"[Metrics] 已签发道具总数: {_totalItemsIssued}");
        }
        
        return item;
    }
    
    public bool VerifyItem(SignedItem item)
    {
        var isValid = /* ... */;
        
        if (!isValid)
        {
            _validationFailures++;
            Console.WriteLine($"[Security] 验证失败! 累计: {_validationFailures}");
        }
        
        return isValid;
    }
}
```

## 🔄 升级路径

### 阶段1：试运行（建议2-4周）

- 在测试服务器部署钱包系统
- 选择10-20%的用户进行灰度测试
- 收集性能数据和反馈

### 阶段2：逐步推广

- 使用 `WalletInventoryStore` 兼容层
- 逐步迁移用户数据
- 保留旧系统作为回退方案

### 阶段3：全面启用

- 所有新用户直接使用钱包系统
- 完成所有旧用户的迁移
- 移除旧的 `InventoryStore` 代码（可选）

## 📞 故障排查

### 问题1：编译错误

```bash
# 清理并重新编译
dotnet clean
dotnet build
```

### 问题2：验证失败

```csharp
// 检查公钥是否匹配
var serverPublicKey = walletManager.GetPublicKey();
var clientPublicKey = WalletConfig.ServerPublicKey;

if (serverPublicKey != clientPublicKey)
{
    Console.WriteLine("警告：客户端公钥不匹配！");
}
```

### 问题3：性能问题

```csharp
// 启用并行化
var items = itemRequests.AsParallel().Select(req => 
    walletManager.IssueItem(userId, req.ItemId, req.ItemName, req.Quantity)
).ToList();
```

## 📚 学习资源

- 📘 [完整指南](docs/WALLET_SYSTEM_GUIDE.md) - 深入了解系统设计
- 📗 [快速参考](docs/WALLET_QUICK_REFERENCE.md) - API速查
- 📙 [示例代码](docs/WalletSystemExample.cs) - 实际用例

## ✅ 部署检查表

部署前确认：

- [ ] 代码已编译无错误
- [ ] 私钥已安全备份
- [ ] 公钥已分发给客户端
- [ ] 测试了道具签发和验证
- [ ] 测试了离线验证功能
- [ ] 配置了日志和监控
- [ ] 准备了回滚方案
- [ ] 团队了解新系统的工作原理

## 🎉 完成！

您现在拥有一个强大的、可扩展的钱包系统。无论游戏如何更新、添加多少新功能，这个验证系统都能确保道具的真实性和玩家对战的公平性。

**关键优势：**
- ✅ 离线/局域网环境下的防作弊
- ✅ 通用设计，支持任何未来道具
- ✅ 加密安全，RSA-2048级别
- ✅ 易于维护和扩展

**下一步：**
1. 在测试环境运行示例代码
2. 集成到您的服务器和客户端
3. 进行充分测试
4. 逐步部署到生产环境

祝您使用愉快！🚀
