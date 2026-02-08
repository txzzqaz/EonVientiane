using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// 战斗系统管理器，负责战斗逻辑、输入处理和绘制
/// </summary>
public class BattleManager
{
    private Battle _currentBattle;
    private int _battleLogScrollOffset = 0;
    private bool _isBattleLogOpen = false;  // 默认关闭战斗日志
    private Rectangle _battleLogWindowRect;
    private Rectangle _battleLogToggleRect;
    private Rectangle _surrenderButtonRect;
    
    // 临时提示相关字段
    private string _currentTip = string.Empty;
    private double _tipDurationMs = 0;  // 剩余显示时间（毫秒）
    private const double DEFAULT_TIP_DURATION = 3000;  // 3秒显示时间
    
    private List<Rectangle> _diceButtonRects = new List<Rectangle>();
    private List<Dice> _diceButtons = new List<Dice>();
    private Rectangle _skipActionButtonRect;
    private List<(Player player, Rectangle rect)> _opponentRects = new List<(Player player, Rectangle rect)>();
    private Dice _pendingSelectedDice = null;

    // "预见"饰品的双行规划系统
    private Dictionary<string, int> _plannedDiceSequenceNumbersAD = new Dictionary<string, int>();  // AD行的序号显示
    private Dictionary<string, int> _plannedDiceSequenceNumbersPD = new Dictionary<string, int>();  // PD行的序号显示

    // 手动输入骰子相关
    private bool _manualInputOpen = false;
    private bool _manualInputIsDefense = false;
    private string _manualInputDiceName = string.Empty;
    private string _manualInputTargetPlayerId = null;
    private string _manualInputText = string.Empty;
    private string _manualInputError = string.Empty;
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamePadState;
    private bool _manualInputForPlanning = false;  // 用于规划系统的手动输入
    private bool _manualInputForPlanningAD = false;  // 是否为AD行的规划输入

    private static readonly Keys[] DiceShortcutRowPrimary = new[]
    {
        Keys.Q, Keys.W, Keys.E, Keys.R, Keys.U, Keys.I, Keys.O, Keys.P
    };

    private static readonly Keys[] DiceShortcutRowSecondary = new[]
    {
        Keys.A, Keys.S, Keys.D, Keys.F, Keys.J, Keys.K, Keys.L, Keys.OemSemicolon
    };

    private static readonly string[] DiceShortcutRowPrimaryLabels =
    {
        "Q", "W", "E", "R", "U", "I", "O", "P"
    };

    private static readonly string[] DiceShortcutRowSecondaryLabels =
    {
        "A", "S", "D", "F", "J", "K", "L", ";"
    };

    private static readonly string[] GamePadSlotLabels =
    {
        "LS↑/D↑", "LS→/D→", "LS↓/D↓", "LS←/D←",
        "RS↑/Y", "RS→/B", "RS↓/A", "RS←/X"
    };

    private const float ShortcutLabelScale = 0.7f;

    private InventoryManager _inventoryManager;
    private int _menuWidth;
    private ItemIconProvider _iconProvider;

    // 快捷键配置
    private KeyBindingConfig _keyBindingConfig;
    private bool _settingsUIOpen = false;
    private Rectangle _settingsButtonRect;
    private Rectangle _closeSettingsButtonRect;
    private Rectangle _pdKeyboardButtonRect;
    private Rectangle _pdGamePadButtonRect;
    private bool _isBindingKey = false;  // 是否正在绑定快捷键
    private bool _isBindingGamePad = false;
    private string _bindingPromptMessage = string.Empty;
    
    // 多人战斗相关
    private bool _isMultiplayerBattle = false;
    private string _localPlayerId;
    private BattleStateUpdateNotification _currentBattleState;
    
    // 战斗结算相关
    private BattleEndNotification _battleEndNotification;
    private Rectangle _returnToLobbyButtonRect;
    
    // 多人战斗事件
    public event Action<string, string, int?> BattleActionRequested; // (diceName, targetPlayerId, manualValue)
    public event Action<string, int?> BattleDefenseRequested; // (diceName, manualValue)
    public event Action BattleSurrenderRequested;
    public event Action ReturnToLobbyRequested;

    public Battle CurrentBattle => _currentBattle;
    public bool IsBattleActive => _currentBattle != null && !_currentBattle.IsBattleOver;
    public bool IsMultiplayerBattle => _isMultiplayerBattle;

    public BattleManager(InventoryManager inventoryManager, int menuWidth)
    {
        _inventoryManager = inventoryManager;
        _menuWidth = menuWidth;
        _keyBindingConfig = new KeyBindingConfig();
    }

    public void SetIconProvider(ItemIconProvider iconProvider)
    {
        _iconProvider = iconProvider;
    }

    /// <summary>
    /// 初始化战斗
    /// </summary>
    // 单人模式已移除，不再提供本地初始化
    public void InitializeBattle()
    {
        _currentBattle = null;
        _battleLogScrollOffset = 0;
        _isBattleLogOpen = false;  // 默认关闭战斗日志
        _pendingSelectedDice = null;
        _diceButtonRects.Clear();
        _diceButtons.Clear();
        _opponentRects.Clear();
        _currentTip = string.Empty;
        _tipDurationMs = 0;
        ClearManualInputState();
    }

    /// <summary>
    /// 初始化多人战斗（服务器驱动模式）
    /// </summary>
    public void InitializeMultiplayerBattle(List<PlayerInfo> playerInfoList, string localPlayerId)
    {
        _currentBattle = new Battle();
        _battleLogScrollOffset = 0;
        _isBattleLogOpen = false;  // 默认关闭战斗日志
        _pendingSelectedDice = null;
        _diceButtonRects.Clear();
        _diceButtons.Clear();
        _opponentRects.Clear();
        _currentTip = string.Empty;
        _tipDurationMs = 0;
        ClearManualInputState();
        
        _isMultiplayerBattle = true;
        _localPlayerId = localPlayerId;

        // 创建本地显示用的玩家对象（仅用于显示，不运行逻辑）
        foreach (var playerInfo in playerInfoList)
        {
            PlayerCamp camp = playerInfo.TeamId == 1 ? PlayerCamp.Team1 : PlayerCamp.Team2;
            var player = new Player(playerInfo.PlayerId, playerInfo.PlayerName, camp);
            
            // 为本地玩家装备当前背包中的物品
            if (playerInfo.PlayerId == localPlayerId)
            {
                SetupPlayerEquipmentFromInventory(player);
            }
            
            _currentBattle.AddPlayer(player);
        }
        
        // 不调用 InitializeBattle()，等待服务器状态更新
    }
    /// <summary>
    /// 将当前背包中已装备的道具同步给玩家
    /// </summary>
    private void SetupPlayerEquipmentFromInventory(Player player)
    {
        player.EquippedItems.Clear();
        foreach (var equipment in _inventoryManager.EquippedItems)
        {
            // 使用克隆避免与背包共享同一实例
            player.AddEquipment((Equipment)equipment.Clone());
        }
    }

    /// <summary>
    /// 显示临时提示
    /// </summary>
    private void ShowTip(string message, double durationMs = DEFAULT_TIP_DURATION)
    {
        _currentTip = message;
        _tipDurationMs = durationMs;
    }

    private static bool ShouldSkipTipLog(string log)
    {
        if (string.IsNullOrWhiteSpace(log))
            return true;

        if (log.StartsWith("  "))
            return true;

        if (log.Contains("等待玩家选择") || log.Contains("点击跳过") || log.Contains("请防守方选择"))
            return true;

        return false;
    }

    private static string FindPreviousActionLine(IReadOnlyList<string> logs, int startIndex)
    {
        for (int i = startIndex - 1; i >= 0; i--)
        {
            var log = logs[i];
            if (ShouldSkipTipLog(log))
                continue;

            if ((log.Contains("使用") && log.Contains("发动")) || log.Contains("掷出"))
                return log;
        }

        return null;
    }

    private static string BuildTipFromLogs(IReadOnlyList<string> newLogs)
    {
        if (newLogs == null || newLogs.Count == 0)
            return null;

        for (int i = newLogs.Count - 1; i >= 0; i--)
        {
            var log = newLogs[i];
            if (ShouldSkipTipLog(log))
                continue;

            if (log.Contains("攻击点数"))
            {
                var actionLine = FindPreviousActionLine(newLogs, i);
                return string.IsNullOrEmpty(actionLine) ? log : $"{actionLine} | {log}";
            }
        }

        for (int i = newLogs.Count - 1; i >= 0; i--)
        {
            var log = newLogs[i];
            if (!ShouldSkipTipLog(log))
                return log;
        }

        return newLogs[newLogs.Count - 1];
    }

    /// <summary>
    /// 根据骰子名称列表同步玩家装备（用于多人战斗中的其他玩家）
    /// </summary>
    private void SyncPlayerDiceEquipment(Player player, List<string> equippedDiceNames)
    {
        // 只同步骰子，不同步饰品（饰品可能会改变游戏逻辑）
        player.EquippedItems.Clear();
        
        foreach (var diceName in equippedDiceNames ?? new List<string>())
        {
            var dice = CreateDiceByName(diceName);
            if (dice != null)
            {
                player.AddEquipment(dice);
            }
        }
    }

    /// <summary>
    /// 根据名称创建对应的骰子对象
    /// </summary>
    private Dice CreateDiceByName(string diceName)
    {
        return diceName switch
        {
            "D6骰子" => new D6Dice(DiceUsageType.Both),
            "飞羽" => new FeatheredDice(),
            "春风" => new SpringBreezeDice(),
            "刮痧师傅" => new GuaShaParquetDice(),
            "ERROR" => new ErrorDice(),
            // 可以根据需要添加更多骰子类型
            _ => null
        };
    }

    /// <summary>
    /// 为电脑配置默认装备：D6、飞羽、自我
    /// </summary>
    // 电脑自动操控已移除
    private void SetupComputerEquipment(Player computer) { }

    /// <summary>
    /// 设置战斗结算数据（从服务器接收）
    /// </summary>
    public void SetBattleEndNotification(BattleEndNotification notification)
    {
        _battleEndNotification = notification;
    }

    /// <summary>
    /// 更新临时提示的显示时间
    /// </summary>
    public void UpdateTip(GameTime gameTime)
    {
        if (_tipDurationMs > 0)
        {
            _tipDurationMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_tipDurationMs <= 0)
            {
                _currentTip = string.Empty;
                _tipDurationMs = 0;
            }
        }
    }

    /// <summary>
    /// 更新战斗逻辑
    /// </summary>
    public void Update()
    {
        // 客户端不运行本地战斗逻辑（多人模式由服务器驱动）
        return;
    }

    /// <summary>
    /// 处理战斗输入
    /// </summary>
    public void HandleInput(MouseState mouseState, MouseState previousMouseState, int panelWidth, int panelHeight)
    {
        int panelX = _menuWidth;
        var keyboardState = Keyboard.GetState();
        var gamePadState = GamePad.GetState(PlayerIndex.One);

        // 战斗已结束，处理结算界面的输入
        if (_currentBattle != null && _currentBattle.IsBattleOver && _battleEndNotification != null)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                Point mp = new Point(mouseState.X, mouseState.Y);
                if (_returnToLobbyButtonRect.Contains(mp))
                {
                    ReturnToLobbyRequested?.Invoke();
                    _battleEndNotification = null;
                    return;
                }
            }
            _previousKeyboardState = keyboardState;
            _previousGamePadState = gamePadState;
            return;
        }

        if (_manualInputOpen)
        {
            HandleManualInput(keyboardState, mouseState, previousMouseState, panelX, panelWidth, panelHeight);
            _previousKeyboardState = keyboardState;
            _previousGamePadState = gamePadState;
            return;
        }

        _battleLogToggleRect = new Rectangle(panelX + panelWidth - 90, 10, 80, 30);
        _surrenderButtonRect = new Rectangle(panelX + panelWidth - 180, 10, 80, 30);
        _settingsButtonRect = new Rectangle(panelX + 10, panelHeight - 45, 80, 35);

        // 处理设置界面
        if (_settingsUIOpen)
        {
            HandleSettingsUIInput(mouseState, previousMouseState, keyboardState);
            _previousKeyboardState = keyboardState;
            _previousGamePadState = gamePadState;
            return;
        }

        // 设置按钮点击
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mp = new Point(mouseState.X, mouseState.Y);
            if (_settingsButtonRect.Contains(mp))
            {
                _settingsUIOpen = true;
                _previousKeyboardState = keyboardState;
                _previousGamePadState = gamePadState;
                return;
            }
        }

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mp = new Point(mouseState.X, mouseState.Y);
            if (_surrenderButtonRect.Contains(mp))
            {
                if (_currentBattle != null && !_currentBattle.IsBattleOver)
                {
                    BattleSurrenderRequested?.Invoke();
                }
                return;
            }
            if (_battleLogToggleRect.Contains(mp))
            {
                _isBattleLogOpen = !_isBattleLogOpen;
                return;
            }
        }

        if (_isBattleLogOpen)
        {
            _battleLogWindowRect = new Rectangle(panelX + panelWidth - 420, 60, 400, 250);
            if (mouseState.X >= _battleLogWindowRect.X && mouseState.X <= _battleLogWindowRect.Right &&
                mouseState.Y >= _battleLogWindowRect.Y && mouseState.Y <= _battleLogWindowRect.Bottom)
            {
                int scrollDelta = mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;
                if (scrollDelta != 0 && _currentBattle != null)
                {
                    int lineHeight = 20;
                    int linesPerPage = Math.Max(1, (_battleLogWindowRect.Height - 20) / lineHeight);
                    int totalLines = _currentBattle.BattleLog.Count;
                    int maxScroll = Math.Max(0, totalLines - linesPerPage);
                    _battleLogScrollOffset -= scrollDelta / 120;
                    _battleLogScrollOffset = Math.Clamp(_battleLogScrollOffset, 0, maxScroll);
                }
            }
        }

        // 如果装备"预见"，处理规划输入
        if (_currentBattle != null && HasForesightAccessory())
        {
            HandleForesightPlanningInput(mouseState, previousMouseState, panelX, panelWidth, panelHeight);
            HandleForesightPlanningShortcuts(keyboardState, gamePadState);
        }

        if (_currentBattle != null && _currentBattle.IsWaitingForPlayerInput)
        {
            if (!HasForesightAccessory())
            {
                if (HandleBattleActionShortcuts(keyboardState, gamePadState))
                {
                    _previousKeyboardState = keyboardState;
                    _previousGamePadState = gamePadState;
                    return;
                }
            }
            HandleBattleActionInput(mouseState, previousMouseState, panelX, panelWidth, panelHeight);
        }

        _previousKeyboardState = keyboardState;
        _previousGamePadState = gamePadState;
    }

    private void HandleBattleActionInput(MouseState mouseState, MouseState previousMouseState, int panelX, int panelWidth, int panelHeight)
    {
        int diceAreaY = panelHeight - 120;
        int btnW = 110;
        int btnH = 40;
        int spacing = 10;
        int startX = panelX + 20;

        _diceButtonRects.Clear();
        _diceButtons.Clear();

        var displayDice = GetDisplayDiceForCurrentContext();
        var availableNames = BuildAvailableDiceNameSet(_currentBattle.InputContext);

        if (_currentBattle.InputContext == BattleInputContext.AttackSelection)
        {
            for (int i = 0; i < displayDice.Count; i++)
            {
                var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                _diceButtonRects.Add(rect);
                _diceButtons.Add(displayDice[i]);
            }
            _skipActionButtonRect = new Rectangle(panelX + panelWidth - 120, diceAreaY, 100, btnH);

            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                Point mp = new Point(mouseState.X, mouseState.Y);
                if (_skipActionButtonRect.Contains(mp))
                {
                    _pendingSelectedDice = null;
                    // 仅多人战斗：发送到服务器
                    BattleActionRequested?.Invoke(null, null, null);
                    return;
                }

                for (int i = 0; i < _diceButtonRects.Count; i++)
                {
                    if (_diceButtonRects[i].Contains(mp))
                    {
                        var dice = _diceButtons[i];
                        if (!IsDiceEnabledForContext(dice, availableNames, BattleInputContext.AttackSelection))
                            return;
                        var opponents = _currentBattle.AvailableOpponents;
                        if (opponents.Count <= 1)
                        {
                            var targetId = opponents.FirstOrDefault()?.PlayerId;
                            if (DiceRequiresManualInput(dice))
                            {
                                StartManualInput(dice.Name, targetId, false);
                            }
                            else
                            {
                                // 仅多人战斗：发送到服务器
                                BattleActionRequested?.Invoke(dice.Name, targetId, null);
                            }
                        }
                        else
                        {
                            _pendingSelectedDice = dice;
                        }
                        return;
                    }
                }
            }

            if (_pendingSelectedDice != null)
            {
                HandleOpponentSelection(mouseState, previousMouseState, panelX, panelWidth);
            }
        }
        else if (_currentBattle.InputContext == BattleInputContext.DefenseSelection)
        {
            for (int i = 0; i < displayDice.Count; i++)
            {
                var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                _diceButtonRects.Add(rect);
                _diceButtons.Add(displayDice[i]);
            }
            _skipActionButtonRect = new Rectangle(panelX + panelWidth - 120, diceAreaY, 100, btnH);

            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                Point mp = new Point(mouseState.X, mouseState.Y);
                if (_skipActionButtonRect.Contains(mp))
                {
                    // 仅多人战斗：发送到服务器
                    BattleDefenseRequested?.Invoke(null, null);
                    return;
                }
                for (int i = 0; i < _diceButtonRects.Count; i++)
                {
                    if (_diceButtonRects[i].Contains(mp))
                    {
                        var dice = _diceButtons[i];
                        if (!IsDiceEnabledForContext(dice, availableNames, BattleInputContext.DefenseSelection))
                            return;
                        if (DiceRequiresManualInput(dice))
                        {
                            StartManualInput(dice.Name, null, true);
                        }
                        else
                        {
                            // 仅多人战斗：发送到服务器
                            BattleDefenseRequested?.Invoke(dice.Name, null);
                        }
                        return;
                    }
                }
            }
        }
    }

    private bool HandleBattleActionShortcuts(KeyboardState keyboardState, GamePadState gamePadState)
    {
        if (_currentBattle == null || !_currentBattle.IsWaitingForPlayerInput)
            return false;

        var context = _currentBattle.InputContext;
        if (context != BattleInputContext.AttackSelection && context != BattleInputContext.DefenseSelection)
            return false;

        if (IsSkipShortcutPressed(keyboardState, _previousKeyboardState, gamePadState, _previousGamePadState))
        {
            if (context == BattleInputContext.AttackSelection)
            {
                _pendingSelectedDice = null;
                BattleActionRequested?.Invoke(null, null, null);
            }
            else
            {
                BattleDefenseRequested?.Invoke(null, null);
            }
            return true;
        }

        int? slotIndex = GetNewSlotIndexFromKeyboard(keyboardState, _previousKeyboardState, DiceShortcutRowPrimary);
        if (slotIndex == null)
        {
            slotIndex = GetNewSlotIndexFromGamePad(gamePadState, _previousGamePadState);
        }

        if (slotIndex == null)
            return false;

        var displayDice = GetDisplayDiceForCurrentContext();
        var availableNames = BuildAvailableDiceNameSet(context);
        if (slotIndex.Value < 0 || slotIndex.Value >= displayDice.Count)
            return false;

        var dice = displayDice[slotIndex.Value];
        return TryHandleDiceSelection(dice, context, availableNames);
    }

    private void HandleForesightPlanningShortcuts(KeyboardState keyboardState, GamePadState gamePadState)
    {
        if (_currentBattle == null || !HasForesightAccessory())
            return;

        if (IsUndoShortcutPressed(keyboardState, gamePadState))
        {
            if (TryUndoLastPlannedAction())
            {
                ShowTip("已撤回上一个规划");
            }
            else
            {
                ShowTip("没有可撤回的规划");
            }
            return;
        }

        if (IsSkipShortcutPressed(keyboardState, _previousKeyboardState, gamePadState, _previousGamePadState))
        {
            ShowTip("跳过AD规划");
            return;
        }

        // 检查PD跳过快捷键
        if (IsPDSkipShortcutPressed(keyboardState, _previousKeyboardState, gamePadState, _previousGamePadState))
        {
            ShowTip("跳过PD规划");
            return;
        }

        int? adSlot = GetNewSlotIndexFromKeyboard(keyboardState, _previousKeyboardState, DiceShortcutRowPrimary);
        int? pdSlot = GetNewSlotIndexFromKeyboard(keyboardState, _previousKeyboardState, DiceShortcutRowSecondary);

        if (adSlot == null)
        {
            adSlot = GetNewSlotIndexFromGamePadLeftGroup(gamePadState, _previousGamePadState);
        }

        if (pdSlot == null)
        {
            pdSlot = GetNewSlotIndexFromGamePadRightGroup(gamePadState, _previousGamePadState);
        }

        if (adSlot.HasValue)
        {
            TryPlanDiceByIndex(adSlot.Value, true);
            return;
        }

        if (pdSlot.HasValue)
        {
            TryPlanDiceByIndex(pdSlot.Value, false);
        }
    }

    private bool TryHandleDiceSelection(Dice dice, BattleInputContext context, HashSet<string> availableNames)
    {
        if (!IsDiceEnabledForContext(dice, availableNames, context))
            return false;

        if (context == BattleInputContext.AttackSelection)
        {
            var opponents = _currentBattle.AvailableOpponents;
            if (opponents.Count <= 1)
            {
                var targetId = opponents.FirstOrDefault()?.PlayerId;
                if (DiceRequiresManualInput(dice))
                {
                    StartManualInput(dice.Name, targetId, false);
                }
                else
                {
                    BattleActionRequested?.Invoke(dice.Name, targetId, null);
                }
            }
            else
            {
                _pendingSelectedDice = dice;
            }
            return true;
        }

        if (context == BattleInputContext.DefenseSelection)
        {
            if (DiceRequiresManualInput(dice))
            {
                StartManualInput(dice.Name, null, true);
            }
            else
            {
                BattleDefenseRequested?.Invoke(dice.Name, null);
            }
            return true;
        }

        return false;
    }

    private void TryPlanDiceByIndex(int slotIndex, bool isAD)
    {
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null)
            return;

        var displayDice = localPlayer.GetEquippedDice();
        if (slotIndex < 0 || slotIndex >= displayDice.Count)
            return;

        var dice = displayDice[slotIndex];

        if (DiceRequiresManualInput(dice))
        {
            _manualInputOpen = true;
            _manualInputForPlanning = true;
            _manualInputForPlanningAD = isAD;
            _manualInputDiceName = dice.Name;
            _manualInputIsDefense = !isAD;
            _manualInputTargetPlayerId = null;
            _manualInputText = string.Empty;
            return;
        }

        if (isAD)
        {
            var opponents = _currentBattle.AvailableOpponents;
            if (opponents.Count <= 1)
            {
                var targetId = opponents.FirstOrDefault()?.PlayerId;
                AddPlannedAction(dice.Name, true, targetId, 0);
                ShowTip($"已规划AD: {dice.Name}");
            }
            else
            {
                ShowTip("需要选择目标进行AD规划");
            }
        }
        else
        {
            AddPlannedAction(dice.Name, false, null, 0);
            ShowTip($"已规划PD: {dice.Name}");
        }
    }

    private bool TryUndoLastPlannedAction()
    {
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null)
            return false;

        PlannedAction latestAction = null;
        PlannedActionSequence latestSequence = null;
        bool latestIsAd = true;
        int latestIndex = -1;

        foreach (var kvp in localPlayer.PlannedActionsAD)
        {
            var actions = kvp.Value.Actions;
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                var action = actions[i];
                if (latestAction == null || action.CreatedTick > latestAction.CreatedTick)
                {
                    latestAction = action;
                    latestSequence = kvp.Value;
                    latestIsAd = true;
                    latestIndex = i;
                }
            }
        }

        foreach (var kvp in localPlayer.PlannedActionsPD)
        {
            var actions = kvp.Value.Actions;
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                var action = actions[i];
                if (latestAction == null || action.CreatedTick > latestAction.CreatedTick)
                {
                    latestAction = action;
                    latestSequence = kvp.Value;
                    latestIsAd = false;
                    latestIndex = i;
                }
            }
        }

        if (latestAction == null || latestSequence == null || latestIndex < 0)
            return false;

        latestSequence.Actions.RemoveAt(latestIndex);

        if (latestSequence.Actions.Count == 0)
        {
            if (latestIsAd)
            {
                localPlayer.PlannedActionsAD.Remove(latestSequence.DiceName);
            }
            else
            {
                localPlayer.PlannedActionsPD.Remove(latestSequence.DiceName);
            }
        }

        UpdatePlannedActionSequenceNumbers();
        return true;
    }

    private static bool IsSkipShortcutPressed(KeyboardState currentKeyboard, KeyboardState previousKeyboard, GamePadState currentGamePad, GamePadState previousGamePad)
    {
        bool keyboardSkip = currentKeyboard.IsKeyDown(Keys.Space) && previousKeyboard.IsKeyUp(Keys.Space);
        bool gamepadSkip = currentGamePad.Triggers.Right > 0.5f && previousGamePad.Triggers.Right <= 0.5f;
        return keyboardSkip || gamepadSkip;
    }

    private bool IsPDSkipShortcutPressed(KeyboardState currentKeyboard, KeyboardState previousKeyboard, GamePadState currentGamePad, GamePadState previousGamePad)
    {
        // PD跳过键检查（如果已配置）
        if (_keyBindingConfig.PDSkipKeyboard.HasValue)
        {
            if (currentKeyboard.IsKeyDown(_keyBindingConfig.PDSkipKeyboard.Value) && previousKeyboard.IsKeyUp(_keyBindingConfig.PDSkipKeyboard.Value))
                return true;
        }

        if (_keyBindingConfig.PDSkipGamePad.HasValue)
        {
            if (IsGamePadButtonPressed(currentGamePad, previousGamePad, _keyBindingConfig.PDSkipGamePad.Value))
                return true;
        }

        return false;
    }

    private bool IsGamePadButtonPressed(GamePadState current, GamePadState previous, KeyBindingConfig.GamePadButton button)
    {
        return button switch
        {
            KeyBindingConfig.GamePadButton.A => IsNewButtonPressed(current, previous, Buttons.A),
            KeyBindingConfig.GamePadButton.B => IsNewButtonPressed(current, previous, Buttons.B),
            KeyBindingConfig.GamePadButton.X => IsNewButtonPressed(current, previous, Buttons.X),
            KeyBindingConfig.GamePadButton.Y => IsNewButtonPressed(current, previous, Buttons.Y),
            KeyBindingConfig.GamePadButton.LB => IsNewButtonPressed(current, previous, Buttons.LeftShoulder),
            KeyBindingConfig.GamePadButton.RB => IsNewButtonPressed(current, previous, Buttons.RightShoulder),
            KeyBindingConfig.GamePadButton.LT => current.Triggers.Left > 0.5f && previous.Triggers.Left <= 0.5f,
            KeyBindingConfig.GamePadButton.RT => current.Triggers.Right > 0.5f && previous.Triggers.Right <= 0.5f,
            KeyBindingConfig.GamePadButton.DPadUp => IsNewButtonPressed(current, previous, Buttons.DPadUp),
            KeyBindingConfig.GamePadButton.DPadDown => IsNewButtonPressed(current, previous, Buttons.DPadDown),
            KeyBindingConfig.GamePadButton.DPadLeft => IsNewButtonPressed(current, previous, Buttons.DPadLeft),
            KeyBindingConfig.GamePadButton.DPadRight => IsNewButtonPressed(current, previous, Buttons.DPadRight),
            KeyBindingConfig.GamePadButton.LeftStickUp => IsNewDirectionPressed(current, previous, StickDirection.Up, true),
            KeyBindingConfig.GamePadButton.LeftStickDown => IsNewDirectionPressed(current, previous, StickDirection.Down, true),
            KeyBindingConfig.GamePadButton.LeftStickLeft => IsNewDirectionPressed(current, previous, StickDirection.Left, true),
            KeyBindingConfig.GamePadButton.LeftStickRight => IsNewDirectionPressed(current, previous, StickDirection.Right, true),
            KeyBindingConfig.GamePadButton.RightStickUp => IsNewDirectionPressed(current, previous, StickDirection.Up, false),
            KeyBindingConfig.GamePadButton.RightStickDown => IsNewDirectionPressed(current, previous, StickDirection.Down, false),
            KeyBindingConfig.GamePadButton.RightStickLeft => IsNewDirectionPressed(current, previous, StickDirection.Left, false),
            KeyBindingConfig.GamePadButton.RightStickRight => IsNewDirectionPressed(current, previous, StickDirection.Right, false),
            _ => false
        };
    }

    private bool IsUndoShortcutPressed(KeyboardState keyboardState, GamePadState gamePadState)
    {
        bool keyboardUndo = keyboardState.IsKeyDown(Keys.Back) && _previousKeyboardState.IsKeyUp(Keys.Back);
        bool gamepadUndo = gamePadState.Triggers.Left > 0.5f && _previousGamePadState.Triggers.Left <= 0.5f;
        return keyboardUndo || gamepadUndo;
    }

    private void HandleSettingsUIInput(MouseState mouseState, MouseState previousMouseState, KeyboardState keyboardState)
    {
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mp = new Point(mouseState.X, mouseState.Y);

            // 关闭按钮
            if (_closeSettingsButtonRect.Contains(mp))
            {
                _settingsUIOpen = false;
                _isBindingKey = false;
                _isBindingGamePad = false;
                return;
            }

            // PD键盘快捷键按钮
            if (_pdKeyboardButtonRect.Contains(mp))
            {
                _isBindingKey = true;
                _isBindingGamePad = false;
                _bindingPromptMessage = "按下要绑定的键盘按键...";
                return;
            }

            // PD手柄快捷键按钮
            if (_pdGamePadButtonRect.Contains(mp))
            {
                _isBindingKey = false;
                _isBindingGamePad = true;
                _bindingPromptMessage = "按下要绑定的手柄按键...";
                return;
            }
        }

        // 处理快捷键绑定
        if (_isBindingKey)
        {
            foreach (var key in keyboardState.GetPressedKeys())
            {
                if (_previousKeyboardState.IsKeyUp(key) && key != Keys.Escape)
                {
                    _keyBindingConfig.PDSkipKeyboard = key;
                    _isBindingKey = false;
                    _bindingPromptMessage = string.Empty;
                    return;
                }
            }

            if (keyboardState.IsKeyDown(Keys.Escape) && _previousKeyboardState.IsKeyUp(Keys.Escape))
            {
                _keyBindingConfig.PDSkipKeyboard = null;
                _isBindingKey = false;
                _bindingPromptMessage = string.Empty;
                return;
            }
        }

        if (_isBindingGamePad)
        {
            var gamePadState = GamePad.GetState(PlayerIndex.One);
            KeyBindingConfig.GamePadButton? boundButton = TryGetGamePadButtonPress(gamePadState, _previousGamePadState);

            if (boundButton.HasValue)
            {
                if (boundButton.Value == KeyBindingConfig.GamePadButton.None)
                {
                    _keyBindingConfig.PDSkipGamePad = null;
                }
                else
                {
                    _keyBindingConfig.PDSkipGamePad = boundButton.Value;
                }
                _isBindingGamePad = false;
                _bindingPromptMessage = string.Empty;
                return;
            }
        }

        // ESC关闭设置界面
        if (keyboardState.IsKeyDown(Keys.Escape) && _previousKeyboardState.IsKeyUp(Keys.Escape))
        {
            _settingsUIOpen = false;
            _isBindingKey = false;
            _isBindingGamePad = false;
            return;
        }
    }

    private KeyBindingConfig.GamePadButton? TryGetGamePadButtonPress(GamePadState current, GamePadState previous)
    {
        if (IsNewButtonPressed(current, previous, Buttons.A))
            return KeyBindingConfig.GamePadButton.A;
        if (IsNewButtonPressed(current, previous, Buttons.B))
            return KeyBindingConfig.GamePadButton.B;
        if (IsNewButtonPressed(current, previous, Buttons.X))
            return KeyBindingConfig.GamePadButton.X;
        if (IsNewButtonPressed(current, previous, Buttons.Y))
            return KeyBindingConfig.GamePadButton.Y;
        if (IsNewButtonPressed(current, previous, Buttons.LeftShoulder))
            return KeyBindingConfig.GamePadButton.LB;
        if (IsNewButtonPressed(current, previous, Buttons.RightShoulder))
            return KeyBindingConfig.GamePadButton.RB;
        if (current.Triggers.Left > 0.5f && previous.Triggers.Left <= 0.5f)
            return KeyBindingConfig.GamePadButton.LT;
        if (current.Triggers.Right > 0.5f && previous.Triggers.Right <= 0.5f)
            return KeyBindingConfig.GamePadButton.RT;
        if (IsNewButtonPressed(current, previous, Buttons.DPadUp))
            return KeyBindingConfig.GamePadButton.DPadUp;
        if (IsNewButtonPressed(current, previous, Buttons.DPadDown))
            return KeyBindingConfig.GamePadButton.DPadDown;
        if (IsNewButtonPressed(current, previous, Buttons.DPadLeft))
            return KeyBindingConfig.GamePadButton.DPadLeft;
        if (IsNewButtonPressed(current, previous, Buttons.DPadRight))
            return KeyBindingConfig.GamePadButton.DPadRight;

        if (IsNewDirectionPressed(current, previous, StickDirection.Up, true))
            return KeyBindingConfig.GamePadButton.LeftStickUp;
        if (IsNewDirectionPressed(current, previous, StickDirection.Down, true))
            return KeyBindingConfig.GamePadButton.LeftStickDown;
        if (IsNewDirectionPressed(current, previous, StickDirection.Left, true))
            return KeyBindingConfig.GamePadButton.LeftStickLeft;
        if (IsNewDirectionPressed(current, previous, StickDirection.Right, true))
            return KeyBindingConfig.GamePadButton.LeftStickRight;

        if (IsNewDirectionPressed(current, previous, StickDirection.Up, false))
            return KeyBindingConfig.GamePadButton.RightStickUp;
        if (IsNewDirectionPressed(current, previous, StickDirection.Down, false))
            return KeyBindingConfig.GamePadButton.RightStickDown;
        if (IsNewDirectionPressed(current, previous, StickDirection.Left, false))
            return KeyBindingConfig.GamePadButton.RightStickLeft;
        if (IsNewDirectionPressed(current, previous, StickDirection.Right, false))
            return KeyBindingConfig.GamePadButton.RightStickRight;

        return null;
    }

    private static int? GetNewSlotIndexFromKeyboard(KeyboardState current, KeyboardState previous, Keys[] mapping)
    {
        for (int i = 0; i < mapping.Length; i++)
        {
            if (current.IsKeyDown(mapping[i]) && previous.IsKeyUp(mapping[i]))
            {
                return i;
            }
        }
        return null;
    }

    private static int? GetNewSlotIndexFromGamePad(GamePadState current, GamePadState previous)
    {
        var leftIndex = GetNewSlotIndexFromGamePadLeftGroup(current, previous);
        if (leftIndex.HasValue)
            return leftIndex.Value;

        var rightIndex = GetNewSlotIndexFromGamePadRightGroup(current, previous);
        return rightIndex;
    }

    private static int? GetNewSlotIndexFromGamePadLeftGroup(GamePadState current, GamePadState previous)
    {
        if (IsNewDirectionPressed(current, previous, StickDirection.Up, true) || IsNewButtonPressed(current, previous, Buttons.DPadUp))
            return 0;
        if (IsNewDirectionPressed(current, previous, StickDirection.Right, true) || IsNewButtonPressed(current, previous, Buttons.DPadRight))
            return 1;
        if (IsNewDirectionPressed(current, previous, StickDirection.Down, true) || IsNewButtonPressed(current, previous, Buttons.DPadDown))
            return 2;
        if (IsNewDirectionPressed(current, previous, StickDirection.Left, true) || IsNewButtonPressed(current, previous, Buttons.DPadLeft))
            return 3;
        return null;
    }

    private static int? GetNewSlotIndexFromGamePadRightGroup(GamePadState current, GamePadState previous)
    {
        if (IsNewDirectionPressed(current, previous, StickDirection.Up, false) || IsNewButtonPressed(current, previous, Buttons.Y))
            return 4;
        if (IsNewDirectionPressed(current, previous, StickDirection.Right, false) || IsNewButtonPressed(current, previous, Buttons.B))
            return 5;
        if (IsNewDirectionPressed(current, previous, StickDirection.Down, false) || IsNewButtonPressed(current, previous, Buttons.A))
            return 6;
        if (IsNewDirectionPressed(current, previous, StickDirection.Left, false) || IsNewButtonPressed(current, previous, Buttons.X))
            return 7;
        return null;
    }

    private enum StickDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    private static bool IsNewDirectionPressed(GamePadState current, GamePadState previous, StickDirection direction, bool useLeftStick)
    {
        Vector2 currentStick = useLeftStick ? current.ThumbSticks.Left : current.ThumbSticks.Right;
        Vector2 previousStick = useLeftStick ? previous.ThumbSticks.Left : previous.ThumbSticks.Right;
        const float threshold = 0.5f;

        bool currentPressed = direction switch
        {
            StickDirection.Up => currentStick.Y > threshold,
            StickDirection.Right => currentStick.X > threshold,
            StickDirection.Down => currentStick.Y < -threshold,
            StickDirection.Left => currentStick.X < -threshold,
            _ => false
        };

        bool previousPressed = direction switch
        {
            StickDirection.Up => previousStick.Y > threshold,
            StickDirection.Right => previousStick.X > threshold,
            StickDirection.Down => previousStick.Y < -threshold,
            StickDirection.Left => previousStick.X < -threshold,
            _ => false
        };

        return currentPressed && !previousPressed;
    }

    private static bool IsNewButtonPressed(GamePadState current, GamePadState previous, Buttons button)
    {
        return current.IsButtonDown(button) && previous.IsButtonUp(button);
    }

    private Player GetLocalPlayer()
    {
        return _currentBattle?.AllPlayers.FirstOrDefault(p => p.PlayerId == _localPlayerId);
    }

    private List<Dice> GetDisplayDiceForCurrentContext()
    {
        if (_currentBattle == null)
            return new List<Dice>();

        var context = _currentBattle.InputContext;
        Player owner = null;

        if (context == BattleInputContext.AttackSelection)
        {
            owner = _currentBattle.CurrentActionPlayer ?? GetLocalPlayer();
        }
        else if (context == BattleInputContext.DefenseSelection)
        {
            owner = GetLocalPlayer() ?? _currentBattle.CurrentActionPlayer;
        }

        return owner?.GetEquippedDice() ?? new List<Dice>();
    }

    private HashSet<string> BuildAvailableDiceNameSet(BattleInputContext context)
    {
        if (_currentBattle == null)
            return null;

        IReadOnlyList<Dice> options = context switch
        {
            BattleInputContext.AttackSelection => _currentBattle.AvailableActiveDice,
            BattleInputContext.DefenseSelection => _currentBattle.AvailablePassiveDice,
            _ => null
        };

        return options == null ? null : new HashSet<string>(options.Select(d => d.Name));
    }

    private bool IsDiceUsageCompatible(Dice dice, BattleInputContext context)
    {
        return context switch
        {
            BattleInputContext.AttackSelection => dice.UsageType == DiceUsageType.Active || dice.UsageType == DiceUsageType.Both,
            BattleInputContext.DefenseSelection => dice.UsageType == DiceUsageType.Passive || dice.UsageType == DiceUsageType.Both,
            _ => false
        };
    }

    private bool IsDiceEnabledForContext(Dice dice, HashSet<string> availableNames, BattleInputContext context)
    {
        if (!IsDiceUsageCompatible(dice, context))
            return false;

        if (availableNames == null)
            return true;

        if (availableNames.Count == 0)
            return false;

        return availableNames.Contains(dice.Name);
    }

    private void HandleOpponentSelection(MouseState mouseState, MouseState previousMouseState, int panelX, int panelWidth)
    {
        _opponentRects.Clear();
        var opponents = _currentBattle.AvailableOpponents;
        int barW = 300;
        int barH = 20;
        int topY = 60;
        int barSpacing = 35;

        // 添加所有Team1的可攻击对手到碰撞检测列表
        var team1Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team1).ToList();
        for (int i = 0; i < team1Opponents.Count; i++)
        {
            int verticalOffset = i * barSpacing;
            var rect = new Rectangle(panelX + 20 - 10, topY - 10 + verticalOffset, barW + 20, barH + 40);
            _opponentRects.Add((team1Opponents[i], rect));
        }

        // 添加所有Team2的可攻击对手到碰撞检测列表
        var team2Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team2).ToList();
        for (int i = 0; i < team2Opponents.Count; i++)
        {
            int verticalOffset = i * barSpacing;
            var rect = new Rectangle(panelX + panelWidth - 20 - barW - 10, topY - 10 + verticalOffset, barW + 20, barH + 40);
            _opponentRects.Add((team2Opponents[i], rect));
        }

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mp = new Point(mouseState.X, mouseState.Y);
            foreach (var (player, rect) in _opponentRects)
            {
                if (rect.Contains(mp))
                {
                    if (_pendingSelectedDice != null && DiceRequiresManualInput(_pendingSelectedDice))
                    {
                        StartManualInput(_pendingSelectedDice.Name, player.PlayerId, false);
                    }
                    else
                    {
                        // 仅多人战斗：发送到服务器
                        BattleActionRequested?.Invoke(_pendingSelectedDice?.Name, player.PlayerId, null);
                    }
                    _pendingSelectedDice = null;
                    return;
                }
            }
        }
    }

    private bool DiceRequiresManualInput(Dice dice) => dice is IManualRollDice manualDice && manualDice.RequiresManualInput;

    private void StartManualInput(string diceName, string targetPlayerId, bool isDefense)
    {
        _manualInputOpen = true;
        _manualInputDiceName = diceName;
        _manualInputTargetPlayerId = targetPlayerId;
        _manualInputIsDefense = isDefense;
        _manualInputText = string.Empty;
        _manualInputError = string.Empty;
        _pendingSelectedDice = null;
    }

    private (Rectangle dialogRect, Rectangle inputRect, Rectangle confirmRect, Rectangle cancelRect) GetManualInputLayout(int panelX, int panelWidth, int panelHeight)
    {
        int dialogWidth = 360;
        int dialogHeight = 200;
        int dialogX = panelX + (panelWidth - dialogWidth) / 2;
        int dialogY = panelHeight / 2 - dialogHeight / 2;

        var dialogRect = new Rectangle(dialogX, dialogY, dialogWidth, dialogHeight);
        var inputRect = new Rectangle(dialogX + 20, dialogY + 70, dialogWidth - 40, 38);
        var confirmRect = new Rectangle(dialogX + 40, dialogY + dialogHeight - 60, 110, 36);
        var cancelRect = new Rectangle(dialogX + dialogWidth - 40 - 110, dialogY + dialogHeight - 60, 110, 36);

        return (dialogRect, inputRect, confirmRect, cancelRect);
    }

    private void HandleManualInput(KeyboardState keyboardState, MouseState mouseState, MouseState previousMouseState, int panelX, int panelWidth, int panelHeight)
    {
        var layout = GetManualInputLayout(panelX, panelWidth, panelHeight);

        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (_previousKeyboardState.IsKeyUp(key))
            {
                if (key == Keys.Enter)
                {
                    ConfirmManualInput();
                }
                else if (key == Keys.Escape)
                {
                    CancelManualInput();
                }
                else if (key == Keys.Back)
                {
                    if (_manualInputText.Length > 0)
                        _manualInputText = _manualInputText[..^1];
                }
                else
                {
                    char? c = InputManager.GetCharFromKey(key, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
                    if (c.HasValue && char.IsDigit(c.Value) && _manualInputText.Length < 18)
                    {
                        _manualInputText += c.Value;
                    }
                }
            }
        }

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mp = new Point(mouseState.X, mouseState.Y);
            if (layout.confirmRect.Contains(mp))
            {
                ConfirmManualInput();
            }
            else if (layout.cancelRect.Contains(mp))
            {
                CancelManualInput();
            }
            else if (layout.inputRect.Contains(mp))
            {
                _manualInputError = string.Empty;
            }
        }
    }

    private void ConfirmManualInput()
    {
        if (string.IsNullOrWhiteSpace(_manualInputText))
        {
            _manualInputError = "请输入点数";
            return;
        }

        if (!long.TryParse(_manualInputText, out var parsed))
        {
            _manualInputError = "点数必须是数字";
            return;
        }

        int manualValue = parsed > int.MaxValue ? int.MaxValue : (int)parsed;
        manualValue = Math.Max(0, manualValue);

        if (_manualInputForPlanning)
        {
            // 规划系统的手动输入确认
            AddPlannedAction(_manualInputDiceName, _manualInputForPlanningAD, null, manualValue);
            ShowTip($"已规划{(_manualInputForPlanningAD ? "AD" : "PD")}: {_manualInputDiceName}");
        }
        else if (_manualInputIsDefense)
        {
            BattleDefenseRequested?.Invoke(_manualInputDiceName, manualValue);
        }
        else
        {
            BattleActionRequested?.Invoke(_manualInputDiceName, _manualInputTargetPlayerId, manualValue);
        }

        ClearManualInputState();
    }

    private void CancelManualInput()
    {
        ClearManualInputState();
    }

    private void ClearManualInputState()
    {
        _manualInputOpen = false;
        _manualInputIsDefense = false;
        _manualInputDiceName = string.Empty;
        _manualInputTargetPlayerId = null;
        _manualInputText = string.Empty;
        _manualInputError = string.Empty;
        _manualInputForPlanning = false;
        _manualInputForPlanningAD = false;
    }


    /// <summary>
    /// 绘制战斗面板
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, GraphicsDevice graphicsDevice, int panelWidth, int panelHeight)
    {
        if (_currentBattle == null)
            return;

        int panelX = _menuWidth;

        // 如果战斗已结束，显示结算界面或等待消息
        if (_currentBattle.IsBattleOver)
        {
            spriteBatch.Begin();
            
            if (_battleEndNotification != null)
            {
                // 显示结算界面
                DrawBattleSettlement(spriteBatch, texture, font, panelX, panelWidth, panelHeight);
            }
            else
            {
                // 等待服务器的战斗结束消息
                spriteBatch.Draw(texture, new Rectangle(panelX, 0, panelWidth, panelHeight), Color.Black * 0.7f);
                string waitMsg = "战斗结束，正在加载结算数据...";
                Vector2 msgSize = font.MeasureString(waitMsg);
                spriteBatch.DrawString(font, waitMsg, 
                    new Vector2(panelX + (panelWidth - msgSize.X) / 2, panelHeight / 2 - msgSize.Y / 2), 
                    Color.Gold);
            }
            
            spriteBatch.End();
            return;
        }

        spriteBatch.Begin();

        spriteBatch.Draw(texture, new Rectangle(panelX, 0, panelWidth, panelHeight), Color.DarkSlateGray * 0.8f);

        spriteBatch.DrawString(font, "战斗进行中", new Vector2(panelX + 20, 10), Color.White);
        spriteBatch.DrawString(font, $"回合: {_currentBattle.CurrentRound}", new Vector2(panelX + 120, 10), Color.Yellow);

        int barW = 300;
        int barH = 20;
        int barTop = 60;
        int barSpacing = 35;  // 血条间距

        // 显示所有Team1玩家的血条
        var team1AlivePlayer = _currentBattle.Team1Players.Where(p => !p.IsDead).ToList();
        for (int i = 0; i < team1AlivePlayer.Count; i++)
        {
            int verticalOffset = i * barSpacing;
            DrawPlayerHealthBar(spriteBatch, texture, font, panelX, team1AlivePlayer[i], barW, barH, barTop, true, verticalOffset);
        }

        // 显示所有Team2玩家的血条
        var team2AlivePlayer = _currentBattle.Team2Players.Where(p => !p.IsDead).ToList();
        for (int i = 0; i < team2AlivePlayer.Count; i++)
        {
            int verticalOffset = i * barSpacing;
            DrawPlayerHealthBar(spriteBatch, texture, font, panelX + panelWidth, team2AlivePlayer[i], barW, barH, barTop, false, verticalOffset);
        }

        _battleLogToggleRect = new Rectangle(panelX + panelWidth - 90, 10, 80, 30);
        spriteBatch.Draw(texture, _battleLogToggleRect, Color.DimGray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _battleLogToggleRect, Color.White, 2);
        string toggleText = _isBattleLogOpen ? "日志 ▾" : "日志 ▸";
        spriteBatch.DrawString(font, toggleText, new Vector2(_battleLogToggleRect.X + 10, _battleLogToggleRect.Y + 5), Color.White);

        _surrenderButtonRect = new Rectangle(panelX + panelWidth - 180, 10, 80, 30);
        spriteBatch.Draw(texture, _surrenderButtonRect, Color.DarkRed * 0.85f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _surrenderButtonRect, Color.White, 2);
        spriteBatch.DrawString(font, "认输", new Vector2(_surrenderButtonRect.X + 18, _surrenderButtonRect.Y + 5), Color.White);

        spriteBatch.End();

        DrawBattleLog(spriteBatch, texture, font, graphicsDevice, panelX, panelWidth);

        // 绘制设置按钮
        spriteBatch.Begin();
        _settingsButtonRect = new Rectangle(panelX + 10, panelHeight - 45, 80, 35);
        spriteBatch.Draw(texture, _settingsButtonRect, Color.DimGray * 0.85f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _settingsButtonRect, Color.White, 2);
        spriteBatch.DrawString(font, "设置", new Vector2(_settingsButtonRect.X + 15, _settingsButtonRect.Y + 5), Color.White);
        spriteBatch.End();

        // 如果装备"预见"，显示双行规划框
        if (HasForesightAccessory())
        {
            // 装备预见时只显示规划框，不显示普通骰子框
            DrawForesightPlannedActions(spriteBatch, texture, font, panelX, panelWidth, panelHeight, barW, barH, barTop);
        }
        else
        {
            // 未装备预见时显示普通骰子框
            DrawBattleActions(spriteBatch, texture, font, panelX, panelWidth, panelHeight, barW, barH, barTop);
        }
        
        DrawTemporaryTip(spriteBatch, texture, font, panelX, panelWidth, panelHeight);

        // 绘制设置UI
        if (_settingsUIOpen)
        {
            DrawSettingsUI(spriteBatch, texture, font, panelX, panelWidth, panelHeight);
        }
    }

    /// <summary>
    /// 绘制快捷键设置界面
    /// </summary>
    private void DrawSettingsUI(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int panelX, int panelWidth, int panelHeight)
    {
        spriteBatch.Begin();

        // 半透明遮罩
        spriteBatch.Draw(texture, new Rectangle(panelX, 0, panelWidth, panelHeight), Color.Black * 0.6f);

        // 设置窗口
        int windowWidth = 400;
        int windowHeight = 300;
        int windowX = panelX + (panelWidth - windowWidth) / 2;
        int windowY = (panelHeight - windowHeight) / 2;

        var settingsWindow = new Rectangle(windowX, windowY, windowWidth, windowHeight);
        spriteBatch.Draw(texture, settingsWindow, Color.DarkSlateGray * 0.95f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, settingsWindow, Color.Gold, 3);

        // 标题
        spriteBatch.DrawString(font, "快捷键设置", new Vector2(windowX + 20, windowY + 15), Color.Gold);

        // PD跳过键设置
        int contentY = windowY + 60;
        int lineHeight = 60;

        spriteBatch.DrawString(font, "PD跳过键：", new Vector2(windowX + 20, contentY), Color.White);

        // 键盘快捷键按钮
        _pdKeyboardButtonRect = new Rectangle(windowX + 20, contentY + 30, 150, 35);
        Color keyboardBtnColor = _isBindingKey ? Color.Yellow : Color.DimGray;
        spriteBatch.Draw(texture, _pdKeyboardButtonRect, keyboardBtnColor * 0.8f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _pdKeyboardButtonRect, Color.White, 2);

        string keyboardLabel = _keyBindingConfig.PDSkipKeyboard.HasValue 
            ? _keyBindingConfig.PDSkipKeyboard.Value.ToString() 
            : "未设置";
        if (_isBindingKey)
            keyboardLabel = "等待输入...";

        spriteBatch.DrawString(font, $"键盘: {keyboardLabel}", new Vector2(_pdKeyboardButtonRect.X + 10, _pdKeyboardButtonRect.Y + 7), Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        // 手柄快捷键按钮
        _pdGamePadButtonRect = new Rectangle(windowX + 200, contentY + 30, 180, 35);
        Color gamepadBtnColor = _isBindingGamePad ? Color.Yellow : Color.DimGray;
        spriteBatch.Draw(texture, _pdGamePadButtonRect, gamepadBtnColor * 0.8f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _pdGamePadButtonRect, Color.White, 2);

        string gamepadLabel = KeyBindingConfig.GetButtonDisplayName(_keyBindingConfig.PDSkipGamePad);
        if (_isBindingGamePad)
            gamepadLabel = "等待输入...";

        spriteBatch.DrawString(font, $"手柄: {gamepadLabel}", new Vector2(_pdGamePadButtonRect.X + 10, _pdGamePadButtonRect.Y + 7), Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        // 绑定提示信息
        if (!string.IsNullOrEmpty(_bindingPromptMessage))
        {
            Vector2 promptSize = font.MeasureString(_bindingPromptMessage);
            spriteBatch.DrawString(font, _bindingPromptMessage, 
                new Vector2(windowX + (windowWidth - promptSize.X) / 2, contentY + 75), 
                Color.Cyan);
        }

        // 关闭按钮
        _closeSettingsButtonRect = new Rectangle(windowX + (windowWidth - 100) / 2, windowY + windowHeight - 50, 100, 35);
        spriteBatch.Draw(texture, _closeSettingsButtonRect, Color.DarkRed * 0.85f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _closeSettingsButtonRect, Color.White, 2);
        Vector2 closeBtnSize = font.MeasureString("关闭");
        spriteBatch.DrawString(font, "关闭", new Vector2(_closeSettingsButtonRect.X + (100 - closeBtnSize.X) / 2, _closeSettingsButtonRect.Y + 7), Color.White);

        spriteBatch.End();
    }

    /// <summary>
    /// 绘制临时提示信息
    /// </summary>
    private void DrawTemporaryTip(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int panelX, int panelWidth, int panelHeight)
    {
        if (string.IsNullOrEmpty(_currentTip) || _tipDurationMs <= 0)
            return;

        spriteBatch.Begin();

        // 计算提示文本的大小
        Vector2 tipSize = font.MeasureString(_currentTip);
        int padding = 20;
        
        // 在屏幕中央上方显示
        int tipX = panelX + (panelWidth - (int)tipSize.X) / 2;
        int tipY = panelHeight / 3;
        
        // 绘制背景
        Rectangle tipBG = new Rectangle(tipX - padding, tipY - padding / 2, (int)tipSize.X + padding * 2, (int)tipSize.Y + padding);
        spriteBatch.Draw(texture, tipBG, Color.Black * 0.7f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, tipBG, Color.Gold, 2);
        
        // 绘制文本，根据剩余时间调整透明度
        float alpha = Math.Min(1f, (float)(_tipDurationMs / 500.0));  // 最后500ms淡出
        spriteBatch.DrawString(font, _currentTip, new Vector2(tipX, tipY), Color.Gold * alpha);

        spriteBatch.End();
    }

    private void DrawShortcutLabel(SpriteBatch spriteBatch, SpriteFont font, string label, Rectangle rect, Color color)
    {
        if (string.IsNullOrWhiteSpace(label))
            return;

        Vector2 size = font.MeasureString(label) * ShortcutLabelScale;
        Vector2 pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y - size.Y - 4f);
        spriteBatch.DrawString(font, label, pos, color * 0.6f, 0f, Vector2.Zero, ShortcutLabelScale, SpriteEffects.None, 0f);
    }

    private string GetPDSkipShortcutLabel()
    {
        var keyboardKey = _keyBindingConfig.PDSkipKeyboard;
        var gamepadButton = _keyBindingConfig.PDSkipGamePad;

        if (!keyboardKey.HasValue && !gamepadButton.HasValue)
            return "未设置";

        List<string> labels = new List<string>();
        if (keyboardKey.HasValue)
            labels.Add(keyboardKey.Value.ToString());
        if (gamepadButton.HasValue)
            labels.Add(KeyBindingConfig.GetButtonDisplayName(gamepadButton));

        return string.Join(" / ", labels);
    }

    private void DrawPlayerHealthBar(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int xPosition, Player player, int barW, int barH, int barTop, bool isLeft, int verticalOffset = 0)
    {
        if (player == null)
            return;

        int adjustedBarTop = barTop + verticalOffset;

        if (isLeft)
        {
            var leftName = $"{player.PlayerName}";
            spriteBatch.DrawString(font, leftName, new Vector2(xPosition + 20, adjustedBarTop - 28), Color.LightBlue);
            Rectangle hpBG = new Rectangle(xPosition + 20, adjustedBarTop, barW, barH);
            spriteBatch.Draw(texture, hpBG, Color.Black * 0.5f);
            float pct = player.MaxHP > 0 ? Math.Clamp(player.CurrentHP / (float)player.MaxHP, 0f, 1f) : 0f;
            Rectangle hpFG = new Rectangle(xPosition + 20, adjustedBarTop, (int)(barW * pct), barH);
            spriteBatch.Draw(texture, hpFG, Color.Green * 0.9f);
            spriteBatch.DrawString(font, $"{player.CurrentHP}/{player.MaxHP}", new Vector2(xPosition + 25, adjustedBarTop + 24), Color.White);
            if (player.ShieldLayers > 0)
            {
                spriteBatch.DrawString(font, $"护盾:{player.ShieldLayers}", new Vector2(xPosition + 160, adjustedBarTop + 24), Color.CornflowerBlue);
            }
        }
        else
        {
            var rightName = $"{player.PlayerName}";
            Vector2 nameSize = font.MeasureString(rightName);
            spriteBatch.DrawString(font, rightName, new Vector2(xPosition - 20 - nameSize.X, adjustedBarTop - 28), Color.LightCoral);
            Rectangle hpBG = new Rectangle(xPosition - 20 - barW, adjustedBarTop, barW, barH);
            spriteBatch.Draw(texture, hpBG, Color.Black * 0.5f);
            float pct = player.MaxHP > 0 ? Math.Clamp(player.CurrentHP / (float)player.MaxHP, 0f, 1f) : 0f;
            Rectangle hpFG = new Rectangle(xPosition - 20 - barW, adjustedBarTop, (int)(barW * pct), barH);
            spriteBatch.Draw(texture, hpFG, Color.Green * 0.9f);
            Vector2 hpSize = font.MeasureString($"{player.CurrentHP}/{player.MaxHP}");
            spriteBatch.DrawString(font, $"{player.CurrentHP}/{player.MaxHP}", new Vector2(xPosition - 25 - hpSize.X, adjustedBarTop + 24), Color.White);
            if (player.ShieldLayers > 0)
            {
                Vector2 sSize = font.MeasureString($"护盾:{player.ShieldLayers}");
                spriteBatch.DrawString(font, $"护盾:{player.ShieldLayers}", new Vector2(xPosition - 30 - sSize.X, adjustedBarTop + 24), Color.CornflowerBlue);
            }
        }
    }

    private void DrawBattleLog(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, GraphicsDevice graphicsDevice, int panelX, int panelWidth)
    {
        if (!_isBattleLogOpen)
            return;

        _battleLogWindowRect = new Rectangle(panelX + panelWidth - 420, 60, 400, 250);
        RasterizerState rs = new RasterizerState { ScissorTestEnable = true };
        spriteBatch.Begin(rasterizerState: rs);
        graphicsDevice.ScissorRectangle = _battleLogWindowRect;
        spriteBatch.Draw(texture, _battleLogWindowRect, Color.Black * 0.6f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _battleLogWindowRect, Color.White, 2);

        int lineHeight = 20;
        int innerPad = 10;
        int linesPerPage = Math.Max(1, (_battleLogWindowRect.Height - innerPad * 2) / lineHeight);
        int totalLines = _currentBattle.BattleLog.Count;
        int maxScroll = Math.Max(0, totalLines - linesPerPage);
        _battleLogScrollOffset = Math.Clamp(_battleLogScrollOffset, 0, maxScroll);
        int startIndex = _battleLogScrollOffset;
        for (int i = 0; i < linesPerPage && startIndex + i < totalLines; i++)
        {
            string logLine = _currentBattle.BattleLog[startIndex + i];
            Color logColor = logLine.StartsWith("===") ? Color.LimeGreen :
                             logLine.Contains("伤害") ? Color.Red :
                             (logLine.Contains("防御") || logLine.Contains("闪避")) ? Color.LimeGreen :
                             Color.White;
            spriteBatch.DrawString(font, logLine, new Vector2(_battleLogWindowRect.X + innerPad, _battleLogWindowRect.Y + innerPad + i * lineHeight), logColor);
        }
        spriteBatch.End();
    }

    private void DrawBattleActions(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int panelX, int panelWidth, int panelHeight, int barW, int barH, int barTop)
    {
        spriteBatch.Begin();

        if (_currentBattle.IsWaitingForPlayerInput)
        {
            int diceAreaY = panelHeight - 120;
            spriteBatch.Draw(texture, new Rectangle(panelX + 10, diceAreaY - 10, panelWidth - 20, 70), Color.Black * 0.4f);
            string tip = _currentBattle.InputContext == BattleInputContext.AttackSelection ?
                (_pendingSelectedDice == null ? "选择一个可用的AD骰子进行攻击（PD会置灰），或点击跳过" : "请选择攻击目标") :
                "选择一个可用的PD骰子进行防御（AD会置灰），或点击跳过";
            spriteBatch.DrawString(font, tip, new Vector2(panelX + 20, diceAreaY - 30), Color.White);

            int btnW = 110;
            int btnH = 40;
            int spacing = 10;
            int startX = panelX + 20;

            _diceButtonRects.Clear();
            _diceButtons.Clear();

            var displayDice = GetDisplayDiceForCurrentContext();
            var availableNames = BuildAvailableDiceNameSet(_currentBattle.InputContext);

            if (_currentBattle.InputContext == BattleInputContext.AttackSelection)
            {
                for (int i = 0; i < displayDice.Count; i++)
                {
                    var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                    var dice = displayDice[i];
                    string shortcutLabel = i < DiceShortcutRowPrimaryLabels.Length && i < GamePadSlotLabels.Length
                        ? $"{DiceShortcutRowPrimaryLabels[i]} / {GamePadSlotLabels[i]}"
                        : string.Empty;
                    DrawShortcutLabel(spriteBatch, font, shortcutLabel, rect, Color.White);
                    bool enabled = IsDiceEnabledForContext(dice, availableNames, BattleInputContext.AttackSelection);
                    Color c = enabled ? Color.DarkSlateGray * 0.9f : Color.DimGray * 0.6f;
                    spriteBatch.Draw(texture, rect, c);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, enabled ? Color.White : Color.Gray, 2);
                    int iconSize = 24;
                    Rectangle iconRect = new Rectangle(rect.X + 6, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
                    bool hasIcon = _iconProvider?.TryDrawIcon(spriteBatch, dice, iconRect, enabled ? Color.White : Color.Gray) ?? false;
                    int textX = rect.X + 10 + (hasIcon ? iconSize + 6 : 0);
                    spriteBatch.DrawString(font, dice.Name, new Vector2(textX, rect.Y + 8), enabled ? Color.White : Color.Gray);
                    
                    // 显示计数器（如果有）
                    if (dice is ICounterDice counterDice)
                    {
                        string counterText = $"[{counterDice.Counter}]";
                        spriteBatch.DrawString(font, counterText, new Vector2(textX, rect.Y + 26), Color.Orange, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                    }
                    
                    _diceButtonRects.Add(rect);
                    _diceButtons.Add(dice);
                }
            }
            else if (_currentBattle.InputContext == BattleInputContext.DefenseSelection)
            {
                for (int i = 0; i < displayDice.Count; i++)
                {
                    var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                    var dice = displayDice[i];
                    string shortcutLabel = i < DiceShortcutRowPrimaryLabels.Length && i < GamePadSlotLabels.Length
                        ? $"{DiceShortcutRowPrimaryLabels[i]} / {GamePadSlotLabels[i]}"
                        : string.Empty;
                    DrawShortcutLabel(spriteBatch, font, shortcutLabel, rect, Color.White);
                    bool enabled = IsDiceEnabledForContext(dice, availableNames, BattleInputContext.DefenseSelection);
                    Color c = enabled ? Color.DarkSlateGray * 0.9f : Color.DimGray * 0.6f;
                    spriteBatch.Draw(texture, rect, c);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, enabled ? Color.White : Color.Gray, 2);
                    int iconSize = 24;
                    Rectangle iconRect = new Rectangle(rect.X + 6, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
                    bool hasIcon = _iconProvider?.TryDrawIcon(spriteBatch, dice, iconRect, enabled ? Color.White : Color.Gray) ?? false;
                    int textX = rect.X + 10 + (hasIcon ? iconSize + 6 : 0);
                    spriteBatch.DrawString(font, dice.Name, new Vector2(textX, rect.Y + 8), enabled ? Color.White : Color.Gray);
                    
                    // 显示计数器（如果有）
                    if (dice is ICounterDice counterDice)
                    {
                        string counterText = $"[{counterDice.Counter}]";
                        spriteBatch.DrawString(font, counterText, new Vector2(textX, rect.Y + 26), Color.Orange, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                    }
                    
                    _diceButtonRects.Add(rect);
                    _diceButtons.Add(dice);
                }
            }

            _skipActionButtonRect = new Rectangle(panelX + panelWidth - 120, diceAreaY, 100, btnH);
            DrawShortcutLabel(spriteBatch, font, "Space / RT", _skipActionButtonRect, Color.White);
            spriteBatch.Draw(texture, _skipActionButtonRect, Color.DimGray * 0.9f);
            DrawingHelper.DrawRectangle(spriteBatch, texture, _skipActionButtonRect, Color.White, 2);
            spriteBatch.DrawString(font, "跳过", new Vector2(_skipActionButtonRect.X + 20, _skipActionButtonRect.Y + 8), Color.White);

            if (_pendingSelectedDice != null)
            {
                _opponentRects.Clear();
                var opponents = _currentBattle.AvailableOpponents;
                int barSpacing = 35;

                // 高亮所有Team1的可攻击对手
                var team1Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team1).ToList();
                for (int i = 0; i < team1Opponents.Count; i++)
                {
                    int verticalOffset = i * barSpacing;
                    var rect = new Rectangle(panelX + 20 - 10, barTop - 10 + verticalOffset, barW + 20, barH + 40);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.Yellow, 3);
                    _opponentRects.Add((team1Opponents[i], rect));
                }

                // 高亮所有Team2的可攻击对手
                var team2Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team2).ToList();
                for (int i = 0; i < team2Opponents.Count; i++)
                {
                    int verticalOffset = i * barSpacing;
                    var rect = new Rectangle(panelX + panelWidth - 20 - barW - 10, barTop - 10 + verticalOffset, barW + 20, barH + 40);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.Yellow, 3);
                    _opponentRects.Add((team2Opponents[i], rect));
                }
            }
        }

        if (_manualInputOpen)
        {
            DrawManualInputOverlay(spriteBatch, texture, font, panelX, panelWidth, panelHeight);
        }

        spriteBatch.End();
    }

    private void DrawManualInputOverlay(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int panelX, int panelWidth, int panelHeight)
    {
        var layout = GetManualInputLayout(panelX, panelWidth, panelHeight);

        // 半透明遮罩
        spriteBatch.Draw(texture, new Rectangle(panelX, 0, panelWidth, panelHeight), Color.Black * 0.35f);

        // 对话框背景
        spriteBatch.Draw(texture, layout.dialogRect, Color.DimGray * 0.95f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, layout.dialogRect, Color.Gold, 2);

        string title = $"输入{_manualInputDiceName}点数";
        string subtitle = _manualInputIsDefense ? "用作防御点数（Enter确认 / Esc取消）" : "用作攻击点数（Enter确认 / Esc取消）";

        spriteBatch.DrawString(font, title, new Vector2(layout.dialogRect.X + 16, layout.dialogRect.Y + 14), Color.White);
        spriteBatch.DrawString(font, subtitle, new Vector2(layout.dialogRect.X + 16, layout.dialogRect.Y + 40), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        // 输入框
        spriteBatch.Draw(texture, layout.inputRect, Color.Black * 0.7f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, layout.inputRect, Color.White, 2);

        string inputDisplay = string.IsNullOrEmpty(_manualInputText) ? "请输入数字，支持超大值" : _manualInputText;
        var inputColor = string.IsNullOrEmpty(_manualInputText) ? Color.Gray : Color.White;
        spriteBatch.DrawString(font, inputDisplay, new Vector2(layout.inputRect.X + 8, layout.inputRect.Y + 8), inputColor);

        if (!string.IsNullOrEmpty(_manualInputError))
        {
            spriteBatch.DrawString(font, _manualInputError, new Vector2(layout.inputRect.X, layout.inputRect.Bottom + 6), Color.IndianRed, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }

        // 按钮
        spriteBatch.Draw(texture, layout.confirmRect, Color.DarkOliveGreen * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, layout.confirmRect, Color.White, 2);
        spriteBatch.DrawString(font, "确认", new Vector2(layout.confirmRect.X + 26, layout.confirmRect.Y + 8), Color.White);

        spriteBatch.Draw(texture, layout.cancelRect, Color.DarkRed * 0.85f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, layout.cancelRect, Color.White, 2);
        spriteBatch.DrawString(font, "取消", new Vector2(layout.cancelRect.X + 26, layout.cancelRect.Y + 8), Color.White);
    }

    /// <summary>
    /// 绘制战斗结算界面
    /// </summary>
    private void DrawBattleSettlement(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int panelX, int panelWidth, int panelHeight)
    {
        // 半透明遮罩
        spriteBatch.Draw(texture, new Rectangle(panelX, 0, panelWidth, panelHeight), Color.Black * 0.7f);

        // 结算窗口
        int windowWidth = panelWidth - 40;
        int windowHeight = panelHeight - 80;
        int windowX = panelX + 20;
        int windowY = 40;
        
        var settleWindow = new Rectangle(windowX, windowY, windowWidth, windowHeight);
        spriteBatch.Draw(texture, settleWindow, Color.DarkSlateGray * 0.95f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, settleWindow, Color.Gold, 3);

        // 标题
        string title = _battleEndNotification.WinnerCamp != null 
            ? $"{_battleEndNotification.WinnerCamp}阵营获胜!" 
            : "战斗结束";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, 
            new Vector2(windowX + (windowWidth - titleSize.X) / 2, windowY + 20), 
            Color.Gold);

        // 基础信息
        int contentX = windowX + 30;
        int contentY = windowY + 60;
        int lineHeight = 25;
        
        spriteBatch.DrawString(font, $"战斗时长: {_battleEndNotification.BattleDuration.Minutes}分{_battleEndNotification.BattleDuration.Seconds}秒", 
            new Vector2(contentX, contentY), Color.LightGray);
        spriteBatch.DrawString(font, $"总回合数: {_battleEndNotification.TotalRounds}", 
            new Vector2(contentX, contentY + lineHeight), Color.LightGray);

        // 玩家统计
        contentY += lineHeight * 3;
        spriteBatch.DrawString(font, "=== 战斗统计 ===", new Vector2(contentX, contentY), Color.Yellow);
        contentY += lineHeight + 5;

        if (_battleEndNotification.PlayerStats != null && _battleEndNotification.PlayerStats.Count > 0)
        {
            int scrollableArea = windowHeight - (contentY - windowY) - 80;
            int visibleLines = Math.Max(1, scrollableArea / lineHeight);
            
            for (int i = 0; i < Math.Min(visibleLines, _battleEndNotification.PlayerStats.Count); i++)
            {
                var stat = _battleEndNotification.PlayerStats[i];
                string mvpTag = stat.IsMVP ? " 🏆MVP🏆" : "";
                string playerInfo = $"{stat.PlayerName}{mvpTag}: 伤害{stat.TotalDamageDealt} | 承受{stat.TotalDamageTaken} | 格挡{stat.TotalDamageBlocked} | 击杀{stat.KillCount}";
                
                Color textColor = stat.IsMVP ? Color.Gold : Color.White;
                spriteBatch.DrawString(font, playerInfo, new Vector2(contentX, contentY), textColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                contentY += lineHeight;
            }
        }

        // 返回大厅按钮
        int btnWidth = 120;
        int btnHeight = 40;
        _returnToLobbyButtonRect = new Rectangle(
            windowX + (windowWidth - btnWidth) / 2, 
            windowY + windowHeight - 50, 
            btnWidth, 
            btnHeight
        );
        
        spriteBatch.Draw(texture, _returnToLobbyButtonRect, Color.DarkGreen * 0.85f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _returnToLobbyButtonRect, Color.White, 2);
        
        Vector2 btnTextSize = font.MeasureString("返回大厅");
        spriteBatch.DrawString(font, "返回大厅", 
            new Vector2(_returnToLobbyButtonRect.X + (btnWidth - btnTextSize.X) / 2, 
                       _returnToLobbyButtonRect.Y + (btnHeight - btnTextSize.Y) / 2), 
            Color.White);
    }
    
    /// <summary>
    /// 处理服务器战斗状态更新（多人战斗）
    /// </summary>
    public void ApplyServerBattleState(BattleStateUpdateNotification state)
    {
        if (!_isMultiplayerBattle || _currentBattle == null)
            return;
        
        _currentBattleState = state;
        
        // 更新玩家状态
        foreach (var playerState in state.Players)
        {
            var player = _currentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == playerState.PlayerId);
            if (player != null)
            {
                player.CurrentHP = playerState.CurrentHP;
                player.MaxHP = playerState.MaxHP;
                player.ShieldLayers = playerState.ShieldLayers;
                // IsDead 会根据 CurrentHP 自动更新
                
                // 同步装备的骰子（如果玩家还没有装备）
                if (player.GetEquippedDice().Count == 0 && playerState.EquippedDiceNames != null && playerState.EquippedDiceNames.Count > 0)
                {
                    SyncPlayerDiceEquipment(player, playerState.EquippedDiceNames);
                }
                
                // 同步骰子计数器状态
                if (playerState.DiceCounters != null && playerState.DiceCounters.Count > 0)
                {
                    foreach (var dice in player.GetEquippedDice())
                    {
                        if (dice is ICounterDice counterDice && playerState.DiceCounters.ContainsKey(dice.Name))
                        {
                            counterDice.Counter = playerState.DiceCounters[dice.Name];
                        }
                    }
                }
            }
        }
        
        // 添加新的战斗日志
        if (state.NewBattleLogs != null && state.NewBattleLogs.Count > 0)
        {
            foreach (var log in state.NewBattleLogs)
            {
                _currentBattle.BattleLog.Add(log);
            }
            
            // 显示最后一条战斗日志作为临时提示
            if (state.NewBattleLogs.Count > 0)
            {
                string tipLog = BuildTipFromLogs(state.NewBattleLogs);
                if (!string.IsNullOrEmpty(tipLog))
                {
                    ShowTip(tipLog, 2500);  // 显示2.5秒
                }
            }
        }
        
        // 同步基础战斗状态
        _currentBattle.CurrentRound = state.CurrentRound;
        if (Enum.TryParse<BattleState>(state.CurrentState, out var parsedState))
        {
            _currentBattle.CurrentState = parsedState;
        }
        if (Enum.TryParse<PlayerCamp>(state.CurrentCamp, out var parsedCamp))
        {
            _currentBattle.CurrentCamp = parsedCamp;
        }

        // 更新行动/输入上下文
        if (Enum.TryParse<BattleInputContext>(state.InputContext, out var parsedContext))
        {
            _currentBattle.SetInputContext(parsedContext);
        }
        else
        {
            _currentBattle.SetInputContext(BattleInputContext.None);
        }

        _currentBattle.CurrentActionPlayer = !string.IsNullOrEmpty(state.CurrentActionPlayerId)
            ? _currentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == state.CurrentActionPlayerId)
            : null;

        _currentBattle.IsBattleOver = state.IsBattleOver;
        if (!string.IsNullOrEmpty(state.WinnerCamp) && Enum.TryParse<PlayerCamp>(state.WinnerCamp, out var parsedWinner))
        {
            _currentBattle.WinnerCamp = parsedWinner;
        }
        else
        {
            _currentBattle.WinnerCamp = null;
        }

        // 战斗结束则不再等待输入
        if (state.IsBattleOver)
        {
            _currentBattle.IsWaitingForPlayerInput = false;
            return;
        }
        
        // 更新等待输入状态
        if (!string.IsNullOrEmpty(state.WaitingInputPlayerId) && state.WaitingInputPlayerId == _localPlayerId)
        {
            // 本地玩家需要输入
            _currentBattle.IsWaitingForPlayerInput = true;
            
            // 尝试自动执行预设的行动（用于"预见"饰品）
            if (TryExecutePlannedAction(state.InputContext))
            {
                return;  // 已自动执行，无需等待玩家输入
            }
            
            if (state.InputContext == "AttackSelection")
            {
                // 设置可用的AD骰子（从服务器状态）
                // 注意：这里我们使用 CurrentActionPlayerId 而不是 CurrentActionPlayer
                if (!string.IsNullOrEmpty(state.CurrentActionPlayerId))
                {
                    UpdateAvailableActiveDice(state.AvailableActiveDiceNames, state.CurrentActionPlayerId);
                }
                // 设置可攻击的对手
                UpdateAvailableOpponents(state.AvailableOpponentIds);
            }
            else if (state.InputContext == "DefenseSelection")
            {
                // 设置可用的PD骰子
                UpdateAvailablePassiveDice(state.AvailablePassiveDiceNames);
            }
        }
        else
        {
            _currentBattle.IsWaitingForPlayerInput = false;
        }
    }
    
    private void UpdateAvailableActiveDice(List<string> diceNames, string playerId = null)
    {
        if (diceNames == null)
            return;
        
        // 如果提供了玩家ID，使用它；否则使用 CurrentActionPlayer
        Player actionPlayer = null;
        if (!string.IsNullOrEmpty(playerId))
        {
            actionPlayer = _currentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == playerId);
        }
        else
        {
            actionPlayer = _currentBattle.CurrentActionPlayer;
        }
        
        if (actionPlayer == null)
        {
            Console.WriteLine($"[Warning] Could not find action player for dice update: {playerId}");
            return;
        }
        
        var availableDice = new List<Dice>();
        foreach (var diceName in diceNames)
        {
            var dice = actionPlayer.GetEquippedDice().FirstOrDefault(d => d.Name == diceName);
            if (dice != null)
            {
                availableDice.Add(dice);
            }
        }
        
        Console.WriteLine($"[BattleManager] Updating available dice for {actionPlayer.PlayerName}: {string.Join(", ", availableDice.Select(d => d.Name))}");
        
        // 通过反射设置私有字段（暂时方案）
        var fieldInfo = typeof(Battle).GetField("_currentActiveDiceChoices", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo?.SetValue(_currentBattle, availableDice);
    }
    
    private void UpdateAvailablePassiveDice(List<string> diceNames)
    {
        if (diceNames == null)
            return;
        
        var localPlayer = _currentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == _localPlayerId);
        if (localPlayer == null)
            return;
        
        var availableDice = new List<Dice>();
        foreach (var diceName in diceNames)
        {
            var dice = localPlayer.GetEquippedDice().FirstOrDefault(d => d.Name == diceName);
            if (dice != null)
            {
                availableDice.Add(dice);
            }
        }
        
        var fieldInfo = typeof(Battle).GetField("_currentPassiveDiceChoices", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo?.SetValue(_currentBattle, availableDice);
    }
    
    private void UpdateAvailableOpponents(List<string> opponentIds)
    {
        if (opponentIds == null)
            return;
        
        var opponents = new List<Player>();
        foreach (var opponentId in opponentIds)
        {
            var opponent = _currentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == opponentId);
            if (opponent != null)
            {
                opponents.Add(opponent);
            }
        }
        
        var fieldInfo = typeof(Battle).GetField("_currentOpponents", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo?.SetValue(_currentBattle, opponents);
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    public void EndBattle()
    {
        _currentBattle = null;
        _pendingSelectedDice = null;
        _diceButtonRects.Clear();
        _diceButtons.Clear();
        _opponentRects.Clear();
        _isMultiplayerBattle = false;
        _localPlayerId = null;
        _currentBattleState = null;
        _plannedDiceSequenceNumbersAD.Clear();
        _plannedDiceSequenceNumbersPD.Clear();
    }

    /// <summary>
    /// 处理"预见"饰品的规划输入
    /// </summary>
    private void HandleForesightPlanningInput(MouseState mouseState, MouseState previousMouseState, int panelX, int panelWidth, int panelHeight)
    {
        if (mouseState.LeftButton != ButtonState.Pressed || previousMouseState.LeftButton == ButtonState.Pressed)
            return;

        int btnW = 110;
        int btnH = 40;
        int spacing = 10;
        int startX = panelX + 20;

        var localPlayer = GetLocalPlayer();
        if (localPlayer == null)
            return;

        var displayDice = localPlayer.GetEquippedDice();
        Point mp = new Point(mouseState.X, mouseState.Y);

        // AD规划框处理
        int adDiceAreaY = panelHeight - 200;
        for (int i = 0; i < displayDice.Count; i++)
        {
            var rect = new Rectangle(startX + i * (btnW + spacing), adDiceAreaY, btnW, btnH);
            if (rect.Contains(mp))
            {
                var dice = displayDice[i];
                var opponents = _currentBattle.AvailableOpponents;

                if (DiceRequiresManualInput(dice))
                {
                    _manualInputOpen = true;
                    _manualInputForPlanning = true;
                    _manualInputForPlanningAD = true;
                    _manualInputDiceName = dice.Name;
                    _manualInputIsDefense = false;
                    _manualInputTargetPlayerId = null;
                    _manualInputText = string.Empty;
                }
                else if (opponents.Count <= 1)
                {
                    var targetId = opponents.FirstOrDefault()?.PlayerId;
                    AddPlannedAction(dice.Name, true, targetId, 0);
                    ShowTip($"已规划AD: {dice.Name}");
                }
                else
                {
                    // 需要选择目标 - 这里需要额外的逻辑
                    ShowTip($"需要选择目标进行AD规划");
                }
                return;
            }
        }

        // PD规划框处理
        int pdDiceAreaY = panelHeight - 120;
        for (int i = 0; i < displayDice.Count; i++)
        {
            var rect = new Rectangle(startX + i * (btnW + spacing), pdDiceAreaY, btnW, btnH);
            if (rect.Contains(mp))
            {
                var dice = displayDice[i];

                if (DiceRequiresManualInput(dice))
                {
                    _manualInputOpen = true;
                    _manualInputForPlanning = true;
                    _manualInputForPlanningAD = false;
                    _manualInputDiceName = dice.Name;
                    _manualInputIsDefense = true;
                    _manualInputTargetPlayerId = null;
                    _manualInputText = string.Empty;
                }
                else
                {
                    AddPlannedAction(dice.Name, false, null, 0);
                    ShowTip($"已规划PD: {dice.Name}");
                }
                return;
            }
        }

        // AD框的跳过按钮处理
        int adDiceAreaY_skip = panelHeight - 200;
        int skipBtnW = 80;
        int skipBtnH = 30;
        Rectangle skipAdButtonRect = new Rectangle(panelX + panelWidth - skipBtnW - 20, adDiceAreaY_skip + 20, skipBtnW, skipBtnH);
        if (skipAdButtonRect.Contains(mp))
        {
            ShowTip("跳过AD规划");
            return;
        }

        // PD框的跳过按钮处理
        int pdDiceAreaY_skip = panelHeight - 120;
        Rectangle skipPdButtonRect = new Rectangle(panelX + panelWidth - skipBtnW - 20, pdDiceAreaY_skip + 20, skipBtnW, skipBtnH);
        if (skipPdButtonRect.Contains(mp))
        {
            ShowTip("跳过PD规划");
            return;
        }
    }

    /// <summary>
    /// 检查本地玩家是否装备"预见"饰品
    /// </summary>
    private bool HasForesightAccessory()
    {
        var localPlayer = GetLocalPlayer();
        return localPlayer?.HasForesightAccessory() ?? false;
    }

    /// <summary>
    /// 尝试自动执行预设的行动（预见饰品功能）
    /// 返回true表示成功执行了预设行动
    /// </summary>
    private bool TryExecutePlannedAction(string inputContext)
    {
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null || !localPlayer.HasForesightAccessory())
            return false;

        Dictionary<string, PlannedActionSequence> plannedActions = null;

        if (inputContext == "AttackSelection")
        {
            plannedActions = localPlayer.PlannedActionsAD;
        }
        else if (inputContext == "DefenseSelection")
        {
            plannedActions = localPlayer.PlannedActionsPD;
        }

        if (plannedActions == null || plannedActions.Count == 0)
            return false;

        // 查找第一个有待执行行动的骰子
        foreach (var kvp in plannedActions)
        {
            var sequence = kvp.Value;
            if (sequence.HasPendingActions)
            {
                var action = sequence.GetAndRemoveFirstAction();
                if (action != null)
                {
                    ExecutePlannedAction(action, inputContext);
                    UpdatePlannedActionSequenceNumbers();
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 执行一个预设的行动
    /// </summary>
    private void ExecutePlannedAction(PlannedAction action, string inputContext)
    {
        if (action == null)
            return;

        if (inputContext == "AttackSelection")
        {
            // 执行AD行动
            BattleActionRequested?.Invoke(action.DiceName, action.TargetPlayerId, action.CustomValue > 0 ? action.CustomValue : (int?)null);
            ShowTip($"自动执行AD预设: {action.DiceName}");
        }
        else if (inputContext == "DefenseSelection")
        {
            // 执行PD行动
            BattleDefenseRequested?.Invoke(action.DiceName, action.CustomValue > 0 ? action.CustomValue : (int?)null);
            ShowTip($"自动执行PD预设: {action.DiceName}");
        }
    }

    /// <summary>
    /// 更新规划行动的序号显示
    /// </summary>
    private void UpdatePlannedActionSequenceNumbers()
    {
        _plannedDiceSequenceNumbersAD.Clear();
        _plannedDiceSequenceNumbersPD.Clear();

        var localPlayer = GetLocalPlayer();
        if (localPlayer == null)
            return;

        foreach (var kvp in localPlayer.PlannedActionsAD)
        {
            _plannedDiceSequenceNumbersAD[kvp.Key] = kvp.Value.Actions.Count;
        }

        foreach (var kvp in localPlayer.PlannedActionsPD)
        {
            _plannedDiceSequenceNumbersPD[kvp.Key] = kvp.Value.Actions.Count;
        }
    }

    /// <summary>
    /// 为规划系统添加一个计划行动
    /// </summary>
    private void AddPlannedAction(string diceName, bool isAD, string targetPlayerId = null, int customValue = 0)
    {
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null)
            return;

        if (isAD)
        {
            localPlayer.AddPlannedActionAD(diceName, targetPlayerId, customValue);
        }
        else
        {
            localPlayer.AddPlannedActionPD(diceName, targetPlayerId, customValue);
        }

        UpdatePlannedActionSequenceNumbers();
    }

    /// <summary>
    /// 绘制"预见"饰品的双行骰子规划框
    /// </summary>
    private void DrawForesightPlannedActions(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int panelX, int panelWidth, int panelHeight, int barW, int barH, int barTop)
    {
        spriteBatch.Begin();

        int btnW = 110;
        int btnH = 40;
        int spacing = 10;
        int startX = panelX + 20;

        var localPlayer = GetLocalPlayer();
        if (localPlayer == null)
        {
            spriteBatch.End();
            return;
        }

        var displayDice = localPlayer.GetEquippedDice();
        
        // AD回合规划框（上行）
        int adDiceAreaY = panelHeight - 200;
        spriteBatch.Draw(texture, new Rectangle(panelX + 10, adDiceAreaY - 10, panelWidth - 20, 70), Color.DarkBlue * 0.4f);
        spriteBatch.DrawString(font, "AD规划（提前规划攻击防守）", new Vector2(panelX + 20, adDiceAreaY - 30), Color.Cyan);

        for (int i = 0; i < displayDice.Count; i++)
        {
            var rect = new Rectangle(startX + i * (btnW + spacing), adDiceAreaY, btnW, btnH);
            var dice = displayDice[i];
            string shortcutLabel = i < DiceShortcutRowPrimaryLabels.Length && i < GamePadSlotLabels.Length
                ? $"{DiceShortcutRowPrimaryLabels[i]} / {GamePadSlotLabels[i]}"
                : string.Empty;
            DrawShortcutLabel(spriteBatch, font, shortcutLabel, rect, Color.Cyan);
            Color c = Color.DarkSlateGray * 0.7f;
            spriteBatch.Draw(texture, rect, c);
            DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.Cyan, 2);

            // 绘制骰子图标
            int iconSize = 24;
            Rectangle iconRect = new Rectangle(rect.X + 6, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
            bool hasIcon = _iconProvider?.TryDrawIcon(spriteBatch, dice, iconRect, Color.White) ?? false;
            int textX = rect.X + 10 + (hasIcon ? iconSize + 6 : 0);
            spriteBatch.DrawString(font, dice.Name, new Vector2(textX, rect.Y + 8), Color.White);
            
            // 显示计数器（如果有）
            if (dice is ICounterDice counterDice)
            {
                string counterText = $"[{counterDice.Counter}]";
                spriteBatch.DrawString(font, counterText, new Vector2(textX, rect.Y + 26), Color.Orange, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }

            // 绘制序号
            if (_plannedDiceSequenceNumbersAD.ContainsKey(dice.Name) && _plannedDiceSequenceNumbersAD[dice.Name] > 0)
            {
                string sequenceText = "①②③④⑤⑥⑦⑧⑨⑩".Substring(0, Math.Min(_plannedDiceSequenceNumbersAD[dice.Name], 10));
                Vector2 seqSize = font.MeasureString(sequenceText);
                spriteBatch.DrawString(font, sequenceText, new Vector2(rect.Right - seqSize.X - 5, rect.Y + 5), Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }

        // PD回合规划框（下行）
        int pdDiceAreaY = panelHeight - 120;
        spriteBatch.Draw(texture, new Rectangle(panelX + 10, pdDiceAreaY - 10, panelWidth - 20, 70), Color.DarkGreen * 0.4f);
        spriteBatch.DrawString(font, "PD规划（提前规划防守响应）", new Vector2(panelX + 20, pdDiceAreaY - 30), Color.LimeGreen);

        for (int i = 0; i < displayDice.Count; i++)
        {
            var rect = new Rectangle(startX + i * (btnW + spacing), pdDiceAreaY, btnW, btnH);
            var dice = displayDice[i];
            string shortcutLabel = i < DiceShortcutRowSecondaryLabels.Length && i < GamePadSlotLabels.Length
                ? $"{DiceShortcutRowSecondaryLabels[i]} / {GamePadSlotLabels[i]}"
                : string.Empty;
            DrawShortcutLabel(spriteBatch, font, shortcutLabel, rect, Color.LimeGreen);
            Color c = Color.DarkSlateGray * 0.7f;
            spriteBatch.Draw(texture, rect, c);
            DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.LimeGreen, 2);

            // 绘制骰子图标
            int iconSize = 24;
            Rectangle iconRect = new Rectangle(rect.X + 6, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize);
            bool hasIcon = _iconProvider?.TryDrawIcon(spriteBatch, dice, iconRect, Color.White) ?? false;
            int textX = rect.X + 10 + (hasIcon ? iconSize + 6 : 0);
            spriteBatch.DrawString(font, dice.Name, new Vector2(textX, rect.Y + 8), Color.White);
            
            // 显示计数器（如果有）
            if (dice is ICounterDice counterDice)
            {
                string counterText = $"[{counterDice.Counter}]";
                spriteBatch.DrawString(font, counterText, new Vector2(textX, rect.Y + 26), Color.Orange, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }

            // 绘制序号
            if (_plannedDiceSequenceNumbersPD.ContainsKey(dice.Name) && _plannedDiceSequenceNumbersPD[dice.Name] > 0)
            {
                string sequenceText = "①②③④⑤⑥⑦⑧⑨⑩".Substring(0, Math.Min(_plannedDiceSequenceNumbersPD[dice.Name], 10));
                Vector2 seqSize = font.MeasureString(sequenceText);
                spriteBatch.DrawString(font, sequenceText, new Vector2(rect.Right - seqSize.X - 5, rect.Y + 5), Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }

        // AD框的跳过按钮
        int skipBtnW = 80;
        int skipBtnH = 30;
        _skipActionButtonRect = new Rectangle(panelX + panelWidth - skipBtnW - 20, adDiceAreaY + 20, skipBtnW, skipBtnH);
        DrawShortcutLabel(spriteBatch, font, "Space / RT", _skipActionButtonRect, Color.White);
        spriteBatch.Draw(texture, _skipActionButtonRect, Color.DimGray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _skipActionButtonRect, Color.White, 2);
        spriteBatch.DrawString(font, "跳过", new Vector2(_skipActionButtonRect.X + 15, _skipActionButtonRect.Y + 3), Color.White);

        // PD框的跳过按钮
        Rectangle skipPdButtonRect = new Rectangle(panelX + panelWidth - skipBtnW - 20, pdDiceAreaY + 20, skipBtnW, skipBtnH);
        string pdSkipLabel = GetPDSkipShortcutLabel();
        DrawShortcutLabel(spriteBatch, font, pdSkipLabel, skipPdButtonRect, Color.White);
        spriteBatch.Draw(texture, skipPdButtonRect, Color.DimGray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, skipPdButtonRect, Color.White, 2);
        spriteBatch.DrawString(font, "跳过", new Vector2(skipPdButtonRect.X + 15, skipPdButtonRect.Y + 3), Color.White);

        string undoHint = "Backspace / LT 撤回";
        Vector2 undoSize = font.MeasureString(undoHint) * ShortcutLabelScale;
        Vector2 undoPos = new Vector2(panelX + panelWidth - undoSize.X - 20, adDiceAreaY - 30);
        spriteBatch.DrawString(font, undoHint, undoPos, Color.White * 0.6f, 0f, Vector2.Zero, ShortcutLabelScale, SpriteEffects.None, 0f);

        spriteBatch.End();
    }
}

