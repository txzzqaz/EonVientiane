namespace EonVientiane.CLI;

using System.Collections.Concurrent;
using EonVientiane.Core.Models;
using EonVientiane.Core.Services;

public sealed class AutoLogicPackageRuntime : IDisposable
{
    private readonly LogicPackageService logicPackageService;
    private readonly User user;
    private readonly string userPrivateKeyPem;
    private readonly string serverPublicKeyFilePath;
    private readonly string userPackageDirectory;
    private readonly FileSystemWatcher watcher;
    private readonly ConcurrentDictionary<string, DateTime> processedFiles = new();

    public AutoLogicPackageRuntime(
        LogicPackageService logicPackageService,
        User user,
        string userPrivateKeyPem,
        string serverPublicKeyFilePath,
        string userPackageDirectory)
    {
        this.logicPackageService = logicPackageService;
        this.user = user;
        this.userPrivateKeyPem = userPrivateKeyPem;
        this.serverPublicKeyFilePath = serverPublicKeyFilePath;
        this.userPackageDirectory = userPackageDirectory;

        Directory.CreateDirectory(this.userPackageDirectory);

        watcher = new FileSystemWatcher(this.userPackageDirectory)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            Filter = "*.json",
            EnableRaisingEvents = false,
        };

        watcher.Created += (_, e) => TryLoadWithRetry(e.FullPath);
        watcher.Changed += (_, e) => TryLoadWithRetry(e.FullPath);
        watcher.Renamed += (_, e) => TryLoadWithRetry(e.FullPath);
    }

    public void Start()
    {
        LoadExistingPackages();
        watcher.EnableRaisingEvents = true;
    }

    public void ReloadExistingNow()
    {
        LoadExistingPackages();
    }

    private void LoadExistingPackages()
    {
        if (!File.Exists(serverPublicKeyFilePath))
        {
            return;
        }

        var files = Directory.GetFiles(userPackageDirectory, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            TryLoadPackage(file);
        }
    }

    private void TryLoadWithRetry(string filePath)
    {
        if (!File.Exists(filePath) || Path.GetExtension(filePath) != ".json")
        {
            return;
        }

        for (var i = 0; i < 5; i++)
        {
            try
            {
                if (TryLoadPackage(filePath))
                {
                    return;
                }
            }
            catch
            {
            }

            Thread.Sleep(120);
        }
    }

    private bool TryLoadPackage(string filePath)
    {
        if (!File.Exists(serverPublicKeyFilePath))
        {
            return false;
        }

        var writeTime = File.GetLastWriteTimeUtc(filePath);
        if (processedFiles.TryGetValue(filePath, out var prev) && prev >= writeTime)
        {
            return false;
        }

        var serverPublicKeyPem = File.ReadAllText(serverPublicKeyFilePath);
        var loaded = logicPackageService.LoadPackageFromFile(filePath, user, userPrivateKeyPem, serverPublicKeyPem);

        processedFiles[filePath] = writeTime;
        Console.WriteLine($"✓ 自动加载逻辑包: {loaded.ModuleId}@{loaded.Version} ({loaded.Kind})");
        return true;
    }

    public void Dispose()
    {
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
    }
}
