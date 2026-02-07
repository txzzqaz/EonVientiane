# 🔐 钱包系统集成快速测试指南

## ✅ 重构完成确认

```bash
✓ 编译状态: 成功 (0 错误)
✓ GameServer 集成: 完成
✓ 网络协议: 更新完成
✓ 公钥发送: 已实现
✓ 日志记录: 已添加
```

## 🚀 快速开始

### 1. 启动服务器

```bash
cd /home/qazokmwsxijn/Documents/EonVientiane/EonVientiane

# 编译（可选，已验证成功）
dotnet build

# 运行服务器
dotnet run --project EonVientianeServer/EonVientianeServer.csproj
```

**预期输出**:
```
[GameServer] 已启用区块链风格钱包系统（RSA-2048加密）
[Server] Started on port 7777
[WalletManager] Initialized with RSA-2048 encryption
```

### 2. 验证密钥文件

```bash
ls -la data/wallets/

# 应该看到:
# - server_keys.xml (私钥，绝密)
# - public_key.xml (公钥，可分发)
# - wallets/ (玩家钱包目录)
```

### 3. 测试公钥发送

```bash
# 使用客户端连接并请求公钥
# 服务器日志应显示:
[钱包系统] 发送公钥给客户端 PlayerName
```

## 📋 验证检查清单

### 服务器端验证

- [ ] 服务器启动时输出钱包系统初始化消息
- [ ] 服务器数据目录中创建了 `data/wallets/` 目录
- [ ] `server_keys.xml` 文件已生成
- [ ] `public_key.xml` 文件已生成
- [ ] 客户端连接时能正确响应GetPublicKey消息

### 背包验证

- [ ] 玩家请求背包时有验证日志
- [ ] 日志格式: `[钱包验证] 用户 {userId}: 已验证 {count} 个签名道具`
- [ ] 背包中所有道具都有有效签名
- [ ] 没有验证错误日志

### 网络协议验证

- [ ] GetPublicKey 消息类型可识别
- [ ] GetPublicKeyResponse 消息类型可识别
- [ ] 公钥响应包含完整的RSA XML公钥
- [ ] 网络消息序列化/反序列化正常

## 🧪 测试场景

### 场景 1: 基本公钥获取

```
步骤:
1. 客户端连接到服务器
2. 客户端发送 GetPublicKey 消息
3. 服务器响应 GetPublicKeyResponse

预期结果:
✓ 收到公钥
✓ 公钥格式为 RSA XML
✓ 公钥可用于验证签名
✓ 服务器日志记录了公钥发送
```

### 场景 2: 背包加载验证

```
步骤:
1. 玩家登录
2. 请求背包
3. 服务器返回背包
4. 服务器验证所有道具

预期结果:
✓ 日志显示验证成功
✓ 所有道具都有签名
✓ 背包数据完整
```

### 场景 3: 测试账号

```
登录账号: qaz1 或 qaz2
密码: 任意（测试账号）

预期结果:
✓ 测试账号包含所有可用道具
✓ 所有道具都已正确签名
✓ 背包验证通过
```

## 🔍 日志关键字

在服务器日志中查找这些关键字来验证重构:

| 关键字 | 含义 | 优先级 |
|-------|------|-------|
| `已启用区块链风格钱包系统` | 初始化成功 | 🔴 必需 |
| `[WalletManager] Initialized` | 钱包管理器就绪 | 🔴 必需 |
| `发送公钥给客户端` | 公钥发送成功 | 🟡 重要 |
| `[钱包验证]` | 背包验证日志 | 🟡 重要 |
| `已验证 X 个签名道具` | 道具数量统计 | 🟢 参考 |

## 📊 性能监控

### 启动时间

```
预期:
- 服务器初始化: < 1秒
- WalletManager 初始化: < 500ms
- 密钥生成（首次）: < 2秒
- 密钥加载（后续）: < 100ms
```

### 运行时性能

```
预期:
- 公钥获取请求: < 10ms
- 背包加载: < 50ms
- 100个道具验证: < 100ms
```

## ⚠️ 常见问题

### Q1: 启动时没有看到钱包系统消息

**排查步骤**:
```csharp
1. 检查GameServer初始化代码
2. 确认WalletManager已创建
3. 查看完整控制台输出
```

### Q2: 公钥发送失败

**排查步骤**:
```csharp
1. 检查GetPublicKey消息处理
2. 确认_walletManager 不为 null
3. 查看异常日志
4. 验证网络连接
```

### Q3: 背包验证失败

**排查步骤**:
```csharp
1. 检查WalletInventoryStore 转换
2. 验证道具签名有效
3. 查看异常堆栈跟踪
```

## 📂 关键文件位置

```
EonVientianeServer/
├── GameServer.cs          ← 集成点
├── WalletManager.cs       ← 钱包管理
├── WalletInventoryStore.cs ← 兼容层
└── ...

Shared/
├── NetworkProtocol.cs     ← 消息定义
├── WalletCrypto.cs        ← 加密核心
├── WalletTypes.cs         ← 数据结构
└── ...

EonVientiane/
└── WalletValidator.cs     ← 客户端验证

data/
└── wallets/              ← 密钥和钱包文件
    ├── server_keys.xml
    ├── public_key.xml
    └── wallets/
```

## 🔐 安全检查

### 必检项目

- [ ] server_keys.xml 文件权限设置正确 (600)
- [ ] public_key.xml 是否包含公钥数据
- [ ] 没有在代码中硬编码密钥
- [ ] 私钥已安全备份
- [ ] .gitignore 包含私钥文件

### 验证命令

```bash
# 检查私钥文件权限
stat data/wallets/server_keys.xml

# 验证公钥有效性
grep -c "RSAKeyValue" data/wallets/public_key.xml

# 查看钱包数据（示例）
ls -la data/wallets/wallets/
```

## 📞 支持信息

### 文档参考

1. **总体架构**: WALLET_SYSTEM_README.md
2. **实施指南**: WALLET_IMPLEMENTATION_GUIDE.md
3. **完整报告**: WALLET_IMPLEMENTATION_COMPLETE.md
4. **重构总结**: WALLET_REFACTORING_SUMMARY.md

### 获取帮助

```bash
# 查看最近的提交
git log --oneline -10

# 查看重构的具体更改
git show 29b03c0  # 重构提交ID

# 比较前后代码
git diff HEAD~1 HEAD -- EonVientianeServer/GameServer.cs
```

## ✨ 验收标准

✅ **功能验收**:
- [ ] 服务器启动时初始化钱包系统
- [ ] 客户端能获取公钥
- [ ] 背包验证日志正常输出
- [ ] 所有道具都有有效签名
- [ ] 编译无错误

✅ **性能验收**:
- [ ] 服务器启动时间 < 2秒
- [ ] 公钥请求响应 < 50ms
- [ ] 背包加载 < 100ms
- [ ] 没有内存泄漏

✅ **安全验收**:
- [ ] 私钥文件安全存储
- [ ] 所有道具都被签名
- [ ] 没有在日志中泄露敏感信息
- [ ] 密钥权限正确设置

## 🎯 下一步

### 立即执行

1. [ ] 启动服务器验证初始化
2. [ ] 检查密钥文件生成
3. [ ] 运行基本功能测试

### 本周计划

1. [ ] 完整的功能测试
2. [ ] 负载测试
3. [ ] 客户端集成

### 本月计划

1. [ ] 生产部署
2. [ ] 备份策略实施
3. [ ] 监控和告警配置

---

**最后更新**: 2026年2月7日  
**状态**: 就绪，可测试
