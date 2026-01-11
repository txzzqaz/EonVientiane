using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane;

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
    private int _lastSentLogIndex = 0;
    
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
        
        // 为每个客户端创建玩家对象
        foreach (var client in clients)
        {
            var player = new Player(client.UserId, client.PlayerName, client.TeamId == 1 ? PlayerCamp.Team1 : PlayerCamp.Team2);
            _players[client.UserId] = player;
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
        foreach (var player in _players.Values)
        {
            _playerInitialHP[player.PlayerId] = player.MaxHP;
            _playerTookDamage[player.PlayerId] = false;
        }
        
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
    }
    
    /// <summary>
    /// 处理玩家的攻击选择
    /// </summary>
    public void ProcessPlayerAttackChoice(string playerId, string selectedDiceName, string targetPlayerId)
    {
        if (CurrentInputContext != BattleInputContext.AttackSelection || CurrentActionPlayerId != playerId)
        {
            AddLog($"非法的攻击选择：来自 {playerId}");
            return;
        }
        
        if (!_players.TryGetValue(playerId, out var attacker))
            return;
        
        if (selectedDiceName == null)
        {
            // 跳过行动
            AddLog($"{attacker.PlayerName}选择跳过行动");
            CurrentInputContext = BattleInputContext.None;
            AdvanceAfterAction();
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
        
        var actionResult = selectedDice.ExecuteActiveAction(attacker, opponents);
        if (actionResult == null || !actionResult.Success)
        {
            AddLog($"{attacker.PlayerName}的{selectedDice.Name}发动失败");
            CurrentInputContext = BattleInputContext.None;
            AdvanceAfterAction();
            return;
        }
        
        var resolvedTarget = actionResult.Target ?? target;
        ResolveAttackResult(attacker, selectedDice, resolvedTarget, actionResult.AttackPower, actionResult.Message);
        
        // 如果没有进入防守状态，直接推进
        if (CurrentInputContext == BattleInputContext.None)
        {
            AdvanceAfterAction();
        }
    }
    
    /// <summary>
    /// 处理玩家的防守选择
    /// </summary>
    public void ProcessPlayerDefenseChoice(string playerId, string selectedDiceName)
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
        
        if (selectedDiceName == null)
        {
            defenseResult = new DefenseResult(0, attackPower, 
                $"{defender.PlayerName}选择跳过防御，受到{attackPower}点伤害");
        }
        else
        {
            var selectedDice = defender.GetEquippedDice()
                .FirstOrDefault(d => d.Name == selectedDiceName);
            
            if (selectedDice == null)
            {
                AddLog($"{defender.PlayerName}选择的骰子不存在");
                return;
            }
            
            defenseResult = selectedDice.ExecutePassiveAction(defender, attackPower)
                ?? new DefenseResult(0, attackPower, $"{defender.PlayerName}防御失败，受到{attackPower}点伤害");
        }
        
        var attacker = _players[_pendingAttack.AttackerId];
        ApplyDamage(defenseResult, defender, attacker);
        
        CurrentInputContext = BattleInputContext.None;
        _pendingAttack = null;
        AdvanceAfterAction();
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
        ApplyDamage(defenseResult, target, attacker);
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
        return result ?? new DefenseResult(0, attackDamage, $"{defender.PlayerName}防御失败，受到{attackDamage}点伤害");
    }
    
    /// <summary>
    /// 应用伤害
    /// </summary>
    private void ApplyDamage(DefenseResult defenseResult, Player defender, Player attacker)
    {
        AddLog(defenseResult.Message);
        
        if (defenseResult.ActualDamage > 0)
        {
            int actualDamage = defender.TakeDamage(defenseResult.ActualDamage);
            
            if (actualDamage > 0)
            {
                _playerTookDamage[defender.PlayerId] = true;
            }
            
            AddLog($"{defender.PlayerName}受到{actualDamage}点伤害，当前HP: {defender.CurrentHP}");
            if (defender.IsDead)
            {
                AddLog($"{defender.PlayerName}已被击败！");
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
        
        AddLog($"\n=== 战斗结束 ===");
        AddLog($"{winner}阵营获胜！");
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
}
