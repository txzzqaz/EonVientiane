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
/// 战斗管理器
/// </summary>
public class Battle
{
    /// <summary>
    /// 所有参与者
    /// </summary>
    public List<Player> AllPlayers { get; private set; }
    
    /// <summary>
    /// Team1 玩家
    /// </summary>
    public List<Player> Team1Players => AllPlayers.Where(p => p.Camp == PlayerCamp.Team1).ToList();
    
    /// <summary>
    /// Team2 玩家
    /// </summary>
    public List<Player> Team2Players => AllPlayers.Where(p => p.Camp == PlayerCamp.Team2).ToList();
    
    /// <summary>
    /// 当前战斗状态
    /// </summary>
    public BattleState CurrentState { get; set; }
    
    /// <summary>
    /// 当前回合数
    /// </summary>
    public int CurrentRound { get; set; }
    
    /// <summary>
    /// 当前行动的阵营
    /// </summary>
    public PlayerCamp CurrentCamp { get; set; }
    
    /// <summary>
    /// 战斗日志
    /// </summary>
    public List<string> BattleLog { get; private set; }
    
    /// <summary>
    /// 是否战斗结束
    /// </summary>
    public bool IsBattleOver { get; private set; }
    
    /// <summary>
    /// 赢家阵营（如果战斗结束）
    /// </summary>
    public PlayerCamp? WinnerCamp { get; private set; }
    
    /// <summary>
    /// 当前行动的玩家
    /// </summary>
    public Player CurrentActionPlayer { get; private set; }
    
    /// <summary>
    /// 是否等待玩家输入
    /// </summary>
    public bool IsWaitingForPlayerInput { get; set; }

    /// <summary>
    /// 当前等待的输入类型
    /// </summary>
    public BattleInputContext InputContext => _inputContext;
    
    /// <summary>
    /// 玩家选择的行动（0=跳过，1=攻击，2=防御等）
    /// </summary>
    public int PlayerAction { get; set; } = -1;
    
    /// <summary>
    /// 玩家选择的目标
    /// </summary>
    public Player SelectedTarget { get; set; }

    /// <summary>
    /// 玩家当前可选择的AD列表
    /// </summary>
    public IReadOnlyList<Dice> AvailableActiveDice => _currentActiveDiceChoices ?? _emptyDiceList;

    /// <summary>
    /// 玩家当前可选择的PD列表
    /// </summary>
    public IReadOnlyList<Dice> AvailablePassiveDice => _currentPassiveDiceChoices ?? _emptyDiceList;

    /// <summary>
    /// 当前可攻击的对手列表
    /// </summary>
    public IReadOnlyList<Player> AvailableOpponents => _currentOpponents ?? _emptyPlayerList;

    // 阵营回合顺序
    private List<PlayerCamp> _campTurnOrder;
    private int _currentCampIndex;

    // 供玩家选择的临时数据
    private List<Player> _currentOpponents;
    private List<Dice> _currentActiveDiceChoices;
    private List<Dice> _currentPassiveDiceChoices;
    private BattleInputContext _inputContext;

    private PendingAttack _pendingAttack;

    private static readonly List<Dice> _emptyDiceList = new();
    private static readonly List<Player> _emptyPlayerList = new();

    private class PendingAttack
    {
        public Player Attacker { get; init; }
        public Player Defender { get; init; }
        public int AttackPower { get; init; }
        public Dice AttackDice { get; init; }
    }
    
    public Battle()
    {
        AllPlayers = new List<Player>();
        _campTurnOrder = new List<PlayerCamp>();
        CurrentState = BattleState.Idle;
        CurrentRound = 0;
        BattleLog = new List<string>();
        IsBattleOver = false;
        WinnerCamp = null;
        IsWaitingForPlayerInput = false;
        CurrentActionPlayer = null;
        _currentCampIndex = 0;
        _inputContext = BattleInputContext.None;
    }
    
    /// <summary>
    /// 添加玩家
    /// </summary>
    public void AddPlayer(Player player)
    {
        if (player != null && !AllPlayers.Any(p => p.PlayerId == player.PlayerId))
        {
            AllPlayers.Add(player);
        }
    }
    
    /// <summary>
    /// 初始化战斗
    /// </summary>
    public void InitializeBattle()
    {
        CurrentState = BattleState.Initialization;
        CurrentRound = 1;
        BattleLog.Clear();
        IsBattleOver = false;
        WinnerCamp = null;
        
        AddLog("=== 战斗开始 ===");
        
        // Step 1: 判定所有携带饰品并添加对应效果
        ApplyAccessoryEffects();
        
        // Step 2: 随机决定回合顺序
        RandomizeTurnOrder();
        
        // Step 3: 进入回合开始
        CurrentState = BattleState.RoundStart;
        _currentCampIndex = 0;
    }
    
    /// <summary>
    /// 应用所有饰品效果
    /// </summary>
    private void ApplyAccessoryEffects()
    {
        AddLog("应用饰品效果...");
        
        foreach (var player in AllPlayers)
        {
            // 创建战斗上下文
            var battleContext = new BattleContext();
            
            // 获取所有装备的饰品
            var accessories = player.GetEquippedAccessories();
            
            // 应用每个饰品的效果
            foreach (var accessory in accessories)
            {
                accessory.OnBattleStart(battleContext);
                AddLog($"{player.PlayerName}的{accessory.Name}发动效果");
            }
            
            // 设置玩家的初始HP
            player.MaxHP = battleContext.PlayerHP;
            player.CurrentHP = battleContext.PlayerHP;
            player.ShieldLayers = battleContext.ShieldLayers;
            
            AddLog($"{player.PlayerName} HP: {player.CurrentHP}, 护盾: {player.ShieldLayers}");
        }
    }
    
    /// <summary>
    /// 随机决定回合顺序
    /// </summary>
    private void RandomizeTurnOrder()
    {
        _campTurnOrder = new List<PlayerCamp> { PlayerCamp.Team1, PlayerCamp.Team2 };
        Random random = new Random();
        if (random.Next(2) == 0)
        {
            // 交换顺序
            (_campTurnOrder[0], _campTurnOrder[1]) = (_campTurnOrder[1], _campTurnOrder[0]);
        }
        AddLog("回合顺序已随机（阵营级）");
        AddLog($"  先手阵营: {_campTurnOrder[0]}");
        AddLog($"  后手阵营: {_campTurnOrder[1]}");
    }
    
    /// <summary>
    /// 更新战斗逻辑（每一帧调用）
    /// </summary>
    public void Update()
    {
        if (IsBattleOver || CurrentState == BattleState.Idle)
            return;

        if (IsWaitingForPlayerInput)
            return;
        
        switch (CurrentState)
        {
            case BattleState.RoundStart:
                HandleRoundStart();
                break;
            
            case BattleState.PlayerAction:
                HandlePlayerAction();
                break;
            
            case BattleState.EffectCalculation:
                HandleEffectCalculation();
                break;
            
            case BattleState.RoundEnd:
                HandleRoundEnd();
                break;
        }
    }
    
    /// <summary>
    /// 处理回合开始
    /// </summary>
    private void HandleRoundStart()
    {
        if (_currentCampIndex == 0)
        {
            AddLog($"\n=== 第{CurrentRound}回合开始 ===");
            foreach (var player in AllPlayers)
            {
                player.ResetRoundState();
            }
        }
        
        _currentCampIndex = 0;
        CurrentState = BattleState.PlayerAction;
    }
    
    /// <summary>
    /// 处理玩家行动
    /// </summary>
    private void HandlePlayerAction()
    {
        if (_currentCampIndex >= _campTurnOrder.Count)
        {
            CurrentState = BattleState.EffectCalculation;
            return;
        }

        CurrentCamp = _campTurnOrder[_currentCampIndex];
        var actingPlayers = AllPlayers.Where(p => p.Camp == CurrentCamp && !p.IsDead).ToList();

        if (actingPlayers.Count == 0)
        {
            _currentCampIndex++;
            return;
        }

        var player = actingPlayers[new Random().Next(actingPlayers.Count)];
        var opponents = GetOpponents(player);

        if (HasStunEffect(player))
        {
            AddLog($"{player.PlayerName}被眩晕，无法行动！");
            _currentCampIndex++;
            return;
        }

        if (player.IsDead)
        {
            _currentCampIndex++;
            return;
        }

        CurrentActionPlayer = player;
        _currentOpponents = opponents;

        if (opponents.Count == 0)
        {
            AddLog($"{player.PlayerName}没有对手，跳过行动");
            _currentCampIndex++;
            return;
        }

        if (player.Camp == PlayerCamp.Team1 && player.PlayerId == "player")
        {
            PreparePlayerAttackSelection(player, opponents);
        }
        else
        {
            ExecuteAIAction(player, opponents);
            AdvanceAfterAction();
        }
    }

    private void PreparePlayerAttackSelection(Player player, List<Player> opponents)
    {
        _currentActiveDiceChoices = player.GetEquippedDice()
            .Where(d => d.UsageType == DiceUsageType.Active || d.UsageType == DiceUsageType.Both)
            .ToList();

        if (_currentActiveDiceChoices.Count == 0)
        {
            AddLog($"{player.PlayerName}没有可用的AD，跳过行动");
            AdvanceAfterAction();
            return;
        }

        AddLog($"\n---{player.PlayerName}的行动回合---");
        AddLog("选择一个AD骰子发动（点击选择），点击\"跳过\"可跳过");
        for (int i = 0; i < _currentActiveDiceChoices.Count; i++)
        {
            var dice = _currentActiveDiceChoices[i];
            AddLog($"  {i + 1}. {dice.Name} {dice.GetDiceTypeLabel()}");
        }

        IsWaitingForPlayerInput = true;
        _inputContext = BattleInputContext.AttackSelection;
    }
    
    /// <summary>
    /// 执行电脑AI行动
    /// </summary>
    private void ExecuteAIAction(Player aiPlayer, List<Player> opponents)
    {
        if (opponents.Count == 0)
        {
            AddLog($"{aiPlayer.PlayerName}没有对手，跳过行动");
            return;
        }

        var adDice = aiPlayer.GetEquippedDice()
            .FirstOrDefault(d => d.UsageType == DiceUsageType.Active || d.UsageType == DiceUsageType.Both);

        if (adDice == null)
        {
            AddLog($"{aiPlayer.PlayerName}没有可用的AD，跳过行动");
            return;
        }

        var actionResult = adDice.ExecuteActiveAction(aiPlayer, opponents);
        if (actionResult == null || !actionResult.Success)
        {
            AddLog($"{aiPlayer.PlayerName}的{adDice.Name}发动失败");
            return;
        }

        var target = actionResult.Target ?? opponents.First();
        ResolveAttackResult(aiPlayer, adDice, target, actionResult.AttackPower, actionResult.Message);
    }
    
    /// <summary>
    /// 执行玩家提交的行动
    /// </summary>
    public void SubmitPlayerAttackChoice(Dice selectedDice, Player target)
    {
        if (_inputContext != BattleInputContext.AttackSelection || CurrentActionPlayer == null)
            return;

        if (selectedDice == null)
        {
            AddLog($"{CurrentActionPlayer.PlayerName}选择跳过行动");
            ClearAttackSelectionState(false);
            AdvanceAfterAction();
            return;
        }

        var defenders = _currentOpponents ?? GetOpponents(CurrentActionPlayer);
        if (defenders.Count == 0)
        {
            AddLog("没有可攻击的目标，跳过行动");
            ClearAttackSelectionState(false);
            AdvanceAfterAction();
            return;
        }

        var forcedTarget = target ?? defenders.First();
        var actionResult = selectedDice.ExecuteActiveAction(CurrentActionPlayer, defenders);
        if (actionResult == null || !actionResult.Success)
        {
            AddLog($"{CurrentActionPlayer.PlayerName}的{selectedDice.Name}发动失败");
            ClearAttackSelectionState(false);
            AdvanceAfterAction();
            return;
        }

        var resolvedTarget = actionResult.Target ?? forcedTarget;
        ResolveAttackResult(CurrentActionPlayer, selectedDice, resolvedTarget, actionResult.AttackPower, actionResult.Message);
        bool waitingForDefense = IsWaitingForPlayerInput && _inputContext == BattleInputContext.DefenseSelection;
        ClearAttackSelectionState(waitingForDefense);
        if (!waitingForDefense)
        {
            AdvanceAfterAction();
        }
    }

    public void SubmitPlayerDefenseChoice(Dice selectedDice)
    {
        if (_inputContext != BattleInputContext.DefenseSelection || _pendingAttack == null)
            return;

        var defender = _pendingAttack.Defender;
        var attackPower = _pendingAttack.AttackPower;
        DefenseResult defenseResult;

        if (selectedDice == null)
        {
            defenseResult = new DefenseResult(0, attackPower, $"{defender.PlayerName}选择跳过防御，受到{attackPower}点伤害");
        }
        else
        {
            defenseResult = selectedDice.ExecutePassiveAction(defender, attackPower)
                            ?? new DefenseResult(0, attackPower, $"{defender.PlayerName}防御失败，受到{attackPower}点伤害");
        }

        ApplyDamage(defenseResult, defender, _pendingAttack.Attacker);

        ClearDefenseState();
        AdvanceAfterAction();
    }

    private void ResolveAttackResult(Player attacker, Dice usedDice, Player target, int attackPower, string message)
    {
        AddLog($"{attacker.PlayerName}使用{usedDice.Name}发动：{message}");
        AddLog($"目标: {target.PlayerName} | 攻击点数: {attackPower}");

        var pdChoices = target.GetEquippedDice().Where(d => d.UsageType == DiceUsageType.Passive || d.UsageType == DiceUsageType.Both).ToList();

        // 玩家可选择PD
        if (target.PlayerId == "player" && pdChoices.Count > 0)
        {
            _pendingAttack = new PendingAttack
            {
                Attacker = attacker,
                Defender = target,
                AttackPower = attackPower,
                AttackDice = usedDice
            };

            _currentPassiveDiceChoices = pdChoices;
            IsWaitingForPlayerInput = true;
            _inputContext = BattleInputContext.DefenseSelection;

            AddLog("请选择防御用PD（点击选择），点击\"跳过\"可跳过");
            for (int i = 0; i < pdChoices.Count; i++)
            {
                AddLog($"  {i + 1}. {pdChoices[i].Name} {pdChoices[i].GetDiceTypeLabel()}");
            }
            return;
        }

        var defenseResult = AutoResolveDefense(target, attackPower, pdChoices);
        ApplyDamage(defenseResult, target, attacker);
    }

    private DefenseResult AutoResolveDefense(Player defender, int attackDamage, List<Dice> pdChoices)
    {
        var dice = pdChoices.FirstOrDefault();
        if (dice == null)
        {
            return new DefenseResult(0, attackDamage, $"{defender.PlayerName}无法防御，受到{attackDamage}点伤害");
        }

        var result = dice.ExecutePassiveAction(defender, attackDamage);
        return result ?? new DefenseResult(0, attackDamage, $"{defender.PlayerName}防御失败，受到{attackDamage}点伤害");
    }

    private void ApplyDamage(DefenseResult defenseResult, Player defender, Player attacker)
    {
        AddLog(defenseResult.Message);

        if (defenseResult.ActualDamage > 0)
        {
            int actualDamage = defender.TakeDamage(defenseResult.ActualDamage);
            AddLog($"{defender.PlayerName}受到{actualDamage}点伤害，当前HP: {defender.CurrentHP}");
            if (defender.IsDead)
            {
                AddLog($"{defender.PlayerName}已被击败！");
                if (attacker != null && IsTeamEliminated(defender.Camp))
                {
                    EndBattle(attacker.Camp);
                }
            }
        }
        else
        {
            AddLog($"{defender.PlayerName}未受到伤害");
        }
    }

    private void AdvanceAfterAction()
    {
        _currentCampIndex++;
        CurrentState = _currentCampIndex >= _campTurnOrder.Count ? BattleState.EffectCalculation : BattleState.PlayerAction;
    }

    private void ClearAttackSelectionState(bool keepWaitingForDefense)
    {
        _currentActiveDiceChoices = null;
        _currentOpponents = null;
        if (!keepWaitingForDefense)
        {
            IsWaitingForPlayerInput = false;
            _inputContext = BattleInputContext.None;
            PlayerAction = -1;
            SelectedTarget = null;
        }
    }

    private void ClearDefenseState()
    {
        IsWaitingForPlayerInput = false;
        _inputContext = BattleInputContext.None;
        _currentPassiveDiceChoices = null;
        _pendingAttack = null;
        _currentOpponents = null;
    }
    
    /// <summary>
    /// 处理效果计算
    /// </summary>
    private void HandleEffectCalculation()
    {
        AddLog("\n应用增益减益效果...");
        
        foreach (var player in AllPlayers)
        {
            // 应用所有效果
            foreach (var effect in player.ActiveEffects)
            {
                effect.ApplyEffect(player);
                AddLog($"{player.PlayerName}受到{effect.Name}的影响");
            }
            
            // 更新效果
            player.UpdateEffects();
        }
        
        CurrentState = BattleState.RoundEnd;
    }
    
    /// <summary>
    /// 处理回合结束
    /// </summary>
    private void HandleRoundEnd()
    {
        // 检查是否有阵营全灭
        if (IsTeamEliminated(PlayerCamp.Team1))
        {
            EndBattle(PlayerCamp.Team2);
            return;
        }
        
        if (IsTeamEliminated(PlayerCamp.Team2))
        {
            EndBattle(PlayerCamp.Team1);
            return;
        }
        
        // 进入下一回合
        CurrentRound++;
        _currentCampIndex = 0;
        CurrentState = BattleState.RoundStart;
    }
    
    /// <summary>
    /// 检查是否有眩晕效果
    /// </summary>
    private bool HasStunEffect(Player player)
    {
        return player.ActiveEffects.OfType<StunEffect>().Any();
    }
    
    /// <summary>
    /// 获取对手列表
    /// </summary>
    private List<Player> GetOpponents(Player player)
    {
        return AllPlayers.Where(p => p.Camp != player.Camp && !p.IsDead).ToList();
    }
    
    /// <summary>
    /// 检查阵营是否被全灭
    /// </summary>
    private bool IsTeamEliminated(PlayerCamp camp)
    {
        var teamPlayers = AllPlayers.Where(p => p.Camp == camp).ToList();
        return teamPlayers.All(p => p.IsDead);
    }
    
    /// <summary>
    /// 结束战斗
    /// </summary>
    private void EndBattle(PlayerCamp winner)
    {
        IsBattleOver = true;
        WinnerCamp = winner;
        CurrentState = BattleState.BattleEnd;
        
        AddLog($"\n=== 战斗结束 ===");
        AddLog($"{winner}阵营获胜！");
        
        // 更新飞升之证的计数器（如果需要）
        UpdateAscensionProof(winner);
    }
    
    /// <summary>
    /// 更新飞升之证饰品的计数器
    /// </summary>
    private void UpdateAscensionProof(PlayerCamp winnerCamp)
    {
        foreach (var player in AllPlayers)
        {
            var ascensionProof = player.EquippedItems.OfType<AscensionProofAccessory>().FirstOrDefault();
            
            if (ascensionProof != null)
            {
                if (player.Camp == winnerCamp)
                {
                    ascensionProof.OnWin();
                }
                else
                {
                    ascensionProof.OnLoss();
                }
            }
        }
    }
    
    /// <summary>
    /// 添加战斗日志
    /// </summary>
    private void AddLog(string message)
    {
        BattleLog.Add(message);
        System.Diagnostics.Debug.WriteLine(message);
    }
}
