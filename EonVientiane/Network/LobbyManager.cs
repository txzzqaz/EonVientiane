using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EonVientiane.Shared;

namespace EonVientiane.Network;

/// <summary>
/// 大厅管理器 - 处理房间列表和加入/创建房间
/// </summary>
public class LobbyManager
{
    private readonly NetworkClient _networkClient;
    
    public List<RoomInfo> RoomList { get; private set; } = new();
    public RoomInfo CurrentRoom { get; private set; }
    public List<PlayerInfo> CurrentRoomPlayers { get; private set; } = new();
    
    // 用户认证相关
    public string UserId { get; private set; }
    public string AuthToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(AuthToken);
    
    public event Action RoomListUpdated;
    public event Action RoomJoined;
    public event Action RoomLeft;
    public event Action RoomUpdated;
    public event Action<string> ErrorOccurred;
    public event Action<GameStartedNotification> GameStarted;
    public event Action<GameStartCountdownNotification> GameStartCountdown;
    public event Action LoginSuccess;
    public event Action RegisterSuccess;
    public event Action<InventoryState> InventoryStateReceived;
    public event Action<string> InventoryError;
    public event Action<List<AchievementDto>> AchievementsReceived;
    public event Action<AchievementCompletedNotification> AchievementCompleted;
    
    // 战斗相关事件
    public event Action<BattleStateUpdateNotification> BattleStateUpdated;
    public event Action<BattleEndNotification> BattleEnded;
    
    public string PlayerName { get; private set; }
    
    public LobbyManager(NetworkClient networkClient)
    {
        _networkClient = networkClient;
        _networkClient.MessageReceived += OnMessageReceived;
    }
    
    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task LoginAsync(string username, string password)
    {
        var request = new UserLoginRequest
        {
            Username = username,
            Password = password
        };
        
        System.Diagnostics.Debug.WriteLine($"[LobbyManager] Sending login request for user: {username}");
        var message = NetworkMessage.Create(MessageType.UserLogin, request);
        await _networkClient.SendMessageAsync(message);
        System.Diagnostics.Debug.WriteLine($"[LobbyManager] Login request sent");
    }
    
    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task RegisterAsync(string username, string password, string email)
    {
        var request = new UserRegisterRequest
        {
            Username = username,
            Password = password,
            Email = email
        };
        
        System.Diagnostics.Debug.WriteLine($"[LobbyManager] Sending register request for user: {username}");
        var message = NetworkMessage.Create(MessageType.UserRegister, request);
        await _networkClient.SendMessageAsync(message);
        System.Diagnostics.Debug.WriteLine($"[LobbyManager] Register request sent");
    }
    
    /// <summary>
    /// 获取初始背包
    /// </summary>
    public async Task GetInitialInventoryAsync()
    {
        if (!IsAuthenticated)
            return;
            
        var request = new GetInitialInventoryRequest
        {
            UserId = UserId ?? string.Empty
        };
        
        var message = NetworkMessage.Create(MessageType.GetInitialInventory, request);
        await _networkClient.SendMessageAsync(message);
    }

    /// <summary>
    /// 请求当前背包
    /// </summary>
    public async Task RequestInventoryAsync()
    {
        if (!IsAuthenticated)
            return;

        var request = new RequestInventory { UserId = UserId ?? string.Empty };
        var message = NetworkMessage.Create(MessageType.RequestInventory, request);
        await _networkClient.SendMessageAsync(message);
    }

    /// <summary>
    /// 请求装备指定堆叠
    /// </summary>
    public async Task EquipItemAsync(string stackId)
    {
        if (!IsAuthenticated)
            return;

        var request = new EquipItemRequest { StackId = stackId };
        var message = NetworkMessage.Create(MessageType.EquipItem, request);
        await _networkClient.SendMessageAsync(message);
    }

    /// <summary>
    /// 请求卸下指定堆叠
    /// </summary>
    public async Task UnequipItemAsync(string stackId)
    {
        if (!IsAuthenticated)
            return;

        var request = new UnequipItemRequest { StackId = stackId };
        var message = NetworkMessage.Create(MessageType.UnequipItem, request);
        await _networkClient.SendMessageAsync(message);
    }
    
    /// <summary>
    /// 请求房间列表
    /// </summary>
    public async Task RequestRoomListAsync()
    {
        var message = NetworkMessage.Create(MessageType.GetRoomList);
        await _networkClient.SendMessageAsync(message);
    }
    
    /// <summary>
    /// 创建房间
    /// </summary>
    public async Task CreateRoomAsync(string roomName, int maxPlayers = int.MaxValue)
    {
        var request = new CreateRoomRequest
        {
            RoomName = roomName,
            MaxPlayers = maxPlayers
        };
        
        var message = NetworkMessage.Create(MessageType.CreateRoom, request);
        await _networkClient.SendMessageAsync(message);
    }
    
    /// <summary>
    /// 加入房间
    /// </summary>
    public async Task JoinRoomAsync(string roomId)
    {
        var request = new JoinRoomRequest
        {
            RoomId = roomId
        };
        
        var message = NetworkMessage.Create(MessageType.JoinRoom, request);
        await _networkClient.SendMessageAsync(message);
    }
    
    /// <summary>
    /// 离开房间
    /// </summary>
    public async Task LeaveRoomAsync()
    {
        var message = NetworkMessage.Create(MessageType.LeaveRoom);
        await _networkClient.SendMessageAsync(message);
    }

    /// <summary>
    /// 切换准备状态
    /// </summary>
    public async Task SetReadyAsync(bool isReady)
    {
        var request = new SetReadyRequest
        {
            IsReady = isReady
        };

        var message = NetworkMessage.Create(MessageType.SetReady, request);
        await _networkClient.SendMessageAsync(message);
    }

    /// <summary>
    /// 选择队伍
    /// </summary>
    public async Task SetTeamAsync(int teamId)
    {
        var request = new SetTeamRequest
        {
            TeamId = teamId
        };

        var message = NetworkMessage.Create(MessageType.SetTeam, request);
        await _networkClient.SendMessageAsync(message);
    }
    
    /// <summary>
    /// 处理服务器消息
    /// </summary>
    private void OnMessageReceived(NetworkMessage message)
    {
        switch (message.Type)
        {
            // 用户认证相关
            case MessageType.UserLoginResponse:
                HandleUserLoginResponse(message);
                break;
            
            case MessageType.UserRegisterResponse:
                HandleUserRegisterResponse(message);
                break;
            
            case MessageType.InitialInventoryResponse:
                HandleInitialInventoryResponse(message);
                break;

            case MessageType.InventoryState:
                HandleInventoryState(message);
                break;

            case MessageType.InventoryUpdated:
                HandleInventoryState(message);
                break;

            case MessageType.EquipItemResponse:
                HandleInventoryActionResponse(message);
                break;

            case MessageType.UnequipItemResponse:
                HandleInventoryActionResponse(message);
                break;
            
            // 大厅相关
            case MessageType.RoomListResponse:
                HandleRoomListResponse(message);
                break;
                
            case MessageType.CreateRoomResponse:
                HandleCreateRoomResponse(message);
                break;
                
            case MessageType.JoinRoomResponse:
                HandleJoinRoomResponse(message);
                break;
                
            case MessageType.RoomUpdate:
                HandleRoomUpdate(message);
                break;
                
            case MessageType.LeaveRoomResponse:
                HandleLeaveRoomResponse();
                break;
            
            case MessageType.GameStarted:
                HandleGameStarted(message);
                break;

            case MessageType.GameStartCountdown:
                HandleGameStartCountdown(message);
                break;
            
            // 战斗相关
            case MessageType.BattleStateUpdate:
                HandleBattleStateUpdate(message);
                break;
            
            case MessageType.BattleEnd:
                HandleBattleEnd(message);
                break;
            
            // 成就相关
            case MessageType.GetAchievementsResponse:
                HandleGetAchievementsResponse(message);
                break;
            
            case MessageType.UpdateAchievementResponse:
                HandleUpdateAchievementResponse(message);
                break;
            
            case MessageType.AchievementCompleted:
                HandleAchievementCompleted(message);
                break;
                
            case MessageType.Error:
                HandleError(message);
                break;
        }
    }
    
    /// <summary>
    /// 发送战斗行动请求
    /// </summary>
    public async Task SendBattleActionAsync(string selectedDiceName, string targetPlayerId, int? manualDiceValue = null)
    {
        if (!IsAuthenticated || string.IsNullOrEmpty(CurrentRoom?.RoomId))
            return;
        
        var request = new BattleActionRequest
        {
            RoomId = CurrentRoom.RoomId,
            PlayerId = UserId,
            SelectedDiceName = selectedDiceName,
            TargetPlayerId = targetPlayerId ?? "",
            ManualDiceValue = manualDiceValue
        };
        
        var message = NetworkMessage.Create(MessageType.BattleActionRequest, request);
        await _networkClient.SendMessageAsync(message);
        System.Diagnostics.Debug.WriteLine($"[LobbyManager] Battle action sent: {selectedDiceName} -> {targetPlayerId}");
    }
    
    /// <summary>
    /// 发送战斗防守请求
    /// </summary>
    public async Task SendBattleDefenseAsync(string selectedDiceName, int? manualDiceValue = null)
    {
        if (!IsAuthenticated || string.IsNullOrEmpty(CurrentRoom?.RoomId))
            return;
        
        var request = new BattleDefenseRequest
        {
            RoomId = CurrentRoom.RoomId,
            PlayerId = UserId,
            SelectedDiceName = selectedDiceName,
            ManualDiceValue = manualDiceValue
        };
        
        var message = NetworkMessage.Create(MessageType.BattleDefenseRequest, request);
        await _networkClient.SendMessageAsync(message);
        System.Diagnostics.Debug.WriteLine($"[LobbyManager] Battle defense sent: {selectedDiceName}");
    }

    /// <summary>
    /// 发送战斗认输请求
    /// </summary>
    public async Task SendBattleSurrenderAsync()
    {
        if (!IsAuthenticated || string.IsNullOrEmpty(CurrentRoom?.RoomId))
            return;

        var request = new BattleSurrenderRequest
        {
            RoomId = CurrentRoom.RoomId,
            PlayerId = UserId
        };

        var message = NetworkMessage.Create(MessageType.BattleSurrenderRequest, request);
        await _networkClient.SendMessageAsync(message);
        System.Diagnostics.Debug.WriteLine("[LobbyManager] Battle surrender sent");
    }
    
    /// <summary>
    /// 处理战斗状态更新
    /// </summary>
    private void HandleBattleStateUpdate(NetworkMessage message)
    {
        var notification = message.GetData<BattleStateUpdateNotification>();
        if (notification != null)
        {
            System.Diagnostics.Debug.WriteLine($"[LobbyManager] Battle state update received");
            BattleStateUpdated?.Invoke(notification);
        }
    }
    
    /// <summary>
    /// 处理战斗结束
    /// </summary>
    private void HandleBattleEnd(NetworkMessage message)
    {
        Console.WriteLine($"[Network.LobbyManager] HandleBattleEnd called");
        var notification = message.GetData<BattleEndNotification>();
        if (notification != null)
        {
            Console.WriteLine($"[Network.LobbyManager] Battle ended: WinnerCamp={notification.WinnerCamp}, TotalRounds={notification.TotalRounds}");
            Console.WriteLine($"[Network.LobbyManager] Player stats count: {notification.PlayerStats?.Count ?? 0}");
            Console.WriteLine($"[Network.LobbyManager] Invoking BattleEnded event...");
            BattleEnded?.Invoke(notification);
            Console.WriteLine($"[Network.LobbyManager] BattleEnded event invoked");
        }
        else
        {
            Console.WriteLine($"[Network.LobbyManager] ERROR: BattleEndNotification is null");
        }
    }
    
    private void HandleUserLoginResponse(NetworkMessage message)
    {
        var response = message.GetData<UserLoginResponse>();
        if (response != null)
        {
            if (response.Success)
            {
                UserId = response.UserId;
                AuthToken = response.Token;
                LoginSuccess?.Invoke();
            }
            else
            {
                ErrorOccurred?.Invoke(response.ErrorMessage ?? "Login failed");
            }
        }
    }
    
    private void HandleUserRegisterResponse(NetworkMessage message)
    {
        var response = message.GetData<UserRegisterResponse>();
        if (response != null)
        {
            if (response.Success)
            {
                RegisterSuccess?.Invoke();
            }
            else
            {
                ErrorOccurred?.Invoke(response.ErrorMessage ?? "Registration failed");
            }
        }
    }
    
    private void HandleInitialInventoryResponse(NetworkMessage message)
    {
        var response = message.GetData<InitialInventoryResponse>();
        if (response != null)
        {
            if (response.Success)
            {
                // 处理初始背包物品
                System.Diagnostics.Debug.WriteLine($"Received {response.Items.Count} initial items");
                InventoryStateReceived?.Invoke(new InventoryState
                {
                    Items = response.Items.ConvertAll(item => new InventoryItemDto
                    {
                        StackId = Guid.NewGuid().ToString("N"),
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        IsEquipped = false
                    })
                });
            }
            else
            {
                ErrorOccurred?.Invoke(response.ErrorMessage ?? "Failed to get initial inventory");
            }
        }
    }

    private void HandleInventoryState(NetworkMessage message)
    {
        var state = message.GetData<InventoryState>();
        if (state != null)
        {
            if (!string.IsNullOrEmpty(state.ErrorMessage))
            {
                InventoryError?.Invoke(state.ErrorMessage);
            }
            InventoryStateReceived?.Invoke(state);
        }
    }

    private void HandleInventoryActionResponse(NetworkMessage message)
    {
        var response = message.GetData<InventoryActionResponse>();
        if (response == null)
            return;

        if (!response.Success)
        {
            InventoryError?.Invoke(response.ErrorMessage ?? "背包操作失败");
            return;
        }

        if (response.State != null)
        {
            InventoryStateReceived?.Invoke(response.State);
        }
    }
    
    private void HandleRoomListResponse(NetworkMessage message)
    {
        var roomList = message.GetData<List<RoomInfo>>();
        if (roomList != null)
        {
            RoomList = roomList;
            RoomListUpdated?.Invoke();
        }
    }
    
    private void HandleCreateRoomResponse(NetworkMessage message)
    {
        var response = message.GetData<CreateRoomResponse>();
        if (response != null)
        {
            if (response.Success)
            {
                // 房间创建成功，等待RoomUpdate消息
            }
            else
            {
                ErrorOccurred?.Invoke(response.ErrorMessage ?? "Failed to create room");
            }
        }
    }
    
    private void HandleJoinRoomResponse(NetworkMessage message)
    {
        var response = message.GetData<JoinRoomResponse>();
        if (response != null)
        {
            if (response.Success && response.RoomInfo != null)
            {
                CurrentRoom = response.RoomInfo;
                CurrentRoomPlayers = response.Players ?? new();
                RoomJoined?.Invoke();
            }
            else
            {
                ErrorOccurred?.Invoke(response.ErrorMessage ?? "Failed to join room");
            }
        }
    }
    
    private void HandleRoomUpdate(NetworkMessage message)
    {
        var update = message.GetData<RoomUpdateNotification>();
        if (update != null)
        {
            CurrentRoom = update.RoomInfo;
            CurrentRoomPlayers = update.Players;
            RoomUpdated?.Invoke();
        }
    }
    
    private void HandleLeaveRoomResponse()
    {
        CurrentRoom = null;
        CurrentRoomPlayers.Clear();
        RoomLeft?.Invoke();
    }
    
    private void HandleError(NetworkMessage message)
    {
        var error = message.GetData<ErrorMessage>();
        if (error != null)
        {
            ErrorOccurred?.Invoke(error.Message);
        }
    }

    private void HandleGameStarted(NetworkMessage message)
    {
        var start = message.GetData<GameStartedNotification>();
        if (start != null)
        {
            GameStarted?.Invoke(start);
        }
    }

    private void HandleGameStartCountdown(NetworkMessage message)
    {
        var countdown = message.GetData<GameStartCountdownNotification>();
        if (countdown != null)
        {
            GameStartCountdown?.Invoke(countdown);
        }
    }

    /// <summary>
    /// 获取成就列表
    /// </summary>
    public async Task GetAchievementsAsync()
    {
        if (!IsAuthenticated)
        {
            Console.WriteLine("[LobbyManager] Cannot get achievements: not authenticated");
            return;
        }

        Console.WriteLine($"[LobbyManager] Requesting achievements for user {UserId}");
        var request = new GetAchievementsRequest
        {
            UserId = UserId ?? string.Empty
        };

        var message = NetworkMessage.Create(MessageType.GetAchievements, request);
        await _networkClient.SendMessageAsync(message);
    }

    /// <summary>
    /// 更新成就进度
    /// </summary>
    public async Task UpdateAchievementAsync(string achievementId, int progressDelta)
    {
        if (!IsAuthenticated)
        {
            Console.WriteLine("[LobbyManager] Cannot update achievement: not authenticated");
            return;
        }

        Console.WriteLine($"[LobbyManager] Updating achievement '{achievementId}' with delta {progressDelta}");
        var request = new UpdateAchievementRequest
        {
            UserId = UserId ?? string.Empty,
            AchievementId = achievementId,
            ProgressDelta = progressDelta
        };

        var message = NetworkMessage.Create(MessageType.UpdateAchievement, request);
        await _networkClient.SendMessageAsync(message);
    }

    private void HandleGetAchievementsResponse(NetworkMessage message)
    {
        try
        {
            var response = message.GetData<GetAchievementsResponse>();
            if (response != null && response.Success)
            {
                Console.WriteLine($"[LobbyManager] Received {response.Achievements.Count} achievements from server");
                AchievementsReceived?.Invoke(response.Achievements);
            }
            else
            {
                string errorMsg = response?.ErrorMessage ?? "Failed to get achievements";
                Console.WriteLine($"[LobbyManager] Get achievements failed: {errorMsg}");
                ErrorOccurred?.Invoke(errorMsg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LobbyManager] Error handling GetAchievementsResponse: {ex.Message}");
            ErrorOccurred?.Invoke($"处理成就列表失败: {ex.Message}");
        }
    }

    private void HandleUpdateAchievementResponse(NetworkMessage message)
    {
        try
        {
            var response = message.GetData<UpdateAchievementResponse>();
            if (response != null)
            {
                if (response.Success)
                {
                    Console.WriteLine($"[LobbyManager] Achievement updated successfully. IsCompleted: {response.IsCompleted}, Progress: {response.Progress}");
                }
                else
                {
                    string errorMsg = response.ErrorMessage ?? "Failed to update achievement";
                    Console.WriteLine($"[LobbyManager] Update achievement failed: {errorMsg}");
                    ErrorOccurred?.Invoke(errorMsg);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LobbyManager] Error handling UpdateAchievementResponse: {ex.Message}");
            ErrorOccurred?.Invoke($"更新成就失败: {ex.Message}");
        }
    }

    private void HandleAchievementCompleted(NetworkMessage message)
    {
        try
        {
            var notification = message.GetData<AchievementCompletedNotification>();
            if (notification != null)
            {
                Console.WriteLine($"[LobbyManager] Achievement completed: {notification.AchievementName} (ID: {notification.AchievementId})");
                Console.WriteLine($"[LobbyManager] Rewards count: {notification.Rewards?.Count ?? 0}");
                AchievementCompleted?.Invoke(notification);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LobbyManager] Error handling AchievementCompleted: {ex.Message}");
            ErrorOccurred?.Invoke($"处理成就完成通知失败: {ex.Message}");
        }
    }
}
