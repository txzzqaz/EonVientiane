namespace EonVientiane.Core.Models;

public enum LogicModuleKind
{
    Dll,
    Data
}

public class LogicPackageEnvelope
{
    public string ModuleId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public LogicModuleKind Kind { get; set; }
    public string TargetUserId { get; set; } = string.Empty;
    public string KeyAlgorithm { get; set; } = "RSA-OAEP-SHA256";
    public string ContentAlgorithm { get; set; } = "AES-GCM-256";
    public string HashAlgorithm { get; set; } = "SHA256";
    public string EncryptedAesKey { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string CipherText { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class LoadedLogicModule
{
    public string ModuleId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public LogicModuleKind Kind { get; set; }
    public DateTime LoadedAtUtc { get; set; }
}
