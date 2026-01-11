using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane.Shared;

namespace EonVientianeServer;

/// <summary>
/// 游戏房间
/// </summary>
public class GameRoom
{
    public string RoomId { get; }
    public string RoomName { get; set; }
    public int MaxPlayers { get; set; }
    public RoomStatus Status { get; set; }
    public DateTime? CountdownEndTimeUtc { get; private set; }
    
    private readonly Dictionary<string, ConnectedClient> _players = new();
    private string? _hostPlayerId;
    private static readonly Random _random = new();
    
    /// <summary>
    /// 关联的服务器端战斗（多人战斗）
    /// </summary>
    public ServerBattle? CurrentBattle { get; set; }
    
    public GameRoom(string roomId, string roomName, int maxPlayers, ConnectedClient host)
    {
        RoomId = roomId;
        RoomName = roomName;
        MaxPlayers = maxPlayers;
        Status = RoomStatus.Waiting;
        CurrentBattle = null;
        
        _hostPlayerId = host.UserId;
        _players[host.UserId] = host;
        host.TeamId = 1;
    }
    
    public bool IsFull => MaxPlayers > 0 && _players.Count >= MaxPlayers;
    
    public int PlayerCount => _players.Count;
    
    public IEnumerable<ConnectedClient> Players => _players.Values;
    
    public ConnectedClient? Host => _hostPlayerId != null && _players.ContainsKey(_hostPlayerId) 
        ? _players[_hostPlayerId] 
        : null;
    
    public bool AddPlayer(ConnectedClient client)
    {
        if (Status == RoomStatus.InGame)
            return false;
            
        _players[client.UserId] = client;
        client.CurrentRoomId = RoomId;
        client.TeamId = GetBalancedTeamId();
            
        return true;
    }
    
    public bool RemovePlayer(string playerId)
    {
        if (!_players.Remove(playerId))
            return false;
            
        // 如果是房主离开，选择新房主或关闭房间
        if (playerId == _hostPlayerId)
        {
            _hostPlayerId = _players.Keys.FirstOrDefault();
            
            // 如果房间空了，返回false表示需要删除房间
            if (_hostPlayerId == null)
                return false;
        }
        
        // 更新状态
        if (!IsFull && Status == RoomStatus.Full)
            Status = RoomStatus.Waiting;
            
        return true;
    }
    
    public bool ContainsPlayer(string playerId)
    {
        return _players.ContainsKey(playerId);
    }

    public void SetPlayerReady(string playerId, bool isReady)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.IsReady = isReady;
        }
    }

    public bool AreAllPlayersReady()
    {
        return _players.Count > 0 && _players.Values.All(p => p.IsReady && p.TeamId > 0);
    }

    public void SetPlayerTeam(string playerId, int teamId)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.TeamId = NormalizeTeamId(teamId);
        }
    }

    public void EnsureTeamAssigned(string playerId)
    {
        if (_players.TryGetValue(playerId, out var player) && player.TeamId <= 0)
        {
            player.TeamId = GetBalancedTeamId();
        }
    }

    public void EnsureTeamsAssignedForAll()
    {
        foreach (var player in _players.Values)
        {
            if (player.TeamId <= 0)
            {
                player.TeamId = GetBalancedTeamId();
            }
        }
    }

    public void SetCountdownEnd(DateTime? countdownEnd)
    {
        CountdownEndTimeUtc = countdownEnd;
    }

    private int GetBalancedTeamId()
    {
        var (team1, team2) = GetTeamCounts();
        if (team1 < team2)
            return 1;
        if (team2 < team1)
            return 2;
        return _random.Next(0, 2) == 0 ? 1 : 2;
    }

    private (int team1, int team2) GetTeamCounts()
    {
        int team1 = 0, team2 = 0;
        foreach (var player in _players.Values)
        {
            if (player.TeamId == 1)
                team1++;
            else if (player.TeamId == 2)
                team2++;
        }

        return (team1, team2);
    }

    private static int NormalizeTeamId(int teamId)
    {
        return teamId switch
        {
            1 => 1,
            2 => 2,
            _ => 0
        };
    }
    
    public RoomInfo ToRoomInfo()
    {
        int displayMaxPlayers = MaxPlayers == int.MaxValue ? _players.Count : MaxPlayers;

        return new RoomInfo
        {
            RoomId = RoomId,
            RoomName = RoomName,
            HostPlayerName = Host?.PlayerName ?? "Unknown",
            CurrentPlayers = PlayerCount,
            MaxPlayers = displayMaxPlayers,
            NoPlayerLimit = MaxPlayers == int.MaxValue,
            CountdownEndTimeUtc = CountdownEndTimeUtc,
            Status = Status
        };
    }
    
    public List<PlayerInfo> GetPlayerInfoList()
    {
        return _players.Select(kvp => new PlayerInfo
        {
            PlayerId = kvp.Key,
            PlayerName = kvp.Value.PlayerName,
            IsHost = kvp.Key == _hostPlayerId,
            IsReady = kvp.Value.IsReady,
            TeamId = kvp.Value.TeamId
        }).ToList();
    }
}
