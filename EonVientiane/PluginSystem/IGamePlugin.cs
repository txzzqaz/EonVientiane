using System;

namespace EonVientiane.PluginSystem;

/// <summary>
/// 插件接口基类 - 所有游戏插件必须实现此接口
/// </summary>
public interface IGamePlugin
{
    /// <summary>
    /// 插件名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 插件版本
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// 插件作者
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// 插件描述
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 初始化插件
    /// </summary>
    /// <param name="context">游戏上下文</param>
    void Initialize(IGameContext context);
    
    /// <summary>
    /// 卸载插件
    /// </summary>
    void Shutdown();
    
    /// <summary>
    /// 每帧更新
    /// </summary>
    /// <param name="deltaTime">距上一帧的时间（秒）</param>
    void Update(float deltaTime);
}

/// <summary>
/// 战斗插件接口 - 扩展战斗系统
/// </summary>
public interface IBattlePlugin : IGamePlugin
{
    /// <summary>
    /// 战斗开始时触发
    /// </summary>
    void OnBattleStart(IBattleContext battle);
    
    /// <summary>
    /// 回合开始时触发
    /// </summary>
    void OnRoundStart(IBattleContext battle, int roundNumber);
    
    /// <summary>
    /// 玩家行动前触发
    /// </summary>
    void OnBeforePlayerAction(IBattleContext battle, Player player);
    
    /// <summary>
    /// 玩家行动后触发
    /// </summary>
    void OnAfterPlayerAction(IBattleContext battle, Player player);
    
    /// <summary>
    /// 战斗结束时触发
    /// </summary>
    void OnBattleEnd(IBattleContext battle, PlayerCamp? winner);
}

/// <summary>
/// 物品插件接口 - 扩展物品系统
/// </summary>
public interface IItemPlugin : IGamePlugin
{
    /// <summary>
    /// 注册自定义物品
    /// </summary>
    void RegisterItems(IItemRegistry registry);
    
    /// <summary>
    /// 物品使用时触发
    /// </summary>
    bool OnItemUsed(Item item, Player player);
    
    /// <summary>
    /// 物品装备时触发
    /// </summary>
    void OnItemEquipped(Equipment equipment, Player player);
    
    /// <summary>
    /// 物品卸下时触发
    /// </summary>
    void OnItemUnequipped(Equipment equipment, Player player);
}

/// <summary>
/// UI插件接口 - 扩展UI系统
/// </summary>
public interface IUIPlugin : IGamePlugin
{
    /// <summary>
    /// 添加自定义UI元素
    /// </summary>
    void RegisterUIElements(IUIContext uiContext);
    
    /// <summary>
    /// UI绘制时触发
    /// </summary>
    void OnDraw(IUIContext uiContext);
}
