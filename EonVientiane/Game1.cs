using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientiane;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _buttonTexture;
    private SpriteFont _buttonFont;

    // UI状态管理
    private GameUIState _currentUIState = GameUIState.Game;
    private ContentView _currentContentView = ContentView.Button1;
    private InputField _activeInputField = InputField.None;

    // 核心管理器
    private MenuManager _menuManager;
    private UIManager _uiManager;
    private InputManager _inputManager;
    private InventoryManager _inventoryManager;
    private LoginManager _loginManager;
    private LoginInputHandler _loginInputHandler;
    private InventoryInputHandler _inventoryInputHandler;
    private MultiplayerLobbyManager _lobbyManager;
    private BattleManager _battleManager;
    private AchievementSystem _achievementSystem;
    private bool _isLoggingIn;
    private bool _isRegistering;
    private string _authStatusMessage = string.Empty;

    // 用户状态
    private UserProfile _currentUser;

    // 联机大厅状态
    private LobbyInputField _activeLobbyInputField = LobbyInputField.None;
    private string _lobbyRoomName = "My Room";
    private string _selectedRoomId = string.Empty;

    // 背包界面状态
    private int? _selectedInventoryIndex = null;
    private int? _selectedEquipmentIndex = null;

    // 输入状态
    private MouseState _previousMouseState;

    // 移动端支持
    private PlatformAdapter _platformAdapter;
    private VirtualKeyboard _virtualKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        // 初始化平台适配器
        _platformAdapter = new PlatformAdapter(_graphics);
        
        // 设置窗口大小 - 根据平台调整
        if (_platformAdapter.Platform == PlatformAdapter.DevicePlatform.Mobile)
        {
            // 移动设备: 使用竖屏分辨率
            _graphics.PreferredBackBufferWidth = 540;
            _graphics.PreferredBackBufferHeight = 960;
        }
        else
        {
            // 桌面: 使用宽屏分辨率
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
        }
    }

    protected override void Initialize()
    {
        _loginManager = new LoginManager();
        _inputManager = new InputManager(_graphics);
        _uiManager = new UIManager(MenuManager.GetMenuWidth(), MenuManager.GetButtonHeight(), 10, _graphics);
        _inventoryManager = new InventoryManager();
        _achievementSystem = new AchievementSystem(_inventoryManager);
        _menuManager = new MenuManager(_graphics);
        _loginInputHandler = new LoginInputHandler(MenuManager.GetMenuWidth(), _inputManager);
        _inventoryInputHandler = new InventoryInputHandler(MenuManager.GetMenuWidth(), OnEquipRequested, OnUnequipRequested);
        _lobbyManager = new MultiplayerLobbyManager();
        _battleManager = new BattleManager(_inventoryManager, MenuManager.GetMenuWidth());

        _lobbyManager.InventoryStateReceived += OnInventoryStateReceived;
        _lobbyManager.InventoryError += OnInventoryError;
        _lobbyManager.GameStarted += OnGameStarted;
        _lobbyManager.AchievementCompleted += OnServerAchievementCompleted;
        _lobbyManager.BattleStateUpdated += OnBattleStateUpdated;
        _lobbyManager.BattleEnded += OnBattleEnded;
        
        // 订阅战斗管理器事件
        _battleManager.BattleActionRequested += OnBattleActionRequested;
        _battleManager.BattleDefenseRequested += OnBattleDefenseRequested;
        
        // 订阅成就完成事件
        _achievementSystem.AchievementCompleted += OnAchievementCompleted;
        _achievementSystem.RewardGiven += OnRewardGiven;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // 创建按钮纹理
        _buttonTexture = new Texture2D(GraphicsDevice, 1, 1);
        _buttonTexture.SetData(new[] { Color.White });

        // 加载字体文件
        _buttonFont = Content.Load<SpriteFont>("Fonts/ButtonFont");

        // 设置UI管理器的资源
        _uiManager.SetTexture(_buttonTexture);
        _uiManager.SetFont(_buttonFont);

        // 初始化菜单
        _menuManager.InitializeButtons(_buttonTexture, _buttonFont);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            if (_currentUIState != GameUIState.Game)
            {
                _currentUIState = GameUIState.Game;
            }
            else
            {
                Exit();
            }
        }

        MouseState mouseState = Mouse.GetState();

        // 处理不同UI状态的输入
        switch (_currentUIState)
        {
            case GameUIState.Login:
                HandleLoginInput(mouseState);
                break;
            case GameUIState.UserProfile:
                HandleUserProfileInput(mouseState);
                break;
            case GameUIState.Game:
                HandleGameInput(mouseState);
                break;
        }

        // 更新战斗状态
            // 客户端不运行本地战斗更新（多人模式由服务器驱动）
            _battleManager.UpdateTip(gameTime);

        _previousMouseState = mouseState;
        _inputManager.Update(gameTime);

        base.Update(gameTime);
    }

    private void HandleGameInput(MouseState mouseState)
    {
        // 处理菜单输入
        var menuResult = _menuManager.HandleInput(mouseState, _previousMouseState);

        if (menuResult.TopButtonClicked)
        {
            if (_currentUser == null)
            {
                _currentUIState = GameUIState.Login;
            }
            else
            {
                _currentUIState = GameUIState.UserProfile;
            }
        }

        if (menuResult.BottomButtonClicked)
        {
            _currentContentView = ContentView.Settings;
        }

        if (menuResult.MiddleButtonClicked)
        {
            _currentContentView = (ContentView)(menuResult.ClickedButtonIndex + 1);

            if (menuResult.ClickedButtonLabel == "战斗")
            {
                // 本地对电脑战斗已移除，默认跳转背包界面
                _currentContentView = ContentView.Button2;
            }
        }

        // 处理背包输入
        if (_currentContentView == ContentView.Button2)
        {
            _inventoryInputHandler.HandleInput(mouseState, _previousMouseState, _inventoryManager,
                ref _selectedInventoryIndex, ref _selectedEquipmentIndex, _graphics.PreferredBackBufferHeight);
        }

        // 处理联机大厅输入
        if (_currentContentView == ContentView.Button1)
        {
            if (_currentUser != null)
            {
                _ = _lobbyManager.EnsureConnectedAsync();
            }
            HandleLobbyInput(mouseState);
            ProcessLobbyKeyboardInput();
        }

        // 处理战斗输入
        if (_currentContentView == ContentView.Battle && _battleManager != null)
        {
            int panelWidth = _graphics.PreferredBackBufferWidth - MenuManager.GetMenuWidth();
            int panelHeight = _graphics.PreferredBackBufferHeight;
            _battleManager.HandleInput(mouseState, _previousMouseState, panelWidth, panelHeight);
        }
    }

    private void HandleLoginInput(MouseState mouseState)
    {
        if (!_isRegistering)
        {
            var loginResult = _loginInputHandler.HandleInput(mouseState, _previousMouseState, _loginManager,
                ref _activeInputField, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            // 触发服务器登录请求（本地校验已通过）
            if (loginResult.LoginRequested)
            {
                _ = _lobbyManager.LoginAsync(_loginManager.Username, _loginManager.Password);
                _lobbyManager.ConfigurePlayer(_loginManager.Username);
                _isLoggingIn = true;
                _authStatusMessage = "登录中...";
            }

            if (loginResult.CancelClicked)
            {
                _currentUIState = GameUIState.Game;
                _activeInputField = InputField.None;
                _authStatusMessage = string.Empty;
            }

            // 打开注册界面
            if (loginResult.RegisterClicked)
            {
                _isRegistering = true;
                _activeInputField = InputField.Username;
                _authStatusMessage = string.Empty;
            }
        }
        else
        {
            var regResult = _loginInputHandler.HandleRegistrationInput(mouseState, _previousMouseState, _loginManager,
                ref _activeInputField, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            if (regResult.RegistrationRequested)
            {
                _ = _lobbyManager.RegisterAsync(_loginManager.Username, _loginManager.Password, _loginManager.Email);
                // 注册请求发送后，返回登录界面等待提示
                _isRegistering = false;
                _activeInputField = InputField.Username;
                _authStatusMessage = "注册中...";
            }

            if (regResult.BackToLoginClicked)
            {
                _isRegistering = false;
                _activeInputField = InputField.Username;
                // 回到登录页时不清空，以便显示上一次的状态
            }
        }
        // 仅在服务器认证成功后才进入用户信息界面
        if (_isLoggingIn && _lobbyManager.IsAuthenticated)
        {
            if (_currentUser == null)
            {
                _currentUser = new UserProfile(_loginManager.Username, string.Empty, DateTime.UtcNow, "Newbie");
                _loginManager.SetCurrentUser(_currentUser);
            }
            _ = _lobbyManager.RequestInventoryAsync();
            _currentUIState = GameUIState.UserProfile;
            _activeInputField = InputField.None;
            _isLoggingIn = false;
            _authStatusMessage = string.Empty;
        }

        // 处理Tab切换输入框
        if (Keyboard.GetState().IsKeyDown(Keys.Tab) && _inputManager.PreviousKeyboardState.IsKeyUp(Keys.Tab))
        {
            if (_isRegistering)
            {
                _activeInputField = _activeInputField == InputField.Username
                    ? InputField.Password
                    : _activeInputField == InputField.Password
                        ? InputField.Email
                        : InputField.Username;
            }
            else
            {
                _activeInputField = _activeInputField == InputField.Username
                    ? InputField.Password
                    : InputField.Username;
            }
        }

        // 同步大厅层的状态消息（若非登录中状态）
        if (!_isLoggingIn)
        {
            _authStatusMessage = _lobbyManager.StatusMessage;
        }
    }

    private void HandleUserProfileInput(MouseState mouseState)
    {
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mousePoint = new Point(mouseState.X, mouseState.Y);

            // 检查是否点击左侧菜单区域
            if (mouseState.X < MenuManager.GetMenuWidth())
            {
                HandleProfileLeftMenuClick(mousePoint);
            }
            else
            {
                // 处理注销按钮
                int panelX = MenuManager.GetMenuWidth();
                int panelWidth = _graphics.PreferredBackBufferWidth - MenuManager.GetMenuWidth();
                int cardHeight = 500;

                int buttonWidth = 120;
                int buttonHeight = 45;
                int buttonY = 50 + cardHeight - 70;
                int logoutButtonX = panelX + 90;

                Rectangle logoutButtonRect = new Rectangle(logoutButtonX, buttonY, buttonWidth, buttonHeight);

                if (logoutButtonRect.Contains(mousePoint))
                {
                    _loginManager.Logout();
                    _lobbyManager.Disconnect();
                    _currentUser = null;
                    _currentUIState = GameUIState.Game;
                }
            }
        }
    }

    private void HandleProfileLeftMenuClick(Point mousePoint)
    {
        var menuResult = _menuManager.HandleInput(Mouse.GetState(), _previousMouseState);

        if (menuResult.TopButtonClicked)
        {
            _currentUIState = GameUIState.Game;
        }

        if (menuResult.BottomButtonClicked)
        {
            _currentContentView = ContentView.Settings;
            _currentUIState = GameUIState.Game;
        }

        if (menuResult.MiddleButtonClicked)
        {
            _currentContentView = (ContentView)(menuResult.ClickedButtonIndex + 1);
            _currentUIState = GameUIState.Game;
        }
    }

    private void HandleLobbyInput(MouseState mouseState)
    {
        if (_currentUser != null)
        {
            _ = _lobbyManager.EnsureConnectedAsync();
        }

        var layout = _uiManager.GetLobbyLayout();
        bool leftClicked = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

        if (!leftClicked)
            return;

        Point mousePoint = new Point(mouseState.X, mouseState.Y);
        LobbyInputField newActiveField = LobbyInputField.None;

        if (layout.RoomNameRect.Contains(mousePoint))
        {
            newActiveField = LobbyInputField.RoomName;
        }

        // 如果点击到输入框，优先切换焦点
        if (newActiveField != LobbyInputField.None)
        {
            _activeLobbyInputField = newActiveField;
            return;
        }

        // 按钮交互
        if (layout.RefreshButtonRect.Contains(mousePoint))
        {
            _lobbyManager.RefreshRoomList();
            return;
        }
        
        // 重新连接按钮
        if (layout.ReconnectButtonRect.Contains(mousePoint) && _lobbyManager.State == LobbyState.Disconnected)
        {
            _ = _lobbyManager.ManualReconnectAsync();
            return;
        }

        if (layout.CreateButtonRect.Contains(mousePoint))
        {
            _lobbyManager.CreateRoom(_lobbyRoomName);
            return;
        }

        if (layout.JoinButtonRect.Contains(mousePoint) && !string.IsNullOrEmpty(_selectedRoomId))
        {
            _lobbyManager.JoinRoom(_selectedRoomId);
            return;
        }

        if (layout.LeaveButtonRect.Contains(mousePoint))
        {
            _lobbyManager.LeaveRoom();
            _selectedRoomId = string.Empty;
            return;
        }

        if (layout.ReadyButtonRect.Contains(mousePoint))
        {
            _lobbyManager.ToggleReady();
            return;
        }

        if (layout.Team1ButtonRect.Contains(mousePoint))
        {
            _lobbyManager.SelectTeam(1);
            return;
        }

        if (layout.Team2ButtonRect.Contains(mousePoint))
        {
            _lobbyManager.SelectTeam(2);
            return;
        }

        // 房间列表选择
        if (layout.RoomListRect.Contains(mousePoint))
        {
            int relativeY = mousePoint.Y - layout.RoomListRect.Y - layout.RoomHeaderHeight;
            if (relativeY >= 0)
            {
                int index = relativeY / layout.RoomRowHeight;
                if (index >= 0 && index < _lobbyManager.RoomList.Count)
                {
                    _selectedRoomId = _lobbyManager.RoomList[index].RoomId;
                }
            }
        }
    }

    private void ProcessLobbyKeyboardInput()
    {
        if (_currentUIState != GameUIState.Game || _currentContentView != ContentView.Button1)
            return;

        if (_currentUser != null)
        {
            _ = _lobbyManager.EnsureConnectedAsync();
        }

        var keyboardState = Keyboard.GetState();
        bool shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (!_inputManager.PreviousKeyboardState.IsKeyUp(key))
                continue;

            if (key == Keys.Tab)
            {
                _activeLobbyInputField = _activeLobbyInputField == LobbyInputField.RoomName
                    ? LobbyInputField.None
                    : LobbyInputField.RoomName;
                continue;
            }

            if (key == Keys.Enter)
            {
                if (_lobbyManager.State == LobbyState.InRoom)
                {
                    _lobbyManager.ToggleReady();
                }
                else if (_lobbyManager.State == LobbyState.InLobby)
                {
                    if (!string.IsNullOrEmpty(_selectedRoomId))
                    {
                        _lobbyManager.JoinRoom(_selectedRoomId);
                    }
                    else
                    {
                        _lobbyManager.CreateRoom(_lobbyRoomName);
                    }
                }
                else
                {
                    _ = _lobbyManager.EnsureConnectedAsync();
                }
                continue;
            }

            switch (_activeLobbyInputField)
            {
                case LobbyInputField.RoomName:
                    _lobbyRoomName = ApplyLobbyTextEdit(_lobbyRoomName, key, shift, 24);
                    break;
            }
        }
    }

    private static string ApplyLobbyTextEdit(string current, Keys key, bool shift, int maxLength)
    {
        if (key == Keys.Back)
        {
            return current.Length > 0 ? current[..^1] : current;
        }

        char? ch = InputManager.GetCharFromKey(key, shift);
        if (ch.HasValue && current.Length < maxLength)
        {
            return current + ch.Value;
        }

        return current;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        _menuManager.Draw(_spriteBatch, _buttonTexture, _buttonFont, GraphicsDevice);
        _spriteBatch.End();

        // 绘制右侧内容区域（仅在游戏态）
        if (_currentUIState == GameUIState.Game)
        {
            // 如果是战斗界面，绘制战斗
            if (_currentContentView == ContentView.Battle && _battleManager != null)
            {
                int panelWidth = _graphics.PreferredBackBufferWidth - MenuManager.GetMenuWidth();
                int panelHeight = _graphics.PreferredBackBufferHeight;
                _battleManager.Draw(_spriteBatch, _buttonTexture, _buttonFont, GraphicsDevice, panelWidth, panelHeight);
            }
            // 如果是背包界面，绘制背包
            else if (_currentContentView == ContentView.Button2)
            {
                _uiManager.DrawInventoryPanel(_spriteBatch, _inventoryManager, _selectedInventoryIndex, _selectedEquipmentIndex);
            }
            // 联机大厅
            else if (_currentContentView == ContentView.Button1)
            {
                var layout = _uiManager.GetLobbyLayout();
                _uiManager.DrawLobbyPanel(
                    _spriteBatch,
                    _lobbyManager,
                    layout,
                    _lobbyRoomName,
                    _selectedRoomId,
                    _activeLobbyInputField);
            }
            // 成就界面
            else if (_currentContentView == ContentView.Button4)
            {
                _uiManager.DrawAchievementPanel(_spriteBatch, _achievementSystem);
            }
            else
            {
                _uiManager.DrawContentPanel(_spriteBatch, _currentContentView);
            }
        }
        else if (_currentUIState == GameUIState.Login)
        {
            if (_isRegistering)
                _uiManager.DrawRegistrationWindow(_spriteBatch, _loginManager, _activeInputField, _authStatusMessage);
            else
                _uiManager.DrawLoginWindow(_spriteBatch, _loginManager, _activeInputField, _authStatusMessage);
        }
        else if (_currentUIState == GameUIState.UserProfile)
        {
            _uiManager.DrawUserProfileWindow(_spriteBatch, _currentUser);
        }

        base.Draw(gameTime);
    }

    // 公共接口方法，供外部使用
    public void AddMiddleButton(string label, Color? color = null, Color? hoverColor = null, int? insertIndex = null)
    {
        _menuManager.AddMiddleButton(label, color, hoverColor, insertIndex);
    }

    public bool RemoveMiddleButton(int index)
    {
        return _menuManager.RemoveMiddleButton(index);
    }

    public int RemoveMiddleButtonByLabel(string label)
    {
        return _menuManager.RemoveMiddleButtonByLabel(label);
    }

    private void OnInventoryStateReceived(InventoryState state)
    {
        _inventoryManager.ApplyServerState(state);
    }

    private void OnInventoryError(string error)
    {
        _authStatusMessage = error;
    }

    private void OnEquipRequested(ItemStack stack)
    {
        if (stack == null)
            return;

        _ = _lobbyManager.EquipItemAsync(stack.StackId);
    }

    private void OnUnequipRequested(ItemStack stack)
    {
        if (stack == null)
            return;

        _ = _lobbyManager.UnequipItemAsync(stack.StackId);
    }

    private void OnGameStarted(GameStartedNotification notification)
    {
        if (_battleManager != null)
        {
            // 使用多人战斗初始化（不再支持单人模式）
            if (notification.Players != null && notification.Players.Count > 0)
            {
                string localPlayerId = _lobbyManager.IsAuthenticated 
                    ? (_lobbyManager.CurrentRoomPlayers?.FirstOrDefault(p => p.PlayerName == _lobbyManager.PlayerName)?.PlayerId ?? "player")
                    : "player";
                _battleManager.InitializeMultiplayerBattle(notification.Players, localPlayerId);
            }

            _currentContentView = ContentView.Battle;
        }
    }

    /// <summary>
    /// 成就完成事件处理
    /// </summary>
    private void OnAchievementCompleted(AchievementSystem.Achievement achievement)
    {
        _authStatusMessage = $"成就完成: {achievement.Name}";
        Console.WriteLine($"[Client] Achievement completed: {achievement.Name}");
    }

    /// <summary>
    /// 奖励发放事件处理
    /// </summary>
    private void OnRewardGiven(AchievementSystem.Reward reward)
    {
        if (reward.Type == "Item")
        {
            _authStatusMessage += $" - 获得物品: {reward.ItemId}";
        }
        else if (reward.Type == "Gold")
        {
            _authStatusMessage += $" - 获得 {reward.Quantity} 金币";
        }
        Console.WriteLine($"[Client] Reward given: {reward.Type} x{reward.Quantity}");
    }

    

    /// <summary>
    /// 服务端成就完成通知处理
    /// </summary>
    private void OnServerAchievementCompleted(AchievementCompletedNotification notification)
    {
        _authStatusMessage = $"成就完成: {notification.AchievementName}!";
        Console.WriteLine($"[Client] Server achievement completed: {notification.AchievementName}");
        
        foreach (var reward in notification.Rewards)
        {
            if (reward.Type == "Item")
            {
                _authStatusMessage += $" 获得: {reward.ItemId}";
            }
            else if (reward.Type == "Gold")
            {
                _authStatusMessage += $" 获得 {reward.Quantity} 金币";
            }
        }
        
        _ = _lobbyManager.RequestInventoryAsync();
    }
    
    /// <summary>
    /// 处理多人战斗状态更新
    /// </summary>
    private void OnBattleStateUpdated(BattleStateUpdateNotification notification)
    {
        if (_battleManager?.CurrentBattle == null)
            return;
        
        // 应用服务器状态到本地战斗
        _battleManager.ApplyServerBattleState(notification);
        
        Console.WriteLine($"[Client] Battle update - Round: {notification.CurrentRound}, State: {notification.CurrentState}");
        Console.WriteLine($"[Client] Waiting input player: {notification.WaitingInputPlayerId}");
    }
    
    /// <summary>
    /// 处理多人战斗结束
    /// </summary>
    private void OnBattleEnded(BattleEndNotification notification)
    {
        _authStatusMessage = $"战斗结束！{notification.WinnerCamp}阵营获胜！";
        Console.WriteLine($"[Client] Battle ended - Winner: {notification.WinnerCamp}");
    }
    
    /// <summary>
    /// 处理战斗行动请求（发送到服务器）
    /// </summary>
    private async void OnBattleActionRequested(string diceName, string targetPlayerId)
    {
        if (_lobbyManager != null)
        {
            await _lobbyManager.SendBattleActionAsync(diceName, targetPlayerId);
            Console.WriteLine($"[Client] Sent battle action: {diceName} -> {targetPlayerId}");
        }
    }
    
    /// <summary>
    /// 处理战斗防守请求（发送到服务器）
    /// </summary>
    private async void OnBattleDefenseRequested(string diceName)
    {
        if (_lobbyManager != null)
        {
            await _lobbyManager.SendBattleDefenseAsync(diceName);
            Console.WriteLine($"[Client] Sent battle defense: {diceName}");
        }
    }
}
    /// <summary>

    /// 服务端成就完成通知处理
