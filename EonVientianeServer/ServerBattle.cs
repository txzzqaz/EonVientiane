using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 服务器端战斗管理器 - 负责处理多人对战的逻辑
/// </summary>
public class ServerBattle
{
    /// <summary>
    /// 房间ID
    /// </summary>
    public string RoomId { get; }
    
    /// <summary>
    /// 所有参与者
    /// </summary>
    private Dictionary<string, Player> _players = new();
    
    /// <summary>
    /// 当前战斗状态
    /// </summary>
    public BattleState CurrentState { get; private set; }
    
    /// <summary>
    /// 当前回合数
    /// </summary>
    public int CurrentRound { get; private set; }
    
    /// <summary>
    /// 当前行动的阵营
    /// </summary>
    public PlayerCamp CurrentCamp { get; private set; }
    
    /// <summary>
    /// 战斗日志
    /// </summary>
    public List<string> BattleLog { get; private set; }
    
    /// <summary>
    /// 是否战斗结束
    /// </summary>
    public bool IsBattleOver { get; private set; }
    
    /// <summary>
    /// 赢家阵营
    /// </summary>
    public PlayerCamp? WinnerCamp { get; private set; }
    
    /// <summary>
    /// 当前行动的玩家ID
    /// </summary>
    public string CurrentActionPlayerId { get; private set; }
    
    /// <summary>
    /// 当前等待输入的玩家ID（如果在防守阶段）
    /// </summary>
    public string CurrentDefenderPlayerId { get; private set; }
    
    /// <summary>
    /// 当前等待的输入上下文
    /// </summary>
    public BattleInputContext CurrentInputContext { get; private set; }
    
    // 内部战斗状态
    private List<PlayerCamp> _campTurnOrder;
    private int _currentCampIndex;
    private List<string> _currentOpponentIds;
    private List<Dice> _currentActiveDiceChoices;
    private List<Dice> _currentPassiveDiceChoices;
    private PendingAttack _pendingAttack;
    private Dictionary<string, int> _playerInitialHP;
    private Dictionary<string, bool> _playerTookDamage;
    private Dictionary<string, HashSet<int>> _playerRollValues;
    private int _lastSentLogIndex = 0;
    
    // 时间追踪
    private DateTime _battleStartTime;
    private DateTime _currentActionStartTime;
    private Dictionary<string, TimeSpan> _playerTotalActionTime; // 每个玩家的总行动时间
    private Dictionary<string, TimeSpan> _opponentTotalActionTime; // 从每个玩家视角看到的对手总行动时间
    private Dictionary<string, TimeSpan> _playerRoundSlowestActionTime; // 每个玩家在本回合的最慢一步时间（用于漫游者之心）
    private bool _currentPlayerHasHolyFireOpponent; // 当前行动的玩家是否面对装备了圣火的对手
    
    // 成就追踪 - 飞羽闪避连续成功
    private Dictionary<string, int> _playerFeatheredDodgeStreak; // 每个玩家使用飞羽闪避的连续成功次数
    private Dictionary<string, bool> _playerLastDefenseWasFeatheredSuccess; // 上一次防御是否为飞羽成功闪避
    
    // 战斗统计追踪
    private Dictionary<string, int> _playerDamageDealt;    // 每个玩家造成的总伤害
    private Dictionary<string, int> _playerDamageTaken;    // 每个玩家承受的总伤害
    private Dictionary<string, int> _playerDamageBlocked;  // 每个玩家格挡的总伤害
    private Dictionary<string, int> _playerAttackCount;    // 每个玩家的攻击次数
    private Dictionary<string, int> _playerDefenseCount;   // 每个玩家的防御次数
    private Dictionary<string, int> _playerKillCount;      // 每个玩家的击杀数
    private Dictionary<string, Dictionary<string, int>> _playerDiceUsage; // 每个玩家使用骰子的统计
    
    private class PendingAttack
    {
        public string AttackerId { get; init; }
        public string DefenderId { get; init; }
        public int AttackPower { get; init; }
        public Dice AttackDice { get; init; }
    }
    
    public ServerBattle(string roomId, List<ConnectedClient> clients)
    {
        RoomId = roomId;
        CurrentState = BattleState.Idle;
        CurrentRound = 0;
        BattleLog = new List<string>();
        IsBattleOver = false;
        WinnerCamp = null;
        CurrentActionPlayerId = null;
        CurrentDefenderPlayerId = null;
        CurrentInputContext = BattleInputContext.None;
        _campTurnOrder = new List<PlayerCamp>();
        _currentCampIndex = 0;
        _currentOpponentIds = new List<string>();
        _playerInitialHP = new Dictionary<string, int>();
        _playerTookDamage = new Dictionary<string, bool>();
        _playerRollValues = new Dictionary<string, HashSet<int>>();
        _playerTotalActionTime = new Dictionary<string, TimeSpan>();
        _opponentTotalActionTime = new Dictionary<string, TimeSpan>();
        _playerRoundSlowestActionTime = new Dictionary<string, TimeSpan>();
        _playerFeatheredDodgeStreak = new Dictionary<string, int>();
        _playerLastDefenseWasFeatheredSuccess = new Dictionary<string, bool>();
        
        // 初始化战斗统计字典
        _playerDamageDealt = new Dictionary<string, int>();
        _playerDamageTaken = new Dictionary<string, int>();
        _playerDamageBlocked = new Dictionary<string, int>();
        _playerAttackCount = new Dictionary<string, int>();
        _playerDefenseCount = new Dictionary<string, int>();
        _playerKillCount = new Dictionary<string, int>();
        _playerDiceUsage = new Dictionary<string, Dictionary<string, int>>();
        
        // 为每个客户端创建玩家对象
        foreach (var client in clients)
        {
            var player = new Player(client.UserId, client.PlayerName, client.TeamId == 1 ? PlayerCamp.Team1 : PlayerCamp.Team2);
            _players[client.UserId] = player;
            
            // 初始化玩家统计数据
            _playerDamageDealt[client.UserId] = 0;
            _playerDamageTaken[client.UserId] = 0;
            _playerDamageBlocked[client.UserId] = 0;
            _playerAttackCount[client.UserId] = 0;
            _playerDefenseCount[client.UserId] = 0;
            _playerKillCount[client.UserId] = 0;
            _playerDiceUsage[client.UserId] = new Dictionary<string, int>();
            _playerRoundSlowestActionTime[client.UserId] = TimeSpan.Zero;
        }
    }
    
    /// <summary>
    /// 初始化战斗
    /// </summary>
    public void InitializeBattle(Dictionary<string, List<Equipment>> playerEquipment)
    {
        CurrentState = BattleState.Initialization;
        CurrentRound = 1;
        BattleLog.Clear();
        IsBattleOver = false;
        WinnerCamp = null;
        
        AddLog("=== 联机战斗开始 ===");
        
        // 为玩家装备
        foreach (var kvp in playerEquipment)
        {
            var playerId = kvp.Key;
            var equipment = kvp.Value;
            
            if (_players.TryGetValue(playerId, out var player))
            {
                player.EquippedItems.Clear();
                foreach (var item in equipment)
                {
                    player.AddEquipment(item);
                }
            }
        }
        
        // 应用饰品效果
        ApplyAccessoryEffects();
        
        // 初始化无伤跟踪
        _playerInitialHP.Clear();
        _playerTookDamage.Clear();
        _playerRollValues.Clear();
        _playerTotalActionTime.Clear();
        _opponentTotalActionTime.Clear();
        _playerFeatheredDodgeStreak.Clear();
        _playerLastDefenseWasFeatheredSuccess.Clear();
        foreach (var player in _players.Values)
        {
            _playerInitialHP[player.PlayerId] = player.MaxHP;
            _playerTookDamage[player.PlayerId] = false;
            _playerRollValues[player.PlayerId] = new HashSet<int>();
            _playerTotalActionTime[player.PlayerId] = TimeSpan.Zero;
            _opponentTotalActionTime[player.PlayerId] = TimeSpan.Zero;
            _playerFeatheredDodgeStreak[player.PlayerId] = 0;
            _playerLastDefenseWasFeatheredSuccess[player.PlayerId] = false;
        }
        
        // 记录战斗开始时间
        _battleStartTime = DateTime.UtcNow;
        
        // 随机决定回合顺序
        RandomizeTurnOrder();
        
        CurrentState = BattleState.RoundStart;
        _currentCampIndex = 0;
    }
    
    /// <summary>
    /// 应用所有饰品效果
    /// </summary>
    private void ApplyAccessoryEffects()
    {
        AddLog("应用饰品效果...");
        
        foreach (var player in _players.Values)
        {
            var battleContext = new BattleContext();
            var accessories = player.GetEquippedAccessories();
            
            foreach (var accessory in accessories)
            {
                accessory.OnBattleStart(battleContext);
                AddLog($"{player.PlayerName}的{accessory.Name}发动效果");
            }
            
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
            (_campTurnOrder[0], _campTurnOrder[1]) = (_campTurnOrder[1], _campTurnOrder[0]);
        }
        AddLog("回合顺序已随机（阵营级）");
        AddLog($"  先手阵营: {_campTurnOrder[0]}");
        AddLog($"  后手阵营: {_campTurnOrder[1]}");
    }
    
    /// <summary>
    /// 更新战斗逻辑（由服务器定期调用）
    /// </summary>
    public void Update()
    {
        if (IsBattleOver || CurrentState == BattleState.Idle)
            return;
        
        // 检查圣火效果：如果等待玩家输入超过0.5秒且对手有圣火，强制跳过
        if (CurrentInputContext == BattleInputContext.AttackSelection && _currentPlayerHasHolyFireOpponent)
        {
            var actionTime = DateTime.UtcNow - _currentActionStartTime;
            if (actionTime.TotalSeconds > 0.5)
            {
                AddLog($"圣火效果触发！{CurrentActionPlayerId}的行动超时，强制跳过");
                ProcessPlayerAttackChoice(CurrentActionPlayerId, null, null, null);
                return;
            }
        }
        
        // 如果等待玩家输入，不自动推进状态
        if (CurrentInputContext != BattleInputContext.None)
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
            foreach (var player in _players.Values)
            {
                player.ResetRoundState();
            }
            // 重置每个玩家的本回合最慢一步时间（用于漫游者之心）
            foreach (var playerId in _players.Keys)
            {
                _playerRoundSlowestActionTime[playerId] = TimeSpan.Zero;
            }
            _currentCampIndex = 0;
        }
        
        CurrentState = BattleState.PlayerAction;
        
        // 立即设置第一个玩家的行动（在RoundStart阶段完成，而不是等待下一个Update）
        SetupNextPlayerTurn();
    }
    
    /// <summary>
    /// 设置下一个玩家的行动
    /// </summary>
    private void SetupNextPlayerTurn()
    {
        while (_currentCampIndex < _campTurnOrder.Count)
        {
            CurrentCamp = _campTurnOrder[_currentCampIndex];
            var actingPlayers = _players.Values
                .Where(p => p.Camp == CurrentCamp && !p.IsDead)
                .ToList();
            
            if (actingPlayers.Count == 0)
            {
                _currentCampIndex++;
                continue;
            }
            
            var player = actingPlayers[new Random().Next(actingPlayers.Count)];
            var opponents = GetOpponents(player.PlayerId);
            
            if (player.IsDead)
            {
                _currentCampIndex++;
                continue;
            }
            
            // 检查眩晕状态但不自动跳过，让玩家自己选择
            if (HasStunEffect(player))
            {
                AddLog($"{player.PlayerName}被眩晕，需要点击跳过按钮！");
            }
            
            // 检查对手但不自动跳过
            if (opponents.Count == 0)
            {
                AddLog($"{player.PlayerName}没有对手，需要点击跳过按钮！");
            }
            
            CurrentActionPlayerId = player.PlayerId;
            _currentOpponentIds = opponents.Select(p => p.PlayerId).ToList();
            
            // 为该玩家准备可用的AD和对手列表
            PreparePlayerAttackSelection(player, opponents);
            
            // 成功设置了一个玩家，返回
            return;
        }
        
        // 没有更多玩家可以行动
        CurrentActionPlayerId = null;
        _currentOpponentIds.Clear();
        CurrentState = BattleState.EffectCalculation;
    }
    
    /// <summary>
    /// 处理玩家行动
    /// </summary>
    private void HandlePlayerAction()
    {
        // 在这个状态下只要等待玩家输入，不需要做其他事
        // 玩家选择由 SetupNextPlayerTurn 设置
    }
    
    /// <summary>
    /// 为玩家准备攻击选择
    /// </summary>
    private void PreparePlayerAttackSelection(Player player, List<Player> opponents)
    {
        _currentActiveDiceChoices = player.GetEquippedDice()
            .Where(d => d.UsageType == DiceUsageType.Active || d.UsageType == DiceUsageType.Both)
            .ToList();
        
        if (_currentActiveDiceChoices.Count == 0)
        {
            AddLog($"{player.PlayerName}没有可用的AD，需要点击跳过按钮！");
        }
        
        AddLog($"\n---{player.PlayerName}的行动回合---");
        AddLog("等待玩家选择AD骰子或点击跳过...");
        CurrentInputContext = BattleInputContext.AttackSelection;
        
        // 记录行动开始时间
        _currentActionStartTime = DateTime.UtcNow;
        
        // 检查对手是否装备了圣火
        _currentPlayerHasHolyFireOpponent = false;
        foreach (var opponent in opponents)
        {
            var accessories = opponent.GetEquippedAccessories();
            if (accessories.Any(a => a is HolyFireAccessory))
            {
                _currentPlayerHasHolyFireOpponent = true;
                AddLog($"{opponent.PlayerName}装备了圣火！若行动超过0.5秒将被强制跳过");
                break;
            }
        }
    }
    
    /// <summary>
    /// 处理玩家的攻击选择
    /// </summary>
    public void ProcessPlayerAttackChoice(string playerId, string selectedDiceName, string targetPlayerId, int? manualDiceValue)
    {
        if (CurrentInputContext != BattleInputContext.AttackSelection || CurrentActionPlayerId != playerId)
        {
            AddLog($"非法的攻击选择：来自 {playerId}");
            return;
        }
        
        // 记录行动时间
        var actionTime = DateTime.UtcNow - _currentActionStartTime;
        if (_playerTotalActionTime.ContainsKey(playerId))
        {
            _playerTotalActionTime[playerId] += actionTime;
        }
        
        // 更新本回合最慢一步时间（用于漫游者之心）
        if (_playerRoundSlowestActionTime.ContainsKey(playerId))
        {
            if (actionTime > _playerRoundSlowestActionTime[playerId])
            {
                _playerRoundSlowestActionTime[playerId] = actionTime;
            }
        }
        
        // 更新对手视角的时间统计
        foreach (var opponentId in _currentOpponentIds)
        {
            if (_opponentTotalActionTime.ContainsKey(opponentId))
            {
                _opponentTotalActionTime[opponentId] += actionTime;
            }
        }
        
        if (!_players.TryGetValue(playerId, out var attacker))
            return;
        
        if (selectedDiceName == null)
        {
            // 跳过行动
            AddLog($"{attacker.PlayerName}选择跳过行动");
            CurrentInputContext = BattleInputContext.None;
            if (!IsBattleOver)
            {
                AdvanceAfterAction();
            }
            return;
        }
        
        var selectedDice = attacker.GetEquippedDice()
            .FirstOrDefault(d => d.Name == selectedDiceName);
        
        if (selectedDice == null)
        {
            AddLog($"{attacker.PlayerName}选择的骰子不存在");
            return;
        }
        
        var opponents = _currentOpponentIds
            .Select(id => _players[id])
            .ToList();
        
        // 如果没有可攻击的对手，记录但允许玩家继续选择或跳过
        if (opponents.Count == 0)
        {
            AddLog("没有可攻击的目标，请点击跳过按钮！");
        }
        
        Player target = null;
        if (opponents.Count > 0)
        {
            if (string.IsNullOrEmpty(targetPlayerId))
            {
                target = opponents.First();
            }
            else if (_players.TryGetValue(targetPlayerId, out var t) && opponents.Contains(t))
            {
                target = t;
            }
            else
            {
                target = opponents.First();
            }
        }
        
        if (selectedDice is IManualRollDice manualDice)
        {
            manualDice.SetManualRoll(manualDiceValue);
        }
        
        var actionResult = selectedDice.ExecuteActiveAction(attacker, opponents);
        if (actionResult == null || !actionResult.Success)
        {
            AddLog($"{attacker.PlayerName}的{selectedDice.Name}发动失败");
            CurrentInputContext = BattleInputContext.None;
            AdvanceAfterAction();
            return;
        }
        
        // 记录攻击次数和骰子使用
        _playerAttackCount[playerId]++;
        if (!_playerDiceUsage[playerId].ContainsKey(selectedDice.Name))
        {
            _playerDiceUsage[playerId][selectedDice.Name] = 0;
        }
        _playerDiceUsage[playerId][selectedDice.Name]++;

        if (!actionResult.TriggersDefense)
        {
            AddLog($"{attacker.PlayerName}使用{selectedDice.Name}发动：{actionResult.Message}");
            CurrentInputContext = BattleInputContext.None;
            if (!IsBattleOver)
            {
                AdvanceAfterAction();
            }
            return;
        }

        // 记录点数并应用戮力同心加成
        int rollValue = Math.Max(0, actionResult.AttackPower);
        bool concertedTriggered;
        int boostedAttackPower = ApplyConcertedEffortBonus(attacker, rollValue, actionResult.AttackPower, out concertedTriggered);
        if (concertedTriggered)
        {
            AddLog($"戮力同心触发！{attacker.PlayerName}的行动效果提升至{boostedAttackPower}");
        }
        
        // 应用漫游者之心倍率（基于本回合最慢的一步时间）
        int finalAttackPower = ApplyWandererHeartMultiplier(attacker, boostedAttackPower);
        if (finalAttackPower != boostedAttackPower)
        {
            AddLog($"漫游者之心触发！根据回合内最慢一步({_playerRoundSlowestActionTime[playerId].TotalSeconds:F2}秒)，攻击力调整为{finalAttackPower}");
        }
        
        var resolvedTarget = actionResult.Target ?? target;
        ResolveAttackResult(attacker, selectedDice, resolvedTarget, finalAttackPower, actionResult.Message);
        
        // 如果没有进入防守状态，直接推进（但需要检查战斗是否已结束）
        if (CurrentInputContext == BattleInputContext.None && !IsBattleOver)
        {
            AdvanceAfterAction();
        }
    }
    
    /// <summary>
    /// 处理玩家的防守选择
    /// </summary>
    public void ProcessPlayerDefenseChoice(string playerId, string selectedDiceName, int? manualDiceValue)
    {
        if (CurrentInputContext != BattleInputContext.DefenseSelection || 
            CurrentDefenderPlayerId != playerId || 
            _pendingAttack == null)
        {
            AddLog($"非法的防守选择：来自 {playerId}");
            return;
        }
        
        if (!_players.TryGetValue(playerId, out var defender))
            return;
        
        var attackPower = _pendingAttack.AttackPower;
        DefenseResult defenseResult;
        Dice selectedDice = null;
        
        // 记录防御次数
        _playerDefenseCount[playerId]++;
        
        if (selectedDiceName == null)
        {
            defenseResult = new DefenseResult(0, attackPower, 
                $"{defender.PlayerName}选择跳过防御，受到{attackPower}点伤害");
            
            // 跳过防御则中断飞羽连击
            _playerFeatheredDodgeStreak[playerId] = 0;
            _playerLastDefenseWasFeatheredSuccess[playerId] = false;
        }
        else
        {
            selectedDice = defender.GetEquippedDice()
                .FirstOrDefault(d => d.Name == selectedDiceName);
            
            if (selectedDice == null)
            {
                AddLog($"{defender.PlayerName}选择的骰子不存在");
                return;
            }
            
            if (selectedDice is IManualRollDice manualDice)
            {
                manualDice.SetManualRoll(manualDiceValue);
            }
            
            defenseResult = selectedDice.ExecutePassiveAction(defender, attackPower)
                ?? new DefenseResult(0, attackPower, $"{defender.PlayerName}防御失败，受到{attackPower}点伤害");
            
            // 追踪飞羽闪避连击
            TrackFeatheredDodgeStreak(playerId, selectedDice, defenseResult);
        }
        
        var attacker = _players[_pendingAttack.AttackerId];
        var usedDice = _pendingAttack.AttackDice;
        ApplyDamage(defenseResult, defender, attacker, usedDice);
        TrackRoll(defender.PlayerId, defenseResult.DefensePower);
        
        CurrentInputContext = BattleInputContext.None;
        _pendingAttack = null;
        
        // 只有在战斗未结束时才推进到下一个行动
        if (!IsBattleOver)
        {
            AdvanceAfterAction();
        }
    }
    
    /// <summary>
    /// 解析攻击结果
    /// </summary>
    private void ResolveAttackResult(Player attacker, Dice usedDice, Player target, 
        int attackPower, string message)
    {
        AddLog($"{attacker.PlayerName}使用{usedDice.Name}发动：{message}");
        AddLog($"目标: {target.PlayerName} | 攻击点数: {attackPower}");
        
        var pdChoices = target.GetEquippedDice()
            .Where(d => d.UsageType == DiceUsageType.Passive || d.UsageType == DiceUsageType.Both)
            .ToList();
        
        // 如果目标是玩家且有防守骰子，等待其防守选择
        if (pdChoices.Count > 0 && !IsAIPlayer(target.PlayerId))
        {
            _pendingAttack = new PendingAttack
            {
                AttackerId = attacker.PlayerId,
                DefenderId = target.PlayerId,
                AttackPower = attackPower,
                AttackDice = usedDice
            };
            
            _currentPassiveDiceChoices = pdChoices;
            CurrentDefenderPlayerId = target.PlayerId;
            CurrentInputContext = BattleInputContext.DefenseSelection;
            
            AddLog("请防守方选择防御用PD（点击选择），点击\"跳过\"可跳过");
            for (int i = 0; i < pdChoices.Count; i++)
            {
                AddLog($"  {i + 1}. {pdChoices[i].Name} {pdChoices[i].GetDiceTypeLabel()}");
            }
            return;
        }
        
        // 自动防守
        var defenseResult = AutoResolveDefense(target, attackPower, pdChoices);
        ApplyDamage(defenseResult, target, attacker, usedDice);
    }
    
    /// <summary>
    /// 自动防守
    /// </summary>
    private DefenseResult AutoResolveDefense(Player defender, int attackDamage, List<Dice> pdChoices)
    {
        var dice = pdChoices.FirstOrDefault();
        if (dice == null)
        {
            return new DefenseResult(0, attackDamage, $"{defender.PlayerName}无法防御，受到{attackDamage}点伤害");
        }
        
        var result = dice.ExecutePassiveAction(defender, attackDamage);
        var finalResult = result ?? new DefenseResult(0, attackDamage, $"{defender.PlayerName}防御失败，受到{attackDamage}点伤害");
        TrackRoll(defender.PlayerId, finalResult.DefensePower);
        return finalResult;
    }
    
    /// <summary>
    /// 应用伤害
    /// </summary>
    private void ApplyDamage(DefenseResult defenseResult, Player defender, Player attacker, Dice usedDice = null)
    {
        AddLog(defenseResult.Message);
        
        // 记录格挡伤害
        int blockedDamage = defenseResult.DefensePower;
        if (blockedDamage > 0)
        {
            _playerDamageBlocked[defender.PlayerId] += blockedDamage;
        }
        
        if (defenseResult.ActualDamage > 0)
        {
            int actualDamage = defender.TakeDamage(defenseResult.ActualDamage);
            
            // 记录造成和承受的伤害
            if (actualDamage > 0)
            {
                _playerTookDamage[defender.PlayerId] = true;
                _playerDamageTaken[defender.PlayerId] += actualDamage;
                _playerDamageDealt[attacker.PlayerId] += actualDamage;
            }
            
            AddLog($"{defender.PlayerName}受到{actualDamage}点伤害，当前HP: {defender.CurrentHP}");
            
            // 检查刮痧师傅的再次掷骰效果
            if (usedDice is GuaShaParquetDice guashaDice && actualDamage > 0)
            {
                // 触发再次掷骰效果
                int additionalDamage = guashaDice.ExecuteRepeatedRoll(actualDamage);
                if (additionalDamage > 0)
                {
                    AddLog($"刮痧师傅触发再次投掷效果！根据{actualDamage}点伤害重投{actualDamage}次");
                    AddLog($"额外投掷结果: {additionalDamage}点");
                    
                    // 应用额外伤害
                    int extraActualDamage = defender.TakeDamage(additionalDamage);
                    if (extraActualDamage > 0)
                    {
                        _playerDamageTaken[defender.PlayerId] += extraActualDamage;
                        _playerDamageDealt[attacker.PlayerId] += extraActualDamage;
                        AddLog($"{defender.PlayerName}受到额外{extraActualDamage}点伤害，当前HP: {defender.CurrentHP}");
                    }
                }
            }
            
            if (defender.IsDead)
            {
                AddLog($"{defender.PlayerName}已被击败！");
                
                // 记录击杀
                _playerKillCount[attacker.PlayerId]++;
                
                if (IsTeamEliminated(defender.Camp))
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
    
    /// <summary>
    /// 推进到下一个行动
    /// </summary>
    private void AdvanceAfterAction()
    {
        _currentCampIndex++;
        CurrentActionPlayerId = null;
        CurrentDefenderPlayerId = null;
        CurrentInputContext = BattleInputContext.None;
        
        if (_currentCampIndex >= _campTurnOrder.Count)
        {
            CurrentState = BattleState.EffectCalculation;
        }
        else
        {
            // 立即设置下一个玩家的行动
            SetupNextPlayerTurn();
        }
    }
    
    /// <summary>
    /// 处理效果计算
    /// </summary>
    private void HandleEffectCalculation()
    {
        AddLog("\n应用增益减益效果...");
        
        foreach (var player in _players.Values)
        {
            foreach (var effect in player.ActiveEffects)
            {
                effect.ApplyEffect(player);
                AddLog($"{player.PlayerName}受到{effect.Name}的影响");
            }
            
            player.UpdateEffects();
        }
        
        CurrentState = BattleState.RoundEnd;
    }
    
    /// <summary>
    /// 处理回合结束
    /// </summary>
    private void HandleRoundEnd()
    {
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
        
        CurrentRound++;
        _currentCampIndex = 0;
        CurrentState = BattleState.RoundStart;
    }
    
    /// <summary>
    /// 结束战斗
    /// </summary>
    private void EndBattle(PlayerCamp winner)
    {
        IsBattleOver = true;
        WinnerCamp = winner;
        CurrentState = BattleState.BattleEnd;
        
        // 清空当前行动玩家信息
        CurrentActionPlayerId = null;
        CurrentDefenderPlayerId = null;
        CurrentInputContext = BattleInputContext.None;
        
        AddLog($"\n=== 战斗结束 ===");
        AddLog($"{winner}阵营获胜！");
        
        // 记录对手行动时间用于成就检测
        foreach (var player in _players.Values)
        {
            var opponentTime = GetOpponentTotalActionTime(player.PlayerId);
            AddLog($"{player.PlayerName} 对手总行动时间: {opponentTime.TotalSeconds:F1}秒");
        }
    }
    
    /// <summary>
    /// 生成战斗统计数据
    /// </summary>
    public List<PlayerBattleStats> GenerateBattleStats()
    {
        var stats = new List<PlayerBattleStats>();
        
        // 计算MVP（基于总伤害和击杀数）
        string mvpPlayerId = CalculateMVP();
        
        foreach (var player in _players.Values)
        {
            var playerStats = new PlayerBattleStats
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                TeamId = player.Camp == PlayerCamp.Team1 ? 1 : 2,
                TotalDamageDealt = _playerDamageDealt.GetValueOrDefault(player.PlayerId, 0),
                TotalDamageTaken = _playerDamageTaken.GetValueOrDefault(player.PlayerId, 0),
                TotalDamageBlocked = _playerDamageBlocked.GetValueOrDefault(player.PlayerId, 0),
                AttackCount = _playerAttackCount.GetValueOrDefault(player.PlayerId, 0),
                DefenseCount = _playerDefenseCount.GetValueOrDefault(player.PlayerId, 0),
                KillCount = _playerKillCount.GetValueOrDefault(player.PlayerId, 0),
                TotalActionTime = GetPlayerTotalActionTime(player.PlayerId),
                DiceUsageCount = _playerDiceUsage.GetValueOrDefault(player.PlayerId, new Dictionary<string, int>()),
                IsMVP = player.PlayerId == mvpPlayerId
            };
            
            stats.Add(playerStats);
        }
        
        return stats;
    }
    
    /// <summary>
    /// 计算MVP玩家
    /// </summary>
    private string CalculateMVP()
    {
        if (_players.Count == 0)
            return null;
        
        // MVP计算：伤害权重70%，击杀权重30%
        var mvpScores = new Dictionary<string, double>();
        
        int maxDamage = _playerDamageDealt.Values.DefaultIfEmpty(0).Max();
        int maxKills = _playerKillCount.Values.DefaultIfEmpty(0).Max();
        
        foreach (var player in _players.Values)
        {
            int damage = _playerDamageDealt.GetValueOrDefault(player.PlayerId, 0);
            int kills = _playerKillCount.GetValueOrDefault(player.PlayerId, 0);
            
            double damageScore = maxDamage > 0 ? (double)damage / maxDamage : 0;
            double killScore = maxKills > 0 ? (double)kills / maxKills : 0;
            
            mvpScores[player.PlayerId] = damageScore * 0.7 + killScore * 0.3;
        }
        
        return mvpScores.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
    }
    
    /// <summary>
    /// 生成战斗奖励
    /// </summary>
    public List<BattleReward> GenerateBattleRewards()
    {
        var rewards = new List<BattleReward>();
        
        foreach (var player in _players.Values)
        {
            bool isWinner = player.Camp == WinnerCamp;
            
            // 基础经验：胜者100，败者50
            int baseExp = isWinner ? 100 : 50;
            
            // 额外经验：根据表现加成
            int damageBonus = _playerDamageDealt.GetValueOrDefault(player.PlayerId, 0) / 10; // 每10点伤害1经验
            int killBonus = _playerKillCount.GetValueOrDefault(player.PlayerId, 0) * 20; // 每次击杀20经验
            int blockBonus = _playerDamageBlocked.GetValueOrDefault(player.PlayerId, 0) / 20; // 每20点格挡1经验
            
            // MVP额外奖励
            var stats = GenerateBattleStats();
            bool isMVP = stats.FirstOrDefault(s => s.PlayerId == player.PlayerId)?.IsMVP ?? false;
            int mvpBonus = isMVP ? 50 : 0;
            
            int totalExp = baseExp + damageBonus + killBonus + blockBonus + mvpBonus;
            
            var reward = new BattleReward
            {
                PlayerId = player.PlayerId,
                ExpGained = totalExp,
                ItemsGained = new List<string>(),
                AchievementsUnlocked = new List<string>()
            };
            
            rewards.Add(reward);
        }
        
        return rewards;
    }

    /// <summary>
    /// 处理玩家主动认输
    /// </summary>
    public bool HandleSurrender(string playerId)
    {
        if (IsBattleOver)
            return false;

        var player = GetPlayer(playerId);
        if (player == null)
            return false;

        var winner = player.Camp == PlayerCamp.Team1 ? PlayerCamp.Team2 : PlayerCamp.Team1;
        AddLog($"{player.PlayerName} 认输");
        EndBattle(winner);
        return true;
    }
    
    /// <summary>
    /// 检查是否应该触发"长考"成就
    /// </summary>
    public List<string> GetPlayersEligibleForLongThinkingAchievement()
    {
        var eligiblePlayers = new List<string>();
        
        foreach (var playerId in _players.Keys)
        {
            var opponentTime = GetOpponentTotalActionTime(playerId);
            // 10分钟 = 600秒
            if (opponentTime.TotalSeconds >= 600)
            {
                eligiblePlayers.Add(playerId);
                AddLog($"{playerId} 达成长考成就条件（对手行动时间: {opponentTime.TotalSeconds:F1}秒）");
            }
        }
        
        return eligiblePlayers;
    }
    
    /// <summary>
    /// 检查是否应该触发"秒了"成就 - 获胜者的总行动时间在5秒内
    /// </summary>
    public List<string> GetPlayersEligibleForBlitzVictoryAchievement()
    {
        var eligiblePlayers = new List<string>();
        
        if (WinnerCamp == null)
            return eligiblePlayers;
        
        var winningTeamPlayers = _players.Values
            .Where(p => p.Camp == WinnerCamp)
            .ToList();
        
        foreach (var player in winningTeamPlayers)
        {
            var playerActionTime = GetPlayerTotalActionTime(player.PlayerId);
            // 5秒内
            if (playerActionTime.TotalSeconds <= 5.0)
            {
                eligiblePlayers.Add(player.PlayerId);
                AddLog($"{player.PlayerName} 达成秒了成就条件（己方总行动时间: {playerActionTime.TotalSeconds:F2}秒）");
            }
        }
        
        return eligiblePlayers;
    }
    
    /// <summary>
    /// 检查阵营是否被全灭
    /// </summary>
    private bool IsTeamEliminated(PlayerCamp camp)
    {
        return _players.Values
            .Where(p => p.Camp == camp)
            .All(p => p.IsDead);
    }
    
    /// <summary>
    /// 获取对手列表
    /// </summary>
    private List<Player> GetOpponents(string playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
            return new List<Player>();
        
        return _players.Values
            .Where(p => p.Camp != player.Camp && !p.IsDead)
            .ToList();
    }
    
    /// <summary>
    /// 检查是否有眩晕效果
    /// </summary>
    private bool HasStunEffect(Player player)
    {
        return player.ActiveEffects.OfType<StunEffect>().Any();
    }
    
    /// <summary>
    /// 检查是否是AI玩家
    /// </summary>
    private bool IsAIPlayer(string playerId)
    {
        // 服务器端多人战斗中所有都是真实玩家
        return false;
    }
    
    /// <summary>
    /// 获取玩家信息
    /// </summary>
    public Player GetPlayer(string playerId)
    {
        _players.TryGetValue(playerId, out var player);
        return player;
    }
    
    /// <summary>
    /// 获取所有玩家
    /// </summary>
    public List<Player> GetAllPlayers()
    {
        return _players.Values.ToList();
    }
    
    /// <summary>
    /// 获取可用的AD骰子列表
    /// </summary>
    public List<Dice> GetAvailableActiveDice(string playerId)
    {
        if (!_players.TryGetValue(playerId, out var player))
            return new List<Dice>();
        
        return player.GetEquippedDice()
            .Where(d => d.UsageType == DiceUsageType.Active || d.UsageType == DiceUsageType.Both)
            .ToList();
    }
    
    /// <summary>
    /// 获取可用的PD骰子列表
    /// </summary>
    public List<Dice> GetAvailablePassiveDice(string playerId)
    {
        return _currentPassiveDiceChoices ?? new List<Dice>();
    }
    
    /// <summary>
    /// 获取新的战斗日志（自上次调用以来）
    /// </summary>
    public List<string> GetNewBattleLogs()
    {
        if (_lastSentLogIndex >= BattleLog.Count)
            return new List<string>();
        
        var newLogs = BattleLog.Skip(_lastSentLogIndex).ToList();
        _lastSentLogIndex = BattleLog.Count;
        return newLogs;
    }
    
    /// <summary>
    /// 获取可攻击的对手列表
    /// </summary>
    public List<Player> GetAvailableOpponents()
    {
        return _currentOpponentIds
            .Select(id => _players[id])
            .ToList();
    }
    
    /// <summary>
    /// 添加战斗日志
    /// </summary>
    private void AddLog(string message)
    {
        BattleLog.Add(message);
        System.Diagnostics.Debug.WriteLine($"[ServerBattle] {message}");
    }
    
    /// <summary>
    /// 获取对手总行动时间（从某个玩家的视角）
    /// </summary>
    public TimeSpan GetOpponentTotalActionTime(string playerId)
    {
        return _opponentTotalActionTime.TryGetValue(playerId, out var time) ? time : TimeSpan.Zero;
    }
    
    /// <summary>
    /// 获取玩家自己的总行动时间
    /// </summary>
    public TimeSpan GetPlayerTotalActionTime(string playerId)
    {
        return _playerTotalActionTime.TryGetValue(playerId, out var time) ? time : TimeSpan.Zero;
    }
    
    /// <summary>
    /// 获取战斗总时长
    /// </summary>
    public TimeSpan GetBattleDuration()
    {
        return DateTime.UtcNow - _battleStartTime;
    }

    /// <summary>
    /// 获取玩家在本局内的掷骰一致性（用于连胜成就统计）
    /// </summary>
    public Dictionary<string, (bool hasRolls, int? uniformValue)> GetPlayerRollUniformity()
    {
        var result = new Dictionary<string, (bool hasRolls, int? uniformValue)>();

        foreach (var kvp in _playerRollValues)
        {
            bool hasRolls = kvp.Value.Count > 0;
            int? uniformValue = kvp.Value.Count == 1 ? kvp.Value.First() : null;
            result[kvp.Key] = (hasRolls, uniformValue);
        }

        return result;
    }
    
    /// <summary>
    /// 追踪飞羽闪避连击
    /// </summary>
    private void TrackFeatheredDodgeStreak(string playerId, Dice usedDice, DefenseResult defenseResult)
    {
        // 检查是否使用的是飞羽骰子
        if (usedDice is FeatheredDice)
        {
            // 检查是否成功闪避（实际伤害为0）
            if (defenseResult.ActualDamage == 0)
            {
                // 成功闪避，增加连击
                _playerFeatheredDodgeStreak[playerId]++;
                _playerLastDefenseWasFeatheredSuccess[playerId] = true;
                
                AddLog($"[成就追踪] {playerId} 飞羽连续闪避成功 {_playerFeatheredDodgeStreak[playerId]} 次");
            }
            else
            {
                // 闪避失败，重置连击
                _playerFeatheredDodgeStreak[playerId] = 0;
                _playerLastDefenseWasFeatheredSuccess[playerId] = false;
            }
        }
        else
        {
            // 使用了其他骰子，重置连击
            _playerFeatheredDodgeStreak[playerId] = 0;
            _playerLastDefenseWasFeatheredSuccess[playerId] = false;
        }
    }

    /// <summary>
    /// 记录玩家本局内的掷骰点数
    /// </summary>
    private void TrackRoll(string playerId, int rollValue)
    {
        if (rollValue <= 0)
            return;

        if (_playerRollValues.TryGetValue(playerId, out var rolls))
        {
            rolls.Add(rollValue);
        }
    }

    /// <summary>
    /// 应用戮力同心的连号加成
    /// </summary>
    private int ApplyConcertedEffortBonus(Player attacker, int rollValue, int baseAttackPower, out bool triggered)
    {
        triggered = false;

        // 记录掷骰点数，确保后续成就统计可用
        TrackRoll(attacker.PlayerId, rollValue);

        if (rollValue <= 0)
            return baseAttackPower;

        var accessory = attacker.GetEquippedAccessories()
            .OfType<ConcertedEffortAccessory>()
            .FirstOrDefault();

        if (accessory == null)
            return baseAttackPower;

        return accessory.ApplyRollBonus(rollValue, baseAttackPower, out triggered);
    }
    
    /// <summary>
    /// 应用漫游者之心的攻击倍率加成
    /// 根据本回合最慢的一步选择时间来调整攻击力
    /// </summary>
    private int ApplyWandererHeartMultiplier(Player attacker, int baseAttackPower)
    {
        var accessory = attacker.GetEquippedAccessories()
            .OfType<WandererHeartAccessory>()
            .FirstOrDefault();

        if (accessory == null)
            return baseAttackPower;

        // 获取这个玩家在本回合最慢的一步时间
        if (!_playerRoundSlowestActionTime.TryGetValue(attacker.PlayerId, out var slowestTime))
        {
            return baseAttackPower;
        }

        // 使用漫游者之心的倍率计算方法
        double multiplier = accessory.GetAttackMultiplier(slowestTime);
        
        // 应用倍率
        int finalAttackPower = (int)Math.Round(baseAttackPower * multiplier);
        return finalAttackPower;
    }
    
    /// <summary>
    /// 检查是否应该触发"奇迹"成就 - 一局内使用飞羽骰子进行闪避连续成功5次
    /// </summary>
    public List<string> GetPlayersEligibleForMiracleAchievement()
    {
        var eligiblePlayers = new List<string>();
        
        foreach (var kvp in _playerFeatheredDodgeStreak)
        {
            if (kvp.Value >= 5)
            {
                eligiblePlayers.Add(kvp.Key);
                AddLog($"{kvp.Key} 达成奇迹成就条件（飞羽连续闪避成功 {kvp.Value} 次）");
            }
        }
        
        return eligiblePlayers;
    }
}
