using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EonVientiane.PluginSystem;

/// <summary>
/// 插件管理器 - 负责加载、管理和卸载插件
/// </summary>
public class PluginManager
{
    private readonly List<IGamePlugin> _loadedPlugins = new();
    private readonly Dictionary<string, IGamePlugin> _pluginsByName = new();
    private readonly IGameContext _gameContext;
    private readonly string _pluginDirectory;
    
    /// <summary>
    /// 已加载的插件列表
    /// </summary>
    public IReadOnlyList<IGamePlugin> LoadedPlugins => _loadedPlugins;
    
    /// <summary>
    /// 插件加载成功事件
    /// </summary>
    public event Action<IGamePlugin> PluginLoaded;
    
    /// <summary>
    /// 插件卸载事件
    /// </summary>
    public event Action<IGamePlugin> PluginUnloaded;
    
    /// <summary>
    /// 插件加载失败事件
    /// </summary>
    public event Action<string, Exception> PluginLoadFailed;
    
    public PluginManager(IGameContext gameContext, string pluginDirectory = "Mods")
    {
        _gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        _pluginDirectory = pluginDirectory;
    }
    
    /// <summary>
    /// 加载所有插件
    /// </summary>
    public void LoadAllPlugins()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            Directory.CreateDirectory(_pluginDirectory);
            _gameContext.Log($"已创建插件目录: {_pluginDirectory}");
            return;
        }
        
        var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.AllDirectories);
        
        foreach (var dllFile in dllFiles)
        {
            try
            {
                LoadPluginFromFile(dllFile);
            }
            catch (Exception ex)
            {
                _gameContext.Log($"加载插件失败 {dllFile}: {ex.Message}");
                PluginLoadFailed?.Invoke(dllFile, ex);
            }
        }
        
        _gameContext.Log($"插件加载完成，共加载 {_loadedPlugins.Count} 个插件");
    }
    
    /// <summary>
    /// 从文件加载插件
    /// </summary>
    public void LoadPluginFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"插件文件不存在: {filePath}");
        }
        
        var assembly = Assembly.LoadFrom(filePath);
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IGamePlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        
        foreach (var type in pluginTypes)
        {
            try
            {
                var plugin = (IGamePlugin)Activator.CreateInstance(type);
                LoadPlugin(plugin);
            }
            catch (Exception ex)
            {
                _gameContext.Log($"实例化插件失败 {type.Name}: {ex.Message}");
                throw;
            }
        }
    }
    
    /// <summary>
    /// 加载插件实例
    /// </summary>
    public void LoadPlugin(IGamePlugin plugin)
    {
        if (plugin == null)
        {
            throw new ArgumentNullException(nameof(plugin));
        }
        
        if (_pluginsByName.ContainsKey(plugin.Name))
        {
            throw new InvalidOperationException($"插件已存在: {plugin.Name}");
        }
        
        try
        {
            plugin.Initialize(_gameContext);
            _loadedPlugins.Add(plugin);
            _pluginsByName[plugin.Name] = plugin;
            
            _gameContext.Log($"插件加载成功: {plugin.Name} v{plugin.Version} by {plugin.Author}");
            PluginLoaded?.Invoke(plugin);
        }
        catch (Exception ex)
        {
            _gameContext.Log($"插件初始化失败 {plugin.Name}: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 卸载插件
    /// </summary>
    public void UnloadPlugin(string pluginName)
    {
        if (!_pluginsByName.TryGetValue(pluginName, out var plugin))
        {
            return;
        }
        
        try
        {
            plugin.Shutdown();
            _loadedPlugins.Remove(plugin);
            _pluginsByName.Remove(pluginName);
            
            _gameContext.Log($"插件卸载成功: {pluginName}");
            PluginUnloaded?.Invoke(plugin);
        }
        catch (Exception ex)
        {
            _gameContext.Log($"插件卸载失败 {pluginName}: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public void UnloadAllPlugins()
    {
        var pluginNames = _pluginsByName.Keys.ToList();
        foreach (var name in pluginNames)
        {
            try
            {
                UnloadPlugin(name);
            }
            catch (Exception ex)
            {
                _gameContext.Log($"卸载插件时发生错误 {name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 获取插件
    /// </summary>
    public T GetPlugin<T>(string pluginName) where T : IGamePlugin
    {
        if (_pluginsByName.TryGetValue(pluginName, out var plugin) && plugin is T typedPlugin)
        {
            return typedPlugin;
        }
        return default;
    }
    
    /// <summary>
    /// 获取所有指定类型的插件
    /// </summary>
    public List<T> GetPlugins<T>() where T : IGamePlugin
    {
        return _loadedPlugins.OfType<T>().ToList();
    }
    
    /// <summary>
    /// 更新所有插件
    /// </summary>
    public void UpdatePlugins(float deltaTime)
    {
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                plugin.Update(deltaTime);
            }
            catch (Exception ex)
            {
                _gameContext.Log($"插件更新错误 {plugin.Name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 检查插件是否已加载
    /// </summary>
    public bool IsPluginLoaded(string pluginName)
    {
        return _pluginsByName.ContainsKey(pluginName);
    }
    
    /// <summary>
    /// 重新加载插件
    /// </summary>
    public void ReloadPlugin(string pluginName)
    {
        if (!_pluginsByName.TryGetValue(pluginName, out var oldPlugin))
        {
            throw new InvalidOperationException($"插件不存在: {pluginName}");
        }
        
        // 获取插件类型信息
        var pluginType = oldPlugin.GetType();
        
        // 卸载旧插件
        UnloadPlugin(pluginName);
        
        // 重新创建并加载
        var newPlugin = (IGamePlugin)Activator.CreateInstance(pluginType);
        LoadPlugin(newPlugin);
    }
}
