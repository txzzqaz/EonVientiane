using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 已连接的客户端
/// </summary>
public class ConnectedClient
{
    public string PlayerId { get; }
    public string PlayerName { get; set; }
    public TcpClient TcpClient { get; }
    public NetworkStream Stream { get; }
    public DateTime ConnectedTime { get; set; }
    public DateTime LastPingTime { get; set; }
    
    public string? CurrentRoomId { get; set; }
    public bool IsReady { get; set; }
    public int TeamId { get; set; }
    
    // 用户认证相关
    public string? UserId { get; set; }
    public string? AuthToken { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(AuthToken);
    
    private readonly object _sendLock = new();
    
    public ConnectedClient(TcpClient tcpClient, string playerId)
    {
        TcpClient = tcpClient;
        Stream = tcpClient.GetStream();
        PlayerId = playerId;
        PlayerName = $"Player_{playerId[..8]}";
        ConnectedTime = DateTime.UtcNow;
        LastPingTime = DateTime.UtcNow;
    }

    
    /// <summary>
    /// 发送消息给客户端
    /// </summary>
    public async Task SendMessageAsync(NetworkMessage message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(data.Length);
            
            lock (_sendLock)
            {
                Stream.Write(length, 0, 4);
                Stream.Write(data, 0, data.Length);
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to send message to {PlayerName}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 接收消息
    /// </summary>
    public async Task<NetworkMessage?> ReceiveMessageAsync()
    {
        try
        {
            // 读取消息长度
            var lengthBuffer = new byte[4];
            int bytesRead = await Stream.ReadAsync(lengthBuffer, 0, 4);
            
            if (bytesRead != 4)
                return null;
                
            int length = BitConverter.ToInt32(lengthBuffer, 0);
            
            if (length <= 0 || length > 1024 * 1024) // 最大1MB
                return null;
            
            // 读取消息内容
            var buffer = new byte[length];
            int totalRead = 0;
            
            while (totalRead < length)
            {
                bytesRead = await Stream.ReadAsync(buffer, totalRead, length - totalRead);
                if (bytesRead == 0)
                    return null;
                    
                totalRead += bytesRead;
            }
            
            var json = Encoding.UTF8.GetString(buffer);
            return JsonSerializer.Deserialize<NetworkMessage>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to receive message from {PlayerName}: {ex.Message}");
            return null;
        }
    }
    
    public void Disconnect()
    {
        try
        {
            Stream?.Close();
            TcpClient?.Close();
        }
        catch { }
    }
}
