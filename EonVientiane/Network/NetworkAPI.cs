using System;
using System.Collections.Generic;
using EonVientiane.Shared;

namespace EonVientiane.Network;

/// <summary>
/// 网络系统API - 提供网络通信的扩展接口
/// </summary>
public static class NetworkAPI
{
    /// <summary>
    /// 自定义消息处理器
    /// </summary>
    private static readonly Dictionary<string, Action<NetworkMessage>> _messageHandlers = new();
    
    /// <summary>
    /// 连接拦截器
    /// </summary>
    private static readonly List<IConnectionInterceptor> _connectionInterceptors = new();
    
    /// <summary>
    /// 消息拦截器
    /// </summary>
    private static readonly List<IMessageInterceptor> _messageInterceptors = new();
    
    /// <summary>
    /// 连接建立事件
    /// </summary>
    public static event Action<string> ConnectionEstablished; // 参数: 服务器地址
    
    /// <summary>
    /// 连接断开事件
    /// </summary>
    public static event Action<string> ConnectionLost; // 参数: 断开原因
    
    /// <summary>
    /// 消息发送前事件
    /// </summary>
    public static event Action<NetworkMessage> MessageSending;
    
    /// <summary>
    /// 消息接收事件
    /// </summary>
    public static event Action<NetworkMessage> MessageReceived;
    
    /// <summary>
    /// 注册自定义消息处理器
    /// 注意：NetworkMessage类型由Shared项目定义，请查阅NetworkProtocol.cs
    /// </summary>
    /*
    public static void RegisterMessageHandler(string messageType, Action<NetworkMessage> handler)
    {
        _messageHandlers[messageType] = handler;
    }
    
    /// <summary>
    /// 处理自定义消息
    /// </summary>
    public static bool HandleCustomMessage(NetworkMessage message)
    {
        if (_messageHandlers.TryGetValue(message.Type, out var handler))
        {
            handler(message);
            return true;
        }
        return false;
    }
    */
    
    /// <summary>
    /// 添加连接拦截器
    /// </summary>
    public static void AddConnectionInterceptor(IConnectionInterceptor interceptor)
    {
        if (interceptor != null && !_connectionInterceptors.Contains(interceptor))
        {
            _connectionInterceptors.Add(interceptor);
        }
    }
    
    /// <summary>
    /// 添加消息拦截器
    /// </summary>
    public static void AddMessageInterceptor(IMessageInterceptor interceptor)
    {
        if (interceptor != null && !_messageInterceptors.Contains(interceptor))
        {
            _messageInterceptors.Add(interceptor);
        }
    }
    
    /// <summary>
    /// 在连接前执行拦截器
    /// </summary>
    public static bool InterceptConnection(string host, int port)
    {
        foreach (var interceptor in _connectionInterceptors)
        {
            if (!interceptor.OnBeforeConnect(host, port))
            {
                return false; // 连接被拦截
            }
        }
        return true;
    }
    
    /// <summary>
    /// 在发送消息前执行拦截器
    /// </summary>
    public static NetworkMessage InterceptMessageSending(NetworkMessage message)
    {
        var modifiedMessage = message;
        foreach (var interceptor in _messageInterceptors)
        {
            modifiedMessage = interceptor.OnBeforeSend(modifiedMessage);
            if (modifiedMessage == null)
            {
                return null; // 消息被拦截
            }
        }
        return modifiedMessage;
    }
    
    /// <summary>
    /// 在接收消息后执行拦截器
    /// </summary>
    public static NetworkMessage InterceptMessageReceiving(NetworkMessage message)
    {
        var modifiedMessage = message;
        foreach (var interceptor in _messageInterceptors)
        {
            modifiedMessage = interceptor.OnAfterReceive(modifiedMessage);
            if (modifiedMessage == null)
            {
                return null; // 消息被拦截
            }
        }
        return modifiedMessage;
    }
    
    /// <summary>
    /// 触发连接建立事件
    /// </summary>
    internal static void InvokeConnectionEstablished(string serverAddress)
    {
        ConnectionEstablished?.Invoke(serverAddress);
    }
    
    /// <summary>
    /// 触发连接断开事件
    /// </summary>
    internal static void InvokeConnectionLost(string reason)
    {
        ConnectionLost?.Invoke(reason);
    }
    
    /// <summary>
    /// 触发消息发送前事件
    /// </summary>
    internal static void InvokeMessageSending(NetworkMessage message)
    {
        MessageSending?.Invoke(message);
    }
    
    /// <summary>
    /// 触发消息接收事件
    /// </summary>
    internal static void InvokeMessageReceived(NetworkMessage message)
    {
        MessageReceived?.Invoke(message);
    }
}

/// <summary>
/// 连接拦截器接口
/// </summary>
public interface IConnectionInterceptor
{
    /// <summary>
    /// 连接前调用
    /// </summary>
    /// <returns>返回false将阻止连接</returns>
    bool OnBeforeConnect(string host, int port);
    
    /// <summary>
    /// 连接成功后调用
    /// </summary>
    void OnConnected(string host, int port);
    
    /// <summary>
    /// 断开连接时调用
    /// </summary>
    void OnDisconnected(string reason);
}

/// <summary>
/// 消息拦截器接口
/// </summary>
public interface IMessageInterceptor
{
    /// <summary>
    /// 发送前调用
    /// </summary>
    /// <returns>返回修改后的消息，或null表示拦截</returns>
    NetworkMessage OnBeforeSend(NetworkMessage message);
    
    /// <summary>
    /// 接收后调用
    /// </summary>
    /// <returns>返回修改后的消息，或null表示拦截</returns>
    NetworkMessage OnAfterReceive(NetworkMessage message);
}

/*
/// <summary>
/// 网络消息构建器 - 便捷地创建网络消息
/// 注意：请使用Shared.NetworkProtocol中定义的NetworkMessage类型
/// </summary>
public class NetworkMessageBuilder
{
    private string _type;
    private readonly Dictionary<string, object> _data = new();
    
    public NetworkMessageBuilder WithType(string type)
    {
        _type = type;
        return this;
    }
    
    public NetworkMessageBuilder AddData(string key, object value)
    {
        _data[key] = value;
        return this;
    }
    
    public NetworkMessage Build()
    {
        return new NetworkMessage
        {
            Type = _type,
            Data = _data
        };
    }
}
*/

/// <summary>
/// 消息日志记录器 - 记录所有网络消息
/// </summary>
public class MessageLogger : IMessageInterceptor
{
    private readonly List<(DateTime timestamp, NetworkMessage message, bool isSent)> _messageLog = new();
    private readonly int _maxLogSize;
    
    public IReadOnlyList<(DateTime timestamp, NetworkMessage message, bool isSent)> MessageLog => _messageLog;
    
    public MessageLogger(int maxLogSize = 1000)
    {
        _maxLogSize = maxLogSize;
    }
    
    public NetworkMessage OnBeforeSend(NetworkMessage message)
    {
        LogMessage(message, true);
        return message;
    }
    
    public NetworkMessage OnAfterReceive(NetworkMessage message)
    {
        LogMessage(message, false);
        return message;
    }
    
    private void LogMessage(NetworkMessage message, bool isSent)
    {
        _messageLog.Add((DateTime.Now, message, isSent));
        
        // 保持日志大小限制
        while (_messageLog.Count > _maxLogSize)
        {
            _messageLog.RemoveAt(0);
        }
    }
    
    public void ClearLog()
    {
        _messageLog.Clear();
    }
}
