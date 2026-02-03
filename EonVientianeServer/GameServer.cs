using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EonVientiane.Shared;
using EonVientiane;

namespace EonVientianeServer;

/// <summary>
/// 游戏服务器
/// </summary>
public class GameServer
{
    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    
    private readonly Dictionary<string, ConnectedClient> _clients = new();
    private readonly Dictionary<string, GameRoom> _rooms = new();
    private readonly Dictionary<string, CancellationTokenSource> _roomCountdowns = new();
    private readonly UserManager _userManager = new();
    private readonly InventoryStore _inventoryStore;
    private readonly AchievementManager _achievementManager = new();
    private readonly TimeSpan _gameStartDelay = TimeSpan.FromSeconds(3);
    private readonly object _lock = new();

    private class AbsoluteLuckState
    {
        public int Streak { get; set; }
        public int ProgressSynced { get; set; }
        public int? UniformValue { get; set; }
    }

    private readonly Dictionary<string, AbsoluteLuckState> _absoluteLuckStates = new();
    
    public bool IsRunning { get; private set; }
    
    public GameServer(int port = 7777)
    {
        _port = port;
        _inventoryStore = new InventoryStore("data/users", _userManager);
    }

    
    /// <summary>
    /// 启动服务器
    /// </summary>
    public async Task StartAsync()
    {
        if (IsRunning)
            return;
            
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        
        IsRunning = true;
        Console.WriteLine($"[Server] Started on port {_port}");
        
        // 开始接受客户端连接
        _ = Task.Run(() => AcceptClientsAsync(_cts.Token));
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 停止服务器
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
            return;
            
        Console.WriteLine("[Server] Stopping...");
        
        _cts?.Cancel();
        _listener?.Stop();
        
        // 断开所有客户端
        lock (_lock)
        {
            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }
            _clients.Clear();
            _rooms.Clear();
        }
        
        IsRunning = false;
        Console.WriteLine("[Server] Stopped");
    }
    
    /// <summary>
    /// 接受客户端连接
    /// </summary>
    private async Task AcceptClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                var playerId = Guid.NewGuid().ToString();
                var client = new ConnectedClient(tcpClient, playerId);
                
                lock (_lock)
                {
                    _clients[playerId] = client;
                }
                
                Console.WriteLine($"[Server] Client connected: {playerId}");
                
                // 为每个客户端创建处理任务
                _ = Task.Run(() => HandleClientAsync(client, ct));
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                Console.WriteLine($"[Error] Accept client failed: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 处理客户端消息
    /// </summary>
    private async Task HandleClientAsync(ConnectedClient client, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && client.TcpClient.Connected)
            {
                var message = await client.ReceiveMessageAsync();
                
                if (message == null)
                    break;
                    
                await ProcessMessageAsync(client, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Handle client {client.PlayerName} failed: {ex.Message}");
        }
        finally
        {
            DisconnectClient(client);
        }
    }
    
    /// <summary>
    /// 处理消息
    /// </summary>
    private async Task ProcessMessageAsync(ConnectedClient client, NetworkMessage message)
    {
        Console.WriteLine($"[Server] Received {message.Type} from {client.PlayerName}");
        
        switch (message.Type)
        {
            case MessageType.Ping:
                client.LastPingTime = DateTime.UtcNow;
                await client.SendMessageAsync(NetworkMessage.Create(MessageType.Pong));
                break;
            
            // 用户认证相关
            case MessageType.UserLogin:
                await HandleUserLoginAsync(client, message);
                break;
            
            case MessageType.UserRegister:
                await HandleUserRegisterAsync(client, message);
                break;
            
            case MessageType.GetInitialInventory:
                await HandleGetInitialInventoryAsync(client, message);
                break;

            case MessageType.RequestInventory:
                await HandleRequestInventoryAsync(client);
                break;
            
            case MessageType.EquipItem:
                await HandleEquipItemAsync(client, message);
                break;
            
            case MessageType.UnequipItem:
                await HandleUnequipItemAsync(client, message);
                break;
                
            // 大厅相关 - 需要先认证
            case MessageType.GetRoomList:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleGetRoomListAsync(client);
                break;
                
            case MessageType.CreateRoom:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleCreateRoomAsync(client, message);
                break;
                
            case MessageType.JoinRoom:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleJoinRoomAsync(client, message);
                break;
                
            case MessageType.LeaveRoom:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleLeaveRoomAsync(client);
                break;
            
            case MessageType.SetReady:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleSetReadyAsync(client, message);
                break;

            case MessageType.SetTeam:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleSetTeamAsync(client, message);
                break;
            
            // 战斗相关
            case MessageType.BattleActionRequest:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleBattleActionAsync(client, message);
                break;
            
            case MessageType.BattleDefenseRequest:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleBattleDefenseAsync(client, message);
                break;

            case MessageType.BattleSurrenderRequest:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleBattleSurrenderAsync(client, message);
                break;
            
            // 成就相关
            case MessageType.GetAchievements:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleGetAchievementsAsync(client);
                break;
            
            case MessageType.UpdateAchievement:
                if (!client.IsAuthenticated)
                {
                    await SendErrorAsync(client, "请先登录");
                    break;
                }
                await HandleUpdateAchievementAsync(client, message);
                break;
                
            default:
                Console.WriteLine($"[Warning] Unhandled message type: {message.Type}");
                break;
        }
    }
    
    /// <summary>
    /// 处理用户登录请求
    /// </summary>
    private async Task HandleUserLoginAsync(ConnectedClient client, NetworkMessage message)
    {
        Console.WriteLine($"[Server] Processing login request from {client.PlayerName}");
        var request = message.GetData<UserLoginRequest>();
        
        if (request == null)
        {
            Console.WriteLine($"[Server] Login request is null or invalid");
            await SendErrorAsync(client, "Invalid login request");
            return;
        }
        
        Console.WriteLine($"[Server] Login request - Username: {request.Username}");
        var (success, userId, token, error) = _userManager.Login(request.Username, request.Password);
        
        if (success && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token))
        {
            var oldPlayerId = client.PlayerId;
            client.UserId = userId;
            client.AuthToken = token;
            client.PlayerName = request.Username;
            
            // 更新_clients字典，使用UserId作为key
            lock (_lock)
            {
                _clients.Remove(oldPlayerId);
                _clients[userId] = client;
            }
            
            var response = NetworkMessage.Create(MessageType.UserLoginResponse, new UserLoginResponse
            {
                Success = true,
                UserId = userId,
                Token = token
            });
            
            await client.SendMessageAsync(response);
            Console.WriteLine($"[Server] User '{request.Username}' authenticated: {userId}");
        }
        else
        {
            Console.WriteLine($"[Server] Login failed for '{request.Username}': {error}");
            var response = NetworkMessage.Create(MessageType.UserLoginResponse, new UserLoginResponse
            {
                Success = false,
                ErrorMessage = error ?? "Login failed"
            });
            
            await client.SendMessageAsync(response);
        }
    }
    
    /// <summary>
    /// 处理用户注册请求
    /// </summary>
    private async Task HandleUserRegisterAsync(ConnectedClient client, NetworkMessage message)
    {
        Console.WriteLine($"[Server] Processing register request from {client.PlayerName}");
        var request = message.GetData<UserRegisterRequest>();
        
        if (request == null)
        {
            Console.WriteLine($"[Server] Register request is null or invalid");
            await SendErrorAsync(client, "Invalid register request");
            return;
        }
        
        Console.WriteLine($"[Server] Register request - Username: {request.Username}, Email: {request.Email}");
        var (success, userId, error) = _userManager.Register(request.Username, request.Password, request.Email);
        
        if (success && !string.IsNullOrEmpty(userId))
        {
            var response = NetworkMessage.Create(MessageType.UserRegisterResponse, new UserRegisterResponse
            {
                Success = true,
                UserId = userId
            });
            
            await client.SendMessageAsync(response);
            Console.WriteLine($"[Server] User '{request.Username}' registered: {userId}");
        }
        else
        {
            Console.WriteLine($"[Server] Registration failed for '{request.Username}': {error}");
            var response = NetworkMessage.Create(MessageType.UserRegisterResponse, new UserRegisterResponse
            {
                Success = false,
                ErrorMessage = error ?? "Registration failed"
            });
            
            await client.SendMessageAsync(response);
        }
    }
    
    /// <summary>
    /// 处理获取初始背包请求
    /// </summary>
    private async Task HandleGetInitialInventoryAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<GetInitialInventoryRequest>();
        
        if (request == null || string.IsNullOrEmpty(request.UserId))
        {
            var errorResponse = NetworkMessage.Create(MessageType.InitialInventoryResponse, new InitialInventoryResponse
            {
                Success = false,
                ErrorMessage = "Invalid request"
            });
            
            await client.SendMessageAsync(errorResponse);
            return;
        }
        
        var items = ItemInitializer.GetInitialInventory(request.UserId);
        
        var response = NetworkMessage.Create(MessageType.InitialInventoryResponse, new InitialInventoryResponse
        {
            Success = true,
            Items = items
        });
        
        await client.SendMessageAsync(response);
        Console.WriteLine($"[Server] Initial inventory sent to {request.UserId}");
    }

    /// <summary>
    /// 处理完整背包请求
    /// </summary>
    private async Task HandleRequestInventoryAsync(ConnectedClient client)
    {
        if (!client.IsAuthenticated || string.IsNullOrEmpty(client.UserId))
        {
            await SendErrorAsync(client, "请先登录");
            return;
        }

        // 根据是否为测试账号选择不同的初始化方法
        var initialFactory = _userManager.IsTestAccount(client.UserId)
            ? (Func<List<InitialInventoryItem>>)ItemInitializer.GetTestAccountInventory
            : () => ItemInitializer.GetInitialInventory(client.UserId);
            
        var state = _inventoryStore.LoadOrCreate(client.UserId, initialFactory);
        var dto = _inventoryStore.ToDto(state);
        await client.SendMessageAsync(NetworkMessage.Create(MessageType.InventoryState, dto));
    }

    /// <summary>
    /// 处理装备请求
    /// </summary>
    private async Task HandleEquipItemAsync(ConnectedClient client, NetworkMessage message)
    {
        if (!client.IsAuthenticated || string.IsNullOrEmpty(client.UserId))
        {
            await SendErrorAsync(client, "请先登录");
            return;
        }

        var request = message.GetData<EquipItemRequest>();
        if (request == null || string.IsNullOrEmpty(request.StackId))
        {
            await SendInventoryActionErrorAsync(client, MessageType.EquipItemResponse, "无效的装备请求");
            return;
        }

        var initialFactory = _userManager.IsTestAccount(client.UserId)
            ? (Func<List<InitialInventoryItem>>)ItemInitializer.GetTestAccountInventory
            : () => ItemInitializer.GetInitialInventory(client.UserId);
        var state = _inventoryStore.LoadOrCreate(client.UserId, initialFactory);
        var stack = state.Items.FirstOrDefault(i => i.StackId == request.StackId);

        if (stack == null)
        {
            await SendInventoryActionErrorAsync(client, MessageType.EquipItemResponse, "未找到指定物品");
            return;
        }

        stack.IsEquipped = true;
        // 调整顺序：让装备顺序与选择顺序一致（最近装备的放在最后）
        state.Items.Remove(stack);
        state.Items.Add(stack);
        state = _inventoryStore.Save(state);
        await SendInventoryUpdatedAsync(client, state, MessageType.EquipItemResponse);
    }

    /// <summary>
    /// 处理卸下请求
    /// </summary>
    private async Task HandleUnequipItemAsync(ConnectedClient client, NetworkMessage message)
    {
        if (!client.IsAuthenticated || string.IsNullOrEmpty(client.UserId))
        {
            await SendErrorAsync(client, "请先登录");
            return;
        }

        var request = message.GetData<UnequipItemRequest>();
        if (request == null || string.IsNullOrEmpty(request.StackId))
        {
            await SendInventoryActionErrorAsync(client, MessageType.UnequipItemResponse, "无效的卸下请求");
            return;
        }

        var initialFactory = _userManager.IsTestAccount(client.UserId)
            ? (Func<List<InitialInventoryItem>>)ItemInitializer.GetTestAccountInventory
            : () => ItemInitializer.GetInitialInventory(client.UserId);
        var state = _inventoryStore.LoadOrCreate(client.UserId, initialFactory);
        var stack = state.Items.FirstOrDefault(i => i.StackId == request.StackId);

        if (stack == null)
        {
            await SendInventoryActionErrorAsync(client, MessageType.UnequipItemResponse, "未找到指定物品");
            return;
        }

        stack.IsEquipped = false;
        state = _inventoryStore.Save(state);
        await SendInventoryUpdatedAsync(client, state, MessageType.UnequipItemResponse);
    }

    private async Task SendInventoryUpdatedAsync(ConnectedClient client, UserInventoryStateData state, MessageType responseType)
    {
        var dto = _inventoryStore.ToDto(state);
        var response = new InventoryActionResponse
        {
            Success = true,
            State = dto
        };

        await client.SendMessageAsync(NetworkMessage.Create(responseType, response));
        await client.SendMessageAsync(NetworkMessage.Create(MessageType.InventoryUpdated, dto));
    }

    private async Task SendInventoryActionErrorAsync(ConnectedClient client, MessageType responseType, string error)
    {
        var response = new InventoryActionResponse
        {
            Success = false,
            ErrorMessage = error
        };

        await client.SendMessageAsync(NetworkMessage.Create(responseType, response));
    }
    
    /// <summary>
    /// 处理获取房间列表请求
    /// </summary>
    private async Task HandleGetRoomListAsync(ConnectedClient client)
    {
        List<RoomInfo> roomList;
        
        lock (_lock)
        {
            roomList = _rooms.Values
                .Where(r => r.Status != RoomStatus.InGame)
                .Select(r => r.ToRoomInfo())
                .ToList();
        }
        
        var response = NetworkMessage.Create(MessageType.RoomListResponse, roomList);
        await client.SendMessageAsync(response);
    }
    
    /// <summary>
    /// 处理创建房间请求
    /// </summary>
    private async Task HandleCreateRoomAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<CreateRoomRequest>();
        
        if (request == null)
        {
            await SendErrorAsync(client, "Invalid create room request");
            return;
        }
        
        // 检查房间名称
        if (string.IsNullOrWhiteSpace(request.RoomName))
        {
            await SendErrorAsync(client, "房间名称不能为空");
            return;
        }
        
        // 检查房间名称长度
        if (request.RoomName.Length > 50)
        {
            await SendErrorAsync(client, "房间名称过长（最多50个字符）");
            return;
        }
        
        // 检查客户端是否已经在房间中
        if (client.CurrentRoomId != null)
        {
            await SendErrorAsync(client, "您已经在另一个房间中");
            return;
        }
        
        // 创建房间
        var roomId = Guid.NewGuid().ToString();
        var room = new GameRoom(roomId, request.RoomName, int.MaxValue, client);
        
        lock (_lock)
        {
            _rooms[roomId] = room;
        }
        
        client.CurrentRoomId = roomId;
        
        Console.WriteLine($"[Server] Room created: {request.RoomName} ({roomId}) by {client.PlayerName}");
        
        var response = NetworkMessage.Create(MessageType.CreateRoomResponse, new CreateRoomResponse
        {
            Success = true,
            RoomId = roomId
        });
        
        await client.SendMessageAsync(response);
        
        // 通知房间更新
        await BroadcastRoomUpdateAsync(room);
    }
    
    /// <summary>
    /// 处理加入房间请求
    /// </summary>
    private async Task HandleJoinRoomAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<JoinRoomRequest>();
        
        if (request == null)
        {
            await SendErrorAsync(client, "Invalid join room request");
            return;
        }
        
        // 检查客户端是否已经在房间中
        if (client.CurrentRoomId != null)
        {
            await SendErrorAsync(client, "您已经在另一个房间中，请先离开");
            return;
        }
        
        GameRoom? room = null;
        bool roomExists = false;
        
        lock (_lock)
        {
            roomExists = _rooms.TryGetValue(request.RoomId, out room);
        }
        
        if (!roomExists || room == null)
        {
            await SendErrorAsync(client, "房间不存在");
            return;
        }
        
        bool canJoin;
        bool cancelCountdown = false;
        lock (_lock)
        {
            canJoin = room.AddPlayer(client);
            cancelCountdown = _roomCountdowns.ContainsKey(room.RoomId) || room.Status == RoomStatus.Countdown;
        }
        
        if (!canJoin)
        {
            if (room.IsFull)
            {
                await SendErrorAsync(client, "房间已满");
            }
            else if (room.Status == RoomStatus.InGame)
            {
                await SendErrorAsync(client, "游戏已开始，无法加入");
            }
            else
            {
                await SendErrorAsync(client, "无法加入房间");
            }
            return;
        }
        
        Console.WriteLine($"[Server] {client.PlayerName} joined room {room.RoomName}");

        client.IsReady = false;

        if (cancelCountdown)
        {
            CancelCountdown(room);
        }
        
        var response = NetworkMessage.Create(MessageType.JoinRoomResponse, new JoinRoomResponse
        {
            Success = true,
            RoomInfo = room.ToRoomInfo(),
            Players = room.GetPlayerInfoList()
        });
        
        await client.SendMessageAsync(response);
        
        // 通知房间内所有玩家
        await BroadcastRoomUpdateAsync(room);
    }
    
    /// <summary>
    /// 处理离开房间请求
    /// </summary>
    private async Task HandleLeaveRoomAsync(ConnectedClient client)
    {
        if (client.CurrentRoomId == null)
        {
            await SendErrorAsync(client, "您不在任何房间中");
            return;
        }
        
        await RemovePlayerFromRoomAsync(client);
        
        // 发送离开房间响应
        var response = NetworkMessage.Create(MessageType.LeaveRoomResponse);
        await client.SendMessageAsync(response);
    }

    /// <summary>
    /// 处理准备状态变更
    /// </summary>
    private async Task HandleSetReadyAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<SetReadyRequest>();
        if (request == null)
        {
            await SendErrorAsync(client, "Invalid ready request");
            return;
        }

        if (client.CurrentRoomId == null)
        {
            await SendErrorAsync(client, "Not in a room");
            return;
        }

        GameRoom? room;
        bool shouldCancelCountdown = false;
        bool shouldStartCountdown = false;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(client.CurrentRoomId, out room))
            {
                room = null;
            }
            else
            {
                room.SetPlayerReady(client.UserId, request.IsReady);
                client.IsReady = request.IsReady;

                room.EnsureTeamsAssignedForAll();

                if (room.Status == RoomStatus.Countdown && !room.AreAllPlayersReady())
                {
                    shouldCancelCountdown = true;
                }
                else if (room.Status == RoomStatus.Waiting && room.AreAllPlayersReady())
                {
                    shouldStartCountdown = true;
                }
            }
        }

        if (room == null)
        {
            await SendErrorAsync(client, "Room not found");
            return;
        }

        if (shouldCancelCountdown)
        {
            CancelCountdown(room);
            await BroadcastRoomUpdateAsync(room);
            return;
        }

        if (shouldStartCountdown)
        {
            await BeginCountdownAsync(room);
        }
        else
        {
            await BroadcastRoomUpdateAsync(room);
        }
    }

    private async Task HandleSetTeamAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<SetTeamRequest>();
        if (request == null)
        {
            await SendErrorAsync(client, "Invalid team request");
            return;
        }

        if (client.CurrentRoomId == null)
        {
            await SendErrorAsync(client, "Not in a room");
            return;
        }

        GameRoom? room;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(client.CurrentRoomId, out room))
            {
                room = null;
            }
            else
            {
                room.SetPlayerTeam(client.UserId, request.TeamId);
                room.EnsureTeamAssigned(client.UserId);
            }
        }

        if (room == null)
        {
            await SendErrorAsync(client, "Room not found");
            return;
        }

        await BroadcastRoomUpdateAsync(room);
    }
    
    /// <summary>
    /// 从房间移除玩家
    /// </summary>
    private async Task RemovePlayerFromRoomAsync(ConnectedClient client)
    {
        if (client.CurrentRoomId == null)
            return;
            
        GameRoom? room = null;
        bool roomEmpty = false;
        bool cancelCountdown = false;
        
        lock (_lock)
        {
            if (_rooms.TryGetValue(client.CurrentRoomId, out room))
            {
                cancelCountdown = _roomCountdowns.ContainsKey(room.RoomId) || room.Status == RoomStatus.Countdown;
                roomEmpty = !room.RemovePlayer(client.UserId);
                
                if (roomEmpty)
                {
                    _rooms.Remove(client.CurrentRoomId);
                    Console.WriteLine($"[Server] Room {room.RoomName} closed (empty)");
                }
            }
        }
        
        client.CurrentRoomId = null;
        client.IsReady = false;
        client.TeamId = 0;
        
        Console.WriteLine($"[Server] {client.PlayerName} left room");

        if (cancelCountdown && room != null)
        {
            CancelCountdown(room);
        }
        
        // 通知房间更新
        if (room != null && !roomEmpty)
        {
            await BroadcastRoomUpdateAsync(room);
        }
    }
    
    /// <summary>
    /// 广播房间更新
    /// </summary>
    private async Task BroadcastRoomUpdateAsync(GameRoom room)
    {
        var update = new RoomUpdateNotification
        {
            RoomInfo = room.ToRoomInfo(),
            Players = room.GetPlayerInfoList()
        };
        
        var message = NetworkMessage.Create(MessageType.RoomUpdate, update);
        
        foreach (var player in room.Players)
        {
            await player.SendMessageAsync(message);
        }
    }

    private async Task BeginCountdownAsync(GameRoom room)
    {
        CancellationTokenSource cts;
        DateTime startTimeUtc;

        lock (_lock)
        {
            if (_roomCountdowns.ContainsKey(room.RoomId))
                return;

            cts = new CancellationTokenSource();
            _roomCountdowns[room.RoomId] = cts;
            room.Status = RoomStatus.Countdown;
            startTimeUtc = DateTime.UtcNow.Add(_gameStartDelay);
            room.SetCountdownEnd(startTimeUtc);
        }

        await BroadcastRoomUpdateAsync(room);
        await BroadcastCountdownAsync(room, startTimeUtc);

        try
        {
            await Task.Delay(_gameStartDelay, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        bool canStart;
        lock (_lock)
        {
            canStart = room.AreAllPlayersReady();
            if (!canStart)
            {
                room.Status = RoomStatus.Waiting;
                room.SetCountdownEnd(null);
                _roomCountdowns.Remove(room.RoomId);
            }
        }

        if (!canStart)
        {
            await BroadcastRoomUpdateAsync(room);
            return;
        }

        await StartGameAsync(room, startTimeUtc);
    }

    private async Task BroadcastCountdownAsync(GameRoom room, DateTime startTimeUtc)
    {
        var seconds = (int)Math.Max(0, Math.Ceiling((startTimeUtc - DateTime.UtcNow).TotalSeconds));

        var notice = new GameStartCountdownNotification
        {
            RoomId = room.RoomId,
            CountdownSeconds = seconds,
            StartTimeUtc = startTimeUtc
        };

        var message = NetworkMessage.Create(MessageType.GameStartCountdown, notice);

        foreach (var player in room.Players)
        {
            await player.SendMessageAsync(message);
        }
    }

    private void CancelCountdown(GameRoom room)
    {
        CancellationTokenSource? cts = null;
        lock (_lock)
        {
            if (_roomCountdowns.TryGetValue(room.RoomId, out cts))
            {
                _roomCountdowns.Remove(room.RoomId);
            }

            room.Status = RoomStatus.Waiting;
            room.SetCountdownEnd(null);
        }

        cts?.Cancel();
    }

    private async Task StartGameAsync(GameRoom room, DateTime? startTimeUtcOverride = null)
    {
        lock (_lock)
        {
            room.Status = RoomStatus.InGame;
            room.SetCountdownEnd(null);
            _roomCountdowns.Remove(room.RoomId);
        }

        var startNotice = new GameStartedNotification
        {
            RoomId = room.RoomId,
                StartTimeUtc = startTimeUtcOverride ?? DateTime.UtcNow,
                Players = room.GetPlayerInfoList()
        };

        var message = NetworkMessage.Create(MessageType.GameStarted, startNotice);

        foreach (var player in room.Players)
        {
            await player.SendMessageAsync(message);
        }

        await BroadcastRoomUpdateAsync(room);
        
        // 初始化服务器端战斗
        await InitializeServerBattleAsync(room);
    }
    
    /// <summary>
    /// 初始化服务器端战斗
    /// </summary>
    private async Task InitializeServerBattleAsync(GameRoom room)
    {
        try
        {
            var clients = room.Players.ToList();
            var serverBattle = new ServerBattle(room.RoomId, clients);
            
            // 获取每个玩家的装备信息
            var playerEquipment = new Dictionary<string, List<Equipment>>();
            
            foreach (var client in clients)
            {
                var initialFactory = _userManager.IsTestAccount(client.UserId)
                    ? (Func<List<InitialInventoryItem>>)ItemInitializer.GetTestAccountInventory
                    : () => ItemInitializer.GetInitialInventory(client.UserId);
                var inventoryState = _inventoryStore.LoadOrCreate(client.UserId, initialFactory);
                
                var equippedItems = inventoryState.Items
                    .Where(item => item.IsEquipped)
                    .Select(item => ItemInitializer.CreateItemFromStackData(item))
                    .OfType<Equipment>()
                    .ToList();
                
                playerEquipment[client.UserId] = equippedItems;
            }
            
            // 初始化战斗
            serverBattle.InitializeBattle(playerEquipment);
            room.CurrentBattle = serverBattle;
            
            Console.WriteLine($"[Server] Battle initialized for room {room.RoomName}");
            
            // 广播战斗初始化信息
            await BroadcastBattleStateAsync(room, serverBattle);
            
            // 启动战斗循环
            _ = RunBattleLoopAsync(room, serverBattle);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to initialize battle: {ex.Message}");
            await SendErrorToRoomAsync(room, "战斗初始化失败");
        }
    }
    
    /// <summary>
    /// 运行战斗循环
    /// </summary>
    private async Task RunBattleLoopAsync(GameRoom room, ServerBattle battle)
    {
        // 定期更新战斗状态，每秒调用一次
        var battleUpdateInterval = 1000; // 毫秒
        
        while (!battle.IsBattleOver && room.Status == RoomStatus.InGame)
        {
            try
            {
                // 更新战斗逻辑
                battle.Update();
                
                // 如果战斗结束，先发送一次最终状态，然后发送结束通知
                if (battle.IsBattleOver)
                {
                    // 发送最终的战斗状态（显示战斗已结束）
                    await BroadcastBattleStateAsync(room, battle);
                    
                    // 广播战斗结束
                    await BroadcastBattleEndAsync(room, battle);
                    break;
                }
                
                // 广播常规更新
                await BroadcastBattleStateAsync(room, battle);
                
                await Task.Delay(battleUpdateInterval);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Battle loop error: {ex.Message}");
                break;
            }
        }
    }
    
    /// <summary>
    /// 广播战斗状态更新
    /// </summary>
    private async Task BroadcastBattleStateAsync(GameRoom room, ServerBattle battle)
    {
        var notification = new BattleStateUpdateNotification
        {
            RoomId = room.RoomId,
            CurrentRound = battle.CurrentRound,
            CurrentState = battle.CurrentState.ToString(),
            CurrentCamp = battle.CurrentCamp.ToString(),
            CurrentActionPlayerId = battle.CurrentActionPlayerId,
            CurrentActionPlayerName = battle.CurrentActionPlayerId != null 
                ? battle.GetPlayer(battle.CurrentActionPlayerId)?.PlayerName ?? "" 
                : "",
            WaitingInputPlayerId = battle.CurrentInputContext != BattleInputContext.None 
                ? (battle.CurrentInputContext == BattleInputContext.AttackSelection ? battle.CurrentActionPlayerId : battle.CurrentDefenderPlayerId)
                : "",
            InputContext = battle.CurrentInputContext.ToString(),
            IsBattleOver = battle.IsBattleOver,
            WinnerCamp = battle.WinnerCamp?.ToString() ?? ""
        };
        
        // 添加玩家状态
        foreach (var player in battle.GetAllPlayers())
        {
            notification.Players.Add(new BattlePlayerStateDto
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                TeamId = player.Camp == PlayerCamp.Team1 ? 1 : 2,
                CurrentHP = player.CurrentHP,
                MaxHP = player.MaxHP,
                ShieldLayers = player.ShieldLayers,
                IsDead = player.IsDead,
                EquippedDiceNames = player.GetEquippedDice().Select(d => d.Name).ToList()
            });
        }
        
        // 为等待输入的玩家添加可用选项
        if (battle.CurrentActionPlayerId != null)
        {
            var availableAD = battle.GetAvailableActiveDice(battle.CurrentActionPlayerId);
            notification.AvailableActiveDiceNames = availableAD.Select(d => d.Name).ToList();
            
            var availableOpponents = battle.GetAvailableOpponents();
            notification.AvailableOpponentIds = availableOpponents.Select(p => p.PlayerId).ToList();
        }
        
        if (battle.CurrentDefenderPlayerId != null)
        {
            var availablePD = battle.GetAvailablePassiveDice(battle.CurrentDefenderPlayerId);
            notification.AvailablePassiveDiceNames = availablePD.Select(d => d.Name).ToList();
        }
        
        // 获取新的战斗日志
        var newLogs = battle.GetNewBattleLogs();
        notification.NewBattleLogs = newLogs;
        
        var message = NetworkMessage.Create(MessageType.BattleStateUpdate, notification);
        
        foreach (var client in room.Players)
        {
            await client.SendMessageAsync(message);
        }
    }

    /// <summary>
    /// 处理"绝对幸运"成就：连胜6局且期间掷出的点数保持一致
    /// </summary>
    private void HandleAbsoluteLuckAchievement(ServerBattle battle)
    {
        var rollUniformity = battle.GetPlayerRollUniformity();
        var players = battle.GetAllPlayers();
        var winnerCamp = battle.WinnerCamp;

        var winners = winnerCamp.HasValue
            ? players.Where(p => p.Camp == winnerCamp.Value).ToList()
            : new List<Player>();

        var losers = winnerCamp.HasValue
            ? players.Where(p => p.Camp != winnerCamp.Value).ToList()
            : players;

        foreach (var loser in losers)
        {
            ResetAbsoluteLuckState(loser.PlayerId);
        }

        foreach (var winner in winners)
        {
            if (!rollUniformity.TryGetValue(winner.PlayerId, out var info) || !info.hasRolls || !info.uniformValue.HasValue)
            {
                ResetAbsoluteLuckState(winner.PlayerId);
                continue;
            }

            var state = GetAbsoluteLuckState(winner.PlayerId);

            if (state.UniformValue.HasValue && state.UniformValue.Value == info.uniformValue.Value)
            {
                state.Streak += 1;
            }
            else
            {
                state.Streak = 1;
            }

            state.UniformValue = info.uniformValue.Value;
            SyncAbsoluteLuckProgress(winner.PlayerId, state);
        }
    }

    private AbsoluteLuckState GetAbsoluteLuckState(string userId)
    {
        if (!_absoluteLuckStates.TryGetValue(userId, out var state))
        {
            state = new AbsoluteLuckState
            {
                Streak = 0,
                ProgressSynced = 0,
                UniformValue = null
            };

            var achievements = _achievementManager.GetUserAchievements(userId);
            var absoluteLuck = achievements.FirstOrDefault(a => a.Id == "absolute_luck");
            if (absoluteLuck != null)
            {
                state.Streak = absoluteLuck.Progress;
                state.ProgressSynced = absoluteLuck.Progress;
            }

            _absoluteLuckStates[userId] = state;
        }

        return state;
    }

    private void ResetAbsoluteLuckState(string userId)
    {
        var state = GetAbsoluteLuckState(userId);
        state.Streak = 0;
        state.UniformValue = null;
        SyncAbsoluteLuckProgress(userId, state);
    }

    private void SyncAbsoluteLuckProgress(string userId, AbsoluteLuckState state)
    {
        int targetProgress = Math.Min(state.Streak, 6);
        int delta = targetProgress - state.ProgressSynced;

        if (delta == 0)
            return;

        var (success, isCompleted, currentProgress, error) = _achievementManager.UpdateAchievementProgress(
            userId,
            "absolute_luck",
            delta
        );

        if (!success)
        {
            Console.WriteLine($"[Server] Failed to update absolute_luck for user '{userId}': {error}");
            return;
        }

        state.ProgressSynced = currentProgress;

        if (isCompleted)
        {
            Console.WriteLine($"[Server] Player {userId} completed 'absolute_luck' achievement!");
        }
    }
    
    /// <summary>
    /// 广播战斗结束
    /// </summary>
    private async Task BroadcastBattleEndAsync(GameRoom room, ServerBattle battle)
    {
        // 生成战斗统计和奖励
        var playerStats = battle.GenerateBattleStats();
        var playerRewards = battle.GenerateBattleRewards();
        
        var notification = new BattleEndNotification
        {
            RoomId = room.RoomId,
            WinnerCamp = battle.WinnerCamp?.ToString() ?? "",
            BattleLogs = new List<string>(battle.BattleLog),
            EndTimeUtc = DateTime.UtcNow,
            BattleDuration = battle.GetBattleDuration(),
            TotalRounds = battle.CurrentRound,
            PlayerStats = playerStats,
            PlayerRewards = playerRewards
        };
        
        foreach (var player in battle.GetAllPlayers())
        {
            notification.FinalPlayerStates.Add(new BattlePlayerStateDto
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                TeamId = player.Camp == PlayerCamp.Team1 ? 1 : 2,
                CurrentHP = player.CurrentHP,
                MaxHP = player.MaxHP,
                ShieldLayers = player.ShieldLayers,
                IsDead = player.IsDead,
                EquippedDiceNames = player.GetEquippedDice().Select(d => d.Name).ToList()
            });
        }
        
        // 检查"长考"成就
        var eligibleForLongThinking = battle.GetPlayersEligibleForLongThinkingAchievement();
        foreach (var playerId in eligibleForLongThinking)
        {
            // 更新成就进度（以秒为单位）
            var opponentTime = battle.GetOpponentTotalActionTime(playerId);
            var (success, isCompleted, currentProgress, error) = _achievementManager.UpdateAchievementProgress(
                playerId, 
                "long_thinking", 
                (int)opponentTime.TotalSeconds
            );
            
            if (success && isCompleted)
            {
                Console.WriteLine($"[Server] Player {playerId} completed 'long_thinking' achievement!");
                // 添加到奖励中
                var reward = playerRewards.FirstOrDefault(r => r.PlayerId == playerId);
                if (reward != null && !reward.AchievementsUnlocked.Contains("long_thinking"))
                {
                    reward.AchievementsUnlocked.Add("long_thinking");
                }
            }
        }
        
        // 检查"秒了"成就 - 获胜方总行动时间在5秒内
        var eligibleForBlitzVictory = battle.GetPlayersEligibleForBlitzVictoryAchievement();
        foreach (var playerId in eligibleForBlitzVictory)
        {
            var (success, isCompleted, currentProgress, error) = _achievementManager.UpdateAchievementProgress(
                playerId,
                "blitz_victory",
                1
            );
            
            if (success && isCompleted)
            {
                Console.WriteLine($"[Server] Player {playerId} completed 'blitz_victory' achievement!");
                var reward = playerRewards.FirstOrDefault(r => r.PlayerId == playerId);
                if (reward != null && !reward.AchievementsUnlocked.Contains("blitz_victory"))
                {
                    reward.AchievementsUnlocked.Add("blitz_victory");
                }
            }
        }
        
        // 检查"奇迹"成就 - 一局内使用飞羽骰子进行闪避连续成功5次
        var eligibleForMiracle = battle.GetPlayersEligibleForMiracleAchievement();
        foreach (var playerId in eligibleForMiracle)
        {
            var (success, isCompleted, currentProgress, error) = _achievementManager.UpdateAchievementProgress(
                playerId,
                "miracle",
                1
            );
            
            if (success && isCompleted)
            {
                Console.WriteLine($"[Server] Player {playerId} completed 'miracle' achievement!");
                var reward = playerRewards.FirstOrDefault(r => r.PlayerId == playerId);
                if (reward != null && !reward.AchievementsUnlocked.Contains("miracle"))
                {
                    reward.AchievementsUnlocked.Add("miracle");
                }
            }
        }

        HandleAbsoluteLuckAchievement(battle);
        
        var message = NetworkMessage.Create(MessageType.BattleEnd, notification);
        
        foreach (var client in room.Players)
        {
            await client.SendMessageAsync(message);
        }
    }
    
    /// <summary>
    /// 处理战斗行动请求
    /// </summary>
    private async Task HandleBattleActionAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<BattleActionRequest>();
        if (request == null || string.IsNullOrEmpty(client.CurrentRoomId))
        {
            await SendErrorAsync(client, "Invalid battle action request");
            return;
        }
        
        GameRoom? room = null;
        lock (_lock)
        {
            _rooms.TryGetValue(client.CurrentRoomId, out room);
        }
        
        if (room?.CurrentBattle == null)
        {
            await SendErrorAsync(client, "No active battle in room");
            return;
        }
        
        // 处理战斗行动
        room.CurrentBattle.ProcessPlayerAttackChoice(client.UserId, request.SelectedDiceName, request.TargetPlayerId, request.ManualDiceValue);
        
        // 立即广播战斗状态更新
        await BroadcastBattleStateAsync(room, room.CurrentBattle);
        
        Console.WriteLine($"[Server] Battle action from {client.PlayerName}: {request.SelectedDiceName} -> {request.TargetPlayerId}");
    }
    
    /// <summary>
    /// 处理战斗防守请求
    /// </summary>
    private async Task HandleBattleDefenseAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<BattleDefenseRequest>();
        if (request == null || string.IsNullOrEmpty(client.CurrentRoomId))
        {
            await SendErrorAsync(client, "Invalid battle defense request");
            return;
        }
        
        GameRoom? room = null;
        lock (_lock)
        {
            _rooms.TryGetValue(client.CurrentRoomId, out room);
        }
        
        if (room?.CurrentBattle == null)
        {
            await SendErrorAsync(client, "No active battle in room");
            return;
        }
        
        // 处理防守行动
        room.CurrentBattle.ProcessPlayerDefenseChoice(client.UserId, request.SelectedDiceName, request.ManualDiceValue);
        
        // 立即广播战斗状态更新
        await BroadcastBattleStateAsync(room, room.CurrentBattle);
        
        Console.WriteLine($"[Server] Battle defense from {client.PlayerName}: {request.SelectedDiceName}");
    }

    /// <summary>
    /// 处理战斗认输请求
    /// </summary>
    private async Task HandleBattleSurrenderAsync(ConnectedClient client, NetworkMessage message)
    {
        var request = message.GetData<BattleSurrenderRequest>();
        if (request == null || string.IsNullOrEmpty(client.CurrentRoomId))
        {
            await SendErrorAsync(client, "Invalid battle surrender request");
            return;
        }

        GameRoom? room = null;
        lock (_lock)
        {
            _rooms.TryGetValue(client.CurrentRoomId, out room);
        }

        if (room?.CurrentBattle == null)
        {
            await SendErrorAsync(client, "No active battle in room");
            return;
        }

        bool success = room.CurrentBattle.HandleSurrender(client.UserId);
        if (!success)
        {
            await SendErrorAsync(client, "Failed to surrender");
            return;
        }

        Console.WriteLine($"[Server] Battle surrender from {client.PlayerName}");
        
        // 立即广播战斗状态和结束通知
        await BroadcastBattleStateAsync(room, room.CurrentBattle);
        await BroadcastBattleEndAsync(room, room.CurrentBattle);
    }
    
    /// <summary>
    /// 发送错误信息给房间的所有玩家
    /// </summary>
    private async Task SendErrorToRoomAsync(GameRoom room, string errorMessage)
    {
        var message = NetworkMessage.Create(MessageType.Error, new ErrorMessage { Message = errorMessage });
        foreach (var client in room.Players)
        {
            await client.SendMessageAsync(message);
        }
    }
    
    /// <summary>
    /// 断开客户端连接
    /// </summary>
    private void DisconnectClient(ConnectedClient client)
    {
        Console.WriteLine($"[Server] Client disconnected: {client.PlayerName}");
        
        // 从房间移除
        _ = RemovePlayerFromRoomAsync(client);
        
        lock (_lock)
        {
            _clients.Remove(client.UserId);
        }
        
        client.Disconnect();
    }
    
    /// <summary>
    /// 发送错误消息
    /// </summary>
    private async Task SendErrorAsync(ConnectedClient client, string errorMessage)
    {
        var message = NetworkMessage.Create(MessageType.Error, new ErrorMessage
        {
            Message = errorMessage
        });
        
        await client.SendMessageAsync(message);
    }
    
    /// <summary>
    /// 处理获取成就请求
    /// </summary>
    private async Task HandleGetAchievementsAsync(ConnectedClient client)
    {
        try
        {
            Console.WriteLine($"[Server] HandleGetAchievements called for user '{client.UserId}'");
            
            var achievements = _achievementManager.GetUserAchievements(client.UserId);
            
            var response = NetworkMessage.Create(MessageType.GetAchievementsResponse, new GetAchievementsResponse
            {
                Success = true,
                Achievements = achievements
            });
            
            await client.SendMessageAsync(response);
            Console.WriteLine($"[Server] Successfully sent {achievements.Count} achievements to user '{client.UserId}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Error in HandleGetAchievementsAsync for user '{client.UserId}': {ex.Message}");
            Console.WriteLine($"[Server] Exception details: {ex}");
            await SendErrorAsync(client, $"获取成就列表失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 处理更新成就请求
    /// </summary>
    private async Task HandleUpdateAchievementAsync(ConnectedClient client, NetworkMessage message)
    {
        try
        {
            var request = message.GetData<UpdateAchievementRequest>();
            if (request == null)
            {
                Console.WriteLine("[Server] UpdateAchievementRequest is null");
                await SendErrorAsync(client, "Invalid update achievement request");
                return;
            }

            Console.WriteLine($"[Server] HandleUpdateAchievement called for user '{client.UserId}', achievement '{request.AchievementId}', delta {request.ProgressDelta}");
            
            var (success, isCompleted, progress, error) = _achievementManager.UpdateAchievementProgress(
                client.UserId,
                request.AchievementId,
                request.ProgressDelta
            );
            
            if (!success)
            {
                Console.WriteLine($"[Server] Failed to update achievement: {error}");
                var response = NetworkMessage.Create(MessageType.UpdateAchievementResponse, new UpdateAchievementResponse
                {
                    Success = false,
                    ErrorMessage = error ?? "Failed to update achievement"
                });
                
                await client.SendMessageAsync(response);
                return;
            }
            
            var response2 = NetworkMessage.Create(MessageType.UpdateAchievementResponse, new UpdateAchievementResponse
            {
                Success = true,
                IsCompleted = isCompleted,
                Progress = progress
            });
            
            await client.SendMessageAsync(response2);
            Console.WriteLine($"[Server] Achievement update response sent. Completed: {isCompleted}, Progress: {progress}");
            
            // 如果成就完成，发送完成通知
            if (isCompleted)
            {
                var rewards = _achievementManager.GetCompletionRewards(request.AchievementId);
                var notification = NetworkMessage.Create(MessageType.AchievementCompleted, new AchievementCompletedNotification
                {
                    AchievementId = request.AchievementId,
                    AchievementName = GetAchievementName(request.AchievementId),
                    Rewards = rewards,
                    CompletedTime = DateTime.UtcNow
                });
                
                await client.SendMessageAsync(notification);
                Console.WriteLine($"[Server] User '{client.UserId}' completed achievement '{request.AchievementId}' with {rewards.Count} rewards");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Error in HandleUpdateAchievementAsync for user '{client.UserId}': {ex.Message}");
            Console.WriteLine($"[Server] Exception details: {ex}");
            await SendErrorAsync(client, $"更新成就失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 获取成就名称辅助方法
    /// </summary>
    private string GetAchievementName(string achievementId) => achievementId switch
    {
        // "first_victory" => "初露锋芒",
        // "battle_master" => "战斗好手",
        // "item_collector" => "装备收集家",
        // "no_death_warrior" => "无敌战士",
        // "time_traveler" => "时间旅者",
        _ => "未知成就"
    };
    
    public void PrintStatus()
    {
        lock (_lock)
        {
            Console.WriteLine($"\n=== Server Status ===");
            Console.WriteLine($"Connected Clients: {_clients.Count}");
            Console.WriteLine($"Active Rooms: {_rooms.Count}");
            
            if (_rooms.Count > 0)
            {
                Console.WriteLine("\nRooms:");
                foreach (var room in _rooms.Values)
                {
                    Console.WriteLine($"  - {room.RoomName} ({room.PlayerCount}/{room.MaxPlayers}) [{room.Status}]");
                }
            }
            
            Console.WriteLine("=====================\n");
        }
    }
}
