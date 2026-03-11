namespace EonVientiane.Item.Dice.D6;

using System.Text.Json;

public static class D6Runtime
{
    public static Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["itemId"] = "dice.d6",
            ["name"] = "D6",
            ["kind"] = "Dice",
            ["author"] = "qaz",
            ["description"] = "Reroll your destiny.",
            ["diceModes"] = new[] { "AD", "PD" },
        };
    }

    public static bool CanUseActive(Dictionary<string, object> context)
    {
        return true;
    }

    public static Dictionary<string, object> ExecuteActive(Dictionary<string, object> context)
    {
        var roll = RollD6();
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["attack"] = roll,
            ["message"] = $"D6 主动掷出 {roll}，本次 ATKP = {roll}",
        };
    }

    public static bool CanUsePassive(Dictionary<string, object> context)
    {
        return true;
    }

    public static Dictionary<string, object> ExecutePassive(Dictionary<string, object> context)
    {
        var attack = ReadInt(context, "pendingAttack");
        var defense = RollD6();
        var damage = Math.Max(0, attack - defense);

        if (damage == 0)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["resolvedDamage"] = 0,
                ["message"] = $"D6 被动掷出 {defense}，完全防御（ATKP {attack} <= DEFP {defense}）",
            };
        }

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["resolvedDamage"] = damage,
            ["message"] = $"D6 被动掷出 {defense}，受到 {damage} 点伤害（ATKP {attack} - DEFP {defense}）",
        };
    }

    private static int RollD6()
    {
        return Random.Shared.Next(1, 7);
    }

    private static int ReadInt(Dictionary<string, object> context, string key)
    {
        if (!context.TryGetValue(key, out var value) || value is null)
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