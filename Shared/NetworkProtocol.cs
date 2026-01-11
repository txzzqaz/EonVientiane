using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EonVientiane.Shared;

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    // 连接相关
    Connect,
    Disconnect,
    Ping,
    Pong,
    
    // 用户认证相关
    UserLogin,
    UserLoginResponse,
    UserRegister,
    UserRegisterResponse,
    GetInitialInventory,
    InitialInventoryResponse,
    RequestInventory,
    InventoryState,
    EquipItem,
    EquipItemResponse,
    UnequipItem,
    UnequipItemResponse,
    InventoryUpdated,
    
    // 成就相关
    GetAchievements,
    GetAchievementsResponse,
    UpdateAchievement,
    UpdateAchievementResponse,
    AchievementCompleted,
    
    // 大厅相关
    GetRoomList,
    RoomListResponse,
    CreateRoom,
    CreateRoomResponse,
    JoinRoom,
    JoinRoomResponse,
    LeaveRoom,
    LeaveRoomResponse,
    SetReady,
    SetTeam,
    RoomUpdate,
    GameStartCountdown,
    
    // 游戏相关
    StartGame,
    GameStarted,
    GameAction,
    GameState,
    GameEnd,
    
    // 战斗相关
    BattleInitialize,
    BattleStarted,
    BattleStateUpdate,
    BattleActionRequest,
    BattleActionResponse,
    BattleDefenseRequest,
    BattleDefenseResponse,
    BattleLog,
    BattleEnd,
    
    // 聊天
    ChatMessage,
    
    // 错误
    Error
}

/// <summary>
/// 房间信息
/// </summary>
public class RoomInfo
{
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string HostPlayerName { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public bool NoPlayerLimit { get; set; }
    public DateTime? CountdownEndTimeUtc { get; set; }
    public RoomStatus Status { get; set; }
}

/// <summary>
/// 房间状态
/// </summary>
public enum RoomStatus
{
    Waiting,
    Countdown,
    InGame,
    Full
}

/// <summary>
/// 玩家信息
/// </summary>
public class PlayerInfo
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
    public int TeamId { get; set; }
}

/// <summary>
/// 网络消息基类
/// </summary>
public class NetworkMessage
{
    public MessageType Type { get; set; }
    public string? Data { get; set; }
    
    public T? GetData<T>() where T : class
    {
        if (string.IsNullOrEmpty(Data))
            return null;
            
        return JsonSerializer.Deserialize<T>(Data);
    }
    
    public static NetworkMessage Create<T>(MessageType type, T data)
    {
        return new NetworkMessage
        {
            Type = type,
            Data = JsonSerializer.Serialize(data)
        };
    }
    
    public static NetworkMessage Create(MessageType type)
    {
        return new NetworkMessage
        {
            Type = type
        };
    }
}

/// <summary>
/// 创建房间请求
/// </summary>
public class CreateRoomRequest
{
    public string RoomName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = int.MaxValue;
    public string PlayerName { get; set; } = string.Empty;
}

/// <summary>
/// 创建房间响应
/// </summary>
public class CreateRoomResponse
{
    public bool Success { get; set; }
    public string? RoomId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 加入房间请求
/// </summary>
public class JoinRoomRequest
{
    public string RoomId { get; set; } = string.Empty;
}

/// <summary>
/// 加入房间响应
/// </summary>
public class JoinRoomResponse
{
    public bool Success { get; set; }
    public RoomInfo? RoomInfo { get; set; }
    public List<PlayerInfo>? Players { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 设置准备状态请求
/// </summary>
public class SetReadyRequest
{
    public bool IsReady { get; set; }
}

/// <summary>
/// 选择队伍请求
/// </summary>
public class SetTeamRequest
{
    public int TeamId { get; set; }
}

/// <summary>
/// 房间更新通知
/// </summary>
public class RoomUpdateNotification
{
    public RoomInfo RoomInfo { get; set; } = new();
    public List<PlayerInfo> Players { get; set; } = new();
}

/// <summary>
/// 游戏开始通知
/// </summary>
public class GameStartedNotification
{
    public string RoomId { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
    public List<PlayerInfo> Players { get; set; } = new();
}

/// <summary>
/// 游戏开始倒计时通知
/// </summary>
public class GameStartCountdownNotification
{
    public string RoomId { get; set; } = string.Empty;
    public int CountdownSeconds { get; set; }
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 用户登录请求
/// </summary>
public class UserLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 用户登录响应
/// </summary>
public class UserLoginResponse
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 用户注册请求
/// </summary>
public class UserRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 用户注册响应
/// </summary>
public class UserRegisterResponse
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 获取初始背包请求
/// </summary>
public class GetInitialInventoryRequest
{
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// 初始背包项目
/// </summary>
public class InitialInventoryItem
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>
/// 初始背包响应
/// </summary>
public class InitialInventoryResponse
{
    public bool Success { get; set; }
    public List<InitialInventoryItem> Items { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 单个物品堆叠数据（用于网络同步）
/// </summary>
public class InventoryItemDto
{
    public string StackId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsEquipped { get; set; }
}

/// <summary>
/// 完整背包状态
/// </summary>
public class InventoryState
{
    public List<InventoryItemDto> Items { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 请求背包状态
/// </summary>
public class RequestInventory
{
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// 请求装备物品
/// </summary>
public class EquipItemRequest
{
    public string StackId { get; set; } = string.Empty;
}

/// <summary>
/// 请求卸下物品
/// </summary>
public class UnequipItemRequest
{
    public string StackId { get; set; } = string.Empty;
}

/// <summary>
/// 装备/卸下响应
/// </summary>
public class InventoryActionResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public InventoryState? State { get; set; }
}

/// <summary>
/// 错误消息
/// </summary>
public class ErrorMessage
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 成就数据（用于网络传输）
/// </summary>
public class AchievementDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int RequiredProgress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedTime { get; set; }
}

/// <summary>
/// 获取成就请求
/// </summary>
public class GetAchievementsRequest
{
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// 获取成就响应
/// </summary>
public class GetAchievementsResponse
{
    public bool Success { get; set; }
    public List<AchievementDto> Achievements { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 更新成就请求
/// </summary>
public class UpdateAchievementRequest
{
    public string UserId { get; set; } = string.Empty;
    public string AchievementId { get; set; } = string.Empty;
    public int ProgressDelta { get; set; }
}

/// <summary>
/// 更新成就响应
/// </summary>
public class UpdateAchievementResponse
{
    public bool Success { get; set; }
    public bool IsCompleted { get; set; }
    public int Progress { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 成就完成通知
/// </summary>
public class AchievementCompletedNotification
{
    public string AchievementId { get; set; } = string.Empty;
    public string AchievementName { get; set; } = string.Empty;
    public List<RewardDto> Rewards { get; set; } = new();
    public DateTime CompletedTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 奖励数据（用于网络传输）
/// </summary>
public class RewardDto
{
    public string Type { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
/// <summary>
/// 玩家状态数据（用于网络传输）
/// </summary>
public class BattlePlayerStateDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public int ShieldLayers { get; set; }
    public bool IsDead { get; set; }
    public List<string> EquippedDiceNames { get; set; } = new();
}

/// <summary>
/// 战斗初始化通知
/// </summary>
public class BattleInitializeNotification
{
    public string RoomId { get; set; } = string.Empty;
    public List<BattlePlayerStateDto> Players { get; set; } = new();
    public int CurrentRound { get; set; }
    public string CurrentCamp { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 战斗状态更新通知
/// </summary>
public class BattleStateUpdateNotification
{
    public string RoomId { get; set; } = string.Empty;
    public List<BattlePlayerStateDto> Players { get; set; } = new();
    public int CurrentRound { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string CurrentCamp { get; set; } = string.Empty;
    public string CurrentActionPlayerId { get; set; } = string.Empty;
    public string CurrentActionPlayerName { get; set; } = string.Empty;
    public string WaitingInputPlayerId { get; set; } = string.Empty;
    public string InputContext { get; set; } = string.Empty;
    public List<string> AvailableActiveDiceNames { get; set; } = new();
    public List<string> AvailableOpponentIds { get; set; } = new();
    public List<string> AvailablePassiveDiceNames { get; set; } = new();
    public List<string> NewBattleLogs { get; set; } = new();
    public bool IsBattleOver { get; set; }
    public string WinnerCamp { get; set; } = string.Empty;
}

/// <summary>
/// 战斗行动请求
/// </summary>
public class BattleActionRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string SelectedDiceName { get; set; } = string.Empty;
    public string TargetPlayerId { get; set; } = string.Empty;
}

/// <summary>
/// 防守行动请求
/// </summary>
public class BattleDefenseRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string SelectedDiceName { get; set; } = string.Empty;
}

/// <summary>
/// 战斗结束通知
/// </summary>
public class BattleEndNotification
{
    public string RoomId { get; set; } = string.Empty;
    public string WinnerCamp { get; set; } = string.Empty;
    public List<BattlePlayerStateDto> FinalPlayerStates { get; set; } = new();
    public List<string> BattleLogs { get; set; } = new();
    public DateTime EndTimeUtc { get; set; } = DateTime.UtcNow;
}