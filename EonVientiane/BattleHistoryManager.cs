using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EonVientiane;

/// <summary>
/// 对战历史管理器，负责对战记录的本地持久化存储和读取
/// </summary>
public class BattleHistoryManager
{
    private readonly string _dataPath;
    private readonly string _battleHistoryFile;
    private List<BattleRecord> _battleRecords;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public List<BattleRecord> BattleRecords => _battleRecords;

    public BattleHistoryManager()
    {
        // 使用本地用户数据目录
        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EonVientiane",
            "BattleHistory"
        );

        _battleHistoryFile = Path.Combine(_dataPath, "battle_history.json");
        _battleRecords = new List<BattleRecord>();

        // 确保目录存在
        Directory.CreateDirectory(_dataPath);

        // 加载现有记录
        LoadBattleHistory();
    }

    /// <summary>
    /// 从本地文件加载对战历史记录
    /// </summary>
    private void LoadBattleHistory()
    {
        try
        {
            if (File.Exists(_battleHistoryFile))
            {
                string json = File.ReadAllText(_battleHistoryFile);
                _battleRecords = JsonSerializer.Deserialize<List<BattleRecord>>(json, _jsonOptions) ?? new List<BattleRecord>();
            }
            else
            {
                _battleRecords = new List<BattleRecord>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载对战历史失败: {ex.Message}");
            _battleRecords = new List<BattleRecord>();
        }
    }

    /// <summary>
    /// 添加新的对战记录
    /// </summary>
    public void AddBattleRecord(BattleRecord record)
    {
        if (record == null)
            return;

        // 使用时间戳作为记录ID
        record.RecordId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _battleRecords.Add(record);

        // 立即保存到文件
        SaveBattleHistory();
    }

    /// <summary>
    /// 将对战历史保存到本地文件
    /// </summary>
    private void SaveBattleHistory()
    {
        try
        {
            string json = JsonSerializer.Serialize(_battleRecords, _jsonOptions);
            File.WriteAllText(_battleHistoryFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存对战历史失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取特定账号的对战记录
    /// </summary>
    public List<BattleRecord> GetBattleRecordsByPlayer(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return new List<BattleRecord>();

        var records = new List<BattleRecord>();
        foreach (var record in _battleRecords)
        {
            if (record.LocalPlayerName == playerName)
            {
                records.Add(record);
            }
        }

        // 按时间倒序排列（最新的在前）
        records.Sort((a, b) => b.BattleDateTime.CompareTo(a.BattleDateTime));
        return records;
    }

    /// <summary>
    /// 获取所有对战记录
    /// </summary>
    public List<BattleRecord> GetAllBattleRecords()
    {
        // 按时间倒序排列（最新的在前）
        var sorted = new List<BattleRecord>(_battleRecords);
        sorted.Sort((a, b) => b.BattleDateTime.CompareTo(a.BattleDateTime));
        return sorted;
    }

    /// <summary>
    /// 获取某账号的统计信息
    /// </summary>
    public (int totalBattles, int wins, int losses, int draws) GetPlayerStats(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return (0, 0, 0, 0);

        int totalBattles = 0;
        int wins = 0;
        int losses = 0;
        int draws = 0;

        foreach (var record in _battleRecords)
        {
            if (record.LocalPlayerName == playerName)
            {
                totalBattles++;
                if (record.Result == 1)
                    wins++;
                else if (record.Result == 0)
                    losses++;
                else if (record.Result == 2)
                    draws++;
            }
        }

        return (totalBattles, wins, losses, draws);
    }

    /// <summary>
    /// 删除特定记录
    /// </summary>
    public bool DeleteBattleRecord(long recordId)
    {
        int index = _battleRecords.FindIndex(r => r.RecordId == recordId);
        if (index >= 0)
        {
            _battleRecords.RemoveAt(index);
            SaveBattleHistory();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 清空所有对战历史
    /// </summary>
    public void ClearAllBattleHistory()
    {
        _battleRecords.Clear();
        SaveBattleHistory();
    }

    /// <summary>
    /// 获取对战结果描述
    /// </summary>
    public static string GetResultDescription(int result)
    {
        return result switch
        {
            0 => "失败",
            1 => "胜利",
            2 => "平手",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取对战结果的颜色
    /// </summary>
    public static Microsoft.Xna.Framework.Color GetResultColor(int result)
    {
        return result switch
        {
            0 => Microsoft.Xna.Framework.Color.Red,       // 失败
            1 => Microsoft.Xna.Framework.Color.Green,     // 胜利
            2 => Microsoft.Xna.Framework.Color.Yellow,    // 平手
            _ => Microsoft.Xna.Framework.Color.White
        };
    }
}
