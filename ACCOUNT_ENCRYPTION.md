# Eon Vientiane 账户和加密系统

## 系统概览

本项目已整合了完整的本地账户管理和数据加密系统，为游戏提供安全的用户认证和数据保护。

## 核心组件

### 1. 用户模型 (User.cs)
```csharp
public class User
{
    public string UserId { get; set; }           // 唯一用户ID
    public string Username { get; set; }         // 用户名
    public string PasswordHash { get; set; }     // 密码哈希（单向加密）
    public string Email { get; set; }            // 邮箱地址
    public DateTime CreatedAt { get; set; }      // 创建时间
    public DateTime LastLoginAt { get; set; }    // 最后登录时间
    public UserStatus Status { get; set; }       // 账户状态
    public Dictionary<string, string> EncryptedData { get; set; } // 加密的用户数据
}

public enum UserStatus { Active, Inactive, Suspended, Deleted }
```

### 2. 加密服务 (EncryptionService.cs)

提供以下加密功能：

- **密码哈希** - 使用 PBKDF2 + SHA256 算法
  - 每个密码生成唯一的随机盐
  - 10,000 次迭代确保安全性
  - 单向加密，无法逆向解密

- **数据加密/解密** - 使用 AES-256-CBC 算法
  - 对敏感数据进行加密存储
  - 支持加密和解密字符串数据
  - 使用本地密钥进行加密

- **校验和计算** - 使用 SHA256 算法
  - 验证数据完整性
  - 检测数据篡改

### 3. 账户服务 (AccountService.cs)

主要功能：

| 功能 | 方法 | 说明 |
|------|------|------|
| 创建账户 | `CreateAccount(username, password, email)` | 注册新账户 |
| 用户登录 | `Login(username, password)` | 验证凭据并登录 |
| 用户登出 | `Logout()` | 结束当前会话 |
| 更改密码 | `ChangePassword(oldPassword, newPassword)` | 修改用户密码 |
| 保存加密数据 | `SaveEncryptedData(key, data)` | 保存加密的用户相关数据 |
| 读取加密数据 | `GetEncryptedData(key)` | 读取并解密用户数据 |
| 删除账户 | `DeleteAccount(password)` | 删除用户账户 |
| 获取用户列表 | `GetAllUsernames()` | 列出所有活跃用户 |

### 4. 账户持久化

账户数据存储位置：
```
Windows:     %APPDATA%\EonVientiane\Accounts\
Linux/Mac:   ~/.local/share/EonVientiane/Accounts/  (或类似位置)
```

每个账户保存为加密的 JSON 文件：
```
Accounts/
├── {UserId1}.json (加密)
├── {UserId2}.json (加密)
└── ...
```

## 安全特性

### ✅ 密码安全
- **PBKDF2-SHA256** 哈希算法
- 10,000 次迭代
- 每个密码的独特盐值
- 密码不可逆向

### ✅ 数据加密
- **AES-256-CBC** 加密方式
- 所有敏感用户数据加密存储
- 安全的密钥派生

### ✅ 完整性验证
- SHA256 校验和
- 检测数据篡改

## 游戏中的账户命令

### 创建账户
```bash
register <用户名> <邮箱>
```
示例：
```
> register player001 player@example.com
请输入密码: ****
请确认密码: ****
✓ 账户创建成功! 用户名: player001
```

### 登录
```bash
login <用户名>
```
示例：
```
> login player001
请输入密码: ****
✓ 登录成功! 欢迎, player001!
```

### 登出
```bash
logout
```

### 查看账户信息
```bash
account
```
输出示例：
```
=== 账户信息 ===
用户ID: 550e8400-e29b-41d4-a716-446655440000
用户名: player001
邮箱: player@example.com
状态: 活跃
创建时间: 2026-03-09 10:30:45
最后登录: 2026-03-09 10:35:20
```

### 查看用户列表
```bash
users             # 显示所有活跃用户
users list        # 显示账户统计
```

### 更改密码
```bash
changepwd
```
系统将提示输入旧密码和新密码。

## 技术实现细节

### 密码哈希流程
```
用户密码 "admin123"
    ↓
生成随机盐 (16 bytes)
    ↓
PBKDF2(password, salt, 10000 iterations, SHA256)
    ↓
生成哈希值 (32 bytes)
    ↓
盐 (16 bytes) + 哈希 (32 bytes) = hashWithSalt (48 bytes)
    ↓
Base64 编码存储
```

### 数据加密流程
```
明文数据
    ↓
AES-256-CBC
(使用派生密钥 + IV)
    ↓
密文
    ↓
Base64 编码存储
```

### 账户文件结构
```json
{
  "UserId": "550e8400-e29b-41d4-a716-446655440000",
  "Username": "player001",
  "PasswordHash": "base64_encoded_hash_with_salt",
  "Email": "player@example.com",
  "CreatedAt": "2026-03-09T10:30:45",
  "LastLoginAt": "2026-03-09T10:35:20",
  "Status": 0,
  "EncryptedData": {
    "savedata": "encrypted_base64_string",
    "settings": "encrypted_base64_string"
  }
}
```
（文件本身也被加密存储）

## 示例使用场景

### 场景 1：新玩家注册和创建存档
```
1. 游戏启动 → 显示登录/注册界面
2. 选择"创建账户" 
3. 输入用户名、邮箱、密码
4. 系统创建加密的账户文件
5. 自动登录后进入游戏
6. 游戏数据可加密保存
```

### 场景 2：选择现有账户登录
```
1. 游戏启动 → 显示登录/注册界面
2. 选择"登录"
3. 系统从加密文件解密用户数据
4. 验证密码
5. 加载用户的游戏存档和设置
```

### 场景 3：保存加密的游戏数据
```csharp
// 在游戏代码中
var saveData = new {
    Level = 5,
    Score = 1000,
    Inventory = new[] { "剑", "盾" }
};

string json = JsonSerializer.Serialize(saveData);
accountService.SaveEncryptedData("gameplay", json);

// 读取加密数据
string decrypted = accountService.GetEncryptedData("gameplay");
var loadedData = JsonSerializer.Deserialize<GameSaveData>(decrypted);
```

## 安全建议

### 生产环境
1. **使用强密钥** - 不要用默认密钥，从环境变量或安全配置读取
2. **HTTPS/加密通信** - 如果涉及网络通信
3. **定期备份** - 保护用户数据
4. **日志审计** - 记录敏感操作
5. **密钥管理** - 定期轮换密钥

### 开发建议
```csharp
// 好示例：从环境读取密钥
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
    ?? throw new InvalidOperationException("ENCRYPTION_KEY not set");
var accountService = new AccountService(encryptionKey);

// 避免：硬编码密钥
// var accountService = new AccountService("my-secret-key");
```

## 测试账户系统

### 手动测试
```bash
# 启动游戏
dotnet run --project EonVientiane.CLI

# 选择创建账户，然后进行各种操作
```

### 检查账户文件
```bash
# Linux/Mac
ls -la ~/.local/share/EonVientiane/Accounts/

# Windows PowerShell
dir $env:APPDATA\EonVientiane\Accounts\
```

## 注意事项

1. **密钥安全** - 加密密钥存储在内存中，仅在程序运行期间有效
2. **密码找回** - 当前实现不支持密码重置（可根据需要添加）
3. **多设备同步** - 账户和存档目前仅本地存储（可扩展为云同步）
4. **数据导出** - 加密的账户文件无法直接读取（需要正确的密钥）

## 后续扩展

1. **云同步** - 将加密账户数据同步到云服务器
2. **多账户支持** - 一台机器上管理多个账户
3. **账户恢复** - 密码重置、邮箱验证
4. **两因素认证** - 增强账户安全
5. **数据备份** - 定期备份加密数据
6. **账户迁移** - 从其他游戏导入账户

---

**安全提示**：该系统已采用行业标准的加密算法，但在生产环境部署前，建议进行专业安全审计。
