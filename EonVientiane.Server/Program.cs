using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using EonVientiane.Server;
using EonVientiane.Core.Models;
using EonVientiane.Core.Services;
using EonVientiane.AchievementModule;
using EonVientiane.AchievementConnectionModule;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

var logicStoreRoot = Environment.GetEnvironmentVariable("EV_LOGIC_STORE")
    ?? Path.Combine(app.Environment.ContentRootPath, "logic-store");
var publicKeyPath = Path.Combine(logicStoreRoot, "keys", "server_public.pem");
var privateKeyPath = Path.Combine(logicStoreRoot, "keys", "server_private.pem");
var serverModules = ServerModuleRegistry.Load(app.Environment.ContentRootPath);
var encryptionService = new EncryptionService();

Directory.CreateDirectory(logicStoreRoot);
Directory.CreateDirectory(Path.Combine(logicStoreRoot, "keys"));
var legacyPackagesDirectory = Path.Combine(logicStoreRoot, "packages");
if (Directory.Exists(legacyPackagesDirectory))
{
    Directory.Delete(legacyPackagesDirectory, recursive: true);
}

EnsureServerSigningKeys(encryptionService, publicKeyPath, privateKeyPath);
var serverPublicKeyPem = File.ReadAllText(publicKeyPath);
var serverPrivateKeyPem = File.ReadAllText(privateKeyPath);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/logic/public-key", () =>
{
    return Results.Text(serverPublicKeyPem, "text/plain");
});

app.MapPost("/api/logic/connect", (ConnectBootstrapRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.UserPublicKeyPem))
    {
        return Results.BadRequest(new { message = "userId and userPublicKeyPem are required" });
    }

    var existingAchievements = request.ExistingAchievementIds
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .ToHashSet(StringComparer.Ordinal);

    var existingModules = request.ExistingModuleIds
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .ToHashSet(StringComparer.Ordinal);

    var moduleVersions = request.ModuleVersions
        .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
        .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim(), StringComparer.Ordinal);

    var issuedFiles = new List<string>();
    var manifests = new List<PackageManifestItem>();
    foreach (var module in serverModules.Values.Where(x => x.IssueOnConnect))
    {
        IssueDllModuleIfNeeded(module);
    }

    return Results.Ok(new
    {
        issuedFiles,
        manifests,
    });

    void IssueDllModuleIfNeeded(ServerModuleDefinition module)
    {
        if (!ShouldIssueModule(module.ModuleId, module.Version))
        {
            return;
        }

        var dllBytes = File.ReadAllBytes(module.DllPath);
        var envelope = BuildSignedEncryptedEnvelope(
            encryptionService,
            serverPrivateKeyPem,
            request.UserPublicKeyPem,
            request.UserId,
            moduleId: module.ModuleId,
            version: module.Version,
            kind: LogicModuleKind.Dll,
            payloadBytes: dllBytes);

        AddPackage(module.FileName, envelope);
        issuedFiles.Add(module.FileName);
    }

    void AddPackage(string fileName, LogicPackageEnvelope envelope)
    {
        var packageJson = JsonSerializer.Serialize(envelope);
        manifests.Add(new PackageManifestItem
        {
            FileName = fileName,
            ModuleId = envelope.ModuleId,
            Version = envelope.Version,
            Kind = envelope.Kind.ToString(),
            TargetUserId = envelope.TargetUserId,
            UpdatedAtUtc = DateTime.UtcNow,
            SizeBytes = Encoding.UTF8.GetByteCount(packageJson),
            PackageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(packageJson)),
        });
    }

    bool ShouldIssueModule(string moduleId, string serverVersion)
    {
        if (!existingModules.Contains(moduleId))
        {
            return true;
        }

        if (!moduleVersions.TryGetValue(moduleId, out var localVersion))
        {
            return false;
        }

        return CompareModuleVersion(localVersion, serverVersion) < 0;
    }
});

app.MapPost("/api/logic/achievement/verify", (AchievementVerifyRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId) ||
        string.IsNullOrWhiteSpace(request.UserPublicKeyPem) ||
        string.IsNullOrWhiteSpace(request.Trigger))
    {
        return Results.BadRequest(new { message = "userId, userPublicKeyPem and trigger are required" });
    }

    var existingAchievements = request.ExistingAchievementIds
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .ToHashSet(StringComparer.Ordinal);

    var existingModules = request.ExistingModuleIds
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .ToHashSet(StringComparer.Ordinal);

    var moduleVersions = request.ModuleVersions
        .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
        .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim(), StringComparer.Ordinal);

    var userAchievementState = AchievementStateStore.Load(logicStoreRoot, request.UserId);
    var unlockedAchievementIds = userAchievementState.UnlockedAchievementIds
        .Concat(existingAchievements)
        .ToHashSet(StringComparer.Ordinal);

    var recentBattleRecords = ValidateRecentClientBattleRecords(request, serverPublicKeyPem, maxAcceptedCount: 20);

    var grantedAchievementIds = new List<string>();
    var issueModuleIds = new HashSet<string>(StringComparer.Ordinal);

    if (ConnectionAchievementRuntime.VerifyOnServer(request.Trigger, unlockedAchievementIds))
    {
        unlockedAchievementIds.Add(ConnectionAchievementRuntime.AchievementId);
        grantedAchievementIds.Add(ConnectionAchievementRuntime.AchievementId);
        foreach (var moduleId in ConnectionAchievementRuntime.GetModulesToIssueOnUnlock())
        {
            issueModuleIds.Add(moduleId);
        }
    }

    userAchievementState.UnlockedAchievementIds = unlockedAchievementIds.OrderBy(x => x, StringComparer.Ordinal).ToList();
    AchievementStateStore.Save(logicStoreRoot, request.UserId, userAchievementState);

    var manifests = new List<PackageManifestItem>();
    var issuedFiles = new List<string>();

    foreach (var moduleId in issueModuleIds)
    {
        if (!serverModules.TryGetValue(moduleId, out var module))
        {
            continue;
        }

        if (!ShouldIssueModule(module.ModuleId, module.Version))
        {
            continue;
        }

        var dllBytes = File.ReadAllBytes(module.DllPath);
        var envelope = BuildSignedEncryptedEnvelope(
            encryptionService,
            serverPrivateKeyPem,
            request.UserPublicKeyPem,
            request.UserId,
            moduleId: module.ModuleId,
            version: module.Version,
            kind: LogicModuleKind.Dll,
            payloadBytes: dllBytes);

        var packageJson = JsonSerializer.Serialize(envelope);
        manifests.Add(new PackageManifestItem
        {
            FileName = module.FileName,
            ModuleId = envelope.ModuleId,
            Version = envelope.Version,
            Kind = envelope.Kind.ToString(),
            TargetUserId = envelope.TargetUserId,
            UpdatedAtUtc = DateTime.UtcNow,
            SizeBytes = Encoding.UTF8.GetByteCount(packageJson),
            PackageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(packageJson)),
        });

        issuedFiles.Add(module.FileName);
    }

    return Results.Ok(new
    {
        issuedFiles,
        grantedAchievementIds,
        recentBattleRecordsAccepted = recentBattleRecords.Count,
        manifests,
    });

    bool ShouldIssueModule(string moduleId, string serverVersion)
    {
        if (!existingModules.Contains(moduleId))
        {
            return true;
        }

        if (!moduleVersions.TryGetValue(moduleId, out var localVersion))
        {
            return false;
        }

        return CompareModuleVersion(localVersion, serverVersion) < 0;
    }
});

app.MapPost("/api/logic/battle/verify", (BattleVerifyRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.BattleRecordJson))
    {
        return Results.BadRequest(new { message = "userId and battleRecordJson are required" });
    }

    JsonElement battleRecord;
    try
    {
        battleRecord = JsonSerializer.Deserialize<JsonElement>(request.BattleRecordJson);
    }
    catch
    {
        return Results.BadRequest(new { message = "battleRecordJson is invalid json" });
    }

    if (battleRecord.ValueKind != JsonValueKind.Object)
    {
        return Results.BadRequest(new { message = "battleRecordJson must be a json object" });
    }

    if (!battleRecord.TryGetProperty("battleId", out var battleIdElement) || string.IsNullOrWhiteSpace(battleIdElement.GetString()))
    {
        return Results.BadRequest(new { message = "battleId is required in battleRecordJson" });
    }

    if (!battleRecord.TryGetProperty("log", out var logElement) || logElement.ValueKind != JsonValueKind.Array || logElement.GetArrayLength() == 0)
    {
        return Results.BadRequest(new { message = "battle log is required in battleRecordJson" });
    }

    var now = DateTime.UtcNow;
    var battleId = battleIdElement.GetString()!.Trim();
    var battleHashBase64 = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.BattleRecordJson)));

    var responsePayload = new VerifiedBattleRecord
    {
        UserId = request.UserId.Trim(),
        BattleId = battleId,
        BattleRecordJson = request.BattleRecordJson,
        RecordHash = battleHashBase64,
        VerifiedAtUtc = now,
    };

    var signaturePayload = BuildBattleVerificationSignaturePayload(responsePayload);
    responsePayload.Signature = Convert.ToBase64String(encryptionService.SignData(signaturePayload, serverPrivateKeyPem));

    SaveVerifiedBattleRecord(logicStoreRoot, responsePayload);
    return Results.Ok(responsePayload);
});

app.Run();

static void EnsureServerSigningKeys(EncryptionService encryptionService, string publicKeyPath, string privateKeyPath)
{
    var publicExists = File.Exists(publicKeyPath);
    var privateExists = File.Exists(privateKeyPath);

    if (publicExists && privateExists)
    {
        return;
    }

    var (publicPem, privatePem) = encryptionService.GenerateRsaKeyPair();
    File.WriteAllText(publicKeyPath, publicPem);
    File.WriteAllText(privateKeyPath, privatePem);
}

static LogicPackageEnvelope BuildSignedEncryptedEnvelope(
    EncryptionService encryptionService,
    string serverPrivateKeyPem,
    string userPublicKeyPem,
    string targetUserId,
    string moduleId,
    string version,
    LogicModuleKind kind,
    byte[] payloadBytes)
{
    var plain = payloadBytes;

    var aesKey = RandomNumberGenerator.GetBytes(32);
    var nonce = RandomNumberGenerator.GetBytes(12);
    var cipher = new byte[plain.Length];
    var tag = new byte[16];
    using (var aes = new AesGcm(aesKey, 16))
    {
        aes.Encrypt(nonce, plain, cipher, tag);
    }

    var envelope = new LogicPackageEnvelope
    {
        ModuleId = moduleId,
        Version = version,
        Kind = kind,
        TargetUserId = targetUserId,
        KeyAlgorithm = "RSA-OAEP-SHA256",
        ContentAlgorithm = "AES-GCM-256",
        HashAlgorithm = "SHA256",
        EncryptedAesKey = Convert.ToBase64String(encryptionService.EncryptKeyWithPublicKey(aesKey, userPublicKeyPem)),
        Nonce = Convert.ToBase64String(nonce),
        CipherText = Convert.ToBase64String(cipher),
        Tag = Convert.ToBase64String(tag),
        ContentHash = Convert.ToBase64String(SHA256.HashData(plain)),
    };

    var signaturePayload = BuildSignaturePayload(envelope);
    envelope.Signature = Convert.ToBase64String(encryptionService.SignData(signaturePayload, serverPrivateKeyPem));
    return envelope;
}

static byte[] BuildSignaturePayload(LogicPackageEnvelope envelope)
{
    var payload = string.Join('\n',
        envelope.ModuleId,
        envelope.Version,
        envelope.Kind.ToString(),
        envelope.TargetUserId,
        envelope.KeyAlgorithm,
        envelope.ContentAlgorithm,
        envelope.HashAlgorithm,
        envelope.EncryptedAesKey,
        envelope.Nonce,
        envelope.CipherText,
        envelope.Tag,
        envelope.ContentHash);

    return Encoding.UTF8.GetBytes(payload);
}

static int CompareModuleVersion(string localVersion, string serverVersion)
{
    if (Version.TryParse(localVersion, out var local) && Version.TryParse(serverVersion, out var server))
    {
        return local.CompareTo(server);
    }

    return string.Compare(localVersion, serverVersion, StringComparison.Ordinal);
}

static byte[] BuildBattleVerificationSignaturePayload(VerifiedBattleRecord record)
{
    var payload = string.Join('\n',
        record.UserId,
        record.BattleId,
        record.RecordHash,
        record.VerifiedAtUtc.ToString("O"),
        record.BattleRecordJson);

    return Encoding.UTF8.GetBytes(payload);
}

static void SaveVerifiedBattleRecord(string logicStoreRoot, VerifiedBattleRecord record)
{
    var safeUserId = SanitizePathPart(record.UserId);
    var safeBattleId = SanitizePathPart(record.BattleId);
    var fileName = $"{safeBattleId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.signed.json";
    var directory = Path.Combine(logicStoreRoot, "battle-records", safeUserId);
    Directory.CreateDirectory(directory);

    var filePath = Path.Combine(directory, fileName);
    var json = JsonSerializer.Serialize(record, new JsonSerializerOptions
    {
        WriteIndented = true,
    });

    File.WriteAllText(filePath, json);
}

static string SanitizePathPart(string raw)
{
    var safe = string.Concat(raw.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
    return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
}

static List<ClientVerifiedBattleRecord> ValidateRecentClientBattleRecords(
    AchievementVerifyRequest request,
    string serverPublicKeyPem,
    int maxAcceptedCount)
{
    var validRecords = new List<ClientVerifiedBattleRecord>();
    if (request.RecentBattleRecords is null || request.RecentBattleRecords.Count == 0 || maxAcceptedCount <= 0)
    {
        return validRecords;
    }

    var expectedUserId = request.UserId.Trim();

    foreach (var record in request.RecentBattleRecords
                 .OrderByDescending(x => x.VerifiedAtUtc)
                 .Take(maxAcceptedCount))
    {
        if (!IsValidRecordShape(record))
        {
            continue;
        }

        if (!string.Equals(record.UserId.Trim(), expectedUserId, StringComparison.Ordinal))
        {
            continue;
        }

        var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(record.BattleRecordJson)));
        if (!string.Equals(expectedHash, record.RecordHash, StringComparison.Ordinal))
        {
            continue;
        }

        if (!TryParseBattleRecordJson(record.BattleRecordJson, out var battleRecord) ||
            battleRecord.ValueKind != JsonValueKind.Object ||
            !battleRecord.TryGetProperty("battleId", out var battleIdElement))
        {
            continue;
        }

        var battleId = battleIdElement.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(battleId) || !string.Equals(battleId, record.BattleId.Trim(), StringComparison.Ordinal))
        {
            continue;
        }

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(record.Signature);
        }
        catch
        {
            continue;
        }

        var signaturePayload = BuildBattleVerificationSignaturePayload(new VerifiedBattleRecord
        {
            UserId = record.UserId.Trim(),
            BattleId = record.BattleId.Trim(),
            BattleRecordJson = record.BattleRecordJson,
            RecordHash = record.RecordHash,
            VerifiedAtUtc = record.VerifiedAtUtc,
            Signature = record.Signature,
        });

        if (!VerifySignatureByServerPublicKey(signaturePayload, signatureBytes, serverPublicKeyPem))
        {
            continue;
        }

        validRecords.Add(record);
    }

    return validRecords;
}

static bool IsValidRecordShape(ClientVerifiedBattleRecord record)
{
    return record is not null
        && !string.IsNullOrWhiteSpace(record.UserId)
        && !string.IsNullOrWhiteSpace(record.BattleId)
        && !string.IsNullOrWhiteSpace(record.BattleRecordJson)
        && !string.IsNullOrWhiteSpace(record.RecordHash)
        && !string.IsNullOrWhiteSpace(record.Signature);
}

static bool TryParseBattleRecordJson(string json, out JsonElement battleRecord)
{
    try
    {
        battleRecord = JsonSerializer.Deserialize<JsonElement>(json);
        return true;
    }
    catch
    {
        battleRecord = default;
        return false;
    }
}

static bool VerifySignatureByServerPublicKey(byte[] data, byte[] signature, string serverPublicKeyPem)
{
    try
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(serverPublicKeyPem);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
    catch
    {
        return false;
    }
}

sealed class ConnectBootstrapRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserPublicKeyPem { get; set; } = string.Empty;
    public List<string> ExistingAchievementIds { get; set; } = new();
    public List<string> ExistingModuleIds { get; set; } = new();
    public Dictionary<string, string> ModuleVersions { get; set; } = new();
}

sealed class AchievementVerifyRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserPublicKeyPem { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public List<string> ExistingAchievementIds { get; set; } = new();
    public List<string> ExistingModuleIds { get; set; } = new();
    public Dictionary<string, string> ModuleVersions { get; set; } = new();
    public List<ClientVerifiedBattleRecord> RecentBattleRecords { get; set; } = new();
}

sealed class ClientVerifiedBattleRecord
{
    public string UserId { get; set; } = string.Empty;
    public string BattleId { get; set; } = string.Empty;
    public string BattleRecordJson { get; set; } = string.Empty;
    public string RecordHash { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; }
    public string Signature { get; set; } = string.Empty;
}

sealed class PackageManifestItem
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

sealed class BattleVerifyRequest
{
    public string UserId { get; set; } = string.Empty;
    public string BattleRecordJson { get; set; } = string.Empty;
}

sealed class VerifiedBattleRecord
{
    public string UserId { get; set; } = string.Empty;
    public string BattleId { get; set; } = string.Empty;
    public string BattleRecordJson { get; set; } = string.Empty;
    public string RecordHash { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; }
    public string Signature { get; set; } = string.Empty;
}

sealed class UserAchievementState
{
    public List<string> UnlockedAchievementIds { get; set; } = new();
}

static class AchievementStateStore
{
    public static UserAchievementState Load(string logicStoreRoot, string userId)
    {
        var path = GetUserAchievementStatePath(logicStoreRoot, userId);
        if (!File.Exists(path))
        {
            return new UserAchievementState();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UserAchievementState>(json) ?? new UserAchievementState();
        }
        catch
        {
            return new UserAchievementState();
        }
    }

    public static void Save(string logicStoreRoot, string userId, UserAchievementState state)
    {
        var path = GetUserAchievementStatePath(logicStoreRoot, userId);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        File.WriteAllText(path, json);
    }

    private static string GetUserAchievementStatePath(string logicStoreRoot, string userId)
    {
        var safeUserId = string.Concat(userId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrWhiteSpace(safeUserId))
        {
            safeUserId = "unknown";
        }

        return Path.Combine(logicStoreRoot, "achievement-state", $"{safeUserId}.json");
    }
}
