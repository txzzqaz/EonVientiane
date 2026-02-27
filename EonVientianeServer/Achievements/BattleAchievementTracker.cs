using System;
using System.Collections.Generic;
using System.Linq;
using EonVientiane;

namespace EonVientianeServer.Achievements;

public sealed class BattleAchievementTracker
{
    private readonly Dictionary<string, int> _featheredDodgeStreak = new();
    private readonly Dictionary<string, bool> _wandererHeartTriggered = new();
    private readonly Dictionary<string, bool> _usedPD = new();
    private readonly Dictionary<string, HashSet<int>> _rollValues = new();
    private readonly Dictionary<string, List<int>> _damageSequence = new();

    public void RegisterPlayer(string playerId)
    {
        if (!_featheredDodgeStreak.ContainsKey(playerId))
            _featheredDodgeStreak[playerId] = 0;
        if (!_wandererHeartTriggered.ContainsKey(playerId))
            _wandererHeartTriggered[playerId] = false;
        if (!_usedPD.ContainsKey(playerId))
            _usedPD[playerId] = false;
        if (!_rollValues.ContainsKey(playerId))
            _rollValues[playerId] = new HashSet<int>();
        if (!_damageSequence.ContainsKey(playerId))
            _damageSequence[playerId] = new List<int>();
    }

    public void ResetForBattle(IEnumerable<Player> players)
    {
        foreach (var player in players)
        {
            RegisterPlayer(player.PlayerId);
            _featheredDodgeStreak[player.PlayerId] = 0;
            _wandererHeartTriggered[player.PlayerId] = false;
            _usedPD[player.PlayerId] = false;
            _rollValues[player.PlayerId].Clear();
            _damageSequence[player.PlayerId].Clear();
        }
    }

    public void TrackFeatheredDodgeStreak(string playerId, Dice? usedDice, DefenseResult defenseResult, Action<string>? log = null)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        if (!_featheredDodgeStreak.ContainsKey(playerId))
            _featheredDodgeStreak[playerId] = 0;

        if (usedDice is FeatheredDice)
        {
            if (defenseResult.ActualDamage == 0)
            {
                _featheredDodgeStreak[playerId]++;
                log?.Invoke($"[成就追踪] {playerId} 飞羽连续闪避成功 {_featheredDodgeStreak[playerId]} 次");
            }
            else
            {
                _featheredDodgeStreak[playerId] = 0;
            }
        }
        else
        {
            _featheredDodgeStreak[playerId] = 0;
        }
    }

    public void TrackRoll(string playerId, int rollValue)
    {
        if (rollValue <= 0 || string.IsNullOrEmpty(playerId))
            return;

        if (_rollValues.TryGetValue(playerId, out var rolls))
        {
            rolls.Add(rollValue);
        }
    }

    public void RecordDamage(string playerId, int damage)
    {
        if (damage <= 0 || string.IsNullOrEmpty(playerId))
            return;

        if (!_damageSequence.ContainsKey(playerId))
            _damageSequence[playerId] = new List<int>();

        _damageSequence[playerId].Add(damage);
    }

    public void MarkUsedPD(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        _usedPD[playerId] = true;
    }

    public void MarkWandererHeartTriggered(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        _wandererHeartTriggered[playerId] = true;
    }

    public bool HasUsedPD(string playerId)
    {
        return _usedPD.TryGetValue(playerId, out var used) && used;
    }

    public bool HasWandererHeartTriggered(string playerId)
    {
        return _wandererHeartTriggered.TryGetValue(playerId, out var triggered) && triggered;
    }

    public int GetFeatheredDodgeStreak(string playerId)
    {
        return _featheredDodgeStreak.TryGetValue(playerId, out var streak) ? streak : 0;
    }

    public IReadOnlyList<int> GetDamageSequence(string playerId)
    {
        return _damageSequence.TryGetValue(playerId, out var damageSeq) ? damageSeq : Array.Empty<int>();
    }

    public Dictionary<string, (bool hasRolls, int? uniformValue)> GetRollUniformity()
    {
        var result = new Dictionary<string, (bool hasRolls, int? uniformValue)>();

        foreach (var kvp in _rollValues)
        {
            bool hasRolls = kvp.Value.Count > 0;
            int? uniformValue = kvp.Value.Count == 1 ? kvp.Value.First() : null;
            result[kvp.Key] = (hasRolls, uniformValue);
        }

        return result;
    }
}
