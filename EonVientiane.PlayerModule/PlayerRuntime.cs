namespace EonVientiane.PlayerModule;

using EonVientiane.Core.Models;
using System.Text.Json;

public sealed partial class PlayerRuntime : IRemoteGameRuntime
{
    private readonly Dictionary<string, object> sharedState = new(StringComparer.Ordinal);

    private string playerName = "玩家";

    public string RuntimeId => "module.player.core";
    public string RuntimeVersion => "1.0.0";

    public void Initialize(string playerName)
    {
        this.playerName = playerName;
        sharedState["player.name"] = playerName;
        sharedState["level.current"] = string.Empty;

        InvokeOptional("EonVientiane.BattleModule", "EonVientiane.BattleModule.BattleApi", "Initialize", sharedState);
        InvokeOptional("EonVientiane.NetworkBattleModule", "EonVientiane.NetworkBattleModule.NetworkBattleApi", "Initialize", sharedState);
        InvokeOptional("EonVientiane.InventoryModule", "EonVientiane.InventoryModule.InventoryApi", "Initialize", sharedState);
        InvokeOptional("EonVientiane.RankModule", "EonVientiane.RankModule.RankApi", "Initialize", sharedState);
        InvokeOptional("EonVientiane.AchievementModule", "EonVientiane.AchievementModule.AchievementRuntime", "Initialize", sharedState);
        InvokeOptional("EonVientiane.AchievementConnectionModule", "EonVientiane.AchievementConnectionModule.ConnectionAchievementRuntime", "Initialize", sharedState);
        RefreshUnlockedFromLocalPackages();
    }

    public string GetPrompt()
    {
        var currentLevelJson = GetCurrentLevelJson();
        if (string.IsNullOrWhiteSpace(currentLevelJson))
        {
            return "等待中";
        }

        try
        {
            using var doc = JsonDocument.Parse(currentLevelJson);
            if (doc.RootElement.TryGetProperty("Name", out var nameProp))
            {
                return nameProp.GetString() ?? "等待中";
            }
        }
        catch
        {
        }

        return "等待中";
    }

    public RuntimeCommandResult Execute(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return new RuntimeCommandResult { Handled = true, Output = string.Empty };
        }

        var parts = commandLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        return cmd switch
        {
            "help" => Ok(GetHelp()),
            "status" => Ok(GetStatus()),
            _ when TryExecuteModuleCommand(cmd, args, out var output) => Ok(output),
            _ => new RuntimeCommandResult { Handled = false },
        };
    }

    private static RuntimeCommandResult Ok(string output) => new() { Handled = true, Output = output };

    private string GetHelp()
    {
        var moduleHelp = BuildModuleHelp();
        return $"""
=== 游戏命令（远程模块）===
status

=== 模块命令 ===
{moduleHelp}
""";
    }
}
