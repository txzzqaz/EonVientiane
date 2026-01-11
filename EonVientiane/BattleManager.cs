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
    
    // 临时提示相关字段
    private string _currentTip = string.Empty;
    private double _tipDurationMs = 0;  // 剩余显示时间（毫秒）
    private const double DEFAULT_TIP_DURATION = 3000;  // 3秒显示时间
    
    private List<Rectangle> _diceButtonRects = new List<Rectangle>();
    private List<Dice> _diceButtons = new List<Dice>();
    private Rectangle _skipActionButtonRect;
    private List<(Player player, Rectangle rect)> _opponentRects = new List<(Player player, Rectangle rect)>();
    private Dice _pendingSelectedDice = null;

    private InventoryManager _inventoryManager;
    private int _menuWidth;
    
    // 多人战斗相关
    private bool _isMultiplayerBattle = false;
    private string _localPlayerId;
    private BattleStateUpdateNotification _currentBattleState;
    
    // 多人战斗事件
    public event Action<string, string> BattleActionRequested; // (diceName, targetPlayerId)
    public event Action<string> BattleDefenseRequested; // (diceName)

    public Battle CurrentBattle => _currentBattle;
    public bool IsBattleActive => _currentBattle != null && !_currentBattle.IsBattleOver;
    public bool IsMultiplayerBattle => _isMultiplayerBattle;

    public BattleManager(InventoryManager inventoryManager, int menuWidth)
    {
        _inventoryManager = inventoryManager;
        _menuWidth = menuWidth;
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
            "飞羽骰子" => new FeatheredDice(),
            // 可以根据需要添加更多骰子类型
            _ => null
        };
    }

    /// <summary>
    /// 为电脑配置默认装备：D6、飞羽骰子、自我
    /// </summary>
    // 电脑自动操控已移除
    private void SetupComputerEquipment(Player computer) { }

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

        _battleLogToggleRect = new Rectangle(panelX + panelWidth - 90, 10, 80, 30);

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mp = new Point(mouseState.X, mouseState.Y);
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

        if (_currentBattle != null && _currentBattle.IsWaitingForPlayerInput)
        {
            HandleBattleActionInput(mouseState, previousMouseState, panelX, panelWidth, panelHeight);
        }
    }

    private void HandleBattleActionInput(MouseState mouseState, MouseState previousMouseState, int panelX, int panelWidth, int panelHeight)
    {
        int diceAreaY = panelHeight - 120;
        int btnW = 140;
        int btnH = 40;
        int spacing = 10;
        int startX = panelX + 20;

        _diceButtonRects.Clear();
        _diceButtons.Clear();

        if (_currentBattle.InputContext == BattleInputContext.AttackSelection)
        {
            var options = _currentBattle.AvailableActiveDice;
            for (int i = 0; i < options.Count; i++)
            {
                var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                _diceButtonRects.Add(rect);
                _diceButtons.Add(options[i]);
            }
            _skipActionButtonRect = new Rectangle(panelX + panelWidth - 120, diceAreaY, 100, btnH);

            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                Point mp = new Point(mouseState.X, mouseState.Y);
                if (_skipActionButtonRect.Contains(mp))
                {
                    _pendingSelectedDice = null;
                    // 仅多人战斗：发送到服务器
                    BattleActionRequested?.Invoke(null, null);
                    return;
                }

                for (int i = 0; i < _diceButtonRects.Count; i++)
                {
                    if (_diceButtonRects[i].Contains(mp))
                    {
                        var dice = _diceButtons[i];
                        var opponents = _currentBattle.AvailableOpponents;
                        if (opponents.Count <= 1)
                        {
                            // 仅多人战斗：发送到服务器
                            BattleActionRequested?.Invoke(dice.Name, opponents.FirstOrDefault()?.PlayerId);
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
            var options = _currentBattle.AvailablePassiveDice;
            for (int i = 0; i < options.Count; i++)
            {
                var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                _diceButtonRects.Add(rect);
                _diceButtons.Add(options[i]);
            }
            _skipActionButtonRect = new Rectangle(panelX + panelWidth - 120, diceAreaY, 100, btnH);

            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                Point mp = new Point(mouseState.X, mouseState.Y);
                if (_skipActionButtonRect.Contains(mp))
                {
                    // 仅多人战斗：发送到服务器
                    BattleDefenseRequested?.Invoke(null);
                    return;
                }
                for (int i = 0; i < _diceButtonRects.Count; i++)
                {
                    if (_diceButtonRects[i].Contains(mp))
                    {
                        var dice = _diceButtons[i];
                        // 仅多人战斗：发送到服务器
                        BattleDefenseRequested?.Invoke(dice.Name);
                        return;
                    }
                }
            }
        }
    }

    private void HandleOpponentSelection(MouseState mouseState, MouseState previousMouseState, int panelX, int panelWidth)
    {
        _opponentRects.Clear();
        var opponents = _currentBattle.AvailableOpponents;
        int barW = 300;
        int barH = 20;
        int topY = 60;
        var leftPlayer = _currentBattle.Team1Players.FirstOrDefault(p => !p.IsDead);
        var rightPlayer = _currentBattle.Team2Players.FirstOrDefault(p => !p.IsDead);

        if (leftPlayer != null && opponents.Contains(leftPlayer))
        {
            var rect = new Rectangle(panelX + 20 - 10, topY - 10, barW + 20, barH + 40);
            _opponentRects.Add((leftPlayer, rect));
        }
        if (rightPlayer != null && opponents.Contains(rightPlayer))
        {
            var rect = new Rectangle(panelX + panelWidth - 20 - barW - 10, topY - 10, barW + 20, barH + 40);
            _opponentRects.Add((rightPlayer, rect));
        }

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mp = new Point(mouseState.X, mouseState.Y);
            foreach (var (player, rect) in _opponentRects)
            {
                if (rect.Contains(mp))
                {
                    // 仅多人战斗：发送到服务器
                    BattleActionRequested?.Invoke(_pendingSelectedDice?.Name, player.PlayerId);
                    _pendingSelectedDice = null;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 绘制战斗面板
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, GraphicsDevice graphicsDevice, int panelWidth, int panelHeight)
    {
        if (_currentBattle == null)
            return;

        int panelX = _menuWidth;

        spriteBatch.Begin();

        spriteBatch.Draw(texture, new Rectangle(panelX, 0, panelWidth, panelHeight), Color.DarkSlateGray * 0.8f);

        spriteBatch.DrawString(font, "战斗进行中", new Vector2(panelX + 20, 10), Color.White);
        spriteBatch.DrawString(font, $"回合: {_currentBattle.CurrentRound}", new Vector2(panelX + 120, 10), Color.Yellow);

        int barW = 300;
        int barH = 20;
        int barTop = 60;

        var leftPlayer = _currentBattle.Team1Players.FirstOrDefault();
        var rightPlayer = _currentBattle.Team2Players.FirstOrDefault();

        DrawPlayerHealthBar(spriteBatch, texture, font, panelX, leftPlayer, barW, barH, barTop, true);
        DrawPlayerHealthBar(spriteBatch, texture, font, panelX + panelWidth, rightPlayer, barW, barH, barTop, false);

        _battleLogToggleRect = new Rectangle(panelX + panelWidth - 90, 10, 80, 30);
        spriteBatch.Draw(texture, _battleLogToggleRect, Color.DimGray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, texture, _battleLogToggleRect, Color.White, 2);
        string toggleText = _isBattleLogOpen ? "日志 ▾" : "日志 ▸";
        spriteBatch.DrawString(font, toggleText, new Vector2(_battleLogToggleRect.X + 10, _battleLogToggleRect.Y + 5), Color.White);

        spriteBatch.End();

        DrawBattleLog(spriteBatch, texture, font, graphicsDevice, panelX, panelWidth);

        DrawBattleActions(spriteBatch, texture, font, panelX, panelWidth, panelHeight, barW, barH, barTop);
        
        DrawTemporaryTip(spriteBatch, texture, font, panelX, panelWidth, panelHeight);
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

    private void DrawPlayerHealthBar(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, int xPosition, Player player, int barW, int barH, int barTop, bool isLeft)
    {
        if (player == null)
            return;

        if (isLeft)
        {
            var leftName = $"{player.PlayerName}";
            spriteBatch.DrawString(font, leftName, new Vector2(xPosition + 20, barTop - 28), Color.LightBlue);
            Rectangle hpBG = new Rectangle(xPosition + 20, barTop, barW, barH);
            spriteBatch.Draw(texture, hpBG, Color.Black * 0.5f);
            float pct = player.MaxHP > 0 ? Math.Clamp(player.CurrentHP / (float)player.MaxHP, 0f, 1f) : 0f;
            Rectangle hpFG = new Rectangle(xPosition + 20, barTop, (int)(barW * pct), barH);
            spriteBatch.Draw(texture, hpFG, Color.Green * 0.9f);
            spriteBatch.DrawString(font, $"{player.CurrentHP}/{player.MaxHP}", new Vector2(xPosition + 25, barTop + 24), Color.White);
            if (player.ShieldLayers > 0)
            {
                spriteBatch.DrawString(font, $"护盾:{player.ShieldLayers}", new Vector2(xPosition + 160, barTop + 24), Color.CornflowerBlue);
            }
        }
        else
        {
            var rightName = $"{player.PlayerName}";
            Vector2 nameSize = font.MeasureString(rightName);
            spriteBatch.DrawString(font, rightName, new Vector2(xPosition - 20 - nameSize.X, barTop - 28), Color.LightCoral);
            Rectangle hpBG = new Rectangle(xPosition - 20 - barW, barTop, barW, barH);
            spriteBatch.Draw(texture, hpBG, Color.Black * 0.5f);
            float pct = player.MaxHP > 0 ? Math.Clamp(player.CurrentHP / (float)player.MaxHP, 0f, 1f) : 0f;
            Rectangle hpFG = new Rectangle(xPosition - 20 - barW, barTop, (int)(barW * pct), barH);
            spriteBatch.Draw(texture, hpFG, Color.Green * 0.9f);
            Vector2 hpSize = font.MeasureString($"{player.CurrentHP}/{player.MaxHP}");
            spriteBatch.DrawString(font, $"{player.CurrentHP}/{player.MaxHP}", new Vector2(xPosition - 25 - hpSize.X, barTop + 24), Color.White);
            if (player.ShieldLayers > 0)
            {
                Vector2 sSize = font.MeasureString($"护盾:{player.ShieldLayers}");
                spriteBatch.DrawString(font, $"护盾:{player.ShieldLayers}", new Vector2(xPosition - 30 - sSize.X, barTop + 24), Color.CornflowerBlue);
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
                (_pendingSelectedDice == null ? "选择一个AD骰子进行攻击，或点击跳过" : "请选择攻击目标") :
                "选择一个PD骰子进行防御，或点击跳过";
            spriteBatch.DrawString(font, tip, new Vector2(panelX + 20, diceAreaY - 30), Color.White);

            int btnW = 140;
            int btnH = 40;
            int spacing = 10;
            int startX = panelX + 20;

            _diceButtonRects.Clear();
            _diceButtons.Clear();

            if (_currentBattle.InputContext == BattleInputContext.AttackSelection)
            {
                var options = _currentBattle.AvailableActiveDice;
                for (int i = 0; i < options.Count; i++)
                {
                    var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                    Color c = Color.DarkSlateGray * 0.9f;
                    spriteBatch.Draw(texture, rect, c);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.White, 2);
                    spriteBatch.DrawString(font, options[i].Name, new Vector2(rect.X + 10, rect.Y + 8), Color.White);
                    _diceButtonRects.Add(rect);
                    _diceButtons.Add(options[i]);
                }
            }
            else if (_currentBattle.InputContext == BattleInputContext.DefenseSelection)
            {
                var options = _currentBattle.AvailablePassiveDice;
                for (int i = 0; i < options.Count; i++)
                {
                    var rect = new Rectangle(startX + i * (btnW + spacing), diceAreaY, btnW, btnH);
                    Color c = Color.DarkSlateGray * 0.9f;
                    spriteBatch.Draw(texture, rect, c);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.White, 2);
                    spriteBatch.DrawString(font, options[i].Name, new Vector2(rect.X + 10, rect.Y + 8), Color.White);
                    _diceButtonRects.Add(rect);
                    _diceButtons.Add(options[i]);
                }
            }

            _skipActionButtonRect = new Rectangle(panelX + panelWidth - 120, diceAreaY, 100, btnH);
            spriteBatch.Draw(texture, _skipActionButtonRect, Color.DimGray * 0.9f);
            DrawingHelper.DrawRectangle(spriteBatch, texture, _skipActionButtonRect, Color.White, 2);
            spriteBatch.DrawString(font, "跳过", new Vector2(_skipActionButtonRect.X + 20, _skipActionButtonRect.Y + 8), Color.White);

            if (_pendingSelectedDice != null)
            {
                _opponentRects.Clear();
                var opponents = _currentBattle.AvailableOpponents;
                var leftPlayer = _currentBattle.Team1Players.FirstOrDefault(p => !p.IsDead);
                var rightPlayer = _currentBattle.Team2Players.FirstOrDefault(p => !p.IsDead);
                if (leftPlayer != null && opponents.Contains(leftPlayer))
                {
                    var rect = new Rectangle(panelX + 20 - 10, barTop - 10, barW + 20, barH + 40);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.Yellow, 3);
                    _opponentRects.Add((leftPlayer, rect));
                }
                if (rightPlayer != null && opponents.Contains(rightPlayer))
                {
                    var rect = new Rectangle(panelX + panelWidth - 20 - barW - 10, barTop - 10, barW + 20, barH + 40);
                    DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.Yellow, 3);
                    _opponentRects.Add((rightPlayer, rect));
                }
            }
        }

        if (_currentBattle.IsBattleOver)
        {
            string over = _currentBattle.WinnerCamp.HasValue ? $"{_currentBattle.WinnerCamp.Value} 获胜" : "战斗结束";
            Vector2 size = font.MeasureString(over);
            spriteBatch.DrawString(font, over, new Vector2(panelX + (panelWidth - size.X) / 2, barTop + 80), Color.Gold);
        }

        spriteBatch.End();
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
                string lastLog = state.NewBattleLogs[state.NewBattleLogs.Count - 1];
                ShowTip(lastLog, 2500);  // 显示2.5秒
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
    }
}
