namespace EonVientiane.Item.Accessory.Self;

using System.Text.Json;

public static class SelfRuntime
{
    public static Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["itemId"] = "accessory.self",
            ["name"] = "自我",
            ["kind"] = "Accessory",
            ["author"] = "qaz",
            ["description"] = "这就是你自己",
            ["accessorySlotCost"] = 2,
        };
    }

    public static Dictionary<string, object> OnBattleStart(Dictionary<string, object> context)
    {
        var currentHp = ReadOwnerHp(context);
        var nextHp = currentHp + 10;

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["owner.HP"] = nextHp,
            ["message"] = "自我：在一切开始前，看见自己。获得 10 点 HP。",
        };
    }

    private static int ReadOwnerHp(Dictionary<string, object> context)
    {
        if (!context.TryGetValue("owner", out var ownerObj) || ownerObj is null)
        {
            return 0;
        }

        var owner = ToDictionary(ownerObj);
        if (!owner.TryGetValue("publicValues", out var publicValuesObj) || publicValuesObj is null)
        {
            return 0;
        }

        var publicValues = ToDictionary(publicValuesObj);
        return ReadInt(publicValues, "HP");
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

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var prop in jsonElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value;
            }

            return result;
        }

        return new Dictionary<string, object>(StringComparer.Ordinal);
    }

    private static int ReadInt(Dictionary<string, object> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            int i => i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
            JsonElement e when e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var i) => i,
            JsonElement e when e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var parsed) => parsed,
            _ when int.TryParse(value.ToString(), out var parsed) => parsed,
            _ => 0,
        };
    }
}