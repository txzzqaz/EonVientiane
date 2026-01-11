using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// UI管理器 - 处理所有UI相关的渲染和逻辑
/// </summary>
public class UIManager
{
    private readonly int _menuWidth;
    private readonly int _buttonHeight;
    private readonly int _buttonMargin;
    private readonly GraphicsDeviceManager _graphics;
    private Texture2D _buttonTexture;
    private SpriteFont _buttonFont;
    
    public UIManager(int menuWidth, int buttonHeight, int buttonMargin, GraphicsDeviceManager graphics)
    {
        _menuWidth = menuWidth;
        _buttonHeight = buttonHeight;
        _buttonMargin = buttonMargin;
        _graphics = graphics;
    }
    
    public void SetTexture(Texture2D texture) => _buttonTexture = texture;
    public void SetFont(SpriteFont font) => _buttonFont = font;

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
        int formHeight = 400;
        int windowX = panelX + (panelWidth - formWidth) / 2;
        int windowY = panelY + (panelHeight - formHeight) / 2;
        Rectangle windowRect = new Rectangle(windowX, windowY, formWidth, formHeight);
        
        spriteBatch.Draw(_buttonTexture, windowRect, Color.Gray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, windowRect, Color.LightGray, 2);
        
        // 绘制标题
        string title = "用户登录";
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
            spriteBatch.DrawString(_buttonFont, hint, hintPos, Color.LightGray);
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
        
        // 绘制用户名输入框标签
        string usernameLabel = "用户名:";
        Vector2 usernameLabelPos = new Vector2(windowX + 40, windowY + 120);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, usernameLabel, usernameLabelPos, Color.White);
        
        // 绘制用户名输入框
        Rectangle usernameInputRect = new Rectangle(windowX + 40, windowY + 150, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, usernameInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, usernameInputRect, activeInputField == InputField.Username ? Color.Blue : Color.Black, activeInputField == InputField.Username ? 3 : 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, loginManager.Username, 
                new Vector2(usernameInputRect.X + 10, usernameInputRect.Y + 12), Color.Black);
        
        // 绘制光标（如果用户名输入框激活）
        if (activeInputField == InputField.Username)
        {
            int cursorX = usernameInputRect.X + 10 + (int)(_buttonFont?.MeasureString(loginManager.Username).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, usernameInputRect.Y + 10, 2, 28), Color.Black);
        }
        
        // 绘制密码输入框标签
        string passwordLabel = "密码:";
        Vector2 passwordLabelPos = new Vector2(windowX + 40, windowY + 210);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, passwordLabel, passwordLabelPos, Color.White);
        
        // 绘制密码输入框
        Rectangle passwordInputRect = new Rectangle(windowX + 40, windowY + 240, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, passwordInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, passwordInputRect, activeInputField == InputField.Password ? Color.Blue : Color.Black, activeInputField == InputField.Password ? 3 : 2);
        string passwordDisplay = new string('*', loginManager.Password.Length);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, passwordDisplay, 
                new Vector2(passwordInputRect.X + 10, passwordInputRect.Y + 12), Color.Black);
        
        // 绘制光标（如果密码输入框激活）
        if (activeInputField == InputField.Password)
        {
            int cursorX = passwordInputRect.X + 10 + (int)(_buttonFont?.MeasureString(passwordDisplay).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, passwordInputRect.Y + 10, 2, 28), Color.Black);
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
        int formHeight = 460;
        int windowX = panelX + (panelWidth - formWidth) / 2;
        int windowY = panelY + (panelHeight - formHeight) / 2;
        Rectangle windowRect = new Rectangle(windowX, windowY, formWidth, formHeight);

        spriteBatch.Draw(_buttonTexture, windowRect, Color.Gray * 0.9f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, windowRect, Color.LightGray, 2);

        string title = "用户注册";
        if (_buttonFont != null)
        {
            Vector2 titleSize = _buttonFont.MeasureString(title);
            Vector2 titlePos = new Vector2(windowX + (formWidth - titleSize.X) / 2, windowY + 30);
            spriteBatch.DrawString(_buttonFont, title, titlePos, Color.White);
        }

        string hint = "请输入用户名、密码和邮箱";
        if (_buttonFont != null)
        {
            Vector2 hintSize = _buttonFont.MeasureString(hint);
            Vector2 hintPos = new Vector2(windowX + (formWidth - hintSize.X) / 2, windowY + 70);
            spriteBatch.DrawString(_buttonFont, hint, hintPos, Color.LightGray);
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

        // 用户名
        string usernameLabel = "用户名:";
        Vector2 usernameLabelPos = new Vector2(windowX + 40, windowY + 120);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, usernameLabel, usernameLabelPos, Color.White);
        Rectangle usernameInputRect = new Rectangle(windowX + 40, windowY + 150, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, usernameInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, usernameInputRect, activeInputField == InputField.Username ? Color.Blue : Color.Black, activeInputField == InputField.Username ? 3 : 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, loginManager.Username, new Vector2(usernameInputRect.X + 10, usernameInputRect.Y + 12), Color.Black);
        if (activeInputField == InputField.Username)
        {
            int cursorX = usernameInputRect.X + 10 + (int)(_buttonFont?.MeasureString(loginManager.Username).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, usernameInputRect.Y + 10, 2, 28), Color.Black);
        }

        // 密码
        string passwordLabel = "密码:";
        Vector2 passwordLabelPos = new Vector2(windowX + 40, windowY + 210);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, passwordLabel, passwordLabelPos, Color.White);
        Rectangle passwordInputRect = new Rectangle(windowX + 40, windowY + 240, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, passwordInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, passwordInputRect, activeInputField == InputField.Password ? Color.Blue : Color.Black, activeInputField == InputField.Password ? 3 : 2);
        string passwordDisplay = new string('*', loginManager.Password.Length);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, passwordDisplay, new Vector2(passwordInputRect.X + 10, passwordInputRect.Y + 12), Color.Black);
        if (activeInputField == InputField.Password)
        {
            int cursorX = passwordInputRect.X + 10 + (int)(_buttonFont?.MeasureString(passwordDisplay).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, passwordInputRect.Y + 10, 2, 28), Color.Black);
        }

        // 邮箱
        string emailLabel = "邮箱:";
        Vector2 emailLabelPos = new Vector2(windowX + 40, windowY + 300);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, emailLabel, emailLabelPos, Color.White);
        Rectangle emailInputRect = new Rectangle(windowX + 40, windowY + 330, formWidth - 80, 45);
        spriteBatch.Draw(_buttonTexture, emailInputRect, Color.White);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, emailInputRect, activeInputField == InputField.Email ? Color.Blue : Color.Black, activeInputField == InputField.Email ? 3 : 2);
        if (_buttonFont != null)
            spriteBatch.DrawString(_buttonFont, loginManager.Email, new Vector2(emailInputRect.X + 10, emailInputRect.Y + 12), Color.Black);
        if (activeInputField == InputField.Email)
        {
            int cursorX = emailInputRect.X + 10 + (int)(_buttonFont?.MeasureString(loginManager.Email).X ?? 0);
            spriteBatch.Draw(_buttonTexture, new Rectangle(cursorX, emailInputRect.Y + 10, 2, 28), Color.Black);
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
                string itemName = stack.Item.Name;
                if (stack.Quantity > 1)
                    itemName += $" x{stack.Quantity}";
                
                spriteBatch.DrawString(_buttonFont, itemName, 
                    new Vector2(itemRect.X + 10, itemRect.Y + 10), stack.Item.DisplayColor);
                
                // 物品描述（小字）
                string desc = stack.Item.Description;
                if (desc.Length > 30)
                    desc = desc.Substring(0, 27) + "...";
                spriteBatch.DrawString(_buttonFont, desc, 
                    new Vector2(itemRect.X + 10, itemRect.Y + 35), Color.LightGray * 0.7f, 0f, Vector2.Zero, 0.7f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                
                // 装备类型标记
                if (stack.Item is Equipment equipment)
                {
                    string typeLabel = equipment.EquipmentType == EquipmentType.Dice ? "[骰子]" : "[饰品]";
                    spriteBatch.DrawString(_buttonFont, typeLabel, 
                        new Vector2(itemRect.X + width - 60, itemRect.Y + 10), Color.Cyan * 0.8f, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
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
                // 装备类型标签
                string typeLabel = equipment.EquipmentType == EquipmentType.Dice ? "[骰子]" : "[饰品]";
                spriteBatch.DrawString(_buttonFont, typeLabel, 
                    new Vector2(slotRect.X + 10, slotRect.Y + 8), Color.LightGray, 0f, Vector2.Zero, 0.8f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                
                // 装备名称
                spriteBatch.DrawString(_buttonFont, equipment.Name, 
                    new Vector2(slotRect.X + 10, slotRect.Y + 28), equipment.DisplayColor);
                
                // 装备属性
                string equipStats = equipment.GetStatsDescription();
                if (!string.IsNullOrEmpty(equipStats))
                {
                    spriteBatch.DrawString(_buttonFont, equipStats, 
                        new Vector2(slotRect.X + 10, slotRect.Y + 48), Color.LightGreen, 0f, Vector2.Zero, 0.6f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
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
    public void DrawAchievementPanel(SpriteBatch spriteBatch, AchievementSystem achievementSystem)
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

        // 绘制进度条
        int progressBarX = panelX + 30;
        int progressBarY = panelY + 60;
        int progressBarWidth = panelWidth - 60;
        int progressBarHeight = 30;

        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, 
            new Rectangle(progressBarX, progressBarY, progressBarWidth, progressBarHeight), 
            Color.Black, 2);

        // 绘制进度填充
        int filledWidth = (int)(progressBarWidth * (stats.percentage / 100f));
        spriteBatch.Draw(_buttonTexture, 
            new Rectangle(progressBarX + 2, progressBarY + 2, filledWidth - 4, progressBarHeight - 4),
            Color.LimeGreen * 0.7f);

        // 绘制进度文字
        if (_buttonFont != null)
        {
            string progressText = $"{stats.completed}/{stats.total} ({stats.percentage:F1}%)";
            Vector2 textSize = _buttonFont.MeasureString(progressText);
            spriteBatch.DrawString(_buttonFont, progressText,
                new Vector2(progressBarX + progressBarWidth / 2 - textSize.X / 2, progressBarY + 6),
                Color.White);
        }

        // 绘制成就列表
        int achievementStartY = progressBarY + progressBarHeight + 30;
        int achievementItemHeight = 80;
        int achievementSpacing = 10;
        int achievementDisplayCount = (panelHeight - achievementStartY - 30) / (achievementItemHeight + achievementSpacing);

        for (int i = 0; i < Math.Min(achievementDisplayCount, achievements.Count); i++)
        {
            var achievement = achievements[i];
            int itemY = achievementStartY + i * (achievementItemHeight + achievementSpacing);

            DrawAchievementItem(spriteBatch, panelX + 20, itemY, panelWidth - 40, achievementItemHeight, achievement);
        }

        spriteBatch.End();
    }

    /// <summary>
    /// 绘制单个成就条目
    /// </summary>
    private void DrawAchievementItem(SpriteBatch spriteBatch, int x, int y, int width, int height, AchievementSystem.Achievement achievement)
    {
        // 背景框
        Color bgColor = achievement.IsCompleted ? Color.DarkGreen * 0.5f : Color.DarkSlateGray * 0.5f;
        spriteBatch.Draw(_buttonTexture, new Rectangle(x, y, width, height), bgColor);

        // 边框
        Color borderColor = achievement.IsCompleted ? Color.LimeGreen : Color.Gray;
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, new Rectangle(x, y, width, height), borderColor, 2);

        int contentX = x + 15;
        int contentY = y + 10;
        int contentWidth = width - 30;

        if (_buttonFont != null)
        {
            // 成就名称
            Color nameColor = achievement.IsCompleted ? Color.Gold : Color.White;
            spriteBatch.DrawString(_buttonFont, achievement.Name, 
                new Vector2(contentX, contentY), nameColor);

            // 成就描述
            spriteBatch.DrawString(_buttonFont, achievement.Description,
                new Vector2(contentX, contentY + 25), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            // 进度条
            int progressBarWidth = contentWidth;
            int progressBarHeight = 15;
            int progressBarY = contentY + 50;

            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture,
                new Rectangle(contentX, progressBarY, progressBarWidth, progressBarHeight),
                Color.Black, 1);

            int filledWidth = (int)(progressBarWidth * (achievement.Progress / (float)achievement.RequiredProgress));
            Color progressColor = achievement.IsCompleted ? Color.LimeGreen : Color.SteelBlue;
            spriteBatch.Draw(_buttonTexture,
                new Rectangle(contentX + 1, progressBarY + 1, Math.Max(1, filledWidth - 2), progressBarHeight - 2),
                progressColor);

            // 进度文字
            string progressText = $"{achievement.Progress}/{achievement.RequiredProgress}";
            Vector2 progressTextSize = _buttonFont.MeasureString(progressText);
            spriteBatch.DrawString(_buttonFont, progressText,
                new Vector2(contentX + progressBarWidth / 2 - progressTextSize.X / 2, progressBarY + 1),
                Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            // 完成标记
            if (achievement.IsCompleted)
            {
                string completedText = "已完成";
                Vector2 completedSize = _buttonFont.MeasureString(completedText);
                spriteBatch.DrawString(_buttonFont, completedText,
                    new Vector2(contentX + contentWidth - completedSize.X, contentY + 25),
                    Color.LimeGreen, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
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
