namespace EonVientiane.BattleModule;

using System.Reflection;

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
            string? winnerUnitId,
            bool isCompleted,
            PendingAttack? pendingAttack,
            List<string> log)
        {
            BattleId = battleId;
            Units = units;
            TurnOrder = turnOrder;
            CurrentActorId = currentActorId;
            TurnNumber = turnNumber;
            WinnerUnitId = winnerUnitId;
            IsCompleted = isCompleted;
            PendingAttack = pendingAttack;
            Log = log;
        }

        public string BattleId { get; }
        public Dictionary<string, BattleUnit> Units { get; }
        public List<string> TurnOrder { get; }
        public string CurrentActorId { get; set; }
        public int TurnNumber { get; set; }
        public string? WinnerUnitId { get; set; }
        public bool IsCompleted { get; set; }
        public PendingAttack? PendingAttack { get; set; }
        public List<string> Log { get; }

        public BattleUnit GetCurrentActor() => Units[CurrentActorId];
    }

    private sealed record BattleUnit(
        string UnitId,
        string DisplayName,
        Dictionary<string, int> PublicValues,
        List<BattleItemDescriptor> Loadout);

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

    private sealed record InventoryEquipmentSnapshot(string Id, string Name, string Slot, int ArmorValue, int AttackBonus)
    {
        public static InventoryEquipmentSnapshot? FromObject(object value)
        {
            var type = value.GetType();
            var id = type.GetProperty("Id")?.GetValue(value)?.ToString();
            var name = type.GetProperty("Name")?.GetValue(value)?.ToString();
            var slot = type.GetProperty("Slot")?.GetValue(value)?.ToString() ?? string.Empty;
            var armorValue = TryReadInt(type.GetProperty("ArmorValue")?.GetValue(value));
            var attackBonus = TryReadInt(type.GetProperty("AttackBonus")?.GetValue(value));

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return new InventoryEquipmentSnapshot(
                string.IsNullOrWhiteSpace(id) ? name : id,
                name,
                slot,
                armorValue,
                attackBonus);
        }

        private static int TryReadInt(object? value)
        {
            return value switch
            {
                int i => i,
                long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
                _ when int.TryParse(value?.ToString(), out var parsed) => parsed,
                _ => 0,
            };
        }
    }
}