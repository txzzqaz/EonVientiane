using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using EonVientiane.Core.Models;
using EonVientiane.Core.Services;
using EonVientiane.AchievementModule;
using EonVientiane.AchievementConnectionModule;
using EonVientiane.AchievementStatusModule;

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
var equipmentModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.EquipmentModule", "EonVientiane.EquipmentModule.dll");
var inventoryModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.InventoryModule", "EonVientiane.InventoryModule.dll");
var levelModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.LevelModule", "EonVientiane.LevelModule.dll");
var effectModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.EffectModule", "EonVientiane.EffectModule.dll");
var battleModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.BattleModule", "EonVientiane.BattleModule.dll");
var networkBattleModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.NetworkBattleModule", "EonVientiane.NetworkBattleModule.dll");
var firstLevelModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVinetiane.Levels/EonVientiane.Level.First", "EonVientiane.Level.First.dll");
var selfAccessoryModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.Items/EonVientiane.Item.Accessory.Self", "EonVientiane.Item.Accessory.Self.dll");
var d6ModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.Items/EonVientiane.Item.Dice.D6", "EonVientiane.Item.Dice.D6.dll");
var playerModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.PlayerModule", "EonVientiane.PlayerModule.dll");
var achievementModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.AchievementModule", "EonVientiane.AchievementModule.dll");
var achievementConnectionModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.AchievementConnectionModule", "EonVientiane.AchievementConnectionModule.dll");
var achievementStatusModuleDllPath = ResolveModuleDllPath(app.Environment.ContentRootPath, "EonVientiane.AchievementStatusModule", "EonVientiane.AchievementStatusModule.dll");
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
    const string playerVersion = "1.0.0";
    const string equipmentVersion = "1.0.0";
    const string inventoryVersion = "1.0.0";
    const string levelVersion = "1.0.0";
    const string effectVersion = "1.0.0";
    const string battleVersion = "1.0.0";
    const string networkBattleVersion = "1.0.0";
    const string firstLevelVersion = "1.0.0";
    const string selfAccessoryVersion = "1.0.0";
    const string d6Version = "1.0.0";
    const string achievementSystemVersion = "1.0.0";
    const string firstAchievementVersion = "1.0.0";
    const string statusAchievementVersion = "1.0.0";

    IssueDllModuleIfNeeded("module.player.core", "module.player.core.json", playerModuleDllPath, playerVersion);
    IssueDllModuleIfNeeded("module.equipment.core", "module.equipment.core.json", equipmentModuleDllPath, equipmentVersion);
    IssueDllModuleIfNeeded("module.inventory.core", "module.inventory.core.json", inventoryModuleDllPath, inventoryVersion);
    IssueDllModuleIfNeeded("module.level.core", "module.level.core.json", levelModuleDllPath, levelVersion);
    IssueDllModuleIfNeeded("module.effect.core", "module.effect.core.json", effectModuleDllPath, effectVersion);
    IssueDllModuleIfNeeded("module.battle.core", "module.battle.core.json", battleModuleDllPath, battleVersion);
    IssueDllModuleIfNeeded("module.network-battle.core", "module.network-battle.core.json", networkBattleModuleDllPath, networkBattleVersion);
    IssueDllModuleIfNeeded("module.level.first", "module.level.first.json", firstLevelModuleDllPath, firstLevelVersion);
    IssueDllModuleIfNeeded("module.item.accessory.self", "module.item.accessory.self.json", selfAccessoryModuleDllPath, selfAccessoryVersion);
    IssueDllModuleIfNeeded("module.item.dice.d6", "module.item.dice.d6.json", d6ModuleDllPath, d6Version);
    IssueDllModuleIfNeeded("module.achievement.core", "module.achievement.core.json", achievementModuleDllPath, achievementSystemVersion);
    IssueDllModuleIfNeeded("module.achievement.connection", "module.achievement.connection.json", achievementConnectionModuleDllPath, firstAchievementVersion);
    IssueDllModuleIfNeeded("module.achievement.status", "module.achievement.status.json", achievementStatusModuleDllPath, statusAchievementVersion);

    return Results.Ok(new
    {
        issuedFiles,
        manifests,
    });

    void IssueDllModuleIfNeeded(string moduleId, string fileName, string dllPath, string serverVersion)
    {
        if (!ShouldIssueModule(moduleId, serverVersion))
        {
            return;
        }

        var dllBytes = File.ReadAllBytes(dllPath);
        var envelope = BuildSignedEncryptedEnvelope(
            encryptionService,
            serverPrivateKeyPem,
            request.UserPublicKeyPem,
            request.UserId,
            moduleId: moduleId,
            version: serverVersion,
            kind: LogicModuleKind.Dll,
            payloadBytes: dllBytes);

        AddPackage(fileName, envelope);
        issuedFiles.Add(fileName);
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

    if (StatusFirstAchievementRuntime.VerifyOnServer(request.Trigger, unlockedAchievementIds))
    {
        unlockedAchievementIds.Add(StatusFirstAchievementRuntime.AchievementId);
        grantedAchievementIds.Add(StatusFirstAchievementRuntime.AchievementId);
        foreach (var moduleId in StatusFirstAchievementRuntime.GetModulesToIssueOnUnlock())
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
        if (!TryGetIssueModuleInfo(moduleId, out var fileName, out var dllPath, out var version))
        {
            continue;
        }

        if (!ShouldIssueModule(moduleId, version))
        {
            continue;
        }

        var dllBytes = File.ReadAllBytes(dllPath);
        var envelope = BuildSignedEncryptedEnvelope(
            encryptionService,
            serverPrivateKeyPem,
            request.UserPublicKeyPem,
            request.UserId,
            moduleId: moduleId,
            version: version,
            kind: LogicModuleKind.Dll,
            payloadBytes: dllBytes);

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

        issuedFiles.Add(fileName);
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

    bool TryGetIssueModuleInfo(string moduleId, out string fileName, out string dllPath, out string version)
    {
        switch (moduleId)
        {
            case "module.player.core":
                fileName = "module.player.core.json";
                dllPath = playerModuleDllPath;
                version = "1.0.0";
                return true;
            case "module.effect.core":
                fileName = "module.effect.core.json";
                dllPath = effectModuleDllPath;
                version = "1.0.0";
                return true;
            case "module.battle.core":
                fileName = "module.battle.core.json";
                dllPath = battleModuleDllPath;
                version = "1.0.0";
                return true;
            case "module.item.dice.d6":
                fileName = "module.item.dice.d6.json";
                dllPath = d6ModuleDllPath;
                version = "1.0.0";
                return true;
            case "module.item.accessory.self":
                fileName = "module.item.accessory.self.json";
                dllPath = selfAccessoryModuleDllPath;
                version = "1.0.0";
                return true;
            case "module.achievement.core":
                fileName = "module.achievement.core.json";
                dllPath = achievementModuleDllPath;
                version = "1.0.0";
                return true;
            case "module.achievement.connection":
                fileName = "module.achievement.connection.json";
                dllPath = achievementConnectionModuleDllPath;
                version = "1.0.0";
                return true;
            case "module.achievement.status":
                fileName = "module.achievement.status.json";
                dllPath = achievementStatusModuleDllPath;
                version = "1.0.0";
                return true;
            default:
                fileName = string.Empty;
                dllPath = string.Empty;
                version = string.Empty;
                return false;
        }
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

static string ResolveModuleDllPath(string contentRootPath, string projectName, string dllName)
{
    var envProjectName = projectName
        .Replace('.', '_')
        .Replace('/', '_')
        .Replace('\\', '_');
    var envKey = $"EV_{envProjectName.ToUpperInvariant()}_DLL";
    var fromEnv = Environment.GetEnvironmentVariable(envKey);
    if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
    {
        return fromEnv;
    }

    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(contentRootPath, "..", projectName, "bin", "Debug", "net10.0", dllName)),
        Path.GetFullPath(Path.Combine(contentRootPath, "..", projectName, "bin", "Release", "net10.0", dllName)),
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new FileNotFoundException($"未找到模块DLL: {dllName}");
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
