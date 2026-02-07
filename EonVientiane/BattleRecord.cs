using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 对战记录，用于永久本地保存
/// </summary>
public class BattleRecord
{
    /// <summary>
    /// 记录ID（时间戳）
    /// </summary>
    public long RecordId { get; set; }

    /// <summary>
    /// 对战发生的日期时间
    /// </summary>
    public DateTime BattleDateTime { get; set; }

    /// <summary>
    /// 本地玩家账号
    /// </summary>
    public string LocalPlayerName { get; set; }

    /// <summary>
    /// 本地玩家血量
    /// </summary>
    public int LocalPlayerHp { get; set; }

    /// <summary>
    /// 本地玩家等级
    /// </summary>
    public int LocalPlayerLevel { get; set; }

    /// <summary>
    /// 对手账号
    /// </summary>
    public string OpponentName { get; set; }

    /// <summary>
    /// 对手血量
    /// </summary>
    public int OpponentHp { get; set; }

    /// <summary>
    /// 对手等级
    /// </summary>
    public int OpponentLevel { get; set; }

    /// <summary>
    /// 对战结果 (0=失败, 1=胜利, 2=平手)
    /// </summary>
    public int Result { get; set; }

    /// <summary>
    /// 对战是否为多人模式
    /// </summary>
    public bool IsMultiplayer { get; set; }

    /// <summary>
    /// 获胜玩家账号（如果有）
    /// </summary>
    public string WinnerName { get; set; }

    /// <summary>
    /// 对战持续时间（秒）
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// 本地玩家获得的经验值
    /// </summary>
    public int ExpGained { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Notes { get; set; }
    
    // 扩展的战斗统计数据
    
    /// <summary>
    /// 本地玩家造成的总伤害
    /// </summary>
    public int TotalDamageDealt { get; set; }
    
    /// <summary>
    /// 本地玩家承受的总伤害
    /// </summary>
    public int TotalDamageTaken { get; set; }
    
    /// <summary>
    /// 本地玩家格挡的总伤害
    /// </summary>
    public int TotalDamageBlocked { get; set; }
    
    /// <summary>
    /// 本地玩家的击杀数
    /// </summary>
    public int KillCount { get; set; }
    
    /// <summary>
    /// 本地玩家的总行动时间（秒）
    /// </summary>
    public double TotalActionTimeSeconds { get; set; }
    
    /// <summary>
    /// 是否获得MVP
    /// </summary>
    public bool IsMVP { get; set; }
    
    /// <summary>
    /// 战斗总回合数
    /// </summary>
    public int TotalRounds { get; set; }
    
    /// <summary>
    /// Team1的所有玩家名称列表
    /// </summary>
    public List<string> Team1Players { get; set; } = new List<string>();
    
    /// <summary>
    /// Team2的所有玩家名称列表
    /// </summary>
    public List<string> Team2Players { get; set; } = new List<string>();
}