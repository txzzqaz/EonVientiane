namespace EonVientiane.BattleModule;

using System.Collections;
using System.Reflection;
using System.Text.Json;

public static partial class BattleApi
{
    private static object? InvokeOptional(Type runtimeType, string methodName, params object?[] args)
    {
        var methods = runtimeType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.Name == methodName)
            .ToList();
        if (methods.Count == 0)
        {
            return null;
        }

        var method = methods.FirstOrDefault(x => x.GetParameters().Length == args.Length) ?? methods[0];
        return method.Invoke(null, args);
    }

    private static object? InvokeOptional(string assemblyName, string typeName, string methodName, params object?[] args)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (type is null)
        {
            return null;
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.Name == methodName)
            .ToList();
        if (methods.Count == 0)
        {
            return null;
        }

        var method = methods.FirstOrDefault(x => x.GetParameters().Length == args.Length) ?? methods[0];
        return method.Invoke(null, args);
    }

    private static bool TryParseJsonObject(string text, out Dictionary<string, object?> dict)
    {
        dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            dict = ToDictionary(doc.RootElement);
            return dict.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, object?> ToDictionary(object? value)
    {
        if (value is null)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        if (value is Dictionary<string, object?> typedNullable)
        {
            return new Dictionary<string, object?>(typedNullable, StringComparer.OrdinalIgnoreCase);
        }

        if (value is Dictionary<string, object> typed)
        {
            return typed.ToDictionary(x => x.Key, x => (object?)x.Value, StringComparer.OrdinalIgnoreCase);
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.Object
                ? jsonElement.EnumerateObject().ToDictionary(x => x.Name, x => ConvertJsonValue(x.Value), StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        if (value is IDictionary dictionary)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is null)
                {
                    continue;
                }

                result[entry.Key.ToString() ?? string.Empty] = entry.Value;
            }

            return result;
        }

        return value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead)
            .ToDictionary(x => x.Name, x => x.GetValue(value), StringComparer.OrdinalIgnoreCase);
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => ToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            _ => null,
        };
    }

    private static bool TryGetString(Dictionary<string, object?> dict, string key, out string value)
    {
        if (dict.TryGetValue(key, out var obj) && obj is not null)
        {
            value = obj.ToString() ?? string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string GetStringOrDefault(Dictionary<string, object?> dict, string key, string fallback)
    {
        return TryGetString(dict, key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static bool TryGetInt(Dictionary<string, object?> dict, string key, out int value)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
        {
            value = 0;
            return false;
        }

        switch (obj)
        {
            case int i:
                value = i;
                return true;
            case long l when l <= int.MaxValue && l >= int.MinValue:
                value = (int)l;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var jsonValue):
                value = jsonValue;
                return true;
            default:
                return int.TryParse(obj.ToString(), out value);
        }
    }

    private static List<string> GetStringList(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
        {
            return new List<string>();
        }

        return obj switch
        {
            string s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            IEnumerable<string> list => list.ToList(),
            IEnumerable enumerable => enumerable.Cast<object?>().Where(x => x is not null).Select(x => x!.ToString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            _ => new List<string>(),
        };
    }
}