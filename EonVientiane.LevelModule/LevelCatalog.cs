namespace EonVientiane.LevelModule;

using System.Text;
using System.Text.Json;

public static class LevelApi
{
    private static readonly Dictionary<string, LevelEntry> Levels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["test"] = new("test", "测试关卡", "一个用于测试的基础关卡", 1),
        ["forest"] = new("forest", "森林", "一片神秘的森林", 5),
        ["castle"] = new("castle", "城堡", "一座古老的城堡", 10),
        ["dragon"] = new("dragon", "龙巢", "龙的巢穴，充满危险", 15),
    };

    public static bool CanHandleCommand(string command)
    {
        return command.Equals("levels", StringComparison.OrdinalIgnoreCase)
            || command.Equals("loadlevel", StringComparison.OrdinalIgnoreCase)
            || command.Equals("unloadlevel", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExecuteCommand(IDictionary<string, object> state, string command, string[] args)
    {
        switch (command.ToLowerInvariant())
        {
            case "levels":
                return ListText();

            case "loadlevel":
                if (args.Length == 0)
                {
                    return "❌ 请指定关卡ID. 使用 'levels' 查看可用关卡.";
                }

                var levelJson = LoadLevel(args[0]);
                if (string.IsNullOrWhiteSpace(levelJson))
                {
                    return $"❌ 无法加载关卡 '{args[0]}'";
                }

                state["level.current"] = levelJson;

                using (var doc = JsonDocument.Parse(levelJson))
                {
                    var name = doc.RootElement.TryGetProperty("Name", out var n) ? n.GetString() : args[0];
                    var desc = doc.RootElement.TryGetProperty("Description", out var d) ? d.GetString() : string.Empty;
                    return $"✓ 已加载关卡: {name}\n  描述: {desc}";
                }

            case "unloadlevel":
                state["level.current"] = string.Empty;
                return "✓ 已卸载当前关卡";

            default:
                return null;
        }
    }

    public static string GetHelpText()
    {
        return "loadlevel <关卡ID>\n  加载关卡\nlevels\n  查看可用关卡\nunloadlevel\n  卸载当前关卡";
    }

    public static string ListText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 可用关卡 ===");
        foreach (var lv in Levels.Values.OrderBy(x => x.Difficulty))
        {
            sb.AppendLine($"  • {lv.Id.PadRight(10)} - {lv.Name} (难度: {lv.Difficulty}) - {lv.Description}");
        }

        return sb.ToString();
    }

    public static string LoadLevel(string levelId)
    {
        if (!Levels.TryGetValue(levelId, out var level))
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(level);
    }

    public static string BuildCurrentLevelText(string levelJson)
    {
        var level = JsonSerializer.Deserialize<LevelEntry>(levelJson);
        if (level is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== 当前关卡 ===");
        sb.AppendLine($"关卡: {level.Name}");
        sb.AppendLine($"描述: {level.Description}");
        sb.AppendLine($"难度: {level.Difficulty}");

        if (Type.GetType("EonVientiane.PlayerModule.PlayerRuntime, EonVientiane.PlayerModule") != null)
        {
            sb.AppendLine("扩展: 已检测到 Player 模块联动");
        }

        return sb.ToString();
    }
}

public sealed record LevelEntry(string Id, string Name, string Description, int Difficulty);
