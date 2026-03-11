namespace EonVientiane.PlayerModule;

using System.Text;

public sealed partial class PlayerRuntime
{
    private string GetStatus()
    {
        var triggerNotice = TriggerFirstStatusAchievementIfNeeded();

        var currentLevelJson = GetCurrentLevelJson();
        var sb = new StringBuilder();
        sb.AppendLine("=== 玩家信息 ===");
        sb.AppendLine($"名字: {playerName}");
        sb.AppendLine($"等级: {playerLevel}");
        sb.AppendLine($"经验: {experience}");
        sb.AppendLine($"状态: {(string.IsNullOrWhiteSpace(currentLevelJson) ? "Idle" : "InLevel")}");
        if (!string.IsNullOrWhiteSpace(currentLevelJson))
        {
            sb.AppendLine();
            var levelText = InvokeOptional(
                "EonVientiane.LevelModule",
                "EonVientiane.LevelModule.LevelApi",
                "BuildCurrentLevelText",
                currentLevelJson) as string;
            if (!string.IsNullOrWhiteSpace(levelText))
            {
                sb.AppendLine(levelText.TrimEnd());
            }
        }

        var inventoryAddon = InvokeOptional(
            "EonVientiane.InventoryModule",
            "EonVientiane.InventoryModule.InventoryApi",
            "GetStatusAddon",
            sharedState) as string;
        if (!string.IsNullOrWhiteSpace(inventoryAddon))
        {
            sb.AppendLine();
            sb.AppendLine(inventoryAddon);
        }

        var achievementAddon = InvokeOptional(
            "EonVientiane.AchievementModule",
            "EonVientiane.AchievementModule.AchievementRuntime",
            "GetStatusAddon",
            sharedState) as string;
        if (!string.IsNullOrWhiteSpace(achievementAddon))
        {
            sb.AppendLine(achievementAddon);
        }

        if (!string.IsNullOrWhiteSpace(triggerNotice))
        {
            sb.AppendLine(triggerNotice);
        }

        return sb.ToString();
    }

    private string TriggerFirstStatusAchievementIfNeeded()
    {
        if (firstStatusAchievementTriggered)
        {
            return string.Empty;
        }

        firstStatusAchievementTriggered = true;

        var shouldRequest = InvokeOptional(
            "EonVientiane.AchievementStatusModule",
            "EonVientiane.AchievementStatusModule.StatusFirstAchievementRuntime",
            "ShouldRequestForFirstStatus",
            sharedState) as bool?;

        if (shouldRequest == false)
        {
            return string.Empty;
        }

        try
        {
            var requestResult = RequestAchievementVerification("player.status.first");
            if (!requestResult.Success)
            {
                return string.IsNullOrWhiteSpace(requestResult.Message)
                    ? string.Empty
                    : $"⚠ 成就验证失败: {requestResult.Message}";
            }

            MergeUnlockedAchievements(requestResult.GrantedAchievementIds);
            RefreshUnlockedFromLocalPackages();

            if (requestResult.DownloadedCount == 0)
            {
                return "✓ 成就验证完成（无新增模块）";
            }

            var moduleList = string.Join(", ", requestResult.SyncedModuleIds.Distinct(StringComparer.Ordinal));
            return $"✓ 首次 status 成就验证完成，新增模块: {moduleList}";
        }
        catch (Exception ex)
        {
            return $"⚠ 成就验证失败: {ex.Message}";
        }
    }

    private bool TryExecuteModuleCommand(string command, string[] args, out string output)
    {
        var modules = new[]
        {
            (Assembly: "EonVientiane.BattleModule", Type: "EonVientiane.BattleModule.BattleApi"),
            (Assembly: "EonVientiane.NetworkBattleModule", Type: "EonVientiane.NetworkBattleModule.NetworkBattleApi"),
            (Assembly: "EonVientiane.LevelModule", Type: "EonVientiane.LevelModule.LevelApi"),
            (Assembly: "EonVientiane.InventoryModule", Type: "EonVientiane.InventoryModule.InventoryApi"),
            (Assembly: "EonVientiane.EquipmentModule", Type: "EonVientiane.EquipmentModule.EquipmentApi"),
            (Assembly: "EonVientiane.AchievementModule", Type: "EonVientiane.AchievementModule.AchievementRuntime"),
        };

        foreach (var module in modules)
        {
            var canHandle = InvokeOptional(module.Assembly, module.Type, "CanHandleCommand", command) as bool?;
            if (canHandle != true)
            {
                continue;
            }

            var result = InvokeOptional(module.Assembly, module.Type, "ExecuteCommand", sharedState, command, args) as string;
            output = result ?? string.Empty;

            if (string.Equals(module.Assembly, "EonVientiane.BattleModule", StringComparison.Ordinal))
            {
                var battleVerifyNotice = TryVerifyCompletedBattleAndStore();
                if (!string.IsNullOrWhiteSpace(battleVerifyNotice))
                {
                    output = string.IsNullOrWhiteSpace(output)
                        ? battleVerifyNotice
                        : $"{output}{Environment.NewLine}{battleVerifyNotice}";
                }
            }

            return true;
        }

        output = string.Empty;
        return false;
    }

    private string TryVerifyCompletedBattleAndStore()
    {
        var recordJson = InvokeOptional(
            "EonVientiane.BattleModule",
            "EonVientiane.BattleModule.BattleApi",
            "ConsumeLastCompletedRecord",
            sharedState) as string;

        if (string.IsNullOrWhiteSpace(recordJson))
        {
            return string.Empty;
        }

        try
        {
            var response = RequestBattleVerification(recordJson);
            SaveVerifiedBattleRecordToLocal(response);
            return $"✓ 战斗过程签验完成，记录已保存: {response.BattleId}";
        }
        catch (Exception ex)
        {
            return $"⚠ 战斗过程签验失败: {ex.Message}";
        }
    }

    private string BuildModuleHelp()
    {
        var moduleHelpTexts = new List<string>();
        var modules = new[]
        {
            (Assembly: "EonVientiane.BattleModule", Type: "EonVientiane.BattleModule.BattleApi"),
            (Assembly: "EonVientiane.NetworkBattleModule", Type: "EonVientiane.NetworkBattleModule.NetworkBattleApi"),
            (Assembly: "EonVientiane.LevelModule", Type: "EonVientiane.LevelModule.LevelApi"),
            (Assembly: "EonVientiane.InventoryModule", Type: "EonVientiane.InventoryModule.InventoryApi"),
            (Assembly: "EonVientiane.EquipmentModule", Type: "EonVientiane.EquipmentModule.EquipmentApi"),
            (Assembly: "EonVientiane.AchievementModule", Type: "EonVientiane.AchievementModule.AchievementRuntime"),
        };

        foreach (var module in modules)
        {
            var text = InvokeOptional(module.Assembly, module.Type, "GetHelpText") as string;
            if (!string.IsNullOrWhiteSpace(text))
            {
                moduleHelpTexts.Add(text.TrimEnd());
            }
        }

        if (moduleHelpTexts.Count == 0)
        {
            return "(无可用模块命令)";
        }

        return string.Join(Environment.NewLine, moduleHelpTexts);
    }

    private string GetCurrentLevelJson()
    {
        if (sharedState.TryGetValue("level.current", out var levelObj) && levelObj is string levelJson)
        {
            return levelJson;
        }

        return string.Empty;
    }
}