namespace EonVientiane.BattleModule;

using System.Text;
using System.Text.Json;

public static partial class BattleApi
{
    private static string StartBattle(IDictionary<string, object> state, string[] args)
    {
        var mode = args.Length == 0 ? "mirror" : args[0].ToLowerInvariant();
        if (!string.Equals(mode, "mirror", StringComparison.OrdinalIgnoreCase))
        {
            return "❌ 当前仅实现 mirror 模式。";
        }

        var playerName = GetPlayerName(state);
        var playerUnit = BuildUnitFromState(state, "player", playerName, allowLegacyFallback: true);
        var enemyUnit = CloneUnit(playerUnit, "mirror", $"{playerName}·镜像");

        var units = new Dictionary<string, BattleUnit>(StringComparer.Ordinal)
        {
            [playerUnit.UnitId] = playerUnit,
            [enemyUnit.UnitId] = enemyUnit,
        };

        var order = Random.Shared.Next(0, 2) == 0
            ? new[] { playerUnit.UnitId, enemyUnit.UnitId }
            : new[] { enemyUnit.UnitId, playerUnit.UnitId };

        var session = new BattleSession(
            $"battle-{Guid.NewGuid():N}",
            units,
            order.ToList(),
            order[0],
            1,
            null,
            false,
            null,
            new List<string>());

        ClearEffects(state);
        state[LastCompletedRecordStateKey] = null!;

        foreach (var unit in session.Units.Values)
        {
            ApplyAccessoryBattleStart(state, session, unit);
        }

        Log(session, $"战斗开始，先手: {session.GetCurrentActor().DisplayName}");
        state[SessionStateKey] = session;

        var sb = new StringBuilder();
        sb.AppendLine($"✓ 已开始镜像战斗: {playerUnit.DisplayName} VS {enemyUnit.DisplayName}");
        if (playerUnit.Loadout.Count == 0)
        {
            sb.AppendLine("⚠ 当前未检测到可识别道具，战斗将只能使用现有公共变量。");
        }

        sb.Append(DescribeSession(session));
        return sb.ToString();
    }

    private static string ExecuteActiveCommand(IDictionary<string, object> state, string[] args)
    {
        var session = GetSession(state);
        if (session is null)
        {
            return "❌ 当前没有进行中的战斗。";
        }

        if (session.IsCompleted)
        {
            return DescribeSession(session);
        }

        if (args.Length == 0)
        {
            return "❌ 请指定目标。例如: battle active mirror";
        }

        var actor = session.GetCurrentActor();
        var target = ResolveTarget(session, args[0]);
        if (target is null)
        {
            return $"❌ 找不到目标 '{args[0]}'。";
        }

        var requestedDiceName = args.Length > 1 ? string.Join(' ', args.Skip(1)) : null;
        var attackValue = GetPublicValue(actor.PublicValues, "ATKP");
        var selectedDice = SelectActiveDice(actor, requestedDiceName);
        if (selectedDice is null && attackValue <= 0)
        {
            return $"❌ {actor.DisplayName} 当前既没有可用主动骰子，也没有可结算的 ATKP。";
        }

        if (selectedDice is not null)
        {
            var canUse = InvokeOptional(selectedDice.RuntimeType, "CanUseActive", CreateActionContext(state, session, actor, target, selectedDice, "active", attackValue));
            if (canUse is bool canUseBool && !canUseBool)
            {
                return $"❌ {selectedDice.DisplayName} 当前不可使用。";
            }

            var activeResult = InvokeOptional(selectedDice.RuntimeType, "ExecuteActive", CreateActionContext(state, session, actor, target, selectedDice, "active", attackValue));
            attackValue = MergeActionResult(state, session, actor, target, selectedDice, attackValue, attackValue, activeResult, isDamageResult: false);
        }

        if (attackValue <= 0)
        {
            return $"❌ {actor.DisplayName} 未产生有效的 ATKP。";
        }

        target.PublicValues["ATKP"] = attackValue;
        session.PendingAttack = new PendingAttack(actor.UnitId, target.UnitId, attackValue);
        Log(session, $"{actor.DisplayName} 对 {target.DisplayName} 发起攻击，ATKP = {attackValue}");

        var damage = ResolvePassiveAndDamage(state, session, actor, target, attackValue);
        session.PendingAttack = null;
        target.PublicValues.Remove("ATKP");

        if (session.IsCompleted)
        {
            SaveCompletedBattleRecord(state, session, "completed");
        }

        if (!session.IsCompleted)
        {
            AdvanceTurn(session, target.UnitId);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"✓ 已结算主动回合，最终伤害: {damage}");
        sb.Append(DescribeSession(session));
        return sb.ToString();
    }

    private static string PassTurn(IDictionary<string, object> state)
    {
        var session = GetSession(state);
        if (session is null)
        {
            return "❌ 当前没有进行中的战斗。";
        }

        if (session.IsCompleted)
        {
            return DescribeSession(session);
        }

        var actor = session.GetCurrentActor();
        var nextUnitId = session.TurnOrder.FirstOrDefault(x => !string.Equals(x, actor.UnitId, StringComparison.Ordinal) && session.Units.ContainsKey(x))
            ?? actor.UnitId;
        Log(session, $"{actor.DisplayName} 跳过了本回合");
        AdvanceTurn(session, nextUnitId);
        return DescribeSession(session);
    }

    private static string EndBattle(IDictionary<string, object> state)
    {
        var session = GetSession(state);
        if (session is not null)
        {
            SaveCompletedBattleRecord(state, session, session.IsCompleted ? "completed" : "manual-end");
        }

        state[SessionStateKey] = null!;
        ClearEffects(state);
        return "✓ 当前战斗已结束。";
    }

    private static BattleUnit? ResolveTarget(BattleSession session, string rawTarget)
    {
        foreach (var unit in session.Units.Values)
        {
            if (unit.UnitId.Equals(rawTarget, StringComparison.OrdinalIgnoreCase) ||
                unit.DisplayName.Equals(rawTarget, StringComparison.OrdinalIgnoreCase))
            {
                return unit;
            }
        }

        return null;
    }

    private static BattleSession? GetSession(IDictionary<string, object> state)
    {
        return state.TryGetValue(SessionStateKey, out var sessionObj) ? sessionObj as BattleSession : null;
    }

    private static string DescribeSession(BattleSession? session)
    {
        if (session is null)
        {
            return "当前没有进行中的战斗。";
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== 战斗状态 ===");
        sb.AppendLine($"战斗ID: {session.BattleId}");
        sb.AppendLine($"回合计数: {session.TurnNumber}");
        sb.AppendLine($"当前行动方: {session.GetCurrentActor().DisplayName}");
        sb.AppendLine($"状态: {(session.IsCompleted ? "已结束" : "进行中")}");
        if (!string.IsNullOrWhiteSpace(session.WinnerUnitId) && session.Units.TryGetValue(session.WinnerUnitId, out var winner))
        {
            sb.AppendLine($"胜者: {winner.DisplayName}");
        }

        sb.AppendLine();
        sb.AppendLine("=== 单位信息 ===");
        foreach (var unit in session.Units.Values)
        {
            sb.AppendLine($"- {unit.DisplayName} ({unit.UnitId})");
            sb.AppendLine($"  HP: {DescribePublicValue(unit.PublicValues, "HP")}");
            sb.AppendLine($"  ATKP: {DescribePublicValue(unit.PublicValues, "ATKP")}");
            if (unit.Loadout.Count == 0)
            {
                sb.AppendLine("  道具: (无已识别道具)");
            }
            else
            {
                sb.AppendLine($"  道具: {string.Join(", ", unit.Loadout.Select(x => x.DisplayName))}");
            }
        }

        if (session.Log.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== 最近日志 ===");
            foreach (var entry in session.Log.TakeLast(6))
            {
                sb.AppendLine($"  • {entry}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static int ResolvePassiveAndDamage(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        BattleUnit target,
        int pendingAttack)
    {
        var damage = pendingAttack;
        foreach (var passiveDice in target.Loadout.Where(x => x.IsDice && x.SupportsPassive))
        {
            var canUse = InvokeOptional(passiveDice.RuntimeType, "CanUsePassive", CreateActionContext(state, session, target, actor, passiveDice, "passive", pendingAttack, damage));
            if (canUse is bool canUseBool && !canUseBool)
            {
                continue;
            }

            var passiveResult = InvokeOptional(passiveDice.RuntimeType, "ExecutePassive", CreateActionContext(state, session, target, actor, passiveDice, "passive", pendingAttack, damage));
            damage = MergeActionResult(state, session, target, actor, passiveDice, pendingAttack, damage, passiveResult, isDamageResult: true);
        }

        damage = Math.Max(0, damage);

        if (!target.PublicValues.TryGetValue("HP", out var currentHp))
        {
            session.IsCompleted = true;
            session.WinnerUnitId = actor.UnitId;
            Log(session, $"{target.DisplayName} 在受伤前不存在 HP，判定失败");
            return damage;
        }

        var nextHp = currentHp - damage;
        target.PublicValues["HP"] = nextHp;
        Log(session, $"{target.DisplayName} 受到 {damage} 点伤害，HP: {currentHp} -> {nextHp}");

        if (nextHp <= 0)
        {
            session.IsCompleted = true;
            session.WinnerUnitId = actor.UnitId;
            Log(session, $"{target.DisplayName} 的 HP <= 0，{actor.DisplayName} 获胜");
        }

        return damage;
    }

    private static void AdvanceTurn(BattleSession session, string nextActorId)
    {
        session.CurrentActorId = nextActorId;
        session.TurnNumber++;
        if (!session.IsCompleted)
        {
            Log(session, $"轮到 {session.GetCurrentActor().DisplayName} 行动");
        }
    }

    private static void ApplyAccessoryBattleStart(IDictionary<string, object> state, BattleSession session, BattleUnit unit)
    {
        foreach (var accessory in unit.Loadout.Where(x => x.IsAccessory))
        {
            var result = InvokeOptional(accessory.RuntimeType, "OnBattleStart", CreateActionContext(state, session, unit, unit, accessory, "battleStart", 0));
            _ = MergeActionResult(state, session, unit, unit, accessory, 0, 0, result, isDamageResult: false);
        }
    }

    private static void Log(BattleSession session, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        session.Log.Add(text.Trim());
    }

    private static string DescribePublicValue(Dictionary<string, int> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value.ToString() : "(不存在)";
    }

    private static int GetPublicValue(Dictionary<string, int> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : 0;
    }

    private static void SaveCompletedBattleRecord(IDictionary<string, object> state, BattleSession session, string endReason)
    {
        var unitSnapshots = session.Units.Values
            .Select(unit => new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["unitId"] = unit.UnitId,
                ["displayName"] = unit.DisplayName,
                ["publicValues"] = new Dictionary<string, int>(unit.PublicValues, StringComparer.Ordinal),
                ["loadout"] = unit.Loadout
                    .Select(item => new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["itemId"] = item.ItemId,
                        ["name"] = item.DisplayName,
                        ["kind"] = item.Kind,
                        ["isAccessory"] = item.IsAccessory,
                        ["isDice"] = item.IsDice,
                        ["supportsActive"] = item.SupportsActive,
                        ["supportsPassive"] = item.SupportsPassive,
                    })
                    .ToList(),
            })
            .ToList();

        var record = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["battleId"] = session.BattleId,
            ["turnNumber"] = session.TurnNumber,
            ["endReason"] = endReason,
            ["isCompleted"] = session.IsCompleted,
            ["winnerUnitId"] = session.WinnerUnitId,
            ["currentActorId"] = session.CurrentActorId,
            ["turnOrder"] = session.TurnOrder.ToArray(),
            ["units"] = unitSnapshots,
            ["log"] = session.Log.ToArray(),
            ["capturedAtUtc"] = DateTime.UtcNow,
        };

        state[LastCompletedRecordStateKey] = JsonSerializer.Serialize(record);
    }
}