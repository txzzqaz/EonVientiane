namespace EonVientiane.PlayerModule;

using System.Text;

public sealed partial class PlayerRuntime
{
    private string GetStatus()
    {
        var currentLevelJson = GetCurrentLevelJson();
        var sb = new StringBuilder();
        sb.AppendLine("=== 玩家信息 ===");
        sb.AppendLine($"名字: {playerName}");

        var rankAddon = InvokeOptional(
            "EonVientiane.RankModule",
            "EonVientiane.RankModule.RankApi",
            "GetStatusAddon",
            sharedState) as string;
        if (!string.IsNullOrWhiteSpace(rankAddon))
        {
            sb.AppendLine(rankAddon.TrimEnd());
        }

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

        return sb.ToString();
    }

    private bool TryExecuteModuleCommand(string command, string[] args, out string output)
    {
        var modules = new[]
        {
            (Assembly: "EonVientiane.BattleModule", Type: "EonVientiane.BattleModule.BattleApi"),
            (Assembly: "EonVientiane.NetworkBattleModule", Type: "EonVientiane.NetworkBattleModule.NetworkBattleApi"),
            (Assembly: "EonVientiane.LevelModule", Type: "EonVientiane.LevelModule.LevelApi"),
            (Assembly: "EonVientiane.InventoryModule", Type: "EonVientiane.InventoryModule.InventoryApi"),
            (Assembly: "EonVientiane.RankModule", Type: "EonVientiane.RankModule.RankApi"),
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
            var rankNotice = InvokeOptional(
                "EonVientiane.RankModule",
                "EonVientiane.RankModule.RankApi",
                "ApplyBattleResult",
                sharedState,
                recordJson) as string;

            if (string.IsNullOrWhiteSpace(rankNotice))
            {
                return $"✓ 战斗过程签验完成，记录已保存: {response.BattleId}";
            }

            return $"✓ 战斗过程签验完成，记录已保存: {response.BattleId}{Environment.NewLine}{rankNotice}";
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
            (Assembly: "EonVientiane.RankModule", Type: "EonVientiane.RankModule.RankApi"),
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