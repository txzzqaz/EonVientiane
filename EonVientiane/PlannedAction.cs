using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 代表一个计划的行动（用于"预见"饰品的规划系统）
/// </summary>
public class PlannedAction
{
    /// <summary>
    /// 骰子名称
    /// </summary>
    public string DiceName { get; set; }
    
    /// <summary>
    /// 目标玩家ID（如果需要选择目标，否则为null）
    /// </summary>
    public string TargetPlayerId { get; set; }
    
    /// <summary>
    /// 自定义点数（如果需要手动输入，否则为0）
    /// </summary>
    public int CustomValue { get; set; }
    
    /// <summary>
    /// 创建时间戳，用于排序
    /// </summary>
    public long CreatedTick { get; set; }

    public PlannedAction()
    {
        DiceName = string.Empty;
        TargetPlayerId = null;
        CustomValue = 0;
        CreatedTick = System.DateTime.UtcNow.Ticks;
    }

    public PlannedAction(string diceName, string targetId = null, int customValue = 0)
    {
        DiceName = diceName;
        TargetPlayerId = targetId;
        CustomValue = customValue;
        CreatedTick = System.DateTime.UtcNow.Ticks;
    }
}

/// <summary>
/// 规划序列：存储一个骰子的多个序号的行动
/// </summary>
public class PlannedActionSequence
{
    /// <summary>
    /// 骰子名称
    /// </summary>
    public string DiceName { get; set; }
    
    /// <summary>
    /// 该骰子的多个计划行动（按顺序执行）
    /// </summary>
    public List<PlannedAction> Actions { get; set; }

    public PlannedActionSequence(string diceName)
    {
        DiceName = diceName;
        Actions = new List<PlannedAction>();
    }

    /// <summary>
    /// 获取下一个序号（从1开始）
    /// </summary>
    public int GetNextSequenceNumber()
    {
        return Actions.Count + 1;
    }

    /// <summary>
    /// 添加一个行动
    /// </summary>
    public void AddAction(PlannedAction action)
    {
        Actions.Add(action);
    }

    /// <summary>
    /// 移除指定序号的行动（1-based）
    /// </summary>
    public void RemoveActionAt(int sequenceNumber)
    {
        if (sequenceNumber >= 1 && sequenceNumber <= Actions.Count)
        {
            Actions.RemoveAt(sequenceNumber - 1);
        }
    }

    /// <summary>
    /// 获取第一个行动并移除它，同时其他行动序号-1
    /// </summary>
    public PlannedAction GetAndRemoveFirstAction()
    {
        if (Actions.Count > 0)
        {
            var firstAction = Actions[0];
            Actions.RemoveAt(0);
            return firstAction;
        }
        return null;
    }

    /// <summary>
    /// 清空所有行动
    /// </summary>
    public void Clear()
    {
        Actions.Clear();
    }

    /// <summary>
    /// 是否还有待执行的行动
    /// </summary>
    public bool HasPendingActions => Actions.Count > 0;
}
