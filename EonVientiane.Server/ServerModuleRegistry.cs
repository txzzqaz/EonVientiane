using System.Text.Json;

namespace EonVientiane.Server;

internal static class ServerModuleRegistry
{
    public static IReadOnlyDictionary<string, ServerModuleDefinition> Load(string contentRootPath)
    {
        var workspaceRoot = FindWorkspaceRoot(contentRootPath);
        var manifestPaths = Directory.GetFiles(workspaceRoot, "eon-module.json", SearchOption.AllDirectories);
        var modules = new Dictionary<string, ServerModuleDefinition>(StringComparer.Ordinal);

        foreach (var manifestPath in manifestPaths)
        {
            var manifest = LoadManifest(manifestPath);
            var moduleId = manifest.ModuleId.Trim();
            if (modules.ContainsKey(moduleId))
            {
                throw new InvalidOperationException($"发现重复模块声明: {moduleId}");
            }

            var manifestDirectory = Path.GetDirectoryName(manifestPath)
                ?? throw new InvalidOperationException($"无法确定模块清单目录: {manifestPath}");
            var assemblyName = string.IsNullOrWhiteSpace(manifest.AssemblyName)
                ? new DirectoryInfo(manifestDirectory).Name
                : manifest.AssemblyName.Trim();
            var dllName = string.IsNullOrWhiteSpace(manifest.DllName)
                ? $"{assemblyName}.dll"
                : manifest.DllName.Trim();

            modules[moduleId] = new ServerModuleDefinition(
                moduleId,
                string.IsNullOrWhiteSpace(manifest.FileName) ? $"{moduleId}.json" : manifest.FileName.Trim(),
                ResolveModuleDllPath(manifestDirectory, assemblyName, dllName, moduleId),
                string.IsNullOrWhiteSpace(manifest.Version) ? "1.0.0" : manifest.Version.Trim(),
                manifest.IssueOnConnect);
        }

        return modules;
    }

    private static string FindWorkspaceRoot(string contentRootPath)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(contentRootPath)); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "EonVientiane.slnx"))
                || File.Exists(Path.Combine(current.FullName, "README.md")))
            {
                return current.FullName;
            }
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, ".."));
    }

    private static ServerModuleManifest LoadManifest(string manifestPath)
    {
        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ServerModuleManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (manifest is null || string.IsNullOrWhiteSpace(manifest.ModuleId))
        {
            throw new InvalidOperationException($"模块清单缺少 moduleId: {manifestPath}");
        }

        return manifest;
    }

    private static string ResolveModuleDllPath(string moduleRootPath, string assemblyName, string dllName, string moduleId)
    {
        foreach (var envKey in GetCandidateEnvironmentKeys(assemblyName, moduleId))
        {
            var fromEnv = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
            {
                return fromEnv;
            }
        }

        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var configurationRoot = Path.Combine(moduleRootPath, "bin", configuration);
            if (!Directory.Exists(configurationRoot))
            {
                continue;
            }

            var candidate = Directory.GetFiles(configurationRoot, dllName, SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"未找到模块DLL: {dllName} ({moduleId})");
    }

    private static IEnumerable<string> GetCandidateEnvironmentKeys(string assemblyName, string moduleId)
    {
        yield return $"EV_{NormalizeEnvironmentKey(assemblyName)}_DLL";
        yield return $"EV_{NormalizeEnvironmentKey(moduleId)}_DLL";
    }

    private static string NormalizeEnvironmentKey(string value)
    {
        return value
            .Replace('.', '_')
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('-', '_')
            .ToUpperInvariant();
    }
}

internal sealed record ServerModuleDefinition(
    string ModuleId,
    string FileName,
    string DllPath,
    string Version,
    bool IssueOnConnect);

internal sealed class ServerModuleManifest
{
    public string ModuleId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public string DllName { get; set; } = string.Empty;
    public bool IssueOnConnect { get; set; }
}