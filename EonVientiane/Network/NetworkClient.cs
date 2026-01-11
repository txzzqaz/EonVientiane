using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EonVientiane.Shared;

namespace EonVientiane.Network;

/// <summary>
/// 网络客户端 - 用于连接游戏服务器
/// </summary>
public class NetworkClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isConnected;
    private Task _receiveTask;
    private Task _heartbeatTask;
    private DateTime _lastHeartbeatReceived;
    private CancellationTokenSource _heartbeatCancellation;
    private const int HeartbeatIntervalMs = 5000; // 5秒发送一次心跳
    private const int HeartbeatTimeoutMs = 15000; // 15秒无心跳则断开连接
    private readonly object _sendLock = new(); // 保护发送操作的线程安全性
    
    public bool IsConnected => _isConnected && _client?.Connected == true;
    
    public event Action<NetworkMessage> MessageReceived;
    public event Action Connected;
    public event Action Disconnected;
    
    /// <summary>
    /// 连接到服务器
    /// </summary>
    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _isConnected = true;
            _lastHeartbeatReceived = DateTime.UtcNow;
            
            // 开始接收消息
            _receiveTask = Task.Run(ReceiveMessagesAsync);
            
            // 启动心跳任务
            _heartbeatCancellation = new CancellationTokenSource();
            _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_heartbeatCancellation.Token));
            
            Connected?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
            _isConnected = false;
            return false;
        }
    }
    
    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (!_isConnected)
            return;
            
        _isConnected = false;
        
        // 停止心跳任务
        _heartbeatCancellation?.Cancel();
        
        try
        {
            _stream?.Close();
            _client?.Close();
        }
        catch { }
        
        Disconnected?.Invoke();
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessageAsync(NetworkMessage message)
    {
        if (!IsConnected || _stream == null)
            return;
            
        try
        {
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(data.Length);
            
            lock (_sendLock)
            {
                // 原子性地写入长度前缀和消息数据
                _stream.Write(length, 0, 4);
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send failed: {ex.Message}");
            Disconnect();
        }
    }
    
    /// <summary>
    /// 接收消息循环
    /// </summary>
    private async Task ReceiveMessagesAsync()
    {
        var lengthBuffer = new byte[4];
        
        while (_isConnected && _stream != null)
        {
            try
            {
                // 读取消息长度
                int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4);
                
                if (bytesRead != 4)
                {
                    Disconnect();
                    break;
                }
                
                int length = BitConverter.ToInt32(lengthBuffer, 0);
                
                if (length <= 0 || length > 1024 * 1024)
                {
                    Disconnect();
                    break;
                }
                
                // 读取消息内容
                var buffer = new byte[length];
                int totalRead = 0;
                
                while (totalRead < length)
                {
                    bytesRead = await _stream.ReadAsync(buffer, totalRead, length - totalRead);
                    if (bytesRead == 0)
                    {
                        Disconnect();
                        return;
                    }
                    
                    totalRead += bytesRead;
                }
                
                var json = Encoding.UTF8.GetString(buffer);
                var message = JsonSerializer.Deserialize<NetworkMessage>(json);
                
                if (message != null)
                {
                    // 处理心跳响应
                    if (message.Type == MessageType.Pong)
                    {
                        _lastHeartbeatReceived = DateTime.UtcNow;
                    }
                    
                    MessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    Console.WriteLine($"Receive failed: {ex.Message}");
                    Disconnect();
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 心跳循环 - 定期发送心跳包并检查超时
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isConnected)
        {
            try
            {
                // 发送心跳包
                var heartbeat = NetworkMessage.Create(MessageType.Ping);
                await SendMessageAsync(heartbeat);
                
                // 等待一段时间
                await Task.Delay(HeartbeatIntervalMs, cancellationToken);
                
                // 检查是否超时
                var timeSinceLastHeartbeat = (DateTime.UtcNow - _lastHeartbeatReceived).TotalMilliseconds;
                if (timeSinceLastHeartbeat > HeartbeatTimeoutMs)
                {
                    Console.WriteLine($"Heartbeat timeout: {timeSinceLastHeartbeat}ms");
                    Disconnect();
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，退出循环
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Heartbeat error: {ex.Message}");
            }
        }
    }
}
