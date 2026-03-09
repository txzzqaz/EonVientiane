using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 本地加密数据管理器 - 使用用户账密加密本地游戏数据,支持离线运行和联机同步
/// </summary>
public class LocalDataManager
{
    private readonly string _dataDirectory;
    private string _currentUsername;
    private byte[] _encryptionKey;
    private byte[] _encryptionIV;
    
    /// <summary>
    /// 本地玩家数据结构
    /// </summary>
    public class LocalPlayerData
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime LastModified { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int Coins { get; set; }
        public List<string> UnlockedItems { get; set; } = new();
        public List<InventoryItemData> Inventory { get; set; } = new();
        public List<InventoryItemData> EquippedItems { get; set; } = new();
        public Dictionary<string, AchievementData> Achievements { get; set; } = new();
        public long DataVersion { get; set; } // 用于同步冲突检测
        public string DataHash { get; set; } // 数据完整性校验
    }

    /// <summary>
    /// 库存物品数据
    /// </summary>
    public class InventoryItemData
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 成就数据
    /// </summary>
    public class AchievementData
    {
        public string AchievementId { get; set; }
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public LocalDataManager(string dataDir = "data/local_player_data")
    {
        _dataDirectory = dataDir;
        Directory.CreateDirectory(_dataDirectory);
    }

    /// <summary>
    /// 使用用户账密初始化加密密钥
    /// </summary>
    public void InitializeEncryption(string username, string password)
    {
        _currentUsername = username;
        
        // 使用PBKDF2从密码派生加密密钥
        using (var pbkdf2 = new Rfc2898DeriveBytes(
            password, 
            Encoding.UTF8.GetBytes(username + "EonVientiane_Salt"), 
            100000, // 迭代次数
            HashAlgorithmName.SHA256))
        {
            _encryptionKey = pbkdf2.GetBytes(32); // 256-bit key for AES
            _encryptionIV = pbkdf2.GetBytes(16);  // 128-bit IV
        }
    }

    /// <summary>
    /// 保存玩家数据（加密存储）
    /// </summary>
    public bool SavePlayerData(LocalPlayerData data)
    {
        if (string.IsNullOrEmpty(_currentUsername) || _encryptionKey == null)
        {
            Console.WriteLine("[LocalData] 未初始化加密密钥");
            return false;
        }

        try
        {
            data.Username = _currentUsername;
            data.LastModified = DateTime.UtcNow;
            data.DataVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 序列化数据
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            
            // 计算数据哈希
            data.DataHash = ComputeDataHash(json);
            json = JsonSerializer.Serialize(data, options); // 重新序列化包含哈希
            
            // 加密数据
            var encryptedData = EncryptData(json);
            
            // 保存到文件
            var filePath = GetUserDataFilePath(_currentUsername);
            File.WriteAllBytes(filePath, encryptedData);
            
            Console.WriteLine($"[LocalData] 玩家数据已保存: {_currentUsername}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalData] 保存失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 加载玩家数据（解密）
    /// </summary>
    public (bool success, LocalPlayerData data, string message) LoadPlayerData()
    {
        if (string.IsNullOrEmpty(_currentUsername) || _encryptionKey == null)
        {
            return (false, null, "未初始化加密密钥");
        }

        try
        {
            var filePath = GetUserDataFilePath(_currentUsername);
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[LocalData] 数据文件不存在,创建新数据: {_currentUsername}");
                return (true, CreateNewPlayerData(), "创建新玩家数据");
            }

            // 读取加密数据
            var encryptedData = File.ReadAllBytes(filePath);
            
            // 解密数据
            var json = DecryptData(encryptedData);
            
            // 反序列化
            var data = JsonSerializer.Deserialize<LocalPlayerData>(json);
            
            if (data == null)
            {
                return (false, null, "数据反序列化失败");
            }

            // 验证数据完整性
            var storedHash = data.DataHash;
            data.DataHash = null;
            var computedHash = ComputeDataHash(JsonSerializer.Serialize(data));
            
            if (storedHash != computedHash)
            {
                Console.WriteLine($"[LocalData] 警告: 数据完整性校验失败");
                // 仍然加载数据,但标记为可疑
            }

            data.DataHash = storedHash;
            Console.WriteLine($"[LocalData] 玩家数据已加载: {_currentUsername}");
            return (true, data, "加载成功");
        }
        catch (CryptographicException)
        {
            return (false, null, "密码错误或数据已损坏");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalData] 加载失败: {ex.Message}");
            return (false, null, $"加载失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建新玩家数据
    /// </summary>
    private LocalPlayerData CreateNewPlayerData()
    {
        return new LocalPlayerData
        {
            Username = _currentUsername,
            Email = "",
            LastModified = DateTime.UtcNow,
            Level = 1,
            Experience = 0,
            Coins = 1000,
            UnlockedItems = new List<string>(),
            Inventory = new List<InventoryItemData>(),
            EquippedItems = new List<InventoryItemData>(),
            Achievements = new Dictionary<string, AchievementData>(),
            DataVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// 加密数据
    /// </summary>
    private byte[] EncryptData(string plainText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
            aes.IV = _encryptionIV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return ms.ToArray();
            }
        }
    }

    /// <summary>
    /// 解密数据
    /// </summary>
    private string DecryptData(byte[] cipherText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
            aes.IV = _encryptionIV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherText))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

    /// <summary>
    /// 计算数据哈希
    /// </summary>
    private string ComputeDataHash(string data)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// 获取用户数据文件路径
    /// </summary>
    private string GetUserDataFilePath(string username)
    {
        return Path.Combine(_dataDirectory, $"{username.ToLower()}.edat");
    }

    /// <summary>
    /// 检查用户数据是否存在
    /// </summary>
    public bool HasUserData(string username)
    {
        return File.Exists(GetUserDataFilePath(username));
    }

    /// <summary>
    /// 同步到服务器（上传本地数据）
    /// </summary>
    public async System.Threading.Tasks.Task<(bool success, string message)> SyncToServer(LocalPlayerData data, Func<LocalPlayerData, System.Threading.Tasks.Task<(bool, string)>> uploadFunc)
    {
        try
        {
            Console.WriteLine($"[LocalData] 开始同步到服务器: {_currentUsername}");
            var result = await uploadFunc(data);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalData] 同步失败: {ex.Message}");
            return (false, $"同步失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从服务器同步（下载并合并）
    /// </summary>
    public async System.Threading.Tasks.Task<(bool success, LocalPlayerData data, string message)> SyncFromServer(
        Func<System.Threading.Tasks.Task<(bool, LocalPlayerData, string)>> downloadFunc)
    {
        try
        {
            Console.WriteLine($"[LocalData] 从服务器下载数据: {_currentUsername}");
            var (success, serverData, message) = await downloadFunc();
            
            if (!success)
            {
                return (false, null, message);
            }

            // 加载本地数据
            var (localSuccess, localData, localMessage) = LoadPlayerData();
            
            if (!localSuccess)
            {
                // 本地数据不存在或损坏,直接使用服务器数据
                localData = serverData;
                SavePlayerData(localData);
                return (true, localData, "已下载服务器数据");
            }

            // 合并数据（服务器数据优先，但保留本地离线期间的更改）
            var mergedData = MergePlayerData(localData, serverData);
            SavePlayerData(mergedData);
            
            Console.WriteLine($"[LocalData] 数据同步完成: {_currentUsername}");
            return (true, mergedData, "同步完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalData] 同步失败: {ex.Message}");
            return (false, null, $"同步失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 合并本地和服务器数据
    /// </summary>
    private LocalPlayerData MergePlayerData(LocalPlayerData localData, LocalPlayerData serverData)
    {
        // 使用版本号判断哪个更新
        if (serverData.DataVersion > localData.DataVersion)
        {
            Console.WriteLine($"[LocalData] 服务器数据更新,使用服务器版本");
            return serverData;
        }
        else if (localData.DataVersion > serverData.DataVersion)
        {
            Console.WriteLine($"[LocalData] 本地数据更新,保留本地版本（稍后上传）");
            return localData;
        }
        else
        {
            // 版本相同,合并某些字段
            Console.WriteLine($"[LocalData] 版本相同,合并数据");
            localData.Level = Math.Max(localData.Level, serverData.Level);
            localData.Experience = Math.Max(localData.Experience, serverData.Experience);
            localData.Coins = Math.Max(localData.Coins, serverData.Coins);
            
            // 合并物品（取并集）
            var allItems = new HashSet<string>(localData.UnlockedItems);
            allItems.UnionWith(serverData.UnlockedItems);
            localData.UnlockedItems = allItems.ToList();
            
            return localData;
        }
    }

    /// <summary>
    /// 清除当前会话
    /// </summary>
    public void ClearSession()
    {
        _currentUsername = null;
        if (_encryptionKey != null)
        {
            Array.Clear(_encryptionKey, 0, _encryptionKey.Length);
            _encryptionKey = null;
        }
        if (_encryptionIV != null)
        {
            Array.Clear(_encryptionIV, 0, _encryptionIV.Length);
            _encryptionIV = null;
        }
    }
}
