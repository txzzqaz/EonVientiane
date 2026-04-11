namespace EonVientiane.LevelModule;

using System.Reflection;
using System.Text;
using System.Text.Json;

public static class LevelApi
{
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

                var formation = args.Length > 1 ? args[1] : "1v1";

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
                    var battleResult = InvokeOptional(
                        "EonVientiane.BattleModule",
                        "EonVientiane.BattleModule.BattleApi",
                        "StartSession",
                        state,
                        "level",
                        formation) as string;

                    if (string.IsNullOrWhiteSpace(battleResult))
                    {
                        return $"✓ 已加载关卡: {name}\n  描述: {desc}\n⚠ 战斗模块不可用，尚未进入战斗。";
                    }

                    return $"✓ 已加载关卡: {name}\n  描述: {desc}\n{battleResult}";
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
        return "loadlevel <关卡ID> [阵型]\n  加载关卡并进入战斗（默认 1v1，可用 2v2/3v3v3/1v2）\nlevels\n  查看可用关卡\nunloadlevel\n  卸载当前关卡";
    }

    public static string ListText()
    {
        var levels = DiscoverLevels();
        var sb = new StringBuilder();
        sb.AppendLine("=== 可用关卡 ===");
        if (levels.Count == 0)
        {
            sb.AppendLine("(暂无可用关卡模块)");
            return sb.ToString();
        }

        foreach (var lv in levels.Values.OrderBy(x => x.Difficulty).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"  • {lv.Id.PadRight(10)} - {lv.Name} (难度: {lv.Difficulty}) - {lv.Description}");
        }

        return sb.ToString();
    }

    public static string LoadLevel(string levelId)
    {
        var levels = DiscoverLevels();
        if (!levels.TryGetValue(levelId, out var level))
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
        sb.AppendLine($"关卡模块: {level.AssemblyName}");

        if (Type.GetType("EonVientiane.PlayerModule.PlayerRuntime, EonVientiane.PlayerModule") != null)
        {
            sb.AppendLine("扩展: 已检测到 Player 模块联动");
        }

        return sb.ToString();
    }

    private static Dictionary<string, LevelEntry> DiscoverLevels()
    {
        var levels = new Dictionary<string, LevelEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.StartsWith("EonVientiane.Level.", StringComparison.Ordinal))
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(x => x is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type is null || !type.IsClass || !type.IsAbstract)
                {
                    continue;
                }

                TryCollectLevelsFromMethod(type, "GetLevelMetadata", levels);
                TryCollectLevelsFromMethod(type, "GetLevel", levels);
                TryCollectLevelsFromMethod(type, "GetLevels", levels);
            }
        }

        return levels;
    }

    private static void TryCollectLevelsFromMethod(Type type, string methodName, IDictionary<string, LevelEntry> levels)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method is null || method.GetParameters().Length != 0)
        {
            return;
        }

        var value = method.Invoke(null, null);
        foreach (var level in ExpandLevels(value, type))
        {
            if (!string.IsNullOrWhiteSpace(level.Id))
            {
                levels[level.Id] = level;
            }
        }
    }

    private static IEnumerable<LevelEntry> ExpandLevels(object? value, Type sourceType)
    {
        if (value is null)
        {
            yield break;
        }

        if (TryParseLevel(value, sourceType, out var singleLevel))
        {
            yield return singleLevel;
            yield break;
        }

        if (value is System.Collections.IEnumerable sequence and not string)
        {
            foreach (var item in sequence)
            {
                if (TryParseLevel(item, sourceType, out var level))
                {
                    yield return level;
                }
            }
        }
    }

    private static bool TryParseLevel(object? value, Type sourceType, out LevelEntry level)
    {
        level = default!;

        if (value is null)
        {
            return false;
        }

        if (value is LevelEntry typed)
        {
            level = typed;
            return true;
        }

        var map = ToDictionary(value);
        if (map.Count == 0)
        {
            return false;
        }

        var id = GetString(map, "Id");
        var name = GetString(map, "Name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        level = new LevelEntry(
            id.Trim(),
            name.Trim(),
            GetString(map, "Description"),
            GetInt(map, "Difficulty", 1),
            sourceType.Assembly.GetName().Name ?? string.Empty,
            sourceType.FullName ?? sourceType.Name);
        return true;
    }

    private static Dictionary<string, object> ToDictionary(object value)
    {
        if (value is Dictionary<string, object> dict)
        {
            return new Dictionary<string, object>(dict, StringComparer.Ordinal);
        }

        if (value is IDictionary<string, object?> dictNullable)
        {
            return dictNullable.ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.Ordinal);
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = property.Value;
            }

            return result;
        }

        return new Dictionary<string, object>(StringComparer.Ordinal);
    }

    private static string GetString(Dictionary<string, object> map, string key, string defaultValue = "")
    {
        return map.TryGetValue(key, out var value)
            ? value?.ToString() ?? defaultValue
            : defaultValue;
    }

    private static int GetInt(Dictionary<string, object> map, string key, int defaultValue)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return value switch
        {
            int i => i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
            JsonElement e when e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var i) => i,
            JsonElement e when e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var parsed) => parsed,
            _ when int.TryParse(value.ToString(), out var parsed) => parsed,
            _ => defaultValue,
        };
    }

    private static object? InvokeOptional(string assemblyName, string typeName, string methodName, params object[] args)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (type is null)
        {
            return null;
        }

        var methods = type.GetMethods().Where(m => m.Name == methodName).ToList();
        var target = methods.FirstOrDefault(m => m.GetParameters().Length == args.Length) ?? methods.FirstOrDefault();
        if (target is null)
        {
            return null;
        }

        return target.Invoke(null, args);
    }

}

public sealed record LevelEntry(string Id, string Name, string Description, int Difficulty, string AssemblyName, string RuntimeType);
