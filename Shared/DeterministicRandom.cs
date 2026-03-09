using System;
using System.Security.Cryptography;
using System.Text;

namespace EonVientiane.Shared;

/// <summary>
/// 确定性随机数生成器 - 用于可验证的战斗系统
/// 通过合成双方的种子,确保所有随机操作可重现和验证
/// </summary>
public class DeterministicRandom
{
    private readonly byte[] _seed;
    private long _counter;
    private readonly string _seedHex;

    /// <summary>
    /// 种子的十六进制表示
    /// </summary>
    public string SeedHex => _seedHex;

    /// <summary>
    /// 当前计数器值（用于追踪随机调用次数）
    /// </summary>
    public long Counter => _counter;

    /// <summary>
    /// 从合成种子创建确定性随机数生成器
    /// </summary>
    public DeterministicRandom(byte[] seed)
    {
        if (seed == null || seed.Length == 0)
            throw new ArgumentException("种子不能为空");

        _seed = new byte[seed.Length];
        Array.Copy(seed, _seed, seed.Length);
        _counter = 0;
        _seedHex = BitConverter.ToString(seed).Replace("-", "");
    }

    /// <summary>
    /// 从两个种子合成唯一种子
    /// </summary>
    public static byte[] CombineSeeds(byte[] seed1, byte[] seed2)
    {
        if (seed1 == null || seed1.Length == 0)
            throw new ArgumentException("种子1不能为空");
        if (seed2 == null || seed2.Length == 0)
            throw new ArgumentException("种子2不能为空");

        // 使用SHA256合成两个种子
        using (var sha256 = SHA256.Create())
        {
            var combined = new byte[seed1.Length + seed2.Length];
            Array.Copy(seed1, 0, combined, 0, seed1.Length);
            Array.Copy(seed2, 0, combined, seed1.Length, seed2.Length);
            return sha256.ComputeHash(combined);
        }
    }

    /// <summary>
    /// 从字符串生成种子
    /// </summary>
    public static byte[] GenerateRandomSeed()
    {
        var bytes = new byte[32]; // 256-bit seed
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return bytes;
    }

    /// <summary>
    /// 从十六进制字符串解析种子
    /// </summary>
    public static byte[] ParseSeedHex(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            throw new ArgumentException("十六进制字符串不能为空");

        hexString = hexString.Replace("-", "").Replace(" ", "");
        
        if (hexString.Length % 2 != 0)
            throw new ArgumentException("十六进制字符串长度必须是偶数");

        var bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    /// <summary>
    /// 生成下一个随机整数 [0, maxValue)
    /// </summary>
    public int Next(int maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentException("maxValue必须大于0");

        return Next(0, maxValue);
    }

    /// <summary>
    /// 生成下一个随机整数 [minValue, maxValue)
    /// </summary>
    public int Next(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
            throw new ArgumentException("minValue必须小于maxValue");

        var range = maxValue - minValue;
        var randomValue = NextDouble();
        return minValue + (int)(randomValue * range);
    }

    /// <summary>
    /// 生成下一个随机双精度浮点数 [0.0, 1.0)
    /// </summary>
    public double NextDouble()
    {
        // 使用HMAC-SHA256生成确定性随机数
        using (var hmac = new HMACSHA256(_seed))
        {
            var counterBytes = BitConverter.GetBytes(_counter);
            var hash = hmac.ComputeHash(counterBytes);
            
            // 从哈希值中提取8字节作为ulong
            var value = BitConverter.ToUInt64(hash, 0);
            
            // 递增计数器以确保下次调用产生不同的值
            _counter++;
            
            // 转换为[0.0, 1.0)范围的double
            return (double)value / (double)ulong.MaxValue;
        }
    }

    /// <summary>
    /// 生成下一个随机字节数组
    /// </summary>
    public byte[] NextBytes(int count)
    {
        var result = new byte[count];
        NextBytes(result);
        return result;
    }

    /// <summary>
    /// 填充字节数组
    /// </summary>
    public void NextBytes(byte[] buffer)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        using (var hmac = new HMACSHA256(_seed))
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                var counterBytes = BitConverter.GetBytes(_counter);
                var hash = hmac.ComputeHash(counterBytes);
                _counter++;

                int bytesToCopy = Math.Min(hash.Length, buffer.Length - offset);
                Array.Copy(hash, 0, buffer, offset, bytesToCopy);
                offset += bytesToCopy;
            }
        }
    }

    /// <summary>
    /// 掷骰子 (1到sides)
    /// </summary>
    public int RollDice(int sides)
    {
        if (sides <= 0)
            throw new ArgumentException("骰子面数必须大于0");

        return Next(1, sides + 1);
    }

    /// <summary>
    /// 重置计数器（用于测试或重新开始）
    /// </summary>
    public void ResetCounter()
    {
        _counter = 0;
    }

    /// <summary>
    /// 创建状态快照（用于保存/验证）
    /// </summary>
    public RandomState CreateSnapshot()
    {
        return new RandomState
        {
            SeedHex = _seedHex,
            Counter = _counter
        };
    }

    /// <summary>
    /// 从快照恢复状态
    /// </summary>
    public static DeterministicRandom FromSnapshot(RandomState state)
    {
        var seed = ParseSeedHex(state.SeedHex);
        var random = new DeterministicRandom(seed);
        random._counter = state.Counter;
        return random;
    }
}

/// <summary>
/// 随机数生成器状态（用于保存和验证）
/// </summary>
public class RandomState
{
    public string SeedHex { get; set; }
    public long Counter { get; set; }
}
