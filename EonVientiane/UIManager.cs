using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// UI管理器 - 处理所有UI相关的渲染和逻辑
/// </summary>
public partial class UIManager
{
    private readonly int _menuWidth;
    private readonly int _buttonHeight;
    private readonly int _buttonMargin;
    private readonly GraphicsDeviceManager _graphics;
    private Texture2D _buttonTexture;
    private SpriteFont _buttonFont;
    private ItemIconProvider _iconProvider;
    
    public UIManager(int menuWidth, int buttonHeight, int buttonMargin, GraphicsDeviceManager graphics)
    {
        _menuWidth = menuWidth;
        _buttonHeight = buttonHeight;
        _buttonMargin = buttonMargin;
        _graphics = graphics;
    }
    
    public void SetTexture(Texture2D texture) => _buttonTexture = texture;
    public void SetFont(SpriteFont font) => _buttonFont = font;
    public void SetIconProvider(ItemIconProvider iconProvider) => _iconProvider = iconProvider;

    public LobbyLayout GetLobbyLayout()
    {
        int panelX = _menuWidth;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;

        int padding = 20;
        Rectangle panelRect = new Rectangle(panelX, 0, panelWidth, panelHeight);

        int inputCardHeight = 140;
        Rectangle inputCard = new Rectangle(panelX + padding, padding, panelWidth - padding * 2, inputCardHeight);

        int fieldWidth = 260;
        int fieldHeight = 42;
        int fieldStartX = inputCard.X + 18;
        int fieldStartY = inputCard.Y + 58;

        Rectangle roomNameRect = new Rectangle(fieldStartX, fieldStartY, fieldWidth, fieldHeight);

        int buttonWidth = 128;
        int buttonHeight = 44;
        int buttonSpacing = 12;
        int buttonRight = inputCard.Right - 18;
        Rectangle refreshButton = new Rectangle(buttonRight - buttonWidth, inputCard.Y + 18, buttonWidth, buttonHeight);
        Rectangle reconnectButton = new Rectangle(buttonRight - buttonWidth * 2 - buttonSpacing, inputCard.Y + 18, buttonWidth, buttonHeight);

        int actionY = inputCard.Bottom + 14;
        Rectangle createButton = new Rectangle(panelX + padding, actionY, buttonWidth, buttonHeight);
        Rectangle joinButton = new Rectangle(createButton.Right + buttonSpacing, actionY, buttonWidth, buttonHeight);
        Rectangle leaveButton = new Rectangle(joinButton.Right + buttonSpacing, actionY, buttonWidth, buttonHeight);
        Rectangle readyButton = new Rectangle(leaveButton.Right + buttonSpacing, actionY, buttonWidth, buttonHeight);
        Rectangle team1Button = new Rectangle(readyButton.Right + buttonSpacing, actionY, buttonWidth, buttonHeight);
        Rectangle team2Button = new Rectangle(team1Button.Right + buttonSpacing, actionY, buttonWidth, buttonHeight);

        int listTop = actionY + buttonHeight + 14;
        int listWidth = (int)(panelWidth * 0.45f);
        Rectangle roomListRect = new Rectangle(panelX + padding, listTop, listWidth, panelHeight - listTop - padding);
        Rectangle roomDetailRect = new Rectangle(roomListRect.Right + buttonSpacing, listTop,
            panelWidth - (roomListRect.Width + padding * 2 + buttonSpacing),
            roomListRect.Height);

        return new LobbyLayout
        {
            PanelRect = panelRect,
            InputCardRect = inputCard,
            RoomNameRect = roomNameRect,
            RefreshButtonRect = refreshButton,
            ReconnectButtonRect = reconnectButton,
            CreateButtonRect = createButton,
            JoinButtonRect = joinButton,
            LeaveButtonRect = leaveButton,
            ReadyButtonRect = readyButton,
            Team1ButtonRect = team1Button,
            Team2ButtonRect = team2Button,
            RoomListRect = roomListRect,
            RoomDetailRect = roomDetailRect,
            RoomRowHeight = 46,
            RoomHeaderHeight = 32
        };
    }
    
    /// <summary>
    /// 绘制内容面板
    /// </summary>
    public void DrawContentPanel(SpriteBatch spriteBatch, ContentView currentContentView)
    {
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;
        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

        spriteBatch.Begin();

        // 背景色依据视图不同略作区分
        Color bg = currentContentView switch
        {
            ContentView.Button1 => Color.DarkSlateGray * 0.6f,
            ContentView.Button2 => Color.DarkOliveGreen * 0.6f,
            ContentView.Button3 => Color.DarkCyan * 0.6f,
            ContentView.Button4 => Color.DimGray * 0.6f,
            ContentView.Button5 => Color.MidnightBlue * 0.6f,
            ContentView.Settings => Color.DarkSlateBlue * 0.6f,
            _ => Color.DarkSlateGray * 0.6f
        };

        spriteBatch.Draw(_buttonTexture, panelRect, bg);

        // 标题文字
        string title = currentContentView switch
        {
            ContentView.Button1 => "按钮1界面",
            ContentView.Button2 => "按钮2界面",
            ContentView.Button3 => "按钮3界面",
            ContentView.Button4 => "按钮4界面",
            ContentView.Button5 => "按钮5界面",
            ContentView.Settings => "设置",
            _ => "内容"
        };

        if (_buttonFont != null)
        {
            Vector2 titleSize = _buttonFont.MeasureString(title);
            Vector2 titlePos = new Vector2(panelX + 30, panelY + 30);
            spriteBatch.DrawString(_buttonFont, title, titlePos, Color.White);
        }

        spriteBatch.End();
    }

    public void DrawLobbyPanel(
        SpriteBatch spriteBatch,
        MultiplayerLobbyManager lobbyManager,
        LobbyLayout layout,
        string roomName,
        string selectedRoomId,
        LobbyInputField activeField)
    {
        spriteBatch.Begin();

        spriteBatch.Draw(_buttonTexture, layout.PanelRect, Color.DarkSlateGray * 0.65f);

        string title = "联机大厅";
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, title, new Vector2(layout.PanelRect.X + 26, layout.PanelRect.Y + 18), Color.White);
        }

        string status = lobbyManager.StatusMessage;
        Color statusColor = lobbyManager.State switch
        {
            LobbyState.Connecting => Color.LightBlue,
            LobbyState.InLobby => Color.LightGreen,
            LobbyState.InRoom => Color.Gold,
            _ => Color.LightGray
        };
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(
                _buttonFont,
                status,
                new Vector2(layout.PanelRect.Right - 350, layout.PanelRect.Y + 22),
                statusColor);
        }

        // 顶部输入区
        spriteBatch.Draw(_buttonTexture, layout.InputCardRect, Color.Black * 0.35f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, layout.InputCardRect, Color.Gray, 2);

        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, $"玩家: {lobbyManager.PlayerName}", new Vector2(layout.InputCardRect.X + 18, layout.InputCardRect.Y + 14), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_buttonFont, $"服务器: {lobbyManager.ServerHost}:{lobbyManager.ServerPort}", new Vector2(layout.InputCardRect.X + 18, layout.InputCardRect.Y + 42), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        DrawLabeledInput(spriteBatch, "房间名", roomName, layout.RoomNameRect, activeField == LobbyInputField.RoomName);

        DrawLobbyButton(spriteBatch, layout.RefreshButtonRect, "刷新列表", Color.DodgerBlue, lobbyManager.State == LobbyState.InLobby);
        
        // 重新连接按钮，仅在断开连接时显示
        bool showReconnect = lobbyManager.State == LobbyState.Disconnected;
        if (showReconnect)
        {
            DrawLobbyButton(spriteBatch, layout.ReconnectButtonRect, "重新连接", Color.Orange, true);
        }

        DrawLobbyButton(spriteBatch, layout.CreateButtonRect, "创建房间", Color.DarkCyan, lobbyManager.State == LobbyState.InLobby);
        bool canJoin = lobbyManager.State == LobbyState.InLobby && !string.IsNullOrEmpty(selectedRoomId);
        DrawLobbyButton(spriteBatch, layout.JoinButtonRect, "加入房间", Color.SteelBlue, canJoin);
        DrawLobbyButton(spriteBatch, layout.LeaveButtonRect, "离开房间", Color.DarkOrange, lobbyManager.State == LobbyState.InRoom);

        bool canReady = lobbyManager.State == LobbyState.InRoom;
        string readyText = lobbyManager.LocalReady ? "取消准备" : "准备";
        Color readyColor = lobbyManager.LocalReady ? Color.DarkOrange : Color.ForestGreen;
        DrawLobbyButton(spriteBatch, layout.ReadyButtonRect, readyText, readyColor, canReady);
        DrawLobbyButton(spriteBatch, layout.Team1ButtonRect, "队伍1", Color.CadetBlue, canReady);
        DrawLobbyButton(spriteBatch, layout.Team2ButtonRect, "队伍2", Color.MediumPurple, canReady);

        // 房间列表
        spriteBatch.Draw(_buttonTexture, layout.RoomListRect, Color.Black * 0.25f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, layout.RoomListRect, Color.DimGray, 1);

        if (_buttonFont != null)
        {
            Vector2 headerPos = new Vector2(layout.RoomListRect.X + 10, layout.RoomListRect.Y + 6);
            spriteBatch.DrawString(_buttonFont, "房间", headerPos, Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_buttonFont, "人数", new Vector2(layout.RoomListRect.X + layout.RoomListRect.Width - 160, layout.RoomListRect.Y + 6), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_buttonFont, "状态", new Vector2(layout.RoomListRect.X + layout.RoomListRect.Width - 80, layout.RoomListRect.Y + 6), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        int listY = layout.RoomListRect.Y + layout.RoomHeaderHeight;
        for (int i = 0; i < lobbyManager.RoomList.Count; i++)
        {
            var room = lobbyManager.RoomList[i];
            Rectangle rowRect = new Rectangle(layout.RoomListRect.X, listY + i * layout.RoomRowHeight, layout.RoomListRect.Width, layout.RoomRowHeight - 6);
            bool selected = room.RoomId == selectedRoomId;
            Color rowColor = selected ? Color.Goldenrod * 0.35f : Color.White * 0.05f;
            spriteBatch.Draw(_buttonTexture, rowRect, rowColor);

            if (_buttonFont != null)
            {
                spriteBatch.DrawString(_buttonFont, room.RoomName, new Vector2(rowRect.X + 10, rowRect.Y + 10), Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                string maxText = room.NoPlayerLimit ? "无限" : room.MaxPlayers.ToString();
                string playerCount = $"{room.CurrentPlayers}/{maxText}";
                spriteBatch.DrawString(_buttonFont, playerCount, new Vector2(rowRect.Right - 160, rowRect.Y + 10), Color.LightGreen, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_buttonFont, room.Status.ToString(), new Vector2(rowRect.Right - 80, rowRect.Y + 10), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            }
        }

        // 房间详情
        spriteBatch.Draw(_buttonTexture, layout.RoomDetailRect, Color.Black * 0.2f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, layout.RoomDetailRect, Color.DimGray, 1);

        if (_buttonFont != null)
        {
            string detailTitle = lobbyManager.State == LobbyState.InRoom ? "当前房间" : "房间详情";
            spriteBatch.DrawString(_buttonFont, detailTitle, new Vector2(layout.RoomDetailRect.X + 10, layout.RoomDetailRect.Y + 8), Color.White);

            int detailY = layout.RoomDetailRect.Y + 40;
            if (lobbyManager.CurrentRoom != null)
            {
                var room = lobbyManager.CurrentRoom;
                spriteBatch.DrawString(_buttonFont, $"房间名: {room.RoomName}", new Vector2(layout.RoomDetailRect.X + 10, detailY), Color.LightCyan, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                detailY += 26;
                spriteBatch.DrawString(_buttonFont, $"房主: {room.HostPlayerName}", new Vector2(layout.RoomDetailRect.X + 10, detailY), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                detailY += 26;
                string maxTextDetail = room.NoPlayerLimit ? "无限" : room.MaxPlayers.ToString();
                spriteBatch.DrawString(_buttonFont, $"人数: {room.CurrentPlayers}/{maxTextDetail}", new Vector2(layout.RoomDetailRect.X + 10, detailY), Color.LightGreen, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                detailY += 26;
                spriteBatch.DrawString(_buttonFont, $"状态: {room.Status}", new Vector2(layout.RoomDetailRect.X + 10, detailY), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                detailY += 26;

                if (room.CountdownEndTimeUtc.HasValue)
                {
                    int remaining = Math.Max(0, (int)Math.Ceiling((room.CountdownEndTimeUtc.Value - DateTime.UtcNow).TotalSeconds));
                    spriteBatch.DrawString(_buttonFont, $"倒计时: {remaining}秒", new Vector2(layout.RoomDetailRect.X + 10, detailY), Color.Orange, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                    detailY += 26;
                }

                detailY += 10;

                spriteBatch.DrawString(_buttonFont, "玩家列表:", new Vector2(layout.RoomDetailRect.X + 10, detailY), Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                detailY += 26;

                foreach (var player in lobbyManager.CurrentRoomPlayers)
                {
                    Color nameColor = player.IsHost ? Color.Gold : Color.White;
                    spriteBatch.DrawString(_buttonFont, player.PlayerName, new Vector2(layout.RoomDetailRect.X + 22, detailY), nameColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                    string teamLabel = player.TeamId > 0 ? $"[队伍{player.TeamId}]" : "[未分队]";
                    string flags = player.IsReady ? "[准备]" : "[未准备]";
                    spriteBatch.DrawString(_buttonFont, $"{teamLabel} {flags}", new Vector2(layout.RoomDetailRect.Right - 170, detailY), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                    detailY += 24;
                }
            }
            else
            {
                spriteBatch.DrawString(_buttonFont, "未选择房间。刷新或创建后，从左侧列表选择并点击加入即可查看详情。", new Vector2(layout.RoomDetailRect.X + 10, detailY), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.End();
    }

    private void DrawLabeledInput(SpriteBatch spriteBatch, string label, string value, Rectangle rect, bool active)
    {
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, label, new Vector2(rect.X, rect.Y - 22), Color.LightGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }

        Color bg = active ? Color.White : Color.White * 0.8f;
        spriteBatch.Draw(_buttonTexture, rect, bg);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, rect, active ? Color.DeepSkyBlue : Color.Gray, active ? 3 : 1);

        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, value, new Vector2(rect.X + 10, rect.Y + 10), Color.Black, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }
    }

    private void DrawLobbyButton(SpriteBatch spriteBatch, Rectangle rect, string text, Color fillColor, bool enabled)
    {
        Color bg = enabled ? fillColor : Color.Gray;
        spriteBatch.Draw(_buttonTexture, rect, bg * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, rect, Color.White, 2);

        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, text, new Vector2(rect.X + 12, rect.Y + 12), enabled ? Color.White : Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }
    }
    
    /// <summary>
    /// 绘制登录窗口
    /// </summary>
    public void DrawLoginWindow(SpriteBatch spriteBatch, LoginManager loginManager, InputField activeInputField, string statusMessage = "")
    {
        spriteBatch.Begin();
        
        // 绘制右侧面板区域
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;
        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        
        // 绘制面板背景
        spriteBatch.Draw(_buttonTexture, panelRect, Color.DarkSlateGray);
        
        // 登录表单区域（居中在右侧面板）
        int formWidth = Math.Min(500, panelWidth - 100);
        int formHeight = 450;
        int windowX = panelX + (panelWidth - formWidth) / 2;
        int windowY = panelY + (panelHeight - formHeight) / 2;
        Rectangle windowRect = new Rectangle(windowX, windowY, formWidth, formHeight);
        
        spriteBatch.Draw(_buttonTexture, windowRect, Color.Gray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, windowRect, Color.LightGray, 2);
        
        // 绘制标题
        string title = "账号登录";
        if (_buttonFont != null)
        {
            Vector2 titleSize = _buttonFont.MeasureString(title);
            Vector2 titlePos = new Vector2(windowX + (formWidth - titleSize.X) / 2, windowY + 30);
            spriteBatch.DrawString(_buttonFont, title, titlePos, Color.White);
        }
        
        // 提示文字
        string hint = "请输入您的账号信息";
        if (_buttonFont != null)
        {
            Vector2 hintSize = _buttonFont.MeasureString(hint);
            Vector2 hintPos = new Vector2(windowX + (formWidth - hintSize.X) / 2, windowY + 70);
            spriteBatch.DrawString(_buttonFont, hint, hintPos, Color.LimeGreen);
        }

        // 状态文本（如：登录中 / 登录失败 / 登录成功 等）
        if (!string.IsNullOrEmpty(statusMessage) && _buttonFont != null)
        {
            Color statusColor = Color.LightGray;
            string lower = statusMessage.ToLowerInvariant();
            if (lower.Contains("error") || lower.Contains("失败")) statusColor = Color.OrangeRed;
            else if (lower.Contains("成功") || lower.Contains("success")) statusColor = Color.LimeGreen;
            else if (lower.Contains("登录中") || lower.Contains("注册中") || lower.Contains("connecting")) statusColor = Color.Yellow;

            Vector2 statusSize = _buttonFont.MeasureString(statusMessage);
            Vector2 statusPos = new Vector2(windowX + (formWidth - statusSize.X) / 2, windowY + 100);
            spriteBatch.DrawString(_buttonFont, statusMessage, statusPos, statusColor);
        }
        
        // 绘制账号输入框标签
        string walletAddressLabel = "账号:";
        Vector2 walletAddressLabelPos = new Vector2(windowX + 40, windowY + 120);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, walletAddressLabel, walletAddressLabelPos, Color.White);
        
        // 绘制钱包地址输入框
        Rectangle walletAddressInputRect = new Rectangle(windowX + 40, windowY + 150, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, walletAddressInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, walletAddressInputRect, activeInputField == InputField.WalletAddress ? Color.Blue : Color.Black, activeInputField == InputField.WalletAddress ? 3 : 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, loginManager.WalletAddress, 
                new Vector2(walletAddressInputRect.X + 10, walletAddressInputRect.Y + 12), Color.Black);
        
        // 绘制光标（如果钱包地址输入框激活）
        if (activeInputField == InputField.WalletAddress)
        {
            int cursorX = walletAddressInputRect.X + 10 + (int)(_buttonFont?.MeasureString(loginManager.WalletAddress).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, walletAddressInputRect.Y + 10, 2, 28), Color.Black);
        }
        
        // 绘制密码输入框标签
        string privateKeyLabel = "密码:";
        Vector2 privateKeyLabelPos = new Vector2(windowX + 40, windowY + 210);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, privateKeyLabel, privateKeyLabelPos, Color.White);
        
        // 绘制私钥输入框
        Rectangle privateKeyInputRect = new Rectangle(windowX + 40, windowY + 240, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, privateKeyInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, privateKeyInputRect, activeInputField == InputField.PrivateKey ? Color.Blue : Color.Black, activeInputField == InputField.PrivateKey ? 3 : 2);
        string privateKeyDisplay = new string('*', loginManager.PrivateKey.Length);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, privateKeyDisplay, 
                new Vector2(privateKeyInputRect.X + 10, privateKeyInputRect.Y + 12), Color.Black);
        
        // 绘制光标（如果私钥输入框激活）
        if (activeInputField == InputField.PrivateKey)
        {
            int cursorX = privateKeyInputRect.X + 10 + (int)(_buttonFont?.MeasureString(privateKeyDisplay).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, privateKeyInputRect.Y + 10, 2, 28), Color.Black);
        }
        
        spriteBatch.End();
        
        // 绘制按钮
        DrawLoginButtons(spriteBatch, windowX, windowY, formWidth, formHeight);
    }

    /// <summary>
    /// 绘制注册窗口
    /// </summary>
    public void DrawRegistrationWindow(SpriteBatch spriteBatch, LoginManager loginManager, InputField activeInputField, string statusMessage = "")
    {
        spriteBatch.Begin();

        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;
        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

        spriteBatch.Draw(_buttonTexture, panelRect, Color.DarkSlateGray);

        int formWidth = Math.Min(500, panelWidth - 100);
        int formHeight = 420;
        int windowX = panelX + (panelWidth - formWidth) / 2;
        int windowY = panelY + (panelHeight - formHeight) / 2;
        Rectangle windowRect = new Rectangle(windowX, windowY, formWidth, formHeight);

        spriteBatch.Draw(_buttonTexture, windowRect, Color.Gray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, windowRect, Color.LightGray, 2);

        string title = "账号注册";
        if (_buttonFont != null)
        {
            Vector2 titleSize = _buttonFont.MeasureString(title);
            Vector2 titlePos = new Vector2(windowX + (formWidth - titleSize.X) / 2, windowY + 30);
            spriteBatch.DrawString(_buttonFont, title, titlePos, Color.White);
        }

        string hint = "✓ 快速创建账号";
        if (_buttonFont != null)
        {
            Vector2 hintSize = _buttonFont.MeasureString(hint);
            Vector2 hintPos = new Vector2(windowX + (formWidth - hintSize.X) / 2, windowY + 70);
            spriteBatch.DrawString(_buttonFont, hint, hintPos, Color.LimeGreen);
        }

        // 状态文本（注册中/错误/成功）
        if (!string.IsNullOrEmpty(statusMessage) && _buttonFont != null)
        {
            Color statusColor = Color.LightGray;
            string lower = statusMessage.ToLowerInvariant();
            if (lower.Contains("error") || lower.Contains("失败")) statusColor = Color.OrangeRed;
            else if (lower.Contains("成功") || lower.Contains("success")) statusColor = Color.LimeGreen;
            else if (lower.Contains("登录中") || lower.Contains("注册中") || lower.Contains("connecting")) statusColor = Color.Yellow;

            Vector2 statusSize = _buttonFont.MeasureString(statusMessage);
            Vector2 statusPos = new Vector2(windowX + (formWidth - statusSize.X) / 2, windowY + 100);
            spriteBatch.DrawString(_buttonFont, statusMessage, statusPos, statusColor);
        }

        // 账号
        string walletAddressLabel = "账号:";
        Vector2 walletAddressLabelPos = new Vector2(windowX + 40, windowY + 120);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, walletAddressLabel, walletAddressLabelPos, Color.White);
        Rectangle walletAddressInputRect = new Rectangle(windowX + 40, windowY + 150, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, walletAddressInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, walletAddressInputRect, activeInputField == InputField.WalletAddress ? Color.Blue : Color.Black, activeInputField == InputField.WalletAddress ? 3 : 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, loginManager.WalletAddress, new Vector2(walletAddressInputRect.X + 10, walletAddressInputRect.Y + 12), Color.Black);
        if (activeInputField == InputField.WalletAddress)
        {
            int cursorX = walletAddressInputRect.X + 10 + (int)(_buttonFont?.MeasureString(loginManager.WalletAddress).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, walletAddressInputRect.Y + 10, 2, 28), Color.Black);
        }

        // 密码
        string privateKeyLabel = "密码:";
        Vector2 privateKeyLabelPos = new Vector2(windowX + 40, windowY + 210);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, privateKeyLabel, privateKeyLabelPos, Color.White);
        Rectangle privateKeyInputRect = new Rectangle(windowX + 40, windowY + 240, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, privateKeyInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, privateKeyInputRect, activeInputField == InputField.PrivateKey ? Color.Blue : Color.Black, activeInputField == InputField.PrivateKey ? 3 : 2);
        string privateKeyDisplay = new string('*', loginManager.PrivateKey.Length);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, privateKeyDisplay, new Vector2(privateKeyInputRect.X + 10, privateKeyInputRect.Y + 12), Color.Black);
        if (activeInputField == InputField.PrivateKey)
        {
            int cursorX = privateKeyInputRect.X + 10 + (int)(_buttonFont?.MeasureString(privateKeyDisplay).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, privateKeyInputRect.Y + 10, 2, 28), Color.Black);
        }

        spriteBatch.End();

        // 按钮：提交注册 / 返回
        spriteBatch.Begin();
        int buttonWidth = 140;
        int buttonHeight = 45;
        int buttonY = windowY + formHeight - 80;
        int spacing = 20;
        int totalWidth = buttonWidth * 2 + spacing;
        int startX = windowX + (formWidth - totalWidth) / 2;

        Rectangle submitButtonRect = new Rectangle(startX, buttonY, buttonWidth, buttonHeight);
        spriteBatch.Draw(_buttonTexture, submitButtonRect, Color.SeaGreen);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, submitButtonRect, Color.White, 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, "提交注册", new Vector2(submitButtonRect.X + 15, submitButtonRect.Y + 10), Color.White);

        Rectangle backButtonRect = new Rectangle(startX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight);
        spriteBatch.Draw(_buttonTexture, backButtonRect, Color.SlateGray);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, backButtonRect, Color.White, 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, "返回登录", new Vector2(backButtonRect.X + 15, backButtonRect.Y + 10), Color.White);

        spriteBatch.End();
    }
    
    private void DrawLoginButtons(SpriteBatch spriteBatch, int windowX, int windowY, int formWidth, int formHeight)
    {
        spriteBatch.Begin();
        
        int buttonWidth = 120;
        int buttonHeight = 45;
        int buttonY = windowY + formHeight - 80;
        int spacing = 15;
        int totalWidth = buttonWidth * 3 + spacing * 2;
        int startX = windowX + (formWidth - totalWidth) / 2;

        // 注册按钮（最左）
        Rectangle registerButtonRect = new Rectangle(startX, buttonY, buttonWidth, buttonHeight);
        spriteBatch.Draw(_buttonTexture, registerButtonRect, Color.SteelBlue);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, registerButtonRect, Color.White, 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, "注册",
                new Vector2(registerButtonRect.X + 15, registerButtonRect.Y + 10), Color.White);

        // 登录按钮（中间）
        Rectangle loginButtonRect = new Rectangle(startX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight);
        spriteBatch.Draw(_buttonTexture, loginButtonRect, Color.Green);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, loginButtonRect, Color.White, 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, "登录", 
                new Vector2(loginButtonRect.X + 15, loginButtonRect.Y + 10), Color.White);

        // 取消按钮（最右）
        Rectangle cancelButtonRect = new Rectangle(startX + (buttonWidth + spacing) * 2, buttonY, buttonWidth, buttonHeight);
        spriteBatch.Draw(_buttonTexture, cancelButtonRect, Color.Red);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, cancelButtonRect, Color.White, 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, "取消", 
                new Vector2(cancelButtonRect.X + 15, cancelButtonRect.Y + 10), Color.White);
        
        spriteBatch.End();
    }
    
    /// <summary>
    /// 绘制用户信息窗口
    /// </summary>
    public void DrawUserProfileWindow(SpriteBatch spriteBatch, UserProfile currentUser)
    {
        spriteBatch.Begin();
        
        // 绘制右侧面板区域
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;
        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        
        // 绘制面板背景
        spriteBatch.Draw(_buttonTexture, panelRect, Color.DarkSlateBlue * 0.3f);
        
        // 用户信息卡片区域
        int cardWidth = Math.Min(600, panelWidth - 100);
        int cardHeight = 500;
        int windowX = panelX + 50;
        int windowY = panelY + 50;
        Rectangle windowRect = new Rectangle(windowX, windowY, cardWidth, cardHeight);
        
        spriteBatch.Draw(_buttonTexture, windowRect, Color.SlateGray * 0.95f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, windowRect, Color.LightBlue, 2);
        
        // 绘制标题区域
        Rectangle titleBar = new Rectangle(windowX, windowY, cardWidth, 60);
        spriteBatch.Draw(_buttonTexture, titleBar, Color.SteelBlue);
        
        string title = "用户信息";
        if (_buttonFont != null)
        {
            Vector2 titleSize = _buttonFont.MeasureString(title);
            Vector2 titlePos = new Vector2(windowX + 20, windowY + 20);
            spriteBatch.DrawString(_buttonFont, title, titlePos, Color.White);
        }
        
        // 绘制用户信息内容
        int contentY = windowY + 100;
        int lineHeight = 60;
        
        // 用户信息字段
        if (_buttonFont != null)
        {
            // 用户名
            spriteBatch.DrawString(_buttonFont, "用户名:", 
                new Vector2(windowX + 40, contentY), Color.LightGray);
            spriteBatch.DrawString(_buttonFont, currentUser?.Username ?? "", 
                new Vector2(windowX + 200, contentY), Color.White);
            
            // 邮箱
            spriteBatch.DrawString(_buttonFont, "邮箱:", 
                new Vector2(windowX + 40, contentY + lineHeight), Color.LightGray);
            spriteBatch.DrawString(_buttonFont, currentUser?.Email ?? "", 
                new Vector2(windowX + 200, contentY + lineHeight), Color.White);
            
            // 注册时间
            spriteBatch.DrawString(_buttonFont, "注册时间:", 
                new Vector2(windowX + 40, contentY + lineHeight * 2), Color.LightGray);
            spriteBatch.DrawString(_buttonFont, currentUser?.RegistrationDate.ToString("yyyy-MM-dd") ?? "", 
                new Vector2(windowX + 200, contentY + lineHeight * 2), Color.White);
            
            // 用户等级
            spriteBatch.DrawString(_buttonFont, "用户等级:", 
                new Vector2(windowX + 40, contentY + lineHeight * 3), Color.LightGray);
            spriteBatch.DrawString(_buttonFont, currentUser?.UserLevel ?? "", 
                new Vector2(windowX + 200, contentY + lineHeight * 3), Color.White);
        }
        
        spriteBatch.End();
        
        // 绘制按钮
        DrawUserProfileButtons(spriteBatch, windowX, windowY, cardWidth, cardHeight);
    }
    
    private void DrawUserProfileButtons(SpriteBatch spriteBatch, int windowX, int windowY, int cardWidth, int cardHeight)
    {
        spriteBatch.Begin();
        
        int buttonWidth = 120;
        int buttonHeight = 45;
        int buttonY = windowY + cardHeight - 70;
        int logoutButtonX = windowX + 40;
        
        // 注销按钮
        Rectangle logoutButtonRect = new Rectangle(logoutButtonX, buttonY, buttonWidth, buttonHeight);
        spriteBatch.Draw(_buttonTexture, logoutButtonRect, Color.OrangeRed);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, logoutButtonRect, Color.White, 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, "注销", 
                new Vector2(logoutButtonRect.X + 15, logoutButtonRect.Y + 10), Color.White);
        
        spriteBatch.End();
    }
    
    /// <summary>
    /// 绘制背包界面（按钮2）
    /// </summary>
    public void DrawInventoryPanel(SpriteBatch spriteBatch, InventoryManager inventoryManager, int? selectedInventoryIndex = null, int? selectedEquipmentIndex = null)
    {
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;
        
        spriteBatch.Begin();
        
        // 背景
        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_buttonTexture, panelRect, Color.DarkOliveGreen * 0.6f);
        
        // 标题
        string title = "背包与装备";
        if (_buttonFont != null)
        {
            Vector2 titleSize = _buttonFont.MeasureString(title);
            Vector2 titlePos = new Vector2(panelX + 30, panelY + 20);
            spriteBatch.DrawString(_buttonFont, title, titlePos, Color.White);
        }
        
        // 分隔线 - 左侧背包和右侧装备区域
        int dividerX = panelX + panelWidth / 2;
        spriteBatch.Draw(_buttonTexture, new Rectangle(dividerX - 1, panelY + 60, 2, panelHeight - 60), Color.Gray);
        
        spriteBatch.End();
        
        // 绘制左侧背包
        DrawInventorySection(spriteBatch, inventoryManager, panelX + 20, panelY + 70, panelWidth / 2 - 30, panelHeight - 80, selectedInventoryIndex);
        
        // 绘制右侧装备栏
        DrawEquipmentSection(spriteBatch, inventoryManager, dividerX + 10, panelY + 70, panelWidth / 2 - 20, panelHeight - 80, selectedEquipmentIndex);
    }
    
    /// <summary>
    /// 获取道具的计数器文本（如果有）
    /// </summary>
    private string GetItemCounterText(Item item)
    {
        // 检查骰子计数器
        if (item is ICounterDice counterDice)
        {
            return $"[{counterDice.Counter}]";
        }
        
        // 检查飞升之证
        if (item is AscensionProofAccessory ascensionProof)
        {
            return $"[{ascensionProof.Counter}]";
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// 绘制背包区域
    /// </summary>
    private void DrawInventorySection(SpriteBatch spriteBatch, InventoryManager inventoryManager, int x, int y, int width, int height, int? selectedIndex)
    {
        spriteBatch.Begin();
        
        // 背包标题
        string inventoryTitle = $"背包 ({inventoryManager.UsedSlots}" + 
            (inventoryManager.MaxCapacity > 0 ? $"/{inventoryManager.MaxCapacity}" : "") + ")";
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, inventoryTitle, new Vector2(x, y), Color.LightYellow);
        }
        
        spriteBatch.End();
        
        // 背包项目区域（带剪裁）
        int itemStartY = y + 40;
        int itemHeight = 60;
        int itemSpacing = 5;
        int availableHeight = height - 40;
        int maxVisibleItems = availableHeight / (itemHeight + itemSpacing);
        
        // 设置剪裁区域
        Rectangle scissorRect = new Rectangle(x, itemStartY, width, availableHeight);
        RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };
        
        spriteBatch.Begin(rasterizerState: rasterizerState);
        spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;
        
        // 绘制背包物品
        var items = inventoryManager.InventoryItems;
        int scrollOffset = inventoryManager.InventoryScrollOffset;
        
        for (int i = 0; i < items.Count; i++)
        {
            var stack = items[i];
            int itemY = itemStartY + i * (itemHeight + itemSpacing) - scrollOffset;
            
            // 只绘制可见区域内的物品
            if (itemY + itemHeight < itemStartY || itemY > itemStartY + availableHeight)
                continue;
            
            Rectangle itemRect = new Rectangle(x, itemY, width, itemHeight);
            
            // 背景色（选中高亮）
            Color bgColor = i == selectedIndex ? Color.Yellow * 0.3f : Color.DarkSlateGray * 0.8f;
            spriteBatch.Draw(_buttonTexture, itemRect, bgColor);
            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, itemRect, Color.Gray, 1);
            
            // 物品名称
            if (_buttonFont != null)
            {
                int iconSize = 40;
                Rectangle iconRect = new Rectangle(itemRect.X + 8, itemRect.Y + (itemRect.Height - iconSize) / 2, iconSize, iconSize);
                bool hasIcon = _iconProvider?.TryDrawIcon(spriteBatch, stack.Item, iconRect, Color.White) ?? false;
                int textX = itemRect.X + 10 + (hasIcon ? iconSize + 8 : 0);

                string itemName = stack.Item.Name;
                if (stack.Quantity > 1)
                    itemName += $" x{stack.Quantity}";
                
                spriteBatch.DrawString(_buttonFont, itemName, 
                    new Vector2(textX, itemRect.Y + 10), stack.Item.DisplayColor);
                
                // 物品描述（小字）
                string desc = stack.Item.Description;
                if (desc.Length > 30)
                    desc = desc.Substring(0, 27) + "...";
                spriteBatch.DrawString(_buttonFont, desc, 
                    new Vector2(textX, itemRect.Y + 35), Color.LightGray * 0.7f, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                
                // 装备类型标记和计数器
                if (stack.Item is Equipment equipment)
                {
                    string typeLabel = equipment.EquipmentType == EquipmentType.Dice ? "[骰子]" : "[饰品]";
                    spriteBatch.DrawString(_buttonFont, typeLabel, 
                        new Vector2(itemRect.X + width - 60, itemRect.Y + 10), Color.Cyan * 0.8f, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                    
                    // 显示计数器（如果有）
                    string counterText = GetItemCounterText(stack.Item);
                    if (!string.IsNullOrEmpty(counterText))
                    {
                        spriteBatch.DrawString(_buttonFont, counterText, 
                            new Vector2(itemRect.X + width - 60, itemRect.Y + 32), Color.Orange, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                    }
                }
            }
        }
        
        spriteBatch.End();
        
        // 绘制滚动条
        if (items.Count > maxVisibleItems)
        {
            spriteBatch.Begin();
            
            int totalHeight = items.Count * (itemHeight + itemSpacing);
            int scrollBarWidth = 8;
            int scrollBarX = x + width - scrollBarWidth - 2;
            float scrollBarThumbHeight = Math.Max(20, availableHeight * (availableHeight / (float)totalHeight));
            float maxScrollOffset = totalHeight - availableHeight;
            float scrollBarThumbY = itemStartY + (scrollOffset / maxScrollOffset) * (availableHeight - scrollBarThumbHeight);
            
            // 滚动条背景
            spriteBatch.Draw(_buttonTexture, 
                new Rectangle(scrollBarX, itemStartY, scrollBarWidth, availableHeight), 
                Color.Black * 0.3f);
            
            // 滚动条滑块
            spriteBatch.Draw(_buttonTexture, 
                new Rectangle(scrollBarX, (int)scrollBarThumbY, scrollBarWidth, (int)scrollBarThumbHeight), 
                Color.White * 0.6f);
            
            spriteBatch.End();
        }
    }
    
    /// <summary>
    /// 绘制装备区域
    /// </summary>
    private void DrawEquipmentSection(SpriteBatch spriteBatch, InventoryManager inventoryManager, int x, int y, int width, int height, int? selectedIndex)
    {
        spriteBatch.Begin();
        
        // 装备栏标题
        string equipmentTitle = $"已装备 ({inventoryManager.EquippedStacks.Count})";
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, equipmentTitle, new Vector2(x, y), Color.LightCyan);
            
            // 显示骰子数量和限制
            int diceCount = inventoryManager.EquippedDiceCount;
            int maxDice = inventoryManager.MaxEquippedDice;
            string diceInfo = $"骰子: {diceCount}/{maxDice}";
            Color diceColor = diceCount >= maxDice ? Color.Red : Color.LightGreen;
            spriteBatch.DrawString(_buttonFont, diceInfo, new Vector2(x + width - 120, y), diceColor, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            
            // 显示饰品槽位和限制
            int usedSlots = inventoryManager.UsedAccessorySlots;
            int maxSlots = inventoryManager.MaxAccessorySlots;
            string slotInfo = $"槽位: {usedSlots}/{maxSlots}";
            Color slotColor = usedSlots > maxSlots ? Color.Red : Color.LightYellow;
            spriteBatch.DrawString(_buttonFont, slotInfo, new Vector2(x + width - 120, y + 20), slotColor, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
        }
        
        // 装备槽位
        int slotStartY = y + 40;
        int slotHeight = 70;
        int slotSpacing = 5;
        
        var equippedItems = inventoryManager.EquippedStacks;
        
        for (int i = 0; i < equippedItems.Count; i++)
        {
            var equipmentStack = equippedItems[i];
            var equipment = equipmentStack.Item as Equipment;
            if (equipment == null)
            {
                continue;
            }
            int slotY = slotStartY + i * (slotHeight + slotSpacing);
            
            // 如果超出可见范围，停止绘制
            if (slotY + slotHeight > y + height)
                break;
            
            Rectangle slotRect = new Rectangle(x, slotY, width, slotHeight);
            
            // 背景色（选中高亮）
            Color bgColor = i == selectedIndex ? Color.Yellow * 0.3f : Color.DarkGreen * 0.8f;
            
            spriteBatch.Draw(_buttonTexture, slotRect, bgColor);
            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, slotRect, Color.Gray, 1);
            
            if (_buttonFont != null)
            {
                int iconSize = 46;
                Rectangle iconRect = new Rectangle(slotRect.X + 8, slotRect.Y + (slotRect.Height - iconSize) / 2, iconSize, iconSize);
                bool hasIcon = _iconProvider?.TryDrawIcon(spriteBatch, equipment, iconRect, Color.White) ?? false;
                int textX = slotRect.X + 10 + (hasIcon ? iconSize + 8 : 0);

                // 装备类型标签
                string typeLabel = equipment.EquipmentType == EquipmentType.Dice ? "[骰子]" : "[饰品]";
                spriteBatch.DrawString(_buttonFont, typeLabel, 
                    new Vector2(textX, slotRect.Y + 8), Color.LightGray, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                
                // 装备名称
                spriteBatch.DrawString(_buttonFont, equipment.Name, 
                    new Vector2(textX, slotRect.Y + 28), equipment.DisplayColor);
                
                // 装备属性
                string equipStats = equipment.GetStatsDescription();
                if (!string.IsNullOrEmpty(equipStats))
                {
                    spriteBatch.DrawString(_buttonFont, equipStats, 
                        new Vector2(textX, slotRect.Y + 48), Color.LightGreen, 0f, Vector2.Zero, 0.6f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                }
                
                // 显示计数器（如果有）
                string counterText = GetItemCounterText(equipment);
                if (!string.IsNullOrEmpty(counterText))
                {
                    int counterY = string.IsNullOrEmpty(equipStats) ? slotRect.Y + 48 : slotRect.Y + 62;
                    spriteBatch.DrawString(_buttonFont, counterText, 
                        new Vector2(textX, counterY), Color.Orange, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                }
            }
        }
        
        // 如果没有装备任何物品
        if (equippedItems.Count == 0)
        {
            if (_buttonFont != null)
            {
                string emptyText = "未装备任何物品";
                Vector2 textSize = _buttonFont.MeasureString(emptyText);
                spriteBatch.DrawString(_buttonFont, emptyText, 
                    new Vector2(x + (width - textSize.X) / 2, slotStartY + 50), Color.Gray);
            }
        }
        
        // 总属性加成
        var stats = inventoryManager.GetTotalStats();
        int statsY = slotStartY + Math.Max(equippedItems.Count, 1) * (slotHeight + slotSpacing) + 20;
        
        if (_buttonFont != null && statsY < y + height - 100)
        {
            spriteBatch.DrawString(_buttonFont, "总属性:", new Vector2(x, statsY), Color.Gold);
            statsY += 25;
            
            if (stats.attack > 0)
            {
                spriteBatch.DrawString(_buttonFont, $"攻击: +{stats.attack}", 
                    new Vector2(x + 10, statsY), Color.Red, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                statsY += 20;
            }
            if (stats.defense > 0)
            {
                spriteBatch.DrawString(_buttonFont, $"防御: +{stats.defense}", 
                    new Vector2(x + 10, statsY), Color.Blue, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                statsY += 20;
            }
            if (stats.speed > 0)
            {
                spriteBatch.DrawString(_buttonFont, $"速度: +{stats.speed}", 
                    new Vector2(x + 10, statsY), Color.Yellow, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                statsY += 20;
            }
            if (stats.health > 0)
            {
                spriteBatch.DrawString(_buttonFont, $"生命: +{stats.health}", 
                    new Vector2(x + 10, statsY), Color.Green, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                statsY += 20;
            }
            if (stats.mana > 0)
            {
                spriteBatch.DrawString(_buttonFont, $"魔力: +{stats.mana}", 
                    new Vector2(x + 10, statsY), Color.Purple, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }
        }
        
        spriteBatch.End();
    }

    /// <summary>
    /// 绘制成就面板
    /// </summary>
    public void DrawAchievementPanel(SpriteBatch spriteBatch, AchievementSystem achievementSystem, int scrollOffset = 0, int? selectedAchievementIndex = null)
    {
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;

        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

        spriteBatch.Begin();

        // 背景
        spriteBatch.Draw(_buttonTexture, panelRect, Color.DimGray * 0.6f);

        // 标题
        string title = "成就系统";
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, title, new Vector2(panelX + 30, panelY + 20), Color.Gold, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        }

        // 获取成就完成度统计
        var achievements = achievementSystem.GetAllAchievements();
        var stats = achievementSystem.GetCompletionStats();

        // 绘制进度条区域背景
        int progressAreaY = panelY + 60;
        int progressAreaHeight = 80;
        spriteBatch.Draw(_buttonTexture, 
            new Rectangle(panelX + 20, progressAreaY, panelWidth - 40, progressAreaHeight),
            Color.Black * 0.3f);

        // 绘制进度条
        int progressBarX = panelX + 30;
        int progressBarY = progressAreaY + 15;
        int progressBarWidth = panelWidth - 60;
        int progressBarHeight = 30;

        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, 
            new Rectangle(progressBarX, progressBarY, progressBarWidth, progressBarHeight), 
            Color.Gold, 2);

        // 绘制进度填充
        int filledWidth = (int)(progressBarWidth * (stats.percentage / 100f));
        Color fillColor = stats.percentage >= 100 ? Color.LimeGreen : Color.Lerp(Color.SteelBlue, Color.LimeGreen, stats.percentage / 100f);
        spriteBatch.Draw(_buttonTexture, 
            new Rectangle(progressBarX + 2, progressBarY + 2, filledWidth - 4, progressBarHeight - 4),
            fillColor * 0.8f);

        // 绘制进度文字
        if (_buttonFont != null)
        {
            string progressText = $"{stats.completed}/{stats.total}";
            Vector2 textSize = _buttonFont.MeasureString(progressText);
            spriteBatch.DrawString(_buttonFont, progressText,
                new Vector2(progressBarX + progressBarWidth / 2 - textSize.X / 2, progressBarY + 6),
                Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

            // 进度百分比
            string percentText = $"({stats.percentage:F1}%)";
            Vector2 percentSize = _buttonFont.MeasureString(percentText);
            spriteBatch.DrawString(_buttonFont, percentText,
                new Vector2(progressBarX + progressBarWidth - percentSize.X - 10, progressBarY + 6),
                Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();

        // 绘制成就列表 - 使用剪裁区域和滚动
        int achievementStartY = progressAreaY + progressAreaHeight + 20;
        int achievementItemHeight = 90;
        int achievementSpacing = 12;
        int listAreaHeight = panelHeight - achievementStartY - 30;

        // 设置剪裁区域
        Rectangle scissorRect = new Rectangle(panelX + 20, achievementStartY, panelWidth - 40, listAreaHeight);
        Rectangle originalScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, rasterizerState);
        spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

        // 添加"没有成就"提示
        if (achievements.Count == 0)
        {
            if (_buttonFont != null)
            {
                string noAchievementsText = "暂无成就数据，请登录后重试";
                Vector2 textSize = _buttonFont.MeasureString(noAchievementsText);
                spriteBatch.DrawString(_buttonFont, noAchievementsText,
                    new Vector2(panelX + panelWidth / 2 - textSize.X / 2, achievementStartY + 50),
                    Color.Gray, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            }
        }
        else
        {
            // 计算起始索引和结束索引
            int startIndex = scrollOffset / (achievementItemHeight + achievementSpacing);
            int endIndex = Math.Min(achievements.Count, startIndex + (listAreaHeight / (achievementItemHeight + achievementSpacing)) + 2);

            for (int i = startIndex; i < endIndex; i++)
            {
                var achievement = achievements[i];
                int itemY = achievementStartY + i * (achievementItemHeight + achievementSpacing) - scrollOffset;

                // 只绘制在可视区域内的成就
                if (itemY + achievementItemHeight > achievementStartY && itemY < achievementStartY + listAreaHeight)
                {
                    bool isSelected = selectedAchievementIndex.HasValue && selectedAchievementIndex.Value == i;
                    DrawAchievementItem(spriteBatch, panelX + 20, itemY, panelWidth - 40, achievementItemHeight, achievement, isSelected);
                }
            }
        }

        spriteBatch.End();
        spriteBatch.GraphicsDevice.ScissorRectangle = originalScissor;

        // 绘制详情面板
        if (selectedAchievementIndex.HasValue && selectedAchievementIndex.Value >= 0 && selectedAchievementIndex.Value < achievements.Count)
        {
            DrawAchievementDetail(spriteBatch, achievements[selectedAchievementIndex.Value], panelX + 30, panelHeight - 280, panelWidth - 60);
        }
    }

    /// <summary>
    /// 绘制成就详情面板
    /// </summary>
    private void DrawAchievementDetail(SpriteBatch spriteBatch, AchievementSystem.Achievement achievement, int x, int y, int width)
    {
        int detailHeight = 260;
        Rectangle detailRect = new Rectangle(x, y, width, detailHeight);

        spriteBatch.Begin();

        // 详情面板背景
        Color bgColor = achievement.IsCompleted ? Color.DarkGreen * 0.5f : Color.DarkSlateBlue * 0.5f;
        spriteBatch.Draw(_buttonTexture, detailRect, bgColor);
        Color borderColor = achievement.IsCompleted ? Color.Gold : Color.SteelBlue;
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, detailRect, borderColor, 3);

        if (_buttonFont != null)
        {
            int detailX = x + 20;
            int detailY = y + 15;
            int lineHeight = 30;

            // 详情标题
            spriteBatch.DrawString(_buttonFont, "成就详情", new Vector2(detailX, detailY), Color.Gold, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);
            detailY += 40;

            // 成就名称
            Color nameColor = achievement.IsCompleted ? Color.Gold : Color.White;
            spriteBatch.DrawString(_buttonFont, $"名称: {achievement.Name}", 
                new Vector2(detailX, detailY), nameColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 成就描述
            spriteBatch.DrawString(_buttonFont, $"描述: {achievement.Description}", 
                new Vector2(detailX, detailY), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 提示信息
            if (!string.IsNullOrEmpty(achievement.LockedHint))
            {
                spriteBatch.DrawString(_buttonFont, $"提示: {achievement.LockedHint}", 
                    new Vector2(detailX, detailY), Color.LightYellow, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                detailY += lineHeight;
            }

            // 解锁方法 - 仅在已解锁时显示
            if (achievement.IsCompleted && !string.IsNullOrEmpty(achievement.UnlockedHint))
            {
                spriteBatch.DrawString(_buttonFont, $"解锁方法: {achievement.UnlockedHint}", 
                    new Vector2(detailX, detailY), Color.LimeGreen, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                detailY += lineHeight;
            }
            else if (!achievement.IsCompleted)
            {
                spriteBatch.DrawString(_buttonFont, "解锁方法: ??? (完成后显示)", 
                    new Vector2(detailX, detailY), Color.DarkGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                detailY += lineHeight;
            }

            // 进度信息
            string progressInfo = $"进度: {achievement.Progress}/{achievement.RequiredProgress}";
            Color progressColor = achievement.IsCompleted ? Color.LimeGreen : Color.White;
            spriteBatch.DrawString(_buttonFont, progressInfo, 
                new Vector2(detailX, detailY), progressColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 完成状态和时间
            if (achievement.IsCompleted && achievement.CompletedTime.HasValue)
            {
                string completedInfo = $"完成时间: {achievement.CompletedTime.Value:yyyy-MM-dd HH:mm:ss}";
                spriteBatch.DrawString(_buttonFont, completedInfo, 
                    new Vector2(detailX, detailY), Color.PaleGoldenrod, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            }
            else
            {
                spriteBatch.DrawString(_buttonFont, "状态: 未完成", 
                    new Vector2(detailX, detailY), Color.Orange, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.End();
    }

    /// <summary>
    /// 绘制单个成就条目
    /// </summary>
    private void DrawAchievementItem(SpriteBatch spriteBatch, int x, int y, int width, int height, AchievementSystem.Achievement achievement, bool isSelected = false)
    {
        // 背景框 - 根据完成状态和选中状态使用不同颜色
        Color bgColor;
        if (isSelected)
        {
            bgColor = achievement.IsCompleted ? Color.Green * 0.5f : Color.SlateBlue * 0.5f;
        }
        else
        {
            bgColor = achievement.IsCompleted ? Color.DarkGreen * 0.4f : Color.DarkSlateGray * 0.4f;
        }
        spriteBatch.Draw(_buttonTexture, new Rectangle(x, y, width, height), bgColor);

        // 左侧完成状态条
        int statusBarWidth = 5;
        Color statusColor = achievement.IsCompleted ? Color.LimeGreen : Color.SteelBlue;
        spriteBatch.Draw(_buttonTexture, new Rectangle(x, y, statusBarWidth, height), statusColor);

        // 边框 - 选中时使用更亮的颜色
        Color borderColor;
        if (isSelected)
        {
            borderColor = Color.Gold;
        }
        else
        {
            borderColor = achievement.IsCompleted ? Color.LimeGreen : Color.DarkGray;
        }
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, new Rectangle(x, y, width, height), borderColor, isSelected ? 2 : 1);

        int contentX = x + 15;
        int contentY = y + 8;
        int contentWidth = width - 30;

        if (_buttonFont != null)
        {
            // 成就名称 - 更大的字体
            Color nameColor = achievement.IsCompleted ? Color.Gold : Color.White;
            spriteBatch.DrawString(_buttonFont, achievement.Name, 
                new Vector2(contentX, contentY), nameColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

            // 成就描述 - 根据完成状态显示不同的文本
            string displayText;
            if (achievement.IsCompleted)
            {
                // 已完成：显示解锁方式（如果有），否则显示原描述
                displayText = !string.IsNullOrEmpty(achievement.UnlockedHint) 
                    ? achievement.UnlockedHint 
                    : achievement.Description;
            }
            else
            {
                // 未完成：显示提示文本（如果有），否则显示原描述
                displayText = !string.IsNullOrEmpty(achievement.LockedHint) 
                    ? achievement.LockedHint 
                    : achievement.Description;
            }

            Color descColor = achievement.IsCompleted ? Color.PaleGoldenrod : Color.LightGray;
            spriteBatch.DrawString(_buttonFont, displayText,
                new Vector2(contentX, contentY + 22), descColor, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            // 进度条
            int progressBarX = contentX;
            int progressBarY = contentY + 45;
            int progressBarWidth = contentWidth - 80;
            int progressBarHeight = 14;

            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture,
                new Rectangle(progressBarX, progressBarY, progressBarWidth, progressBarHeight),
                Color.Black, 1);

            // 计算填充宽度
            float progressPercent = achievement.RequiredProgress > 0 
                ? achievement.Progress / (float)achievement.RequiredProgress 
                : 0;
            int filledWidth = (int)(progressBarWidth * progressPercent);
            
            Color progressColor = achievement.IsCompleted ? Color.LimeGreen : Color.SteelBlue;
            if (filledWidth > 0)
            {
                spriteBatch.Draw(_buttonTexture,
                    new Rectangle(progressBarX + 1, progressBarY + 1, Math.Max(1, filledWidth - 2), progressBarHeight - 2),
                    progressColor);
            }

            // 进度文字 - 在进度条旁边
            string progressText = $"{achievement.Progress}/{achievement.RequiredProgress}";
            Vector2 progressTextSize = _buttonFont.MeasureString(progressText);
            Color progressTextColor = achievement.IsCompleted ? Color.LimeGreen : Color.White;
            spriteBatch.DrawString(_buttonFont, progressText,
                new Vector2(progressBarX + progressBarWidth + 10, progressBarY + 1),
                progressTextColor, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            // 完成标记和时间
            if (achievement.IsCompleted && achievement.CompletedTime.HasValue)
            {
                string completedText = "✓ 已完成";
                Vector2 completedSize = _buttonFont.MeasureString(completedText);
                spriteBatch.DrawString(_buttonFont, completedText,
                    new Vector2(contentX + contentWidth - completedSize.X, contentY + 50),
                    Color.LimeGreen, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

                // 显示完成时间
                string completedDate = achievement.CompletedTime.Value.ToString("yyyy-MM-dd");
                Vector2 dateSize = _buttonFont.MeasureString(completedDate);
                spriteBatch.DrawString(_buttonFont, completedDate,
                    new Vector2(contentX + contentWidth - dateSize.X, contentY + 65),
                    Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
    }

    /// <summary>
    /// 绘制对战历史面板（按钮3）
    /// </summary>
    public void DrawBattleHistoryPanel(SpriteBatch spriteBatch, BattleHistoryManager battleHistoryManager, 
        string currentPlayerName, int scrollOffset, int? selectedRecordIndex)
    {
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;

        spriteBatch.Begin();

        // 背景
        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_buttonTexture, panelRect, Color.DarkCyan * 0.6f);

        // 标题
        string title = "对战历史";
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, title, new Vector2(panelX + 30, panelY + 20), Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        }

        // 获取当前玩家的对战记录
        List<BattleRecord> records = string.IsNullOrEmpty(currentPlayerName) 
            ? battleHistoryManager.GetAllBattleRecords() 
            : battleHistoryManager.GetBattleRecordsByPlayer(currentPlayerName);

        // 显示统计信息
        int statsY = panelY + 60;
        if (_buttonFont != null && !string.IsNullOrEmpty(currentPlayerName))
        {
            var (totalBattles, wins, losses, draws) = battleHistoryManager.GetPlayerStats(currentPlayerName);
            spriteBatch.DrawString(_buttonFont, $"玩家: {currentPlayerName}", 
                new Vector2(panelX + 30, statsY), Color.LightYellow);
            
            string statsText = $"总场次: {totalBattles} | 胜利: {wins} | 失败: {losses} | 平手: {draws}";
            spriteBatch.DrawString(_buttonFont, statsText, 
                new Vector2(panelX + 30, statsY + 30), Color.LightCyan, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        // 列表标题行
        int listHeaderY = panelY + 120;
        spriteBatch.Draw(_buttonTexture, new Rectangle(panelX + 20, listHeaderY, panelWidth - 40, 30), Color.DarkSlateGray);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, new Rectangle(panelX + 20, listHeaderY, panelWidth - 40, 30), Color.Gray, 1);
        
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, "时间", new Vector2(panelX + 30, listHeaderY + 6), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_buttonFont, "对手", new Vector2(panelX + 200, listHeaderY + 6), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_buttonFont, "结果", new Vector2(panelX + 450, listHeaderY + 6), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_buttonFont, "等级", new Vector2(panelX + 600, listHeaderY + 6), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();

        // 绘制对战记录列表（带剪裁）
        DrawBattleRecordsList(spriteBatch, panelX + 20, listHeaderY + 35, panelWidth - 40, panelHeight - (listHeaderY + 35 + 40),
            records, scrollOffset, selectedRecordIndex);

        // 绘制详情面板
        if (selectedRecordIndex.HasValue && selectedRecordIndex.Value >= 0 && selectedRecordIndex.Value < records.Count)
        {
            DrawBattleRecordDetail(spriteBatch, records[selectedRecordIndex.Value], panelX + 30, panelHeight - 200, panelWidth - 60);
        }
    }

    /// <summary>
    /// 绘制对战记录列表
    /// </summary>
    private void DrawBattleRecordsList(SpriteBatch spriteBatch, int x, int y, int width, int height, 
        List<BattleRecord> records, int scrollOffset, int? selectedIndex)
    {
        const int recordHeight = 50;
        const int recordSpacing = 5;
        int maxVisibleRecords = height / (recordHeight + recordSpacing);

        // 设置剪裁区域
        Rectangle scissorRect = new Rectangle(x, y, width, height);
        RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };

        spriteBatch.Begin(rasterizerState: rasterizerState);
        spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

        for (int i = 0; i < records.Count; i++)
        {
            int recordY = y + i * (recordHeight + recordSpacing) - scrollOffset;

            if (recordY + recordHeight < y || recordY > y + height)
                continue;

            var record = records[i];
            Rectangle recordRect = new Rectangle(x, recordY, width, recordHeight);

            // 背景色（选中高亮）
            Color bgColor = i == selectedIndex ? Color.Gold * 0.3f : Color.DarkSlateGray * 0.8f;
            spriteBatch.Draw(_buttonTexture, recordRect, bgColor);
            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, recordRect, Color.Gray, 1);

            if (_buttonFont != null)
            {
                // 对战时间
                string timeText = record.BattleDateTime.ToString("MM-dd HH:mm");
                spriteBatch.DrawString(_buttonFont, timeText, new Vector2(recordRect.X + 10, recordRect.Y + 6), Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                // 对手名称
                string opponentText = record.OpponentName ?? "未知";
                spriteBatch.DrawString(_buttonFont, opponentText, new Vector2(recordRect.X + 200, recordRect.Y + 6), Color.LightCyan, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                // 对战结果
                string resultText = BattleHistoryManager.GetResultDescription(record.Result);
                Color resultColor = BattleHistoryManager.GetResultColor(record.Result);
                spriteBatch.DrawString(_buttonFont, resultText, new Vector2(recordRect.X + 450, recordRect.Y + 6), resultColor, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                // 等级信息
                string levelText = $"Lv.{record.OpponentLevel}";
                spriteBatch.DrawString(_buttonFont, levelText, new Vector2(recordRect.X + 600, recordRect.Y + 6), Color.LightYellow, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                // 底部显示赢家
                string winnerText = string.IsNullOrEmpty(record.WinnerName) ? "平手" : $"{record.WinnerName}胜";
                Color winnerColor = record.Result == 1 ? Color.LimeGreen : (record.Result == 0 ? Color.OrangeRed : Color.Yellow);
                spriteBatch.DrawString(_buttonFont, winnerText, new Vector2(recordRect.X + 10, recordRect.Y + 28), winnerColor, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.End();

        // 绘制滚动条
        if (records.Count > maxVisibleRecords)
        {
            int totalHeight = records.Count * (recordHeight + recordSpacing);
            int scrollBarWidth = 8;
            int scrollBarX = x + width - scrollBarWidth - 2;
            float scrollBarThumbHeight = Math.Max(20, height * (height / (float)totalHeight));
            float maxScrollOffset = totalHeight - height;
            float scrollBarThumbY = y + (scrollOffset / maxScrollOffset) * (height - scrollBarThumbHeight);

            spriteBatch.Begin();

            // 滚动条背景
            spriteBatch.Draw(_buttonTexture, new Rectangle(scrollBarX, y, scrollBarWidth, height), Color.Black * 0.3f);

            // 滚动条滑块
            spriteBatch.Draw(_buttonTexture, new Rectangle(scrollBarX, (int)scrollBarThumbY, scrollBarWidth, (int)scrollBarThumbHeight), Color.White * 0.6f);

            spriteBatch.End();
        }
    }

    /// <summary>
    /// 绘制对战记录详情
    /// </summary>
    private void DrawBattleRecordDetail(SpriteBatch spriteBatch, BattleRecord record, int x, int y, int width)
    {
        int detailHeight = 250; // 增加高度以容纳Team信息
        Rectangle detailRect = new Rectangle(x, y, width, detailHeight);

        spriteBatch.Begin();

        // 详情面板背景
        spriteBatch.Draw(_buttonTexture, detailRect, Color.Black * 0.4f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, detailRect, Color.SteelBlue, 2);

        if (_buttonFont != null)
        {
            int detailX = x + 15;
            int detailY = y + 10;
            int lineHeight = 25;

            // 详情标题
            spriteBatch.DrawString(_buttonFont, "对战详情", new Vector2(detailX, detailY), Color.Gold, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

            detailY += 30;

            // 本地玩家信息
            spriteBatch.DrawString(_buttonFont, $"玩家: {record.LocalPlayerName} (Lv.{record.LocalPlayerLevel})", 
                new Vector2(detailX, detailY), Color.LightGreen);
            detailY += lineHeight;

            // 对手信息
            string opponentInfo = string.IsNullOrEmpty(record.OpponentName) ? "未知" : $"{record.OpponentName} (Lv.{record.OpponentLevel})";
            spriteBatch.DrawString(_buttonFont, $"对手: {opponentInfo}", 
                new Vector2(detailX, detailY), Color.LightCyan);
            detailY += lineHeight;

            // Team1玩家列表
            if (record.Team1Players != null && record.Team1Players.Count > 0)
            {
                string team1Text = $"Team1: {string.Join(", ", record.Team1Players)}";
                spriteBatch.DrawString(_buttonFont, team1Text, 
                    new Vector2(detailX, detailY), Color.LightBlue, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                detailY += lineHeight;
            }

            // Team2玩家列表
            if (record.Team2Players != null && record.Team2Players.Count > 0)
            {
                string team2Text = $"Team2: {string.Join(", ", record.Team2Players)}";
                spriteBatch.DrawString(_buttonFont, team2Text, 
                    new Vector2(detailX, detailY), Color.LightCoral, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                detailY += lineHeight;
            }

            // 对战结果
            string resultText = BattleHistoryManager.GetResultDescription(record.Result);
            Color resultColor = BattleHistoryManager.GetResultColor(record.Result);
            spriteBatch.DrawString(_buttonFont, $"结果: {resultText}", 
                new Vector2(detailX, detailY), resultColor);
            detailY += lineHeight;

            // 赢家
            if (!string.IsNullOrEmpty(record.WinnerName))
            {
                spriteBatch.DrawString(_buttonFont, $"获胜方: {record.WinnerName}", 
                    new Vector2(detailX, detailY), Color.Gold);
            }
            else
            {
                spriteBatch.DrawString(_buttonFont, "结果: 平手", 
                    new Vector2(detailX, detailY), Color.Yellow);
            }
            detailY += lineHeight;

            // 对战时间和持续时间
            spriteBatch.DrawString(_buttonFont, $"时间: {record.BattleDateTime:yyyy-MM-dd HH:mm:ss} (耗时{record.DurationSeconds}秒, 回合{record.TotalRounds})", 
                new Vector2(detailX, detailY), Color.LightGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }

    /// <summary>
    /// 绘制 PVE 挑战界面
    /// </summary>
    public void DrawPVEChallengePanel(SpriteBatch spriteBatch, PVEChallengeManager challengeManager, int? selectedChallengeIndex = null, int scrollOffset = 0)
    {
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;

        spriteBatch.Begin();

        // 背景
        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_buttonTexture, panelRect, Color.MidnightBlue * 0.6f);

        // 标题
        string title = "PVE 挑战";
        if (_buttonFont != null)
        {
            Vector2 titleSize = _buttonFont.MeasureString(title);
            Vector2 titlePos = new Vector2(panelX + 30, panelY + 20);
            spriteBatch.DrawString(_buttonFont, title, titlePos, Color.Gold);
        }

        spriteBatch.End();

        // 绘制挑战列表
        var challenges = challengeManager.GetAllChallenges();
        int challengeHeight = 80;
        int challengeSpacing = 10;
        int listStartX = panelX + 20;
        int listStartY = panelY + 70;
        int listWidth = panelWidth - 40;
        int detailHeight = 150;
        int detailBottomPadding = 20;
        int listTopSpacing = 10;
        int detailY = panelY + panelHeight - detailHeight - detailBottomPadding;
        if (detailY < listStartY + challengeHeight + challengeSpacing)
            detailY = listStartY + challengeHeight + challengeSpacing;
        int listHeight = Math.Max(challengeHeight, detailY - listStartY - listTopSpacing);

        spriteBatch.Begin();

        for (int i = 0; i < challenges.Count; i++)
        {
            int displayY = listStartY + i * (challengeHeight + challengeSpacing) - scrollOffset;

            if (displayY + challengeHeight < listStartY || displayY > listStartY + listHeight)
                continue;

            var challenge = challenges[i];
            var challengeRect = new Rectangle(listStartX, displayY, listWidth, challengeHeight);

            // 背景色：已完成为绿色，未完成为蓝色，选中为高亮
            Color bgColor = challenge.IsCompleted ? Color.DarkGreen * 0.4f : Color.DarkBlue * 0.4f;
            if (selectedChallengeIndex == i)
                bgColor = Color.LimeGreen * 0.5f;

            spriteBatch.Draw(_buttonTexture, challengeRect, bgColor);
            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, challengeRect, Color.SteelBlue, 2);

            if (_buttonFont != null)
            {
                int textX = listStartX + 15;
                int textY = displayY + 8;

                // 挑战名称
                spriteBatch.DrawString(_buttonFont, challenge.Name, new Vector2(textX, textY), Color.Gold);
                textY += 20;

                // 难度和完成状态
                string difficultyText = $"难度: {'⭐' * challenge.Difficulty}";
                string statusText = challenge.IsCompleted ? "✓ 已完成" : "未完成";
                Color statusColor = challenge.IsCompleted ? Color.LimeGreen : Color.LightCoral;

                spriteBatch.DrawString(_buttonFont, difficultyText, new Vector2(textX, textY), Color.LightYellow, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_buttonFont, statusText, new Vector2(textX + 150, textY), statusColor, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                textY += 18;

                // 对手骰子和奖励
                string opponentDice = string.Join(", ", challenge.OpponentDiceNames);
                string rewardText = $"奖励: {challenge.RewardGold} 金币";
                spriteBatch.DrawString(_buttonFont, $"对手: {challenge.OpponentName}", new Vector2(textX, textY), Color.LightCyan, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_buttonFont, rewardText, new Vector2(textX + 200, textY), Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.End();

        // 如果有选中的挑战，绘制详情面板
        if (selectedChallengeIndex.HasValue && selectedChallengeIndex.Value >= 0 && selectedChallengeIndex.Value < challenges.Count)
        {
            DrawPVEChallengeDetail(spriteBatch, challenges[selectedChallengeIndex.Value], listStartX, detailY, listWidth);
        }
    }

    /// <summary>
    /// 绘制 PVE 挑战详情
    /// </summary>
    private void DrawPVEChallengeDetail(SpriteBatch spriteBatch, PVEChallenge challenge, int x, int y, int width)
    {
        int detailHeight = 150;
        Rectangle detailRect = new Rectangle(x, y, width, detailHeight);

        spriteBatch.Begin();

        // 详情面板背景
        spriteBatch.Draw(_buttonTexture, detailRect, Color.Black * 0.4f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, detailRect, Color.SteelBlue, 2);

        if (_buttonFont != null)
        {
            int detailX = x + 15;
            int detailY = y + 10;
            int lineHeight = 25;

            // 详情标题
            spriteBatch.DrawString(_buttonFont, "挑战详情", new Vector2(detailX, detailY), Color.Gold);

            detailY += 30;

            // 挑战描述
            spriteBatch.DrawString(_buttonFont, $"描述: {challenge.Description}", 
                new Vector2(detailX, detailY), Color.LightGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 对手信息
            string diceList = string.Join(" | ", challenge.OpponentDiceNames);
            spriteBatch.DrawString(_buttonFont, $"对手骰子: {diceList}", 
                new Vector2(detailX, detailY), Color.LightCyan, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 奖励和完成状态
            string completionStatus = challenge.IsCompleted ? "✓ 已完成" : "未完成";
            Color statusColor = challenge.IsCompleted ? Color.LimeGreen : Color.LightCoral;
            spriteBatch.DrawString(_buttonFont, completionStatus, new Vector2(detailX, detailY), statusColor);

            // 进入挑战按钮
            int buttonWidth = 120;
            int buttonHeight = 36;
            int buttonX = x + width - buttonWidth - 15;
            int buttonY = y + detailHeight - buttonHeight - 12;
            Rectangle buttonRect = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

            Color buttonBg = challenge.IsCompleted ? Color.DimGray * 0.6f : Color.DarkSlateBlue * 0.8f;
            Color buttonBorder = challenge.IsCompleted ? Color.Gray : Color.Gold;
            Color buttonText = challenge.IsCompleted ? Color.LightGray : Color.White;

            spriteBatch.Draw(_buttonTexture, buttonRect, buttonBg);
            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, buttonRect, buttonBorder, 2);

            string buttonLabel = challenge.IsCompleted ? "已完成" : "进入挑战";
            Vector2 labelSize = _buttonFont.MeasureString(buttonLabel) * 0.9f;
            Vector2 labelPos = new Vector2(
                buttonRect.X + (buttonRect.Width - labelSize.X) / 2,
                buttonRect.Y + (buttonRect.Height - labelSize.Y) / 2);
            spriteBatch.DrawString(_buttonFont, buttonLabel, labelPos, buttonText, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }
}

public struct LobbyLayout
{
    public Rectangle PanelRect { get; set; }
    public Rectangle InputCardRect { get; set; }
    public Rectangle RoomNameRect { get; set; }
    public Rectangle RefreshButtonRect { get; set; }
    public Rectangle ReconnectButtonRect { get; set; }
    public Rectangle CreateButtonRect { get; set; }
    public Rectangle JoinButtonRect { get; set; }
    public Rectangle LeaveButtonRect { get; set; }
    public Rectangle ReadyButtonRect { get; set; }
    public Rectangle Team1ButtonRect { get; set; }
    public Rectangle Team2ButtonRect { get; set; }
    public Rectangle RoomListRect { get; set; }
    public Rectangle RoomDetailRect { get; set; }
    public int RoomRowHeight { get; set; }
    public int RoomHeaderHeight { get; set; }
}
