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
    private ItemIconProvider _itemIconProvider;
    private BattleHistoryManager _battleHistoryManager;
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

    // 对战历史界面状态
    private int _battleHistoryScrollOffset = 0;
    private int? _selectedBattleRecordIndex = null;

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
        _battleHistoryManager = new BattleHistoryManager();

        _lobbyManager.InventoryStateReceived += OnInventoryStateReceived;
        _lobbyManager.InventoryError += OnInventoryError;
        _lobbyManager.GameStarted += OnGameStarted;
        _lobbyManager.AchievementCompleted += OnServerAchievementCompleted;
        _lobbyManager.BattleStateUpdated += OnBattleStateUpdated;
        _lobbyManager.BattleEnded += OnBattleEnded;
        _lobbyManager.AchievementsReceived += OnAchievementsReceived;
        
        // 订阅战斗管理器事件
        _battleManager.BattleActionRequested += OnBattleActionRequested;
        _battleManager.BattleDefenseRequested += OnBattleDefenseRequested;
        _battleManager.BattleSurrenderRequested += OnBattleSurrenderRequested;
        _battleManager.ReturnToLobbyRequested += OnReturnToLobbyRequested;
        
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

        // 初始化图标提供器（用于骰子图标）
        _itemIconProvider = new ItemIconProvider(Content);
        _uiManager.SetIconProvider(_itemIconProvider);
        _battleManager.SetIconProvider(_itemIconProvider);

        // 初始化菜单
        _menuManager.InitializeButtons(_buttonTexture, _buttonFont);
    }

    protected override void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            _previousMouseState = Mouse.GetState();
            _inputManager?.ClearInputStates();
            base.Update(gameTime);
            return;
        }

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

        bool battleActive = _battleManager?.IsBattleActive == true;

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
                _currentContentView = ContentView.Battle;
            }
        }

        // 处理背包输入
        if (!battleActive && _currentContentView == ContentView.Button2)
        {
            _inventoryInputHandler.HandleInput(mouseState, _previousMouseState, _inventoryManager,
                ref _selectedInventoryIndex, ref _selectedEquipmentIndex, _graphics.PreferredBackBufferHeight);
        }

        // 处理对战历史输入
        if (!battleActive && _currentContentView == ContentView.Button3)
        {
            HandleBattleHistoryInput(mouseState, _previousMouseState);
        }

        // 处理联机大厅输入
        if (!battleActive && _currentContentView == ContentView.Button1)
        {
            if (_currentUser != null)
            {
                _ = _lobbyManager.EnsureConnectedAsync();
            }
            HandleLobbyInput(mouseState);
            ProcessLobbyKeyboardInput();
        }

        // 处理战斗输入
        if (_currentContentView == ContentView.Battle && battleActive && _battleManager != null)
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
            _achievementSystem.SetUserId(_lobbyManager.UserId);
            _ = _lobbyManager.RequestInventoryAsync();
            _ = _lobbyManager.GetAchievementsAsync();
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

    private void HandleBattleHistoryInput(MouseState mouseState, MouseState previousMouseState)
    {
        if (_currentUser == null)
            return;

        // 获取对战记录列表
        var records = _battleHistoryManager.GetBattleRecordsByPlayer(_currentUser.Username);
        
        int panelX = MenuManager.GetMenuWidth();
        int panelWidth = _graphics.PreferredBackBufferWidth - MenuManager.GetMenuWidth();
        int panelHeight = _graphics.PreferredBackBufferHeight;
        
        // 记录列表的区域
        int listX = panelX + 20;
        int listY = 155; // 标题后的位置
        int listWidth = panelWidth - 40;
        const int recordHeight = 50;
        const int recordSpacing = 5;
        int maxVisibleRecords = (panelHeight - 195) / (recordHeight + recordSpacing);

        bool leftClicked = mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;
        if (!leftClicked)
        {
            // 处理滚轮滚动
            if (mouseState.ScrollWheelValue != _inputManager.PreviousMouseState.ScrollWheelValue)
            {
                int delta = (_inputManager.PreviousMouseState.ScrollWheelValue - mouseState.ScrollWheelValue) / 120;
                _battleHistoryScrollOffset += delta * 15;
                
                int totalHeight = records.Count * (recordHeight + recordSpacing);
                int maxScroll = Math.Max(0, totalHeight - (panelHeight - 195));
                _battleHistoryScrollOffset = Math.Clamp(_battleHistoryScrollOffset, 0, maxScroll);
            }
            return;
        }

        Point mousePoint = new Point(mouseState.X, mouseState.Y);
        Rectangle listAreaRect = new Rectangle(listX, listY, listWidth, panelHeight - 195);

        if (!listAreaRect.Contains(mousePoint))
            return;

        // 计算点击了哪一条记录
        int relativeY = mousePoint.Y - listY + _battleHistoryScrollOffset;
        int clickedIndex = relativeY / (recordHeight + recordSpacing);

        if (clickedIndex >= 0 && clickedIndex < records.Count)
        {
            _selectedBattleRecordIndex = clickedIndex;
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

        if (_itemIconProvider != null)
        {
            _itemIconProvider.TimeProvider = () => (float)gameTime.TotalGameTime.TotalSeconds;
        }

        _spriteBatch.Begin();
        _menuManager.Draw(_spriteBatch, _buttonTexture, _buttonFont, GraphicsDevice);
        _spriteBatch.End();

        // 绘制右侧内容区域（仅在游戏态）
        if (_currentUIState == GameUIState.Game)
        {
            // 如果是战斗界面，绘制战斗
            if (_currentContentView == ContentView.Battle && _battleManager != null && _battleManager.IsBattleActive)
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
            // 对战历史界面
            else if (_currentContentView == ContentView.Button3)
            {
                string playerName = _currentUser?.Username ?? string.Empty;
                _uiManager.DrawBattleHistoryPanel(_spriteBatch, _battleHistoryManager, playerName, _battleHistoryScrollOffset, _selectedBattleRecordIndex);
            }
            // 成就界面
            else if (_currentContentView == ContentView.Button4)
            {
                _uiManager.DrawAchievementPanel(_spriteBatch, _achievementSystem);
            }
            // 战斗界面但非战斗中：保持空白
            else if (_currentContentView == ContentView.Battle)
            {
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
        Console.WriteLine($"[Client] Local achievement completed: {achievement.Name} (ID: {achievement.Id})");
        Console.WriteLine($"[Client] Rewards count: {achievement.Rewards.Count}");
    }

    /// <summary>
    /// 奖励发放事件处理
    /// </summary>
    private void OnRewardGiven(AchievementSystem.Reward reward)
    {
        if (reward.Type == "Item")
        {
            _authStatusMessage += $" - 获得物品: {reward.ItemId}";
            Console.WriteLine($"[Client] Reward given: Item {reward.ItemId} x{reward.Quantity}");
        }
    }

    /// <summary>
    /// 服务端成就完成通知处理
    /// </summary>
    private void OnServerAchievementCompleted(AchievementCompletedNotification notification)
    {
        _authStatusMessage = $"成就完成: {notification.AchievementName}!";
        Console.WriteLine($"[Client] Server achievement completed: {notification.AchievementName} (ID: {notification.AchievementId})");
        Console.WriteLine($"[Client] Completion time: {notification.CompletedTime}");
        Console.WriteLine($"[Client] Rewards: {notification.Rewards?.Count ?? 0}");
        
        foreach (var reward in notification.Rewards)
        {
            if (reward.Type == "Item")
            {
                _authStatusMessage += $" 获得: {reward.ItemId}";
                Console.WriteLine($"[Client] Reward: Item {reward.ItemId} x{reward.Quantity}");
            }
        }
        
        _ = _lobbyManager.RequestInventoryAsync();
    }

    /// <summary>
    /// 从服务器接收成就数据
    /// </summary>
    private void OnAchievementsReceived(List<AchievementDto> achievements)
    {
        Console.WriteLine($"[Client] Received {achievements.Count} achievements from server");
        _achievementSystem.SyncWithServer(achievements);
        _authStatusMessage = $"成就系统已更新，共{achievements.Count}个成就";
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
        Console.WriteLine($"[Client] Battle ended - Winner: {notification.WinnerCamp}");
        
        // 设置战斗结算数据给BattleManager显示
        _battleManager.SetBattleEndNotification(notification);
        
        // 保存对战记录到本地历史
        if (_battleManager?.CurrentBattle != null && _currentUser != null)
        {
            var battleRecord = new BattleRecord
            {
                BattleDateTime = DateTime.Now,
                LocalPlayerName = _currentUser.Username,
                IsMultiplayer = true,
                WinnerName = notification.WinnerCamp,
                DurationSeconds = (int)notification.BattleDuration.TotalSeconds,
                TotalRounds = notification.TotalRounds,
                Notes = $"多人对战 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };
            
            // 从统计数据中获取详细信息
            var myStats = notification.PlayerStats?.FirstOrDefault(s => s.PlayerId == _currentUser.Username);
            if (myStats != null)
            {
                battleRecord.TotalDamageDealt = myStats.TotalDamageDealt;
                battleRecord.TotalDamageTaken = myStats.TotalDamageTaken;
                battleRecord.TotalDamageBlocked = myStats.TotalDamageBlocked;
                battleRecord.KillCount = myStats.KillCount;
                battleRecord.TotalActionTimeSeconds = myStats.TotalActionTime.TotalSeconds;
                battleRecord.IsMVP = myStats.IsMVP;
            }
            
            // 尝试从当前战斗获取更多信息
            if (_battleManager.CurrentBattle.AllPlayers.Count > 0)
            {
                var localPlayer = _battleManager.CurrentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId == _currentUser.Username);
                if (localPlayer != null)
                {
                    battleRecord.LocalPlayerHp = localPlayer.CurrentHP;
                    battleRecord.LocalPlayerLevel = 1;
                }
                
                // 获取对手信息（取第一个对手）
                var opponent = _battleManager.CurrentBattle.AllPlayers.FirstOrDefault(p => p.PlayerId != _currentUser.Username);
                if (opponent != null)
                {
                    battleRecord.OpponentName = opponent.PlayerName;
                    battleRecord.OpponentHp = opponent.CurrentHP;
                    battleRecord.OpponentLevel = 1;
                }
            }
            
            // 从奖励中获取经验值
            var myReward = notification.PlayerRewards?.FirstOrDefault(r => r.PlayerId == _currentUser.Username);
            if (myReward != null)
            {
                battleRecord.ExpGained = myReward.ExpGained;
            }
            
            // 确定对战结果 (0=失败, 1=胜利, 2=平手)
            if (myStats != null)
            {
                bool isWinner = notification.WinnerCamp.Contains(myStats.TeamId == 1 ? "Team1" : "Team2");
                battleRecord.Result = isWinner ? 1 : 0;
            }
            else if (battleRecord.WinnerName == _currentUser.Username)
            {
                battleRecord.Result = 1; // 胜利
            }
            else if (string.IsNullOrEmpty(battleRecord.WinnerName))
            {
                battleRecord.Result = 2; // 平手
            }
            else
            {
                battleRecord.Result = 0; // 失败
            }
            
            _battleHistoryManager.AddBattleRecord(battleRecord);
            Console.WriteLine($"[Client] Battle record saved: {battleRecord.LocalPlayerName} vs {battleRecord.OpponentName}");
        }
    }
    
    /// <summary>
    /// 处理战斗行动请求（发送到服务器）
    /// </summary>
    private async void OnBattleActionRequested(string diceName, string targetPlayerId, int? manualDiceValue)
    {
        if (_lobbyManager != null)
        {
            await _lobbyManager.SendBattleActionAsync(diceName, targetPlayerId, manualDiceValue);
            Console.WriteLine($"[Client] Sent battle action: {diceName} -> {targetPlayerId} (manual:{manualDiceValue})");
        }
    }
    
    /// <summary>
    /// 处理战斗防守请求（发送到服务器）
    /// </summary>
    private async void OnBattleDefenseRequested(string diceName, int? manualDiceValue)
    {
        if (_lobbyManager != null)
        {
            await _lobbyManager.SendBattleDefenseAsync(diceName, manualDiceValue);
            Console.WriteLine($"[Client] Sent battle defense: {diceName} (manual:{manualDiceValue})");
        }
    }

    /// <summary>
    /// 处理战斗认输请求（发送到服务器）
    /// </summary>
    private async void OnBattleSurrenderRequested()
    {
        if (_lobbyManager != null)
        {
            await _lobbyManager.SendBattleSurrenderAsync();
            Console.WriteLine("[Client] Sent battle surrender request");
        }
    }

    /// <summary>
    /// 处理返回大厅请求
    /// </summary>
    private void OnReturnToLobbyRequested()
    {
        Console.WriteLine("[Client] Return to lobby requested");
        
        // 重置战斗状态
        _battleManager.InitializeBattle();
        
        // 返回大厅视图（Button1是Lobby视图）
        _currentContentView = ContentView.Button1;
        
        // 清空消息
        _authStatusMessage = string.Empty;
        
        // 自动退出房间
        if (_lobbyManager != null)
        {
            _lobbyManager.LeaveRoom();
            Console.WriteLine("[Client] Left room after battle end");
        }
    }
}
    /// <summary>

    /// 服务端成就完成通知处理
