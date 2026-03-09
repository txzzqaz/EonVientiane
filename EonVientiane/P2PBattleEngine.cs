using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// P2P战斗引擎 - 客户端独立运行完整战斗逻辑
/// 通过确定性随机种子保证双方结果一致
/// </summary>
public class P2PBattleEngine
{
    private Battle _battle;
    private DeterministicRandom _battleRandom;
    private string _localPlayerId;
    private Dictionary<string, Player> _players = new();
    private List<PlayerCamp> _campTurnOrder = new();
    private int _currentCampIndex = 0;
    private int _currentRound = 1;
    private bool _isBattleOver = false;
    private PlayerCamp? _winnerCamp = null;
    
    // 战斗状态
    private BattleState _currentState = BattleState.Idle;
    private string _currentActionPlayerId;
    private BattleInputContext _currentInputContext = BattleInputContext.None;
    private List<Player> _currentOpponents = new();
    private List<Dice> _currentActiveDiceChoices = new();
    private List<Dice> _currentPassiveDiceChoices = new();
    private PendingAttack _pendingAttack;
    
    // 战斗记录
    private List<string> _battleLog = new();
    private List<BattleActionRecord> _actionRecords = new();
    private Dictionary<string, byte[]> _playerSeeds = new();
    
    private class PendingAttack
    {
        public string AttackerId { get; init; }
        public string DefenderId { get; init; }
        public int AttackPower { get; init; }
        public Dice AttackDice { get; init; }
    }
    
    public Battle CurrentBattle => _battle;
    public bool IsBattleOver => _isBattleOver;
    public PlayerCamp? WinnerCamp => _winnerCamp;
    public string CurrentActionPlayerId => _currentActionPlayerId;
    public BattleInputContext CurrentInputContext => _currentInputContext;
    public IReadOnlyList<Player> CurrentOpponents => _currentOpponents;
    public IReadOnlyList<Dice> CurrentActiveDice => _currentActiveDiceChoices;
    public IReadOnlyList<Dice> CurrentPassiveDice => _currentPassiveDiceChoices;
    public IReadOnlyList<string> BattleLog => _battleLog;
    public int CurrentRound => _currentRound;
    
    /// <summary>
    /// 事件：需要本地玩家输入
    /// </summary>
    public event Action<BattleInputContext> LocalInputRequired;
    
    /// <summary>
    /// 事件：需要发送操作给对手
    /// </summary>
    public event Action<BattleActionRecord> ActionToSend;
    
    /// <summary>
    /// 事件：战斗状态更新
    /// </summary>
    public event Action BattleStateUpdated;
    
    public P2PBattleEngine(string localPlayerId)
    {
        _localPlayerId = localPlayerId;
    }
    
    /// <summary>
    /// 提交本地种子
    /// </summary>
    public byte[] GenerateLocalSeed()
    {
        return DeterministicRandom.GenerateRandomSeed();
    }
    
    /// <summary>
    /// 合成双方种子并初始化战斗
    /// </summary>
    public void InitializeBattle(
        List<PlayerInfo> playerInfos,
        Dictionary<string, List<Equipment>> playerEquipment,
        Dictionary<string, byte[]> allPlayerSeeds)
    {
        _playerSeeds = allPlayerSeeds;
        
        // 合成种子
        var sortedSeeds = allPlayerSeeds.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();
        byte[] combinedSeed = sortedSeeds[0];
        for (int i = 1; i < sortedSeeds.Count; i++)
        {
            combinedSeed = DeterministicRandom.CombineSeeds(combinedSeed, sortedSeeds[i]);
        }
        _battleRandom = new DeterministicRandom(combinedSeed);
        
        AddLog($"=== P2P战斗开始 ===");
        AddLog($"使用确定性随机种子: {_battleRandom.SeedHex.Substring(0, 32)}...");
        
        // 创建战斗实例
        _battle = new Battle();
        _players.Clear();
        
        // 创建玩家
        foreach (var playerInfo in playerInfos)
        {
            var player = new Player(
                playerInfo.PlayerId,
                playerInfo.PlayerName,
                playerInfo.TeamId == 1 ? PlayerCamp.Team1 : PlayerCamp.Team2
            );
            _players[playerInfo.PlayerId] = player;
            _battle.AddPlayer(player);
            
            // 装备道具
            if (playerEquipment.TryGetValue(playerInfo.PlayerId, out var equipment))
            {
                foreach (var item in equipment)
                {
                    player.AddEquipment(item);
                }
            }
        }
        
        // 应用饰品效果
        ApplyAccessoryEffects();
        
        // 记录开始操作
        RecordAction("BattleStart", _localPlayerId);
        
        // 使用确定性随机决定回合顺序
        RandomizeTurnOrder();
        
        _currentState = BattleState.RoundStart;
        _currentRound = 1;
        
        // 开始第一回合
        ProcessNextTurn();
    }
    
    /// <summary>
    /// 应用饰品效果
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
    /// 随机决定回合顺序（使用确定性随机）
    /// </summary>
    private void RandomizeTurnOrder()
    {
        _campTurnOrder = new List<PlayerCamp> { PlayerCamp.Team1, PlayerCamp.Team2 };
        
        if (_battleRandom.Next(2) == 0)
        {
            (_campTurnOrder[0], _campTurnOrder[1]) = (_campTurnOrder[1], _campTurnOrder[0]);
        }
        
        AddLog($"回合顺序: 先手 {_campTurnOrder[0]}, 后手 {_campTurnOrder[1]}");
        RecordAction("TurnOrderDecided", string.Empty, extraData: new Dictionary<string, object>
        {
            { "FirstCamp", _campTurnOrder[0].ToString() },
            { "SecondCamp", _campTurnOrder[1].ToString() }
        });
    }
    
    /// <summary>
    /// 处理下一个回合
    /// </summary>
    private void ProcessNextTurn()
    {
        // 检查胜负
        if (CheckBattleEnd())
        {
            return;
        }
        
        // 回合开始处理
        if (_currentCampIndex == 0)
        {
            AddLog($"\n=== 第{_currentRound}回合 ===");
            foreach (var player in _players.Values)
            {
                player.ResetRoundState();
            }
        }
        
        // 获取当前阵营的活着的玩家
        while (_currentCampIndex < _campTurnOrder.Count)
        {
            var currentCamp = _campTurnOrder[_currentCampIndex];
            var actingPlayers = _players.Values
                .Where(p => p.Camp == currentCamp && !p.IsDead)
                .ToList();
            
            if (actingPlayers.Count == 0)
            {
                _currentCampIndex++;
                continue;
            }
            
            // 使用确定性随机选择行动玩家
            var player = actingPlayers[_battleRandom.Next(actingPlayers.Count)];
            _currentActionPlayerId = player.PlayerId;
            
            // 获取对手
            _currentOpponents = GetOpponents(player);
            
            if (_currentOpponents.Count == 0)
            {
                AddLog($"{player.PlayerName}没有对手，跳过");
                _currentCampIndex++;
                continue;
            }
            
            // 准备可用骰子
            _currentActiveDiceChoices = player.GetEquippedDice()
                .Where(d => d.UsageType == DiceUsageType.Active || d.UsageType == DiceUsageType.Both)
                .ToList();
            
            AddLog($"\n--- {player.PlayerName}的回合 ---");
            _currentState = BattleState.PlayerAction;
            _currentInputContext = BattleInputContext.AttackSelection;
            
            RecordAction("PlayerTurnStart", player.PlayerId);
            
            // 如果是本地玩家，触发输入请求
            if (player.PlayerId == _localPlayerId)
            {
                LocalInputRequired?.Invoke(BattleInputContext.AttackSelection);
            }
            
            BattleStateUpdated?.Invoke();
            return;
        }
        
        // 本阵营回合结束
        _currentCampIndex = 0;
        _currentRound++;
        ProcessNextTurn();
    }
    
    /// <summary>
    /// 处理攻击行动
    /// </summary>
    public void ProcessAttackAction(string playerId, string selectedDiceName, string targetPlayerId, int? manualValue)
    {
        if (playerId != _currentActionPlayerId)
        {
            AddLog($"非法操作：不是 {playerId} 的回合");
            return;
        }
        
        var player = _players[playerId];
        
        // 记录操作
        RecordAction("Attack", playerId, targetPlayerId, selectedDiceName, null, manualValue);
        
        // 如果是远程玩家的操作，通过ActionToSend发送
        if (playerId != _localPlayerId)
        {
            // 远程操作已经记录，继续执行
        }
        
        // 跳过
        if (string.IsNullOrEmpty(selectedDiceName))
        {
            AddLog($"{player.PlayerName}跳过行动");
            AdvanceToNextPlayer();
            return;
        }
        
        var selectedDice = player.GetEquippedDice()
            .FirstOrDefault(d => d.Name == selectedDiceName);
        
        if (selectedDice == null)
        {
            AddLog($"骰子不存在: {selectedDiceName}");
            AdvanceToNextPlayer();
            return;
        }
        
        // 设置手动值
        if (selectedDice is IManualRollDice manualDice && manualValue.HasValue)
        {
            manualDice.SetManualRoll(manualValue.Value);
        }
        
        // 获取目标
        Player target = null;
        if (!string.IsNullOrEmpty(targetPlayerId))
        {
            target = _currentOpponents.FirstOrDefault(p => p.PlayerId == targetPlayerId);
        }
        if (target == null && _currentOpponents.Count > 0)
        {
            target = _currentOpponents[0];
        }
        
        // 执行攻击
        var actionResult = selectedDice.ExecuteActiveAction(player, _currentOpponents);
        
        if (actionResult == null || !actionResult.Success)
        {
            AddLog($"{player.PlayerName}的{selectedDice.Name}发动失败");
            AdvanceToNextPlayer();
            return;
        }
        
        if (!actionResult.TriggersDefense)
        {
            AddLog($"{player.PlayerName}使用{selectedDice.Name}: {actionResult.Message}");
            AdvanceToNextPlayer();
            return;
        }
        
        // 需要防御响应
        _pendingAttack = new PendingAttack
        {
            AttackerId = player.PlayerId,
            DefenderId = target.PlayerId,
            AttackPower = actionResult.AttackPower,
            AttackDice = selectedDice
        };
        
        AddLog($"{player.PlayerName}对{target.PlayerName}发动攻击！攻击力: {actionResult.AttackPower}");
        
        // 准备防御骰子
        _currentActionPlayerId = target.PlayerId;
        _currentPassiveDiceChoices = target.GetEquippedDice()
            .Where(d => d.UsageType == DiceUsageType.Passive || d.UsageType == DiceUsageType.Both)
            .ToList();
        
        _currentState = BattleState.DefenseResponse;
        _currentInputContext = BattleInputContext.DefenseSelection;
        
        RecordAction("DefenseRequest", target.PlayerId);
        
        // 如果防御者是本地玩家
        if (target.PlayerId == _localPlayerId)
        {
            LocalInputRequired?.Invoke(BattleInputContext.DefenseSelection);
        }
        
        BattleStateUpdated?.Invoke();
    }
    
    /// <summary>
    /// 处理防御行动
    /// </summary>
    public void ProcessDefenseAction(string playerId, string selectedDiceName, int? manualValue)
    {
        if (_pendingAttack == null || playerId != _pendingAttack.DefenderId)
        {
            AddLog($"非法防御操作");
            return;
        }
        
        var defender = _players[playerId];
        var attacker = _players[_pendingAttack.AttackerId];
        
        RecordAction("Defense", playerId, diceName: selectedDiceName, manualValue: manualValue);
        
        int defensePower = 0;
        
        if (!string.IsNullOrEmpty(selectedDiceName))
        {
            var selectedDice = defender.GetEquippedDice()
                .FirstOrDefault(d => d.Name == selectedDiceName);
            
            if (selectedDice != null)
            {
                if (selectedDice is IManualRollDice manualDice && manualValue.HasValue)
                {
                    manualDice.SetManualRoll(manualValue.Value);
                }
                
                var defenseResult = selectedDice.ExecutePassiveAction(defender, _pendingAttack.AttackPower);
                if (defenseResult != null)
                {
                    defensePower = defenseResult.DefensePower;
                    AddLog($"{defender.PlayerName}使用{selectedDice.Name}防御！防御力: {defensePower}");
                }
            }
        }
        else
        {
            AddLog($"{defender.PlayerName}选择不防御");
        }
        
        // 计算伤害
        int finalDamage = Math.Max(0, _pendingAttack.AttackPower - defensePower);
        
        if (finalDamage > 0)
        {
            ApplyDamage(defender, finalDamage);
            AddLog($"{defender.PlayerName}受到{finalDamage}点伤害！剩余HP: {defender.CurrentHP}");
        }
        else
        {
            AddLog($"{defender.PlayerName}完全格挡了攻击！");
        }
        
        _pendingAttack = null;
        AdvanceToNextPlayer();
    }
    
    /// <summary>
    /// 造成伤害
    /// </summary>
    private void ApplyDamage(Player target, int damage)
    {
        // 先消耗护盾
        while (damage > 0 && target.ShieldLayers > 0)
        {
            target.ShieldLayers--;
            damage--;
            AddLog($"{target.PlayerName}的护盾层数-1，剩余{target.ShieldLayers}层");
        }
        
        // 扣除生命值
        if (damage > 0)
        {
            target.CurrentHP -= damage;
            if (target.CurrentHP <= 0)
            {
                target.CurrentHP = 0;
                // IsDead是计算属性，会自动更新
                AddLog($"{target.PlayerName}被击败！");
            }
        }
    }
    
    /// <summary>
    /// 前进到下一个玩家
    /// </summary>
    private void AdvanceToNextPlayer()
    {
        _currentInputContext = BattleInputContext.None;
        _currentActionPlayerId = null;
        _currentCampIndex++;
        ProcessNextTurn();
    }
    
    /// <summary>
    /// 检查战斗是否结束
    /// </summary>
    private bool CheckBattleEnd()
    {
        var team1Alive = _players.Values.Any(p => p.Camp == PlayerCamp.Team1 && !p.IsDead);
        var team2Alive = _players.Values.Any(p => p.Camp == PlayerCamp.Team2 && !p.IsDead);
        
        if (!team1Alive || !team2Alive)
        {
            _isBattleOver = true;
            _winnerCamp = team1Alive ? PlayerCamp.Team1 : PlayerCamp.Team2;
            _currentState = BattleState.BattleEnd;
            
            AddLog($"\n=== 战斗结束 ===");
            AddLog($"胜利方: {_winnerCamp}");
            
            RecordAction("BattleEnd", string.Empty, extraData: new Dictionary<string, object>
            {
                { "Winner", _winnerCamp.ToString() }
            });
            
            BattleStateUpdated?.Invoke();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取对手列表
    /// </summary>
    private List<Player> GetOpponents(Player player)
    {
        return _players.Values
            .Where(p => p.Camp != player.Camp && !p.IsDead)
            .ToList();
    }
    
    /// <summary>
    /// 记录操作
    /// </summary>
    private void RecordAction(string actionType, string playerId, string targetPlayerId = "",
        string diceName = "", int? diceValue = null, int? manualValue = null,
        Dictionary<string, object> extraData = null)
    {
        var record = new BattleActionRecord
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Round = _currentRound,
            ActionType = actionType,
            PlayerId = playerId,
            TargetPlayerId = targetPlayerId,
            DiceName = diceName,
            DiceValue = diceValue,
            ManualDiceValue = manualValue,
            RandomCounter = _battleRandom?.Counter ?? 0,
            ExtraData = extraData ?? new Dictionary<string, object>()
        };
        
        _actionRecords.Add(record);
        
        // 如果是本地玩家的操作，发送给对手
        if (playerId == _localPlayerId)
        {
            ActionToSend?.Invoke(record);
        }
    }
    
    /// <summary>
    /// 添加日志
    /// </summary>
    private void AddLog(string message)
    {
        _battleLog.Add(message);
        Console.WriteLine($"[P2PBattle] {message}");
    }
    
    /// <summary>
    /// 生成战斗归档
    /// </summary>
    public BattleArchive CreateBattleArchive()
    {
        var initialStates = _players.Values.Select(p => new BattlePlayerStateDto
        {
            PlayerId = p.PlayerId,
            PlayerName = p.PlayerName,
            TeamId = p.Camp == PlayerCamp.Team1 ? 1 : 2,
            MaxHP = p.MaxHP,
            CurrentHP = p.MaxHP
        }).ToList();
        
        var finalStates = _players.Values.Select(p => new BattlePlayerStateDto
        {
            PlayerId = p.PlayerId,
            PlayerName = p.PlayerName,
            TeamId = p.Camp == PlayerCamp.Team1 ? 1 : 2,
            MaxHP = p.MaxHP,
            CurrentHP = p.CurrentHP,
            IsDead = p.IsDead
        }).ToList();
        
        return new BattleArchive
        {
            BattleId = Guid.NewGuid().ToString(),
            RoomId = "", // 由调用者填充
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            BattleSeedHex = _battleRandom?.SeedHex ?? "",
            InitialPlayerStates = initialStates,
            FinalPlayerStates = finalStates,
            ActionRecords = _actionRecords,
            WinnerCamp = _winnerCamp?.ToString() ?? ""
        };
    }
}
