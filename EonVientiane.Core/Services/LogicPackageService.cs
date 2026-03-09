namespace EonVientiane.Core.Services;

using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EonVientiane.Core.Models;

public class LogicPackageService
{
    private readonly EncryptionService encryptionService;
    private ModuleLoadContext? dllContext;
    private readonly Dictionary<string, byte[]> loadedDllModuleBytes = new();
    private readonly Dictionary<string, Assembly> loadedAssemblies = new();
    private readonly Dictionary<string, byte[]> loadedDataModules = new();
    private readonly Dictionary<string, LoadedLogicModule> loadedModuleStates = new();

    public LogicPackageService(EncryptionService encryptionService)
    {
        this.encryptionService = encryptionService;
    }

    public LoadedLogicModule LoadPackageFromFile(
        string packageFilePath,
        User user,
        string userPrivateKeyPem,
        string serverPublicKeyPem)
    {
        var packageJson = File.ReadAllText(packageFilePath);
        return LoadPackage(packageJson, user, userPrivateKeyPem, serverPublicKeyPem);
    }

    public LoadedLogicModule LoadPackage(
        string packageJson,
        User user,
        string userPrivateKeyPem,
        string serverPublicKeyPem)
    {
        var envelope = JsonSerializer.Deserialize<LogicPackageEnvelope>(packageJson)
            ?? throw new InvalidOperationException("逻辑包格式无效");

        ValidateEnvelope(envelope, user);
        VerifyPackageSignature(envelope, serverPublicKeyPem);

        var encryptedAesKey = Convert.FromBase64String(envelope.EncryptedAesKey);
        var aesKey = encryptionService.DecryptKeyWithPrivateKey(encryptedAesKey, userPrivateKeyPem);

        var content = DecryptContent(
            aesKey,
            Convert.FromBase64String(envelope.Nonce),
            Convert.FromBase64String(envelope.CipherText),
            Convert.FromBase64String(envelope.Tag));

        var contentHash = Convert.ToBase64String(SHA256.HashData(content));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(contentHash),
                Encoding.UTF8.GetBytes(envelope.ContentHash)))
        {
            throw new InvalidOperationException("逻辑包内容哈希校验失败");
        }

        HotReloadModule(envelope, content);

        var loaded = new LoadedLogicModule
        {
            ModuleId = envelope.ModuleId,
            Version = envelope.Version,
            Kind = envelope.Kind,
            LoadedAtUtc = DateTime.UtcNow,
        };

        loadedModuleStates[envelope.ModuleId] = loaded;
        return loaded;
    }

    public IReadOnlyCollection<LoadedLogicModule> GetLoadedModules()
    {
        return loadedModuleStates.Values.ToList().AsReadOnly();
    }

    public void UnloadModule(string moduleId)
    {
        if (loadedDllModuleBytes.Remove(moduleId))
        {
            RebuildDllRuntime();
        }

        loadedDataModules.Remove(moduleId);
        loadedModuleStates.Remove(moduleId);
    }

    public void UnloadAllModules()
    {
        loadedDllModuleBytes.Clear();
        loadedDataModules.Clear();
        loadedModuleStates.Clear();
        loadedAssemblies.Clear();

        if (dllContext != null)
        {
            dllContext.Unload();
            dllContext = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public Assembly? GetLoadedAssembly(string moduleId)
    {
        return loadedAssemblies.GetValueOrDefault(moduleId);
    }

    public byte[]? GetLoadedDataModule(string moduleId)
    {
        return loadedDataModules.GetValueOrDefault(moduleId);
    }

    private void ValidateEnvelope(LogicPackageEnvelope envelope, User user)
    {
        if (string.IsNullOrWhiteSpace(envelope.ModuleId) ||
            string.IsNullOrWhiteSpace(envelope.Version) ||
            string.IsNullOrWhiteSpace(envelope.TargetUserId) ||
            string.IsNullOrWhiteSpace(envelope.EncryptedAesKey) ||
            string.IsNullOrWhiteSpace(envelope.Nonce) ||
            string.IsNullOrWhiteSpace(envelope.CipherText) ||
            string.IsNullOrWhiteSpace(envelope.Tag) ||
            string.IsNullOrWhiteSpace(envelope.ContentHash) ||
            string.IsNullOrWhiteSpace(envelope.Signature))
        {
            throw new InvalidOperationException("逻辑包字段不完整");
        }

        if (!string.Equals(envelope.TargetUserId, user.UserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("逻辑包与当前用户不匹配");
        }

        if (!string.Equals(envelope.KeyAlgorithm, "RSA-OAEP-SHA256", StringComparison.Ordinal) ||
            !string.Equals(envelope.ContentAlgorithm, "AES-GCM-256", StringComparison.Ordinal) ||
            !string.Equals(envelope.HashAlgorithm, "SHA256", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("逻辑包算法标识不受支持");
        }
    }

    private void VerifyPackageSignature(LogicPackageEnvelope envelope, string serverPublicKeyPem)
    {
        var signedData = BuildSignaturePayload(envelope);
        var signature = Convert.FromBase64String(envelope.Signature);

        var ok = encryptionService.VerifySignature(signedData, signature, serverPublicKeyPem);
        if (!ok)
        {
            throw new InvalidOperationException("逻辑包签名校验失败");
        }
    }

    private static byte[] BuildSignaturePayload(LogicPackageEnvelope envelope)
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

    private static byte[] DecryptContent(byte[] aesKey, byte[] nonce, byte[] cipher, byte[] tag)
    {
        var plain = new byte[cipher.Length];
        using var aesGcm = new AesGcm(aesKey, 16);
        aesGcm.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    private void HotReloadModule(LogicPackageEnvelope envelope, byte[] content)
    {
        if (envelope.Kind == LogicModuleKind.Data)
        {
            loadedDataModules[envelope.ModuleId] = content;
            return;
        }

        loadedDllModuleBytes[envelope.ModuleId] = content;
        RebuildDllRuntime();
    }

    private void RebuildDllRuntime()
    {
        if (dllContext != null)
        {
            dllContext.Unload();
            dllContext = null;
            loadedAssemblies.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (loadedDllModuleBytes.Count == 0)
        {
            return;
        }

        dllContext = new ModuleLoadContext();
        var pending = loadedDllModuleBytes
            .Select(x => new PendingDll(x.Key, x.Value))
            .ToList();

        var loadedAnyInPass = true;
        while (pending.Count > 0 && loadedAnyInPass)
        {
            loadedAnyInPass = false;

            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var item = pending[i];
                try
                {
                    using var stream = new MemoryStream(item.Bytes, writable: false);
                    var assembly = dllContext.LoadFromStream(stream);
                    loadedAssemblies[item.ModuleId] = assembly;
                    pending.RemoveAt(i);
                    loadedAnyInPass = true;
                }
                catch
                {
                }
            }
        }

        if (pending.Count > 0)
        {
            var unresolved = string.Join(", ", pending.Select(x => x.ModuleId));
            throw new InvalidOperationException($"模块依赖解析失败: {unresolved}");
        }
    }

    private sealed record PendingDll(string ModuleId, byte[] Bytes);

    private sealed class ModuleLoadContext : AssemblyLoadContext
    {
        public ModuleLoadContext() : base(isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
