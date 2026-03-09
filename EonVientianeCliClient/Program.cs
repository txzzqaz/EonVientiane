using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EonVientiane.Shared;

namespace EonVientianeCliClient;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || HasFlag(args, "--help") || HasFlag(args, "-h"))
        {
            PrintUsage();
            return 0;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var options = ParseOptions(args.Skip(1).ToArray());

        var host = GetOption(options, "host", "127.0.0.1");
        var port = GetIntOption(options, "port", 7777);
        var timeoutSeconds = GetIntOption(options, "timeout", 15);
        var timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds));

        try
        {
            return command switch
            {
                "register" => await RunRegisterAsync(host, port, timeout, options),
                "login" => await RunLoginAsync(host, port, timeout, options),
                "inventory" => await RunInventoryAsync(host, port, timeout, options),
                "get-initial" => await RunGetInitialInventoryAsync(host, port, timeout, options),
                "equip" => await RunEquipAsync(host, port, timeout, options),
                "unequip" => await RunUnequipAsync(host, port, timeout, options),
                _ => PrintError("unknown_command", $"未知命令: {command}")
            };
        }
        catch (TimeoutException tex)
        {
            return PrintError(command, $"请求超时: {tex.Message}");
        }
        catch (Exception ex)
        {
            return PrintError(command, $"执行失败: {ex.Message}");
        }
    }

    private static async Task<int> RunRegisterAsync(string host, int port, TimeSpan timeout, Dictionary<string, string> options)
    {
        var username = GetRequired(options, "username");
        var password = GetRequired(options, "password");
        var email = GetOption(options, "email", $"{username}@cli.local");

        await using var session = new CliSession();
        await session.ConnectAsync(host, port);

        var responseMessage = await session.SendAndWaitAsync(
            NetworkMessage.Create(MessageType.UserRegister, new UserRegisterRequest
            {
                Username = username,
                Password = password,
                Email = email
            }),
            msg => msg.Type is MessageType.UserRegisterResponse or MessageType.Error,
            timeout);

        if (responseMessage.Type == MessageType.Error)
        {
            var err = responseMessage.GetData<ErrorMessage>();
            return PrintError("register", err?.Message ?? "注册失败");
        }

        var response = responseMessage.GetData<UserRegisterResponse>();
        if (response == null)
        {
            return PrintError("register", "注册响应格式错误");
        }

        return response.Success
            ? PrintSuccess("register", "注册成功", new { response.UserId, username, email })
            : PrintError("register", response.ErrorMessage ?? "注册失败");
    }

    private static async Task<int> RunLoginAsync(string host, int port, TimeSpan timeout, Dictionary<string, string> options)
    {
        var username = GetRequired(options, "username");
        var password = GetRequired(options, "password");

        await using var session = new CliSession();
        await session.ConnectAsync(host, port);

        var login = await session.LoginAsync(username, password, timeout);
        if (!login.Success)
        {
            return PrintError("login", login.ErrorMessage ?? "登录失败");
        }

        return PrintSuccess("login", "登录成功", new
        {
            login.UserId,
            login.Token,
            username
        });
    }

    private static async Task<int> RunInventoryAsync(string host, int port, TimeSpan timeout, Dictionary<string, string> options)
    {
        var username = GetRequired(options, "username");
        var password = GetRequired(options, "password");

        await using var session = new CliSession();
        await session.ConnectAsync(host, port);

        var login = await session.LoginAsync(username, password, timeout);
        if (!login.Success)
        {
            return PrintError("inventory", login.ErrorMessage ?? "登录失败");
        }

        var state = await session.RequestInventoryAsync(timeout);
        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
        {
            return PrintError("inventory", state.ErrorMessage!);
        }

        return PrintSuccess("inventory", "获取背包成功", new
        {
            login.UserId,
            itemCount = state.Items.Count,
            items = state.Items
        });
    }

    private static async Task<int> RunGetInitialInventoryAsync(string host, int port, TimeSpan timeout, Dictionary<string, string> options)
    {
        var username = GetRequired(options, "username");
        var password = GetRequired(options, "password");

        await using var session = new CliSession();
        await session.ConnectAsync(host, port);

        var login = await session.LoginAsync(username, password, timeout);
        if (!login.Success || string.IsNullOrWhiteSpace(login.UserId))
        {
            return PrintError("get-initial", login.ErrorMessage ?? "登录失败");
        }

        var response = await session.GetInitialInventoryAsync(login.UserId, timeout);
        if (!response.Success)
        {
            return PrintError("get-initial", response.ErrorMessage ?? "获取初始背包失败");
        }

        return PrintSuccess("get-initial", "获取初始背包成功", new
        {
            login.UserId,
            itemCount = response.Items.Count,
            items = response.Items
        });
    }

    private static async Task<int> RunEquipAsync(string host, int port, TimeSpan timeout, Dictionary<string, string> options)
    {
        return await RunInventoryActionAsync("equip", MessageType.EquipItem, MessageType.EquipItemResponse, host, port, timeout, options);
    }

    private static async Task<int> RunUnequipAsync(string host, int port, TimeSpan timeout, Dictionary<string, string> options)
    {
        return await RunInventoryActionAsync("unequip", MessageType.UnequipItem, MessageType.UnequipItemResponse, host, port, timeout, options);
    }

    private static async Task<int> RunInventoryActionAsync(
        string command,
        MessageType requestType,
        MessageType responseType,
        string host,
        int port,
        TimeSpan timeout,
        Dictionary<string, string> options)
    {
        var username = GetRequired(options, "username");
        var password = GetRequired(options, "password");

        await using var session = new CliSession();
        await session.ConnectAsync(host, port);

        var login = await session.LoginAsync(username, password, timeout);
        if (!login.Success)
        {
            return PrintError(command, login.ErrorMessage ?? "登录失败");
        }

        var stackId = GetOption(options, "stack-id", null);
        if (string.IsNullOrWhiteSpace(stackId))
        {
            var itemId = GetRequired(options, "item-id");
            var nth = Math.Max(1, GetIntOption(options, "nth", 1));

            var state = await session.RequestInventoryAsync(timeout);
            var target = state.Items
                .Where(i => string.Equals(i.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                .Skip(nth - 1)
                .FirstOrDefault();

            if (target == null)
            {
                return PrintError(command, $"未找到 item-id={itemId} 第 {nth} 个堆叠");
            }

            stackId = target.StackId;
        }

        var payload = requestType == MessageType.EquipItem
            ? new { StackId = stackId }
            : new { StackId = stackId };

        var responseMessage = await session.SendAndWaitAsync(
            NetworkMessage.Create(requestType, payload),
            msg => msg.Type == responseType || msg.Type == MessageType.Error,
            timeout);

        if (responseMessage.Type == MessageType.Error)
        {
            var err = responseMessage.GetData<ErrorMessage>();
            return PrintError(command, err?.Message ?? "操作失败");
        }

        var actionResponse = responseMessage.GetData<InventoryActionResponse>();
        if (actionResponse == null)
        {
            return PrintError(command, "响应格式错误");
        }

        if (!actionResponse.Success)
        {
            return PrintError(command, actionResponse.ErrorMessage ?? "操作失败");
        }

        var resultState = actionResponse.State;
        if (resultState == null)
        {
            resultState = await session.WaitInventoryUpdatedAsync(timeout);
        }

        return PrintSuccess(command, "操作成功", new
        {
            stackId,
            itemCount = resultState.Items.Count,
            items = resultState.Items
        });
    }

    private static void PrintUsage()
    {
        Console.WriteLine("EonVientiane CLI 客户端（脚本测试用）\n");
        Console.WriteLine("命令:");
        Console.WriteLine("  register    --username <u> --password <p> [--email <e>] [--host <h>] [--port <n>] [--timeout <sec>]");
        Console.WriteLine("  login       --username <u> --password <p> [--host <h>] [--port <n>] [--timeout <sec>]");
        Console.WriteLine("  inventory   --username <u> --password <p> [--host <h>] [--port <n>] [--timeout <sec>]");
        Console.WriteLine("  get-initial --username <u> --password <p> [--host <h>] [--port <n>] [--timeout <sec>]");
        Console.WriteLine("  equip       --username <u> --password <p> (--stack-id <id> | --item-id <item> [--nth <n>]) [--host <h>] [--port <n>] [--timeout <sec>]");
        Console.WriteLine("  unequip     --username <u> --password <p> (--stack-id <id> | --item-id <item> [--nth <n>]) [--host <h>] [--port <n>] [--timeout <sec>]");
        Console.WriteLine();
        Console.WriteLine("返回:");
        Console.WriteLine("  标准输出始终为 JSON，exit code 0 表示成功，非 0 表示失败。");
    }

    private static bool HasFlag(string[] args, string flag)
    {
        return args.Any(a => string.Equals(a.Trim(), flag, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i].Trim();
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = current[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                dict[key] = args[i + 1];
                i++;
                continue;
            }

            dict[key] = "true";
        }

        return dict;
    }

    private static string GetRequired(Dictionary<string, string> options, string name)
    {
        if (options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new ArgumentException($"缺少必需参数 --{name}");
    }

    private static string? GetOption(Dictionary<string, string> options, string name, string? defaultValue)
    {
        return options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
    }

    private static int GetIntOption(Dictionary<string, string> options, string name, int defaultValue)
    {
        return options.TryGetValue(name, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static int PrintSuccess(string command, string message, object? data)
    {
        Console.WriteLine(JsonSerializer.Serialize(new CliResult
        {
            Command = command,
            Success = true,
            Message = message,
            Data = data
        }, JsonOptions));

        return 0;
    }

    private static int PrintError(string command, string message)
    {
        Console.WriteLine(JsonSerializer.Serialize(new CliResult
        {
            Command = command,
            Success = false,
            Message = message
        }, JsonOptions));

        return 1;
    }
}

internal sealed class CliSession : IAsyncDisposable
{
    private readonly TcpMessageClient _client = new();
    private readonly MessageAwaiter _awaiter;

    public CliSession()
    {
        _awaiter = new MessageAwaiter(_client);
    }

    public Task ConnectAsync(string host, int port)
    {
        return _client.ConnectAsync(host, port);
    }

    public async Task<UserLoginResponse> LoginAsync(string username, string password, TimeSpan timeout)
    {
        var message = await SendAndWaitAsync(
            NetworkMessage.Create(MessageType.UserLogin, new UserLoginRequest
            {
                Username = username,
                Password = password
            }),
            msg => msg.Type is MessageType.UserLoginResponse or MessageType.Error,
            timeout);

        if (message.Type == MessageType.Error)
        {
            var err = message.GetData<ErrorMessage>();
            return new UserLoginResponse
            {
                Success = false,
                ErrorMessage = err?.Message ?? "登录失败"
            };
        }

        return message.GetData<UserLoginResponse>() ?? new UserLoginResponse
        {
            Success = false,
            ErrorMessage = "登录响应解析失败"
        };
    }

    public async Task<InventoryState> RequestInventoryAsync(TimeSpan timeout)
    {
        var message = await SendAndWaitAsync(
            NetworkMessage.Create(MessageType.RequestInventory, new RequestInventory()),
            msg => msg.Type is MessageType.InventoryState or MessageType.Error,
            timeout);

        if (message.Type == MessageType.Error)
        {
            var err = message.GetData<ErrorMessage>();
            return new InventoryState { ErrorMessage = err?.Message ?? "请求背包失败" };
        }

        return message.GetData<InventoryState>() ?? new InventoryState { ErrorMessage = "背包响应解析失败" };
    }

    public async Task<InitialInventoryResponse> GetInitialInventoryAsync(string userId, TimeSpan timeout)
    {
        var message = await SendAndWaitAsync(
            NetworkMessage.Create(MessageType.GetInitialInventory, new GetInitialInventoryRequest { UserId = userId }),
            msg => msg.Type is MessageType.InitialInventoryResponse or MessageType.Error,
            timeout);

        if (message.Type == MessageType.Error)
        {
            var err = message.GetData<ErrorMessage>();
            return new InitialInventoryResponse
            {
                Success = false,
                ErrorMessage = err?.Message ?? "请求初始背包失败"
            };
        }

        return message.GetData<InitialInventoryResponse>() ?? new InitialInventoryResponse
        {
            Success = false,
            ErrorMessage = "初始背包响应解析失败"
        };
    }

    public Task<InventoryState> WaitInventoryUpdatedAsync(TimeSpan timeout)
    {
        return _awaiter.WaitForAsync(
            msg => msg.Type == MessageType.InventoryUpdated,
            timeout,
            msg => msg.GetData<InventoryState>() ?? new InventoryState { ErrorMessage = "InventoryUpdated 解析失败" });
    }

    public Task<NetworkMessage> SendAndWaitAsync(NetworkMessage message, Func<NetworkMessage, bool> predicate, TimeSpan timeout)
    {
        return _awaiter.SendAndWaitAsync(_client, message, predicate, timeout);
    }

    public async ValueTask DisposeAsync()
    {
        _awaiter.Dispose();
        _client.Dispose();
        await Task.CompletedTask;
    }
}

internal sealed class MessageAwaiter : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly List<PendingWaiter> _waiters = [];

    public MessageAwaiter(TcpMessageClient client)
    {
        client.MessageReceived += OnMessageReceived;
    }

    public async Task<NetworkMessage> SendAndWaitAsync(
        TcpMessageClient client,
        NetworkMessage message,
        Func<NetworkMessage, bool> predicate,
        TimeSpan timeout)
    {
        var waitTask = WaitForAsync(predicate, timeout, static msg => msg);
        await client.SendAsync(message);
        return await waitTask;
    }

    public Task<T> WaitForAsync<T>(
        Func<NetworkMessage, bool> predicate,
        TimeSpan timeout,
        Func<NetworkMessage, T> projector)
    {
        var tcs = new TaskCompletionSource<NetworkMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = new PendingWaiter(predicate, tcs);

        lock (_syncRoot)
        {
            _waiters.Add(waiter);
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(timeout);

            bool removed;
            lock (_syncRoot)
            {
                removed = _waiters.Remove(waiter);
            }

            if (removed)
            {
                tcs.TrySetException(new TimeoutException("等待服务器响应超时"));
            }
        });

        return tcs.Task.ContinueWith(task => projector(task.Result), TaskContinuationOptions.ExecuteSynchronously);
    }

    private void OnMessageReceived(NetworkMessage message)
    {
        List<PendingWaiter> matched = [];

        lock (_syncRoot)
        {
            for (var i = _waiters.Count - 1; i >= 0; i--)
            {
                var waiter = _waiters[i];
                if (!waiter.Predicate(message))
                {
                    continue;
                }

                matched.Add(waiter);
                _waiters.RemoveAt(i);
            }
        }

        foreach (var waiter in matched)
        {
            waiter.Completion.TrySetResult(message);
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            foreach (var waiter in _waiters)
            {
                waiter.Completion.TrySetCanceled();
            }

            _waiters.Clear();
        }
    }

    private sealed record PendingWaiter(
        Func<NetworkMessage, bool> Predicate,
        TaskCompletionSource<NetworkMessage> Completion);
}

internal sealed class TcpMessageClient : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private readonly object _sendLock = new();

    public event Action<NetworkMessage>? MessageReceived;

    public bool IsConnected => _client?.Connected == true && _stream != null;

    public async Task ConnectAsync(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();

        _receiveCts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
    }

    public async Task SendAsync(NetworkMessage message)
    {
        if (!IsConnected || _stream == null)
        {
            throw new InvalidOperationException("尚未连接服务器");
        }

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var head = BitConverter.GetBytes(body.Length);

        lock (_sendLock)
        {
            _stream.Write(head, 0, head.Length);
            _stream.Write(body, 0, body.Length);
            _stream.Flush();
        }

        await Task.CompletedTask;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_stream == null)
        {
            return;
        }

        var header = new byte[4];

        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await ReadExactAsync(_stream, header, cancellationToken);
            if (read == 0)
            {
                break;
            }

            var bodyLength = BitConverter.ToInt32(header, 0);
            if (bodyLength <= 0 || bodyLength > 1024 * 1024)
            {
                break;
            }

            var body = new byte[bodyLength];
            read = await ReadExactAsync(_stream, body, cancellationToken);
            if (read == 0)
            {
                break;
            }

            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<NetworkMessage>(json);
            if (message != null)
            {
                if (message.Type == MessageType.Ping)
                {
                    await SendAsync(NetworkMessage.Create(MessageType.Pong));
                    continue;
                }

                MessageReceived?.Invoke(message);
            }
        }
    }

    private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken);
            if (read == 0)
            {
                return 0;
            }

            total += read;
        }

        return total;
    }

    public void Dispose()
    {
        try
        {
            _receiveCts?.Cancel();
        }
        catch
        {
            // ignore
        }

        try
        {
            _stream?.Dispose();
            _client?.Dispose();
        }
        catch
        {
            // ignore
        }
    }
}

internal sealed class CliResult
{
    public string Command { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}
