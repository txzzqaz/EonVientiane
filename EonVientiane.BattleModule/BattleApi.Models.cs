namespace EonVientiane.BattleModule;

using System.Collections;
using System.Reflection;
using System.Text.Json;

public static partial class BattleApi
{
    private sealed class BattleSession
    {
        public BattleSession(
            string battleId,
            Dictionary<string, BattleUnit> units,
            List<string> turnOrder,
            string currentActorId,
            int turnNumber,
            DateTime battleStartedAtUtc,
            DateTime turnStartedAtUtc,
            string? winnerUnitId,
            bool isCompleted,
            PendingAttack? pendingAttack,
            List<string> log,
            string battleMode)
        {
            BattleId = battleId;
            Units = units;
            TurnOrder = turnOrder;
            CurrentActorId = currentActorId;
            TurnNumber = turnNumber;
            BattleStartedAtUtc = battleStartedAtUtc;
            TurnStartedAtUtc = turnStartedAtUtc;
            WinnerUnitId = winnerUnitId;
            IsCompleted = isCompleted;
            PendingAttack = pendingAttack;
            Log = log;
            BattleMode = battleMode;
        }

        public string BattleId { get; }
        public Dictionary<string, BattleUnit> Units { get; }
        public List<string> TurnOrder { get; }
        public string CurrentActorId { get; set; }
        public int TurnNumber { get; set; }
        public DateTime BattleStartedAtUtc { get; }
        public DateTime TurnStartedAtUtc { get; set; }
        public string? WinnerUnitId { get; set; }
        public bool IsCompleted { get; set; }
        public PendingAttack? PendingAttack { get; set; }
        public List<string> Log { get; }
        public string BattleMode { get; }
        public string? WinnerSideId { get; set; }

        public BattleUnit GetCurrentActor() => Units[CurrentActorId];
    }

    private sealed record BattleUnit(
        string UnitId,
        string DisplayName,
        string SideId,
        string SideName,
        Dictionary<string, int> PublicValues,
        List<BattleItemDescriptor> Loadout,
        bool IsLocalControlled,
        bool IsLoadoutVisible,
        Type? ControllerRuntimeType);

    private sealed record BattleItemDescriptor(
        string ItemId,
        string DisplayName,
        string Kind,
        bool IsAccessory,
        bool IsDice,
        bool SupportsActive,
        bool SupportsPassive,
        Type RuntimeType);

    private sealed record PendingAttack(string SourceUnitId, string TargetUnitId, int AttackValue);

    private sealed record InventoryEquipmentSnapshot(string Id, string Name, string Slot)
    {
        public static InventoryEquipmentSnapshot? FromObject(object value)
        {
            object? idObj;
            object? nameObj;
            object? slotObj;

            if (value is IDictionary map)
            {
                idObj = TryGetMapValue(map, "Id");
                nameObj = TryGetMapValue(map, "Name");
                slotObj = TryGetMapValue(map, "Slot");
            }
            else
            {
                var type = value.GetType();
                idObj = type.GetProperty("Id")?.GetValue(value);
                nameObj = type.GetProperty("Name")?.GetValue(value);
                slotObj = type.GetProperty("Slot")?.GetValue(value);
            }

            var id = TryReadString(idObj);
            var name = TryReadString(nameObj);
            var slot = TryReadString(slotObj) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return new InventoryEquipmentSnapshot(
                string.IsNullOrWhiteSpace(id) ? name : id,
                name,
                slot);
        }

        private static object? TryGetMapValue(IDictionary map, string key)
        {
            foreach (DictionaryEntry entry in map)
            {
                if (entry.Key?.ToString()?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return entry.Value;
                }
            }

            return null;
        }

        private static string? TryReadString(object? value)
        {
            return value switch
            {
                JsonElement e when e.ValueKind == JsonValueKind.String => e.GetString(),
                JsonElement e => e.ToString(),
                _ => value?.ToString(),
            };
        }

    }
}