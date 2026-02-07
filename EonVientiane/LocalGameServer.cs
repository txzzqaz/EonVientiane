using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EonVientiane.Shared;

namespace EonVientiane;

/// <summary>
/// 本地游戏服务器 - 支持局域网P2P对战
/// </summary>
public class LocalGameServer
{
    private int _port;
    private TcpListener _listener;
    private CancellationTokenSource _cts;
    private Task _acceptTask;
    private readonly Dictionary<string, LocalGameSession> _games = new();
    private readonly Dictionary<string, ClientConnection> _connections = new();
    private readonly object _lock = new();

    public class ClientConnection
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public class LocalGameSession
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
        public string HostId { get; set; }
        public List<string> PlayerIds { get; set; } = new();
        public Dictionary<string, bool> PlayerReadyStatus { get; set; } = new();
        public GameSessionState State { get; set; } = GameSessionState.Waiting;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int MaxPlayers { get; set; } = 2;
    }

    public enum GameSessionState
    {
        Waiting,
        Countdown,
        InGame,
        Ended
    }

    public event Action<string> GameCreated;
    public event Action<string> GameStarted;
    public event Action<string> GameEnded;

    public LocalGameServer(int port = 18888)
    {
        _port = port;
        Console.WriteLine($"[LocalGameServer] 本地游戏服务器初始化，端口: {port}");
    }

    /// <summary>
    /// 启动本地服务器
    /// </summary>
    public async Task StartAsync()
    {
        try
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            Console.WriteLine($"[LocalGameServer] 本地游戏服务器已启动，监听端口 {_port}");

            _acceptTask = Task.Run(() => AcceptClientsAsync(_cts.Token));

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalGameServer] 启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();

        lock (_lock)
        {
            foreach (var conn in _connections.Values)
            {
                conn.Stream?.Close();
                conn.Client?.Close();
            }
            _connections.Clear();
        }

        Console.WriteLine("[LocalGameServer] 本地游戏服务器已停止");
    }

    /// <summary>
    /// 接受客户端连接
    /// </summary>
    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                var connectionId = Guid.NewGuid().ToString();

                var connection = new ClientConnection
                {
                    Id = connectionId,
                    Client = client,
                    Stream = client.GetStream(),
                    ConnectedAt = DateTime.UtcNow
                };

                lock (_lock)
                {
                    _connections[connectionId] = connection;
                }

                Console.WriteLine($"[LocalGameServer] 新客户端连接: {connectionId}");

                _ = Task.Run(() => HandleClientAsync(connection, cancellationToken));
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalGameServer] 接受连接失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处理客户端连接
    /// </summary>
    private async Task HandleClientAsync(ClientConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var lengthBuffer = new byte[4];

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await connection.Stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);

                if (bytesRead != 4)
                    break;

                int length = BitConverter.ToInt32(lengthBuffer, 0);
                if (length <= 0 || length > 1024 * 1024)
                    break;

                var messageBuffer = new byte[length];
                int totalRead = 0;

                while (totalRead < length)
                {
                    bytesRead = await connection.Stream.ReadAsync(messageBuffer, totalRead, length - totalRead, cancellationToken);
                    if (bytesRead == 0)
                        break;
                    totalRead += bytesRead;
                }

                if (totalRead != length)
                    break;

                var json = Encoding.UTF8.GetString(messageBuffer);
                var message = JsonSerializer.Deserialize<NetworkMessage>(json);

                if (message != null)
                {
                    await HandleMessageAsync(connection, message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalGameServer] 处理客户端错误: {ex.Message}");
        }
        finally
        {
            lock (_lock)
            {
                _connections.Remove(connection.Id);
            }

            connection.Stream?.Close();
            connection.Client?.Close();

            Console.WriteLine($"[LocalGameServer] 客户端断开连接: {connection.Id}");
        }
    }

    /// <summary>
    /// 处理客户端消息
    /// </summary>
    private async Task HandleMessageAsync(ClientConnection connection, NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.LocalGameCreate:
                await HandleLocalGameCreate(connection, message);
                break;

            case MessageType.LocalGameJoin:
                await HandleLocalGameJoin(connection, message);
                break;

            case MessageType.SetReady:
                await HandleSetReady(connection, message);
                break;

            case MessageType.LocalGameStart:
                await HandleLocalGameStart(connection, message);
                break;

            case MessageType.Ping:
                await SendMessageAsync(connection, NetworkMessage.Create(MessageType.Pong));
                break;

            default:
                Console.WriteLine($"[LocalGameServer] 未知消息类型: {message.Type}");
                break;
        }
    }

    /// <summary>
    /// 处理创建本地游戏
    /// </summary>
    private async Task HandleLocalGameCreate(ClientConnection connection, NetworkMessage message)
    {
        try
        {
            var request = message.GetData<LocalGameCreateRequest>();
            if (request == null)
                return;

            var gameId = Guid.NewGuid().ToString();
            var session = new LocalGameSession
            {
                GameId = gameId,
                GameName = request.GameName,
                HostId = connection.Id,
                MaxPlayers = request.MaxPlayers
            };

            session.PlayerIds.Add(connection.Id);
            session.PlayerReadyStatus[connection.Id] = false;
            connection.Username = request.HostUsername;

            lock (_lock)
            {
                _games[gameId] = session;
            }

            Console.WriteLine($"[LocalGameServer] 游戏创建: {gameId} - {request.GameName}");

            // 发送响应
            var response = new LocalGameInfo
            {
                GameId = gameId,
                GameName = request.GameName,
                HostUsername = request.HostUsername,
                MaxPlayers = request.MaxPlayers,
                CurrentPlayers = 1
            };

            await SendMessageAsync(connection, NetworkMessage.Create(MessageType.LocalGameCreateResponse, response));

            GameCreated?.Invoke(gameId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalGameServer] 创建游戏失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理加入本地游戏
    /// </summary>
    private async Task HandleLocalGameJoin(ClientConnection connection, NetworkMessage message)
    {
        try
        {
            var request = message.GetData<LocalGameJoinRequest>();
            if (request == null)
                return;

            LocalGameSession session = null;
            ClientConnection hostConnection = null;
            string errorMessage = null;

            lock (_lock)
            {
                if (!_games.ContainsKey(request.GameId))
                {
                    errorMessage = "游戏不存在";
                    return;
                }

                session = _games[request.GameId];

                if (session.PlayerIds.Count >= session.MaxPlayers)
                {
                    errorMessage = "游戏已满";
                    return;
                }

                session.PlayerIds.Add(connection.Id);
                session.PlayerReadyStatus[connection.Id] = false;
                connection.Username = request.PlayerName;

                hostConnection = _connections.Values.FirstOrDefault(c => c.Id == session.HostId);
            }

            if (errorMessage != null)
            {
                await SendMessageAsync(connection, NetworkMessage.Create(
                    MessageType.LocalGameJoinResponse,
                    new { success = false, message = errorMessage }
                ));
                return;
            }

            Console.WriteLine($"[LocalGameServer] 玩家加入游戏: {request.GameId} - {request.PlayerName}");

            // 通知主机有新玩家加入
            if (hostConnection != null)
            {
                var notifyMessage = NetworkMessage.Create(MessageType.RoomUpdate,
                    new { message = $"{request.PlayerName} 加入了游戏" });
                await SendMessageAsync(hostConnection, notifyMessage);
            }

            // 发送加入成功响应
            await SendMessageAsync(connection, NetworkMessage.Create(
                MessageType.LocalGameJoinResponse,
                new { success = true, message = "加入游戏成功", gameId = request.GameId }
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalGameServer] 加入游戏失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理玩家准备
    /// </summary>
    private async Task HandleSetReady(ClientConnection connection, NetworkMessage message)
    {
        LocalGameSession session = null;
        bool allReady = false;

        lock (_lock)
        {
            session = _games.Values.FirstOrDefault(g => g.PlayerIds.Contains(connection.Id));
            if (session != null)
            {
                session.PlayerReadyStatus[connection.Id] = true;
                Console.WriteLine($"[LocalGameServer] 玩家已准备: {connection.Username}");

                // 检查所有玩家是否都已准备
                if (session.PlayerReadyStatus.Values.All(r => r))
                {
                    session.State = GameSessionState.Countdown;
                    allReady = true;
                }
            }
        }

        if (allReady && session != null)
        {
            Console.WriteLine($"[LocalGameServer] 游戏即将开始: {session.GameId}");
            GameStarted?.Invoke(session.GameId);
        }
    }

    /// <summary>
    /// 处理本地游戏启动
    /// </summary>
    private async Task HandleLocalGameStart(ClientConnection connection, NetworkMessage message)
    {
        LocalGameSession session = null;
        List<ClientConnection> playerConnections = new();

        lock (_lock)
        {
            session = _games.Values.FirstOrDefault(g => g.PlayerIds.Contains(connection.Id));
            if (session != null)
            {
                session.State = GameSessionState.InGame;
                
                // 收集所有玩家连接
                foreach (var playerId in session.PlayerIds)
                {
                    if (_connections.TryGetValue(playerId, out var playerConn))
                    {
                        playerConnections.Add(playerConn);
                    }
                }
            }
        }

        if (session != null)
        {
            Console.WriteLine($"[LocalGameServer] 游戏已启动: {session.GameId}");

            // 通知所有玩家游戏已启动
            foreach (var playerConn in playerConnections)
            {
                await SendMessageAsync(playerConn, NetworkMessage.Create(MessageType.GameStarted, 
                    new { roomId = session.GameId }));
            }
        }
    }

    /// <summary>
    /// 发送消息到客户端
    /// </summary>
    private async Task SendMessageAsync(ClientConnection connection, NetworkMessage message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(data.Length);

            lock (connection)
            {
                connection.Stream.Write(length, 0, 4);
                connection.Stream.Write(data, 0, data.Length);
                connection.Stream.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalGameServer] 发送消息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取活跃游戏列表
    /// </summary>
    public List<LocalGameInfo> GetActiveGames()
    {
        lock (_lock)
        {
            return _games.Values
                .Where(g => g.State == GameSessionState.Waiting)
                .Select(g => new LocalGameInfo
                {
                    GameId = g.GameId,
                    GameName = g.GameName,
                    HostUsername = _connections[g.HostId].Username,
                    MaxPlayers = g.MaxPlayers,
                    CurrentPlayers = g.PlayerIds.Count
                })
                .ToList();
        }
    }
}
