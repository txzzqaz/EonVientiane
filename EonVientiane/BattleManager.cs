using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 战斗系统管理器，负责战斗逻辑、输入处理和绘制
/// </summary>
public class BattleManager
{
    private Battle _currentBattle;
    private int _battleLogScrollOffset = 0;
    private bool _isBattleLogOpen = true;
    private Rectangle _battleLogWindowRect;
    private Rectangle _battleLogToggleRect;
    private List<Rectangle> _diceButtonRects = new List<Rectangle>();
    private List<Dice> _diceButtons = new List<Dice>();
    private Rectangle _skipActionButtonRect;
    private List<(Player player, Rectangle rect)> _opponentRects = new List<(Player player, Rectangle rect)>();
    private Dice _pendingSelectedDice = null;

    private InventoryManager _inventoryManager;
    private int _menuWidth;

    public Battle CurrentBattle => _currentBattle;
    public bool IsBattleActive => _currentBattle != null && !_currentBattle.IsBattleOver;

    public BattleManager(InventoryManager inventoryManager, int menuWidth)
    {
        _inventoryManager = inventoryManager;
        _menuWidth = menuWidth;
    }

    /// <summary>
    /// 初始化战斗
    /// </summary>
    public void InitializeBattle()
    {
        _currentBattle = new Battle();
        _battleLogScrollOffset = 0;
        _isBattleLogOpen = true;
        _pendingSelectedDice = null;
        _diceButtonRects.Clear();
        _diceButtons.Clear();
        _opponentRects.Clear();

        // 创建玩家和电脑对手（1v1）
        var player = new Player("player", "玩家", PlayerCamp.Team1);
        var computer = new Player("computer", "电脑", PlayerCamp.Team2);

        // 玩家装备来源：当前背包已装备的道具
        SetupPlayerEquipmentFromInventory(player);

        // 电脑固定装备：D6、飞羽骰子、自我
        SetupComputerEquipment(computer);

        _currentBattle.AddPlayer(player);
        _currentBattle.AddPlayer(computer);

        _currentBattle.InitializeBattle();
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
    /// 为电脑配置默认装备：D6、飞羽骰子、自我
    /// </summary>
    private void SetupComputerEquipment(Player computer)
    {
        computer.EquippedItems.Clear();
        computer.AddEquipment(new D6Dice(DiceUsageType.Both));
        computer.AddEquipment(new FeatheredDice());
        computer.AddEquipment(new SelfAccessory());
    }

    /// <summary>
    /// 更新战斗逻辑
    /// </summary>
    public void Update()
    {
        if (_currentBattle != null && !_currentBattle.IsBattleOver)
        {
            _currentBattle.Update();
        }
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

        if (_currentBattle.IsWaitingForPlayerInput && _currentBattle.CurrentActionPlayer != null)
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
                    _currentBattle.SubmitPlayerAttackChoice(null, null);
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
                            _currentBattle.SubmitPlayerAttackChoice(dice, opponents.FirstOrDefault());
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
                    _currentBattle.SubmitPlayerDefenseChoice(null);
                    return;
                }
                for (int i = 0; i < _diceButtonRects.Count; i++)
                {
                    if (_diceButtonRects[i].Contains(mp))
                    {
                        var dice = _diceButtons[i];
                        _currentBattle.SubmitPlayerDefenseChoice(dice);
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
                    _currentBattle.SubmitPlayerAttackChoice(_pendingSelectedDice, player);
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
    /// 结束战斗
    /// </summary>
    public void EndBattle()
    {
        _currentBattle = null;
        _pendingSelectedDice = null;
        _diceButtonRects.Clear();
        _diceButtons.Clear();
        _opponentRects.Clear();
    }
}
