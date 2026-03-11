namespace EonVientiane.BattleModule;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public static partial class BattleApi
{
    private static string StartBattle(IDictionary<string, object> state, string[] args)
    {
        var mode = args.Length == 0 ? "mirror" : args[0].ToLowerInvariant();
        if (!mode.Equals("mirror", StringComparison.OrdinalIgnoreCase)
            && !mode.Equals("level", StringComparison.OrdinalIgnoreCase)
            && !mode.Equals("pvp", StringComparison.OrdinalIgnoreCase))
        {
            return "❌ 不支持的 battle 模式。可用: mirror / level / pvp";
        }

        var formationText = args.Length > 1 ? args[1] : "1v1";
        if (!TryParseFormation(formationText, out var sideCounts, out var formationError))
        {
            return formationError;
        }

        var playerName = GetPlayerName(state);
        var playerTemplate = BuildUnitFromState(state, "player", playerName, "side1", "第1方");
        var discoveredItems = DiscoverItemModules();
        var opponentSpec = ResolveOpponentAccountSpec(state, mode);
        var opponentTemplate = mode.Equals("mirror", StringComparison.OrdinalIgnoreCase)
            ? CloneUnit(playerTemplate, "mirror", $"{playerName}·镜像", "side2", "第2方", isLocalControlled: false, isLoadoutVisible: false)
            : BuildUnitFromSpec(opponentSpec, discoveredItems, "side2", "第2方");

        var units = BuildUnitsForFormation(sideCounts, playerTemplate, opponentTemplate, mode);
        var order = BuildTurnOrder(units);
        if (order.Count == 0)
        {
            return "❌ 无法建立有效行动顺序。";
        }

        var battleStartTime = DateTime.UtcNow;
        var session = new BattleSession(
            $"battle-{Guid.NewGuid():N}",
            units,
            order,
            order[0],
            1,
            battleStartTime,
            battleStartTime,
            null,
            false,
            null,
            new List<string>(),
            mode);

        ClearEffects(state);
        state[LastCompletedRecordStateKey] = null!;

        foreach (var unit in session.Units.Values)
        {
            ApplyAccessoryBattleStart(state, session, unit);
        }

        EvaluateBattleCompletion(session, null);

        Log(session, $"战斗开始，先手: {session.GetCurrentActor().DisplayName}");
        state[SessionStateKey] = session;

        var sb = new StringBuilder();
        sb.AppendLine($"✓ 已开始战斗({mode}, {formationText}): {DescribeSideOverview(session)}");
        if (playerTemplate.Loadout.Count == 0)
        {
            sb.AppendLine("⚠ 当前未检测到可识别道具，战斗将只能使用现有公共变量。");
        }

        if (!session.GetCurrentActor().IsLocalControlled)
        {
            ResolveAutoTurns(state, session);
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

        EnsureCurrentActorIsAlive(session);

        if (session.IsCompleted)
        {
            return DescribeSession(session);
        }

        var actor = session.GetCurrentActor();
        if (!actor.IsLocalControlled)
        {
            return "❌ 当前是对手自动行动回合，无法手动执行 active。";
        }

        var gateResult = RunCommandGateHooks(state, session, actor, "active", args);
        if (gateResult.ShouldForcePass)
        {
            return PassTurnInternal(state, invokedByHook: true, reason: gateResult.Message);
        }

        if (gateResult.ShouldCancel)
        {
            return string.IsNullOrWhiteSpace(gateResult.Message)
                ? $"❌ {actor.DisplayName} 的行动被道具 Hook 取消。"
                : gateResult.Message!;
        }

        if (TryGetPendingAttackForActor(session, actor, out var pendingAttack))
        {
            var requestedPassiveDice = string.Join(' ', args).Trim();
            if (string.IsNullOrWhiteSpace(requestedPassiveDice))
            {
                return "❌ 当前为被动回合，请使用: battle active <被动骰子名>";
            }

            var selectedPassiveDice = SelectPassiveDice(actor, requestedPassiveDice);
            if (selectedPassiveDice is null)
            {
                return $"❌ 找不到可用被动骰子 '{requestedPassiveDice}'。";
            }

            if (!session.Units.TryGetValue(pendingAttack.SourceUnitId, out var attackSource))
            {
                return "❌ 攻击来源已失效，无法结算被动回合。";
            }

            var damage = ResolvePassiveAndDamage(state, session, attackSource, actor, pendingAttack.AttackValue, selectedPassiveDice);
            session.PendingAttack = null;
            actor.PublicValues.Remove("ATKP");

            if (session.IsCompleted)
            {
                SaveCompletedBattleRecord(state, session, "completed");
            }
            else
            {
                Log(session, $"{actor.DisplayName} 的被动回合结束，进入主动回合");
                if (!actor.IsLocalControlled)
                {
                    ResolveAutoTurns(state, session);
                }
            }

            var passiveSb = new StringBuilder();
            passiveSb.AppendLine($"✓ 已结算被动回合，最终伤害: {damage}");
            passiveSb.Append(DescribeSession(session));
            return passiveSb.ToString();
        }

        if (args.Length < 2)
        {
            return "❌ 当前为主动回合，请使用: battle active <目标> <主动骰子名>";
        }

        var target = ResolveTarget(session, args[0]);
        if (target is null)
        {
            return $"❌ 找不到目标 '{args[0]}'。";
        }

        if (IsUnitDefeated(target))
        {
            return $"❌ 目标 {target.DisplayName} 已失败，无法被再次指定。";
        }

        var requestedDiceName = string.Join(' ', args.Skip(1)).Trim();
        var attackValue = GetPublicValue(actor.PublicValues, "ATKP");
        var selectedDice = SelectActiveDice(actor, requestedDiceName);
        if (selectedDice is null)
        {
            return $"❌ 找不到可用主动骰子 '{requestedDiceName}'。";
        }

        var canUse = InvokeItemFunctionWithHooks(
            state,
            session,
            actor,
            target,
            selectedDice,
            "CanUseActive",
            CreateActionContext(state, session, actor, target, selectedDice, "active", attackValue));
        if (canUse is bool canUseBool && !canUseBool)
        {
            return $"❌ {selectedDice.DisplayName} 当前不可使用。";
        }

        var activeResult = InvokeItemFunctionWithHooks(
            state,
            session,
            actor,
            target,
            selectedDice,
            "ExecuteActive",
            CreateActionContext(state, session, actor, target, selectedDice, "active", attackValue));
        attackValue = MergeActionResult(state, session, actor, target, selectedDice, attackValue, attackValue, activeResult, isDamageResult: false);

        if (attackValue <= 0)
        {
            return $"❌ {actor.DisplayName} 未产生有效的 ATKP。";
        }

        target.PublicValues["ATKP"] = attackValue;
        session.PendingAttack = new PendingAttack(actor.UnitId, target.UnitId, attackValue);
        Log(session, $"{actor.DisplayName} 对 {target.DisplayName} 发起攻击，ATKP = {attackValue}");

        AdvanceTurn(session, target.UnitId);
        ResolveAutoTurns(state, session);

        var sb = new StringBuilder();
        sb.AppendLine($"✓ 已结算主动回合，待 {target.DisplayName} 执行被动骰结算");
        sb.Append(DescribeSession(session));
        return sb.ToString();
    }

    private static string PassTurn(IDictionary<string, object> state)
    {
        return PassTurnInternal(state, invokedByHook: false, reason: null);
    }

    private static string PassTurnInternal(IDictionary<string, object> state, bool invokedByHook, string? reason)
    {
        var session = GetSession(state);
        if (session is null)
        {
            return "❌ 当前没有进行中的战斗。";
        }

        EnsureCurrentActorIsAlive(session);

        if (session.IsCompleted)
        {
            return DescribeSession(session);
        }

        var actor = session.GetCurrentActor();
        if (!actor.IsLocalControlled)
        {
            return "❌ 当前是对手自动行动回合，无法手动执行 pass。";
        }

        if (!invokedByHook)
        {
            var gateResult = RunCommandGateHooks(state, session, actor, "pass", Array.Empty<string>());
            if (gateResult.ShouldForcePass)
            {
                return PassTurnInternal(state, invokedByHook: true, reason: gateResult.Message);
            }

            if (gateResult.ShouldCancel)
            {
                return string.IsNullOrWhiteSpace(gateResult.Message)
                    ? $"❌ {actor.DisplayName} 的跳过回合被道具 Hook 取消。"
                    : gateResult.Message!;
            }
        }

        if (TryGetPendingAttackForActor(session, actor, out var pendingAttack))
        {
            if (!session.Units.TryGetValue(pendingAttack.SourceUnitId, out var attackSource))
            {
                session.PendingAttack = null;
                actor.PublicValues.Remove("ATKP");
                Log(session, $"{actor.DisplayName} 的被动结算失败：攻击来源不存在，已跳过该次攻击");
                return DescribeSession(session);
            }

            var damage = ResolvePassiveAndDamage(state, session, attackSource, actor, pendingAttack.AttackValue, selectedPassiveDice: null);
            session.PendingAttack = null;
            actor.PublicValues.Remove("ATKP");

            if (session.IsCompleted)
            {
                SaveCompletedBattleRecord(state, session, "completed");
            }
            else
            {
                if (invokedByHook)
                {
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        Log(session, $"{actor.DisplayName} 触发 Hook，在被动回合以 pass 直接承受伤害");
                    }
                    else
                    {
                        Log(session, $"{actor.DisplayName} 触发 Hook，在被动回合以 pass 直接承受伤害: {reason.Trim()}");
                    }
                }
                else
                {
                    Log(session, $"{actor.DisplayName} 在被动回合使用 pass，直接承受 ATKP 伤害");
                }

                Log(session, $"{actor.DisplayName} 的被动回合结束，进入主动回合");
                if (!actor.IsLocalControlled)
                {
                    ResolveAutoTurns(state, session);
                }
            }

            var passiveSb = new StringBuilder();
            passiveSb.AppendLine($"✓ 已在被动回合执行 pass，ATKP 已直接转化为伤害: {damage}");
            passiveSb.Append(DescribeSession(session));
            return passiveSb.ToString();
        }

        var nextUnitId = ResolveNextActorAfterPass(session, actor.UnitId);
        if (invokedByHook)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                Log(session, $"{actor.DisplayName} 触发 Hook 自动跳过本回合");
            }
            else
            {
                Log(session, $"{actor.DisplayName} 触发 Hook 自动跳过本回合: {reason.Trim()}");
            }
        }
        else
        {
            Log(session, $"{actor.DisplayName} 跳过了本回合");
        }

        if (nextUnitId is null)
        {
            EvaluateBattleCompletion(session, null);
        }
        else
        {
            AdvanceTurn(session, nextUnitId);
            ResolveAutoTurns(state, session);
        }

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

    private static bool TryParseFormation(string raw, out List<int> sideCounts, out string error)
    {
        sideCounts = new List<int>();
        error = string.Empty;
        var text = string.IsNullOrWhiteSpace(raw) ? "1v1" : raw.Trim();
        if (!Regex.IsMatch(text, @"^\d+(v\d+)*$", RegexOptions.IgnoreCase))
        {
            error = "❌ 阵型格式错误。示例: 1v1 / 2v2 / 3v3v3 / 1v2";
            return false;
        }

        var tokens = text.Split('v', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (!int.TryParse(token, out var count) || count <= 0)
            {
                error = "❌ 阵型人数必须为正整数。";
                return false;
            }

            if (count > 9)
            {
                error = "❌ 单方单位数量过大，请控制在 9 以内。";
                return false;
            }

            sideCounts.Add(count);
        }

        if (sideCounts.Count < 2)
        {
            error = "❌ 至少需要双方单位，例如 1v1。";
            return false;
        }

        if (sideCounts.Sum() > 24)
        {
            error = "❌ 总单位数量过大，请控制在 24 以内。";
            return false;
        }

        return true;
    }

    private static Dictionary<string, BattleUnit> BuildUnitsForFormation(List<int> sideCounts, BattleUnit playerTemplate, BattleUnit opponentTemplate, string mode)
    {
        var units = new Dictionary<string, BattleUnit>(StringComparer.Ordinal);
        for (var sideIndex = 0; sideIndex < sideCounts.Count; sideIndex++)
        {
            var sideId = $"side{sideIndex + 1}";
            var sideName = $"第{sideIndex + 1}方";
            var count = sideCounts[sideIndex];
            var template = sideIndex == 0 ? playerTemplate : opponentTemplate;

            for (var memberIndex = 0; memberIndex < count; memberIndex++)
            {
                var unitId = CreateFormationUnitId(template, mode, sideIndex, memberIndex, sideCounts);
                var displayName = CreateFormationDisplayName(template, sideIndex, memberIndex, count);
                var unit = CloneUnit(
                    template,
                    unitId,
                    displayName,
                    sideId,
                    sideName,
                    isLocalControlled: sideIndex == 0,
                    isLoadoutVisible: sideIndex == 0 || template.IsLoadoutVisible);
                units[unit.UnitId] = unit;
            }
        }

        return units;
    }

    private static string CreateFormationUnitId(BattleUnit template, string mode, int sideIndex, int memberIndex, List<int> sideCounts)
    {
        if (sideIndex == 0)
        {
            if (memberIndex == 0)
            {
                return "player";
            }

            return $"player.{memberIndex + 1}";
        }

        if (sideIndex == 1 && memberIndex == 0 && sideCounts.Count == 2)
        {
            return template.UnitId;
        }

        return $"{mode}.s{sideIndex + 1}.u{memberIndex + 1}";
    }

    private static string CreateFormationDisplayName(BattleUnit template, int sideIndex, int memberIndex, int sideCount)
    {
        if (sideCount == 1 && memberIndex == 0)
        {
            return template.DisplayName;
        }

        return $"{template.DisplayName}#{memberIndex + 1}";
    }

    private static List<string> BuildTurnOrder(Dictionary<string, BattleUnit> units)
    {
        var bySide = units.Values
            .GroupBy(x => x.SideId)
            .OrderBy(g => ParseSideIndex(g.Key))
            .Select(g => g
                .OrderBy(u => ParseUnitSeat(u.UnitId))
                .ThenBy(u => u.UnitId, StringComparer.Ordinal)
                .ToList())
            .ToList();

        var order = new List<string>();
        var maxSeat = bySide.Count == 0 ? 0 : bySide.Max(x => x.Count);
        for (var seat = 0; seat < maxSeat; seat++)
        {
            foreach (var side in bySide)
            {
                if (seat < side.Count)
                {
                    order.Add(side[seat].UnitId);
                }
            }
        }

        if (order.Count <= 1)
        {
            return order;
        }

        var startIndex = Random.Shared.Next(0, order.Count);
        return order.Skip(startIndex).Concat(order.Take(startIndex)).ToList();
    }

    private static int ParseSideIndex(string sideId)
    {
        var match = Regex.Match(sideId ?? string.Empty, @"(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : int.MaxValue;
    }

    private static int ParseUnitSeat(string unitId)
    {
        var match = Regex.Match(unitId ?? string.Empty, @"(?:\.u|\.)(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : 1;
    }

    private static string DescribeSideOverview(BattleSession session)
    {
        var chunks = session.Units.Values
            .GroupBy(x => x.SideId)
            .OrderBy(x => ParseSideIndex(x.Key))
            .Select(group =>
            {
                var sideName = group.First().SideName;
                var aliveCount = group.Count(x => !IsUnitDefeated(x));
                return $"{sideName} {aliveCount}/{group.Count()}";
            });

        return string.Join(" | ", chunks);
    }

    private static bool IsUnitDefeated(BattleUnit unit)
    {
        return unit.PublicValues.TryGetValue("HP", out var hp) && hp <= 0;
    }

    private static string? ResolveNextActorAfterPass(BattleSession session, string currentActorId)
    {
        return FindNextAliveUnit(session, currentActorId, includeCurrentAsFallback: true);
    }

    private static string ResolveNextActorAfterAttack(BattleSession session, BattleUnit actor, BattleUnit target)
    {
        if (!IsUnitDefeated(target))
        {
            return target.UnitId;
        }

        return FindNextAliveUnit(session, actor.UnitId, includeCurrentAsFallback: true)
            ?? actor.UnitId;
    }

    private static string? FindNextAliveUnit(BattleSession session, string currentActorId, bool includeCurrentAsFallback)
    {
        var count = session.TurnOrder.Count;
        if (count == 0)
        {
            return null;
        }

        var startIndex = session.TurnOrder.FindIndex(x => x.Equals(currentActorId, StringComparison.Ordinal));
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        for (var offset = 1; offset <= count; offset++)
        {
            var candidate = session.TurnOrder[(startIndex + offset) % count];
            if (!session.Units.TryGetValue(candidate, out var unit) || IsUnitDefeated(unit))
            {
                continue;
            }

            return candidate;
        }

        if (!includeCurrentAsFallback)
        {
            return null;
        }

        if (session.Units.TryGetValue(currentActorId, out var current) && !IsUnitDefeated(current))
        {
            return currentActorId;
        }

        return null;
    }

    private static void EnsureCurrentActorIsAlive(BattleSession session)
    {
        if (session.IsCompleted)
        {
            return;
        }

        if (session.Units.TryGetValue(session.CurrentActorId, out var current) && !IsUnitDefeated(current))
        {
            return;
        }

        var next = FindNextAliveUnit(session, session.CurrentActorId, includeCurrentAsFallback: false);
        if (next is null)
        {
            EvaluateBattleCompletion(session, null);
            return;
        }

        session.CurrentActorId = next;
        session.TurnStartedAtUtc = DateTime.UtcNow;
        Log(session, $"行动方失效，自动切换至 {session.GetCurrentActor().DisplayName}");
    }

    private static BattleUnit? SelectDefaultAutoTarget(BattleSession session, BattleUnit actor)
    {
        var enemy = session.Units.Values.FirstOrDefault(x =>
            !x.UnitId.Equals(actor.UnitId, StringComparison.Ordinal) &&
            !x.SideId.Equals(actor.SideId, StringComparison.Ordinal) &&
            !IsUnitDefeated(x));
        if (enemy is not null)
        {
            return enemy;
        }

        return session.Units.Values.FirstOrDefault(x =>
            !x.UnitId.Equals(actor.UnitId, StringComparison.Ordinal) &&
            !IsUnitDefeated(x));
    }

    private static void EvaluateBattleCompletion(BattleSession session, BattleUnit? finisher)
    {
        if (session.IsCompleted)
        {
            return;
        }

        var aliveUnits = session.Units.Values.Where(x => !IsUnitDefeated(x)).ToList();
        var aliveSides = aliveUnits
            .Select(x => x.SideId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (aliveSides.Count > 1)
        {
            return;
        }

        session.IsCompleted = true;
        session.WinnerSideId = aliveSides.FirstOrDefault();
        if (finisher is not null && !IsUnitDefeated(finisher))
        {
            session.WinnerUnitId = finisher.UnitId;
        }
        else
        {
            session.WinnerUnitId = aliveUnits.FirstOrDefault()?.UnitId;
        }

        if (!string.IsNullOrWhiteSpace(session.WinnerSideId))
        {
            Log(session, $"战斗结束，胜方: {session.WinnerSideId}");
        }
        else
        {
            Log(session, "战斗结束，无可行动单位");
        }
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

        var now = DateTime.UtcNow;
        var sb = new StringBuilder();
        sb.AppendLine("=== 战斗状态 ===");
        sb.AppendLine($"战斗ID: {session.BattleId}");
        sb.AppendLine($"回合计数: {session.TurnNumber}");
        sb.AppendLine($"模式: {session.BattleMode}");
        var currentActor = session.GetCurrentActor();
        sb.AppendLine($"当前行动方: {currentActor.DisplayName} ({currentActor.SideName})");
        sb.AppendLine($"状态: {(session.IsCompleted ? "已结束" : "进行中")}");
        sb.AppendLine($"战局用时: {FormatDuration(now - session.BattleStartedAtUtc)}");
        sb.AppendLine($"当前回合用时: {FormatDuration(now - session.TurnStartedAtUtc)}");
        sb.AppendLine($"阵营概览: {DescribeSideOverview(session)}");
        sb.AppendLine($"阶段: {DescribeBattlePhase(session)}");
        if (!string.IsNullOrWhiteSpace(session.WinnerSideId))
        {
            sb.AppendLine($"胜方: {session.WinnerSideId}");
        }

        if (!string.IsNullOrWhiteSpace(session.WinnerUnitId) && session.Units.TryGetValue(session.WinnerUnitId, out var winner))
        {
            sb.AppendLine($"胜者: {winner.DisplayName}");
        }

        sb.AppendLine();
        sb.AppendLine("=== 单位信息 ===");
        foreach (var unit in session.Units.Values)
        {
            sb.AppendLine($"- {unit.DisplayName} ({unit.UnitId}) [{unit.SideName}]");
            sb.AppendLine($"  HP: {DescribePublicValue(unit.PublicValues, "HP")}");
            sb.AppendLine($"  ATKP: {DescribePublicValue(unit.PublicValues, "ATKP")}");
            sb.AppendLine($"  状态: {(IsUnitDefeated(unit) ? "已失败" : "可行动")}");
            if (!unit.IsLoadoutVisible)
            {
                sb.AppendLine("  道具: (对方道具表不可见)");
            }
            else if (unit.Loadout.Count == 0)
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

    private static void ResolveAutoTurns(IDictionary<string, object> state, BattleSession session)
    {
        var safeCounter = 0;
        while (!session.IsCompleted && safeCounter < 64)
        {
            EnsureCurrentActorIsAlive(session);
            if (session.IsCompleted)
            {
                break;
            }

            if (session.GetCurrentActor().IsLocalControlled)
            {
                break;
            }

            safeCounter++;
            ExecuteAutoTurn(state, session);
        }
    }

    private static void ExecuteAutoTurn(IDictionary<string, object> state, BattleSession session)
    {
        var actor = session.GetCurrentActor();

        if (TryGetPendingAttackForActor(session, actor, out var pendingAttack))
        {
            if (!session.Units.TryGetValue(pendingAttack.SourceUnitId, out var sourceActor))
            {
                session.PendingAttack = null;
                actor.PublicValues.Remove("ATKP");
                Log(session, $"{actor.DisplayName} 的被动结算失败：攻击来源不存在，已跳过该次攻击");
                return;
            }

            var passiveDecision = ResolveAutoTurnAction(state, session, actor, sourceActor, pendingAttack.AttackValue, phase: "passive")
                ?? new Dictionary<string, object?>(StringComparer.Ordinal);
            var requestedPassiveDiceName = ResolveRequestedDiceName(passiveDecision);
            if (string.IsNullOrWhiteSpace(requestedPassiveDiceName))
            {
                Log(session, $"{actor.DisplayName} 自动被动失败：未选择被动骰，按原伤害结算");
            }

            var selectedPassiveDice = SelectPassiveDice(actor, requestedPassiveDiceName);
            if (!string.IsNullOrWhiteSpace(requestedPassiveDiceName) && selectedPassiveDice is null)
            {
                Log(session, $"{actor.DisplayName} 自动被动失败：找不到被动骰 '{requestedPassiveDiceName}'，按原伤害结算");
            }

            var damage = ResolvePassiveAndDamage(state, session, sourceActor, actor, pendingAttack.AttackValue, selectedPassiveDice);
            session.PendingAttack = null;
            actor.PublicValues.Remove("ATKP");
            Log(session, $"{actor.DisplayName} 自动被动结算伤害: {damage}");

            if (session.IsCompleted)
            {
                SaveCompletedBattleRecord(state, session, "completed");
                return;
            }

            Log(session, $"{actor.DisplayName} 的被动回合结束，进入主动回合");
            return;
        }

        var target = SelectDefaultAutoTarget(session, actor);
        if (target is null)
        {
            EvaluateBattleCompletion(session, actor);
            return;
        }

        var attackValue = GetPublicValue(actor.PublicValues, "ATKP");
        var actionDecision = ResolveAutoTurnAction(state, session, actor, target, attackValue, phase: "active");
        if (actionDecision is null)
        {
            Log(session, $"{actor.DisplayName} 自动行动失败：未提供有效行动决策，跳过本回合");
            var fallbackNext = ResolveNextActorAfterPass(session, actor.UnitId);
            if (fallbackNext is not null)
            {
                AdvanceTurn(session, fallbackNext);
            }
            return;
        }

        var actionType = GetStringOrDefault(actionDecision, "action", "active").Trim();
        if (actionType.Equals("pass", StringComparison.OrdinalIgnoreCase))
        {
            Log(session, $"{actor.DisplayName} 自动行动：跳过本回合");
            var passNext = ResolveNextActorAfterPass(session, actor.UnitId);
            if (passNext is not null)
            {
                AdvanceTurn(session, passNext);
            }
            return;
        }

        if (TryGetString(actionDecision, "target", out var targetName) && !string.IsNullOrWhiteSpace(targetName))
        {
            var requestedTarget = ResolveTarget(session, targetName);
            if (requestedTarget is not null && !IsUnitDefeated(requestedTarget))
            {
                target = requestedTarget;
            }
        }

        var requestedDiceName = ResolveRequestedDiceName(actionDecision);

        var selectedDice = SelectActiveDice(actor, requestedDiceName);
        if (selectedDice is null)
        {
            Log(session, $"{actor.DisplayName} 自动行动失败：未选择有效主动骰，跳过本回合");
            var fallbackNext = ResolveNextActorAfterPass(session, actor.UnitId);
            if (fallbackNext is not null)
            {
                AdvanceTurn(session, fallbackNext);
            }
            return;
        }

        if (TryGetInt(actionDecision, "attack", out var attackOverride))
        {
            attackValue = Math.Max(0, attackOverride);
        }
        else if (TryGetInt(actionDecision, "ATKP", out attackOverride))
        {
            attackValue = Math.Max(0, attackOverride);
        }

        var canUse = InvokeItemFunctionWithHooks(
            state,
            session,
            actor,
            target,
            selectedDice,
            "CanUseActive",
            CreateActionContext(state, session, actor, target, selectedDice, "active", attackValue));

        if (canUse is bool canUseBool && !canUseBool)
        {
            Log(session, $"{actor.DisplayName} 自动行动失败：{selectedDice.DisplayName} 当前不可用，跳过本回合");
            var fallbackNext = ResolveNextActorAfterPass(session, actor.UnitId);
            if (fallbackNext is not null)
            {
                AdvanceTurn(session, fallbackNext);
            }
            return;
        }

        var activeResult = InvokeItemFunctionWithHooks(
            state,
            session,
            actor,
            target,
            selectedDice,
            "ExecuteActive",
            CreateActionContext(state, session, actor, target, selectedDice, "active", attackValue));
        attackValue = MergeActionResult(state, session, actor, target, selectedDice, attackValue, attackValue, activeResult, isDamageResult: false);

        if (attackValue <= 0)
        {
            Log(session, $"{actor.DisplayName} 自动行动失败：未产生有效的 ATKP，跳过本回合");
            var fallbackNext = ResolveNextActorAfterPass(session, actor.UnitId);
            if (fallbackNext is not null)
            {
                AdvanceTurn(session, fallbackNext);
            }
            return;
        }

        session.PendingAttack = new PendingAttack(actor.UnitId, target.UnitId, attackValue);
        Log(session, $"{actor.DisplayName} 自动行动，ATKP = {attackValue}");

        AdvanceTurn(session, target.UnitId);
    }

    private static int ResolvePassiveAndDamage(
        IDictionary<string, object> state,
        BattleSession session,
        BattleUnit actor,
        BattleUnit target,
        int pendingAttack,
        BattleItemDescriptor? selectedPassiveDice)
    {
        var damage = pendingAttack;
        if (selectedPassiveDice is not null)
        {
            var canUse = InvokeItemFunctionWithHooks(
                state,
                session,
                target,
                actor,
                selectedPassiveDice,
                "CanUsePassive",
                CreateActionContext(state, session, target, actor, selectedPassiveDice, "passive", pendingAttack, damage));
            if (canUse is bool canUseBool && !canUseBool)
            {
                Log(session, $"{selectedPassiveDice.DisplayName} 当前不可使用，按原伤害结算");
            }
            else
            {
                var passiveResult = InvokeItemFunctionWithHooks(
                    state,
                    session,
                    target,
                    actor,
                    selectedPassiveDice,
                    "ExecutePassive",
                    CreateActionContext(state, session, target, actor, selectedPassiveDice, "passive", pendingAttack, damage));
                damage = MergeActionResult(state, session, target, actor, selectedPassiveDice, pendingAttack, damage, passiveResult, isDamageResult: true);
            }
        }

        damage = Math.Max(0, damage);

        if (!target.PublicValues.TryGetValue("HP", out var currentHp))
        {
            target.PublicValues["HP"] = 0;
            Log(session, $"{target.DisplayName} 在受伤前不存在 HP，判定失败");
            EvaluateBattleCompletion(session, actor);
            return damage;
        }

        var nextHp = currentHp - damage;
        target.PublicValues["HP"] = nextHp;
        Log(session, $"{target.DisplayName} 受到 {damage} 点伤害，HP: {currentHp} -> {nextHp}");

        if (nextHp <= 0)
        {
            Log(session, $"{target.DisplayName} 的 HP <= 0，判定失败");
            EvaluateBattleCompletion(session, actor);
        }

        return damage;
    }

    private static bool TryGetPendingAttackForActor(BattleSession session, BattleUnit actor, out PendingAttack pendingAttack)
    {
        pendingAttack = default!;
        if (session.PendingAttack is null)
        {
            return false;
        }

        if (!session.PendingAttack.TargetUnitId.Equals(actor.UnitId, StringComparison.Ordinal))
        {
            return false;
        }

        pendingAttack = session.PendingAttack;
        return true;
    }

    private static string DescribeBattlePhase(BattleSession session)
    {
        var actor = session.GetCurrentActor();
        if (TryGetPendingAttackForActor(session, actor, out var pendingAttack) &&
            session.Units.TryGetValue(pendingAttack.SourceUnitId, out var source))
        {
            return $"被动回合（来自 {source.DisplayName} 的攻击，ATKP={pendingAttack.AttackValue}）";
        }

        return "主动回合";
    }

    private static string ResolveRequestedDiceName(Dictionary<string, object?> actionDecision)
    {
        var requestedDiceName = GetStringOrDefault(actionDecision, "requestedDiceName", string.Empty);
        if (string.IsNullOrWhiteSpace(requestedDiceName))
        {
            requestedDiceName = GetStringOrDefault(actionDecision, "dice", string.Empty);
        }

        if (string.IsNullOrWhiteSpace(requestedDiceName))
        {
            requestedDiceName = GetStringOrDefault(actionDecision, "diceName", string.Empty);
        }

        if (string.IsNullOrWhiteSpace(requestedDiceName))
        {
            requestedDiceName = GetStringOrDefault(actionDecision, "itemId", string.Empty);
        }

        return requestedDiceName.Trim();
    }

    private static void AdvanceTurn(BattleSession session, string nextActorId)
    {
        var turnElapsed = DateTime.UtcNow - session.TurnStartedAtUtc;
        Log(session, $"第 {session.TurnNumber} 回合结束，用时 {FormatDuration(turnElapsed)}");
        session.CurrentActorId = nextActorId;
        session.TurnNumber++;
        session.TurnStartedAtUtc = DateTime.UtcNow;
        if (!session.IsCompleted)
        {
            var actor = session.GetCurrentActor();
            Log(session, $"轮到 {actor.DisplayName}({actor.SideName}) 行动");
        }
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.TotalSeconds:F1}s";
    }

    private static void ApplyAccessoryBattleStart(IDictionary<string, object> state, BattleSession session, BattleUnit unit)
    {
        foreach (var accessory in unit.Loadout.Where(x => x.IsAccessory))
        {
            var result = InvokeItemFunctionWithHooks(
                state,
                session,
                unit,
                unit,
                accessory,
                "OnBattleStart",
                CreateActionContext(state, session, unit, unit, accessory, "battleStart", 0));
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
                ["sideId"] = unit.SideId,
                ["sideName"] = unit.SideName,
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

        var endedAtUtc = DateTime.UtcNow;
        var record = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["battleId"] = session.BattleId,
            ["turnNumber"] = session.TurnNumber,
            ["endReason"] = endReason,
            ["isCompleted"] = session.IsCompleted,
            ["winnerUnitId"] = session.WinnerUnitId,
            ["winnerSideId"] = session.WinnerSideId,
            ["currentActorId"] = session.CurrentActorId,
            ["turnOrder"] = session.TurnOrder.ToArray(),
            ["units"] = unitSnapshots,
            ["log"] = session.Log.ToArray(),
            ["battleStartedAtUtc"] = session.BattleStartedAtUtc,
            ["turnStartedAtUtc"] = session.TurnStartedAtUtc,
            ["capturedAtUtc"] = endedAtUtc,
            ["totalDurationSeconds"] = (endedAtUtc - session.BattleStartedAtUtc).TotalSeconds,
            ["currentTurnDurationSeconds"] = (endedAtUtc - session.TurnStartedAtUtc).TotalSeconds,
        };

        state[LastCompletedRecordStateKey] = JsonSerializer.Serialize(record);
    }
}