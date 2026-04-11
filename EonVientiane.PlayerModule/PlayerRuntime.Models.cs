namespace EonVientiane.PlayerModule;

public sealed partial class PlayerRuntime
{
    private sealed class AchievementVerifyRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string UserPublicKeyPem { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public List<string> ExistingAchievementIds { get; set; } = new();
        public List<string> ExistingModuleIds { get; set; } = new();
        public Dictionary<string, string> ModuleVersions { get; set; } = new();
        public List<ClientVerifiedBattleRecord> RecentBattleRecords { get; set; } = new();
    }

    private sealed class ClientVerifiedBattleRecord
    {
        public string UserId { get; set; } = string.Empty;
        public string BattleId { get; set; } = string.Empty;
        public string BattleRecordJson { get; set; } = string.Empty;
        public string RecordHash { get; set; } = string.Empty;
        public DateTime VerifiedAtUtc { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    private sealed class AchievementVerifyResponse
    {
        public List<string> IssuedFiles { get; set; } = new();
        public List<string> GrantedAchievementIds { get; set; } = new();
        public List<PackageManifestItemDto> Manifests { get; set; } = new();
    }

    private sealed class PackageManifestItemDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ModuleId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
        public long SizeBytes { get; set; }
        public string PackageBase64 { get; set; } = string.Empty;
    }

    private sealed class VerifyAchievementResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DownloadedCount { get; set; }
        public List<string> SyncedModuleIds { get; set; } = new();
        public List<string> GrantedAchievementIds { get; set; } = new();
    }

    private sealed class BattleVerifyRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string BattleRecordJson { get; set; } = string.Empty;
    }

    private sealed class BattleVerifyResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string BattleId { get; set; } = string.Empty;
        public string BattleRecordJson { get; set; } = string.Empty;
        public string RecordHash { get; set; } = string.Empty;
        public DateTime VerifiedAtUtc { get; set; }
        public string Signature { get; set; } = string.Empty;
    }
}