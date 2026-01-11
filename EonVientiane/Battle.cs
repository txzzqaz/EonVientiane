using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 战斗状态枚举
/// </summary>
public enum BattleState
{
    Idle,               // 空闲
    Initialization,     // 初始化阶段
    RoundStart,         // 回合开始
    PlayerAction,       // 玩家行动
    DefenseResponse,    // 防守响应
    EffectCalculation,  // 效果计算
    RoundEnd,           // 回合结束
    BattleEnd           // 战斗结束
}

/// <summary>
/// 当前等待的输入上下文
/// </summary>
public enum BattleInputContext
{
    None,
    AttackSelection,
    DefenseSelection
}

/// <summary>
/// 战斗数据（客户端仅用于显示，逻辑由服务器驱动）
/// </summary>
public class Battle
{
    public List<Player> AllPlayers { get; private set; }
    public List<Player> Team1Players => AllPlayers.Where(p => p.Camp == PlayerCamp.Team1).ToList();
    public List<Player> Team2Players => AllPlayers.Where(p => p.Camp == PlayerCamp.Team2).ToList();

    public BattleState CurrentState { get; set; }
    public int CurrentRound { get; set; }
    public PlayerCamp CurrentCamp { get; set; }
    public List<string> BattleLog { get; private set; }
    public bool IsBattleOver { get; set; }
    public PlayerCamp? WinnerCamp { get; set; }
    public Player CurrentActionPlayer { get; set; }
    public bool IsWaitingForPlayerInput { get; set; }
    public BattleInputContext InputContext => _inputContext;

    public IReadOnlyList<Dice> AvailableActiveDice => _currentActiveDiceChoices ?? _emptyDiceList;
    public IReadOnlyList<Dice> AvailablePassiveDice => _currentPassiveDiceChoices ?? _emptyDiceList;
    public IReadOnlyList<Player> AvailableOpponents => _currentOpponents ?? _emptyPlayerList;

    private List<Player> _currentOpponents;
    private List<Dice> _currentActiveDiceChoices;
    private List<Dice> _currentPassiveDiceChoices;
    private BattleInputContext _inputContext;

    private static readonly List<Dice> _emptyDiceList = new();
    private static readonly List<Player> _emptyPlayerList = new();

    public Battle()
    {
        AllPlayers = new List<Player>();
        CurrentState = BattleState.Idle;
        CurrentRound = 0;
        BattleLog = new List<string>();
        IsBattleOver = false;
        WinnerCamp = null;
        IsWaitingForPlayerInput = false;
        CurrentActionPlayer = null;
        _inputContext = BattleInputContext.None;
    }

    public void AddPlayer(Player player)
    {
        if (player != null && !AllPlayers.Any(p => p.PlayerId == player.PlayerId))
        {
            AllPlayers.Add(player);
        }
    }

    public void SetInputContext(BattleInputContext context)
    {
        _inputContext = context;
    }
}
