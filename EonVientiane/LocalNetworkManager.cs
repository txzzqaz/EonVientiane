using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EonVientiane;

/// <summary>
/// 局域网网络管理器 - 支持本地主机发现和P2P连接
/// </summary>
public class LocalNetworkManager
{
    private const int DISCOVERY_PORT = 17777;
    private const int BROADCAST_INTERVAL = 2000; // 2秒发送一次广播
    private const int DISCOVERY_TIMEOUT = 5000; // 5秒发现超时

    private UdpClient _discoveryClient;
    private CancellationTokenSource _discoveryCts;
    private Task _discoveryTask;
    private Task _broadcastTask;
    private readonly object _lock = new();

    public class LocalHost
    {
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public int GamePort { get; set; }
        public DateTime LastSeen { get; set; }
        public string Version { get; set; } = "1.0.0";
    }

    private Dictionary<string, LocalHost> _discoveredHosts = new();
    private LocalHost _localHostInfo;
    private bool _isPublishing = false;

    public event Action<LocalHost> HostDiscovered;
    public event Action<string> HostLost;
    public List<LocalHost> DiscoveredHosts => _discoveredHosts.Values.ToList();

    public LocalNetworkManager()
    {
        Console.WriteLine("[LocalNetwork] 局域网管理器已初始化");
    }

    /// <summary>
    /// 启动局域网主机发现
    /// </summary>
    public async Task StartDiscoveryAsync(LocalHost localInfo)
    {
        if (_discoveryTask != null && !_discoveryTask.IsCompleted)
            return;

        _localHostInfo = localInfo;
        _discoveryCts = new CancellationTokenSource();

        try
        {
            _discoveryClient = new UdpClient(new IPEndPoint(IPAddress.Any, DISCOVERY_PORT));
            _discoveryClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _discoveryClient.ExclusiveAddressUse = false;
            
            Console.WriteLine($"[LocalNetwork] 启动局域网发现，监听端口 {DISCOVERY_PORT}");

            // 启动接收任务
            _discoveryTask = Task.Run(() => DiscoveryListenAsync(_discoveryCts.Token));

            // 启动广播任务
            _broadcastTask = Task.Run(() => BroadcastHostAsync(_discoveryCts.Token));

            // 启动主机清理任务
            _ = Task.Run(() => CleanupStaleHostsAsync(_discoveryCts.Token));

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalNetwork] 启动发现失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止局域网发现
    /// </summary>
    public void StopDiscovery()
    {
        _discoveryCts?.Cancel();
        _discoveryClient?.Close();
        _isPublishing = false;
        Console.WriteLine("[LocalNetwork] 已停止局域网发现");
    }

    /// <summary>
    /// 发现听取任务 - 接收其他主机的广播
    /// </summary>
    private async Task DiscoveryListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _discoveryClient != null)
        {
            try
            {
                var result = await _discoveryClient.ReceiveAsync();
                
                try
                {
                    var json = Encoding.UTF8.GetString(result.Buffer);
                    var host = JsonSerializer.Deserialize<LocalHost>(json);

                    if (host != null && host.Username != _localHostInfo?.Username)
                    {
                        host.IpAddress = result.RemoteEndPoint.Address.ToString();
                        host.LastSeen = DateTime.UtcNow;

                        lock (_lock)
                        {
                            var key = host.Username.ToLower();
                            bool isNew = !_discoveredHosts.ContainsKey(key);
                            _discoveredHosts[key] = host;

                            if (isNew)
                            {
                                Console.WriteLine($"[LocalNetwork] 发现主机: {host.Username} ({host.IpAddress}:{host.GamePort})");
                                HostDiscovered?.Invoke(host);
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[LocalNetwork] 解析广播消息失败: {ex.Message}");
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"[LocalNetwork] 接收错误: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 广播本地主机信息
    /// </summary>
    private async Task BroadcastHostAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(500); // 延迟启动广播

        while (!cancellationToken.IsCancellationRequested && _localHostInfo != null)
        {
            try
            {
                using (var broadcastClient = new UdpClient())
                {
                    _localHostInfo.LastSeen = DateTime.UtcNow;
                    var json = JsonSerializer.Serialize(_localHostInfo);
                    var data = Encoding.UTF8.GetBytes(json);

                    // 向255.255.255.255广播
                    await broadcastClient.SendAsync(data, data.Length, 
                        new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT));

                    if (!_isPublishing)
                    {
                        _isPublishing = true;
                        Console.WriteLine($"[LocalNetwork] 开始广播本地主机: {_localHostInfo.Username}");
                    }
                }

                await Task.Delay(BROADCAST_INTERVAL, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalNetwork] 广播错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 清理过期的主机信息
    /// </summary>
    private async Task CleanupStaleHostsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(DISCOVERY_TIMEOUT, cancellationToken);

                lock (_lock)
                {
                    var staleHosts = _discoveredHosts
                        .Where(h => DateTime.UtcNow - h.Value.LastSeen > TimeSpan.FromSeconds(10))
                        .ToList();

                    foreach (var host in staleHosts)
                    {
                        _discoveredHosts.Remove(host.Key);
                        Console.WriteLine($"[LocalNetwork] 主机离线: {host.Value.Username}");
                        HostLost?.Invoke(host.Value.Username);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalNetwork] 清理错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取本地IP地址
    /// </summary>
    public static string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65432);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
        }
        catch
        {
            // 备选方案
            var hostname = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(hostname);
            return addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
        }
    }
}
