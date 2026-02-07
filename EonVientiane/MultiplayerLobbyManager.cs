using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// 大厅状态
/// </summary>
public enum LobbyState
{
    Disconnected,
    Connecting,
    InLobby,
    InRoom
}

/// <summary>
/// 联机大厅管理器 - 处理大厅UI和逻辑
/// </summary>
public class MultiplayerLobbyManager
{
    private const string DefaultServerHost = "127.0.0.1";
    private const int DefaultServerPort = 7777;

    private readonly Network.NetworkClient _networkClient;
    private readonly Network.LobbyManager _lobbyManager;
    
    private LobbyState _state = LobbyState.Disconnected;
    private string _statusMessage = "Not connected";
    private string _playerName = "Player";
    private readonly string _serverHost = DefaultServerHost;
    private readonly int _serverPort = DefaultServerPort;
    private bool _autoReconnect = true;
    private bool _connectInProgress;
    private int _reconnectAttempts;
    private Task _reconnectTask;
    private readonly object _reconnectLock = new();
    
    public LobbyState State => _state;
    public string StatusMessage => _statusMessage;
    public List<RoomInfo> RoomList => _lobbyManager.RoomList;
    public RoomInfo CurrentRoom => _lobbyManager.CurrentRoom;
    public List<PlayerInfo> CurrentRoomPlayers => _lobbyManager.CurrentRoomPlayers;
    public string PlayerName => _playerName;
    public string ServerHost => _serverHost;
    public int ServerPort => _serverPort;
    public bool LocalReady => IsLocalPlayerReady();
    public bool IsAuthenticated => _lobbyManager.IsAuthenticated;
    public string UserId => _lobbyManager.UserId;
    public event Action<InventoryState> InventoryStateReceived;
    public event Action<string> InventoryError;
    public event Action<GameStartedNotification> GameStarted;
    public event Action<List<AchievementDto>> AchievementsReceived;
    public event Action<AchievementCompletedNotification> AchievementCompleted;
    
    // 战斗相关事件
    public event Action<BattleStateUpdateNotification> BattleStateUpdated;
    public event Action<BattleEndNotification> BattleEnded;
    
    // 本地游戏相关
    private LocalNetworkManager _localNetworkManager;
    private LocalGameServer _localGameServer;
    private bool _isLocalGameMode = false;
    
    public event Action<LocalNetworkManager.LocalHost> LocalHostDiscovered;
    public event Action<string> LocalHostLost;
    public event Action<LocalGameInfo> LocalGameCreated;
    public List<LocalNetworkManager.LocalHost> DiscoveredHosts => _localNetworkManager?.DiscoveredHosts ?? new();
    
    public MultiplayerLobbyManager()
    {
        _networkClient = new Network.NetworkClient();
        _lobbyManager = new Network.LobbyManager(_networkClient);
        _localNetworkManager = new LocalNetworkManager();
        _localGameServer = new LocalGameServer();
        
        // 订阅事件
        _networkClient.Connected += OnConnected;
        _networkClient.Disconnected += OnDisconnected;
        
        _lobbyManager.RoomListUpdated += OnRoomListUpdated;
        _lobbyManager.RoomJoined += OnRoomJoined;
        _lobbyManager.RoomLeft += OnRoomLeft;
        _lobbyManager.RoomUpdated += OnRoomUpdated;
        _lobbyManager.ErrorOccurred += OnError;
        _lobbyManager.GameStarted += OnGameStarted;
        _lobbyManager.GameStartCountdown += OnGameStartCountdown;
        _lobbyManager.LoginSuccess += OnLoginSuccess;
        _lobbyManager.RegisterSuccess += OnRegisterSuccess;
        _lobbyManager.InventoryStateReceived += OnInventoryStateReceived;
        _lobbyManager.InventoryError += OnInventoryError;
        _lobbyManager.AchievementsReceived += OnAchievementsReceived;
        _lobbyManager.AchievementCompleted += OnAchievementCompleted;
        
        // 订阅战斗相关事件
        _lobbyManager.BattleStateUpdated += OnBattleStateUpdated;
        _lobbyManager.BattleEnded += OnBattleEnded;
        
        // 订阅本地网络事件
        _localNetworkManager.HostDiscovered += (host) => LocalHostDiscovered?.Invoke(host);
        _localNetworkManager.HostLost += (username) => LocalHostLost?.Invoke(username);
        _localGameServer.GameCreated += (gameId) => Console.WriteLine($"[Lobby] 本地游戏创建: {gameId}");
    }
    
    public void ConfigurePlayer(string playerName)
    {
        _playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
        _autoReconnect = true;
        
        // 不再这里直接连接，而是等待登录成功后再连接
    }
    
    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task LoginAsync(string username, string password)
    {
        _playerName = username;
        // 登录前先确保连接到服务器
        await EnsureConnectedAsync();
        
        // 确保连接成功后再发送登录请求
        if (_networkClient.IsConnected)
        {
            await _lobbyManager.LoginAsync(username, password);
        }
        else
        {
            _statusMessage = "无法连接到服务器，请稍后重试";
        }
    }
    
    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task RegisterAsync(string username, string password, string email)
    {
        // 注册前先确保连接到服务器
        await EnsureConnectedAsync();
        
        // 确保连接成功后再发送注册请求
        if (_networkClient.IsConnected)
        {
            await _lobbyManager.RegisterAsync(username, password, email);
        }
        else
        {
            _statusMessage = "无法连接到服务器，请稍后重试";
        }
    }
    
    /// <summary>
    /// 获取初始背包
    /// </summary>
    public async Task GetInitialInventoryAsync()
    {
        await _lobbyManager.GetInitialInventoryAsync();
    }

    public async Task RequestInventoryAsync()
    {
        await _lobbyManager.RequestInventoryAsync();
    }

    public async Task EquipItemAsync(string stackId)
    {
        await _lobbyManager.EquipItemAsync(stackId);
    }

    public async Task UnequipItemAsync(string stackId)
    {
        await _lobbyManager.UnequipItemAsync(stackId);
    }

    /// <summary>
    /// 确保已连接，若未连接则尝试连接
    /// </summary>
    public async Task EnsureConnectedAsync()
    {
        if (_connectInProgress || _networkClient.IsConnected)
            return;

        _connectInProgress = true;
        _state = LobbyState.Connecting;
        _statusMessage = $"Connecting to {_serverHost}:{_serverPort}...";

        bool success = await _networkClient.ConnectAsync(_serverHost, _serverPort);

        _connectInProgress = false;

        if (!success)
        {
            _state = LobbyState.Disconnected;
            _statusMessage = "Connection failed";
            ScheduleReconnect();
        }
    }
    
    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        _autoReconnect = false;
        _networkClient.Disconnect();
    }
    
    /// <summary>
    /// 手动重新连接
    /// </summary>
    public async Task ManualReconnectAsync()
    {
        if (_networkClient.IsConnected)
        {
            _statusMessage = "已连接到服务器";
            return;
        }
        
        _autoReconnect = true;
        _reconnectAttempts = 0;
        await EnsureConnectedAsync();
    }
    
    /// <summary>
    /// 刷新房间列表
    /// </summary>
    public async void RefreshRoomList()
    {
        if (_state == LobbyState.Disconnected)
        {
            await EnsureConnectedAsync();
        }
        
        if (_state != LobbyState.InLobby)
            return;
            
        _statusMessage = "Refreshing room list...";
        await _lobbyManager.RequestRoomListAsync();
    }
    
    /// <summary>
    /// 创建房间
    /// </summary>
    public async void CreateRoom(string roomName, int maxPlayers = int.MaxValue)
    {
        if (_state != LobbyState.InLobby)
        {
            _statusMessage = "未连接到大厅";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(roomName))
        {
            _statusMessage = "房间名称不能为空";
            return;
        }
        
        if (roomName.Length > 50)
        {
            _statusMessage = "房间名称过长（最多50个字符）";
            return;
        }
        
        _statusMessage = "Creating room...";
        await _lobbyManager.CreateRoomAsync(roomName, maxPlayers);
    }
    
    /// <summary>
    /// 加入房间
    /// </summary>
    public async void JoinRoom(string roomId)
    {
        if (_state != LobbyState.InLobby)
        {
            _statusMessage = "未连接到大厅";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(roomId))
        {
            _statusMessage = "房间ID无效";
            return;
        }
            
        _statusMessage = "Joining room...";
        await _lobbyManager.JoinRoomAsync(roomId);
    }
    
    /// <summary>
    /// 离开房间
    /// </summary>
    public async void LeaveRoom()
    {
        Console.WriteLine($"[MultiplayerLobbyManager] LeaveRoom called, current state: {_state}");
        
        if (_state != LobbyState.InRoom)
        {
            _statusMessage = "未在房间中";
            Console.WriteLine($"[MultiplayerLobbyManager] Cannot leave room - not in room (current state: {_state})");
            return;
        }
            
        _statusMessage = "Leaving room...";
        Console.WriteLine($"[MultiplayerLobbyManager] Sending leave room request");
        await _lobbyManager.LeaveRoomAsync();
        Console.WriteLine($"[MultiplayerLobbyManager] Leave room request sent");
    }

    /// <summary>
    /// 切换准备状态
    /// </summary>
    public async void ToggleReady()
    {
        if (_state != LobbyState.InRoom)
        {
            _statusMessage = "未在房间中";
            return;
        }

        bool nextReady = !IsLocalPlayerReady();
        _statusMessage = nextReady ? "Ready..." : "Unready";
        await _lobbyManager.SetReadyAsync(nextReady);
    }

    /// <summary>
    /// 手动选择队伍
    /// </summary>
    public async void SelectTeam(int teamId)
    {
        if (_state != LobbyState.InRoom)
        {
            _statusMessage = "未在房间中";
            return;
        }

        _statusMessage = $"选择队伍 {teamId}";
        await _lobbyManager.SetTeamAsync(teamId);
    }

    /// <summary>
    /// 获取成就列表
    /// </summary>
    public async Task GetAchievementsAsync()
    {
        if (!_networkClient.IsConnected)
        {
            _statusMessage = "未连接到服务器";
            return;
        }

        _statusMessage = "获取成就中...";
        await _lobbyManager.GetAchievementsAsync();
    }

    /// <summary>
    /// 更新成就进度
    /// </summary>
    public async Task UpdateAchievementAsync(string achievementId, int progressDelta)
    {
        if (!_networkClient.IsConnected)
        {
            _statusMessage = "未连接到服务器";
            return;
        }

        await _lobbyManager.UpdateAchievementAsync(achievementId, progressDelta);
    }

    private void OnConnected()
    {
        _reconnectAttempts = 0;
        _state = LobbyState.InLobby;
        _statusMessage = $"Connected to {_serverHost}:{_serverPort}";
        
        // 自动请求房间列表
        RefreshRoomList();
    }
    
    private void OnDisconnected()
    {
        _state = LobbyState.Disconnected;
        _statusMessage = "Disconnected from server";

        if (_autoReconnect)
        {
            ScheduleReconnect();
        }
    }
    
    private void OnRoomListUpdated()
    {
        _statusMessage = $"Found {RoomList.Count} room(s)";
    }
    
    private void OnRoomJoined()
    {
        _state = LobbyState.InRoom;
        _statusMessage = $"Joined room: {CurrentRoom?.RoomName}";
    }
    
    private void OnRoomLeft()
    {
        _state = LobbyState.InLobby;
        _statusMessage = "Left room";
        RefreshRoomList();
    }
    
    private void OnRoomUpdated()
    {
        if (CurrentRoom != null)
        {
            _state = LobbyState.InRoom;
            _statusMessage = $"Room updated: {CurrentRoom.RoomName} ({CurrentRoom.Status})";
        }
    }
    
    private void OnError(string error)
    {
        _statusMessage = $"Error: {error}";
    }

    private void OnGameStarted(GameStartedNotification notification)
    {
        _statusMessage = "Game starting...";
        GameStarted?.Invoke(notification);
    }

    private void OnGameStartCountdown(GameStartCountdownNotification notification)
    {
        _statusMessage = $"游戏将在 {notification.CountdownSeconds} 秒后开始";
    }

    private void OnInventoryStateReceived(InventoryState state)
    {
        InventoryStateReceived?.Invoke(state);
    }

    private void OnInventoryError(string error)
    {
        _statusMessage = error;
        InventoryError?.Invoke(error);
    }

    private void OnAchievementsReceived(List<AchievementDto> achievements)
    {
        _statusMessage = $"成就已更新: {achievements.Count}个";
        AchievementsReceived?.Invoke(achievements);
    }

    private void OnAchievementCompleted(AchievementCompletedNotification notification)
    {
        _statusMessage = $"成就完成: {notification.AchievementName}";
        AchievementCompleted?.Invoke(notification);
    }
    
    private void OnLoginSuccess()
    {
        _statusMessage = "Login successful";
        _ = EnsureConnectedAsync();
    }
    
    private void OnRegisterSuccess()
    {
        _statusMessage = "Registration successful, please login";
    }

    private bool IsLocalPlayerReady()
    {
        return GetLocalPlayer() is { IsReady: true };
    }

    private PlayerInfo GetLocalPlayer()
    {
        return _lobbyManager.CurrentRoomPlayers.Find(p =>
            string.Equals(p.PlayerName, _playerName, StringComparison.OrdinalIgnoreCase));
    }

    private void ScheduleReconnect()
    {
        lock (_reconnectLock)
        {
            if (_reconnectTask != null && !_reconnectTask.IsCompleted)
                return;

            _reconnectTask = Task.Run(ReconnectLoopAsync);
        }
    }

    private async Task ReconnectLoopAsync()
    {
        while (_autoReconnect && !_networkClient.IsConnected)
        {
            int delayMs = Math.Min(10000, 1000 * (int)Math.Pow(2, Math.Min(_reconnectAttempts, 4)));
            _statusMessage = $"Reconnecting... ({delayMs / 1000.0:F1}s)";
            await Task.Delay(delayMs);

            if (!_autoReconnect)
                break;

            _reconnectAttempts++;
            await EnsureConnectedAsync();

            if (_networkClient.IsConnected)
                break;
        }
    }
    
    /// <summary>
    /// 发送战斗行动
    /// </summary>
    public async Task SendBattleActionAsync(string selectedDiceName, string targetPlayerId, int? manualDiceValue = null)
    {
        if (!_networkClient.IsConnected)
        {
            _statusMessage = "连接已断开";
            return;
        }
        
        await _lobbyManager.SendBattleActionAsync(selectedDiceName, targetPlayerId, manualDiceValue);
    }
    
    /// <summary>
    /// 发送战斗防守
    /// </summary>
    public async Task SendBattleDefenseAsync(string selectedDiceName, int? manualDiceValue = null)
    {
        if (!_networkClient.IsConnected)
        {
            _statusMessage = "连接已断开";
            return;
        }
        
        await _lobbyManager.SendBattleDefenseAsync(selectedDiceName, manualDiceValue);
    }

    /// <summary>
    /// 发送战斗认输
    /// </summary>
    public async Task SendBattleSurrenderAsync()
    {
        if (!_networkClient.IsConnected)
        {
            _statusMessage = "连接已断开";
            return;
        }

        await _lobbyManager.SendBattleSurrenderAsync();
    }
    
    private void OnBattleStateUpdated(BattleStateUpdateNotification notification)
    {
        BattleStateUpdated?.Invoke(notification);
    }
    
    private void OnBattleEnded(BattleEndNotification notification)
    {
        BattleEnded?.Invoke(notification);
    }

    // ===== 本地游戏功能 =====
    
    /// <summary>
    /// 启动本地游戏模式（局域网模式）
    /// </summary>
    public async Task StartLocalGameModeAsync(string username)
    {
        try
        {
            _isLocalGameMode = true;
            _playerName = username;

            // 启动本地网络发现
            var localIp = LocalNetworkManager.GetLocalIPAddress();
            var hostInfo = new LocalNetworkManager.LocalHost
            {
                Hostname = Environment.MachineName,
                Username = username,
                IpAddress = localIp,
                GamePort = 18888
            };

            await _localNetworkManager.StartDiscoveryAsync(hostInfo);

            // 启动本地游戏服务器
            await _localGameServer.StartAsync();

            _statusMessage = "本地游戏模式已启动";
            Console.WriteLine($"[Lobby] 本地游戏模式启动成功，IP: {localIp}");
        }
        catch (Exception ex)
        {
            _statusMessage = $"启动本地模式失败: {ex.Message}";
            Console.WriteLine($"[Lobby] 启动本地模式失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止本地游戏模式
    /// </summary>
    public void StopLocalGameMode()
    {
        _isLocalGameMode = false;
        _localNetworkManager.StopDiscovery();
        _localGameServer.Stop();
        _statusMessage = "本地游戏模式已停止";
        Console.WriteLine("[Lobby] 本地游戏模式已停止");
    }

    /// <summary>
    /// 创建本地游戏
    /// </summary>
    public async Task<bool> CreateLocalGameAsync(string gameName, int maxPlayers = 2)
    {
        try
        {
            var request = new LocalGameCreateRequest
            {
                GameName = gameName,
                HostUsername = _playerName,
                MaxPlayers = maxPlayers
            };

            var message = NetworkMessage.Create(MessageType.LocalGameCreate, request);
            // 这里可以发送到本地服务器或直接创建
            Console.WriteLine($"[Lobby] 创建本地游戏: {gameName}, 最大玩家数: {maxPlayers}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Lobby] 创建本地游戏失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 加入本地游戏
    /// </summary>
    public async Task<bool> JoinLocalGameAsync(string hostUsername, string gameId)
    {
        try
        {
            // 查找主机
            var host = _localNetworkManager.DiscoveredHosts
                .FirstOrDefault(h => h.Username == hostUsername);

            if (host == null)
            {
                _statusMessage = $"找不到主机: {hostUsername}";
                return false;
            }

            Console.WriteLine($"[Lobby] 加入本地游戏: {gameId} (主机: {hostUsername})");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Lobby] 加入本地游戏失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取本地发现的主机列表
    /// </summary>
    public List<LocalNetworkManager.LocalHost> GetDiscoveredHosts()
    {
        return _localNetworkManager.DiscoveredHosts;
    }

    /// <summary>
    /// 获取活跃的本地游戏列表
    /// </summary>
    public List<LocalGameInfo> GetActiveLocalGames()
    {
        return _localGameServer.GetActiveGames();
    }

    /// <summary>
    /// 是否处于本地游戏模式
    /// </summary>
    public bool IsLocalGameMode => _isLocalGameMode;
}

