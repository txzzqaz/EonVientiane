using System.Collections.Generic;

namespace EonVientiane.PluginSystem;

/// <summary>
/// 游戏上下文接口 - 提供给插件的游戏核心功能访问
/// </summary>
public interface IGameContext
{
    /// <summary>
    /// 物品管理器
    /// </summary>
    InventoryManager InventoryManager { get; }
    
    /// <summary>
    /// 成就系统
    /// </summary>
    AchievementSystem AchievementSystem { get; }
    
    /// <summary>
    /// 当前用户信息
    /// </summary>
    UserProfile CurrentUser { get; }
    
    /// <summary>
    /// 日志记录
    /// </summary>
    void Log(string message);
    
    /// <summary>
    /// 触发自定义事件
    /// </summary>
    void TriggerEvent(string eventName, object data = null);
    
    /// <summary>
    /// 订阅自定义事件
    /// </summary>
    void SubscribeEvent(string eventName, System.Action<object> handler);
}

/// <summary>
/// 战斗上下文接口 - 提供给插件的战斗系统访问
/// </summary>
public interface IBattleContext
{
    /// <summary>
    /// 当前战斗实例
    /// </summary>
    Battle CurrentBattle { get; }
    
    /// <summary>
    /// 所有玩家
    /// </summary>
    IReadOnlyList<Player> AllPlayers { get; }
    
    /// <summary>
    /// 队伍1玩家
    /// </summary>
    IReadOnlyList<Player> Team1Players { get; }
    
    /// <summary>
    /// 队伍2玩家
    /// </summary>
    IReadOnlyList<Player> Team2Players { get; }
    
    /// <summary>
    /// 当前回合数
    /// </summary>
    int CurrentRound { get; }
    
    /// <summary>
    /// 战斗是否结束
    /// </summary>
    bool IsBattleOver { get; }
    
    /// <summary>
    /// 添加战斗日志
    /// </summary>
    void AddLog(string message);
    
    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    void DealDamage(Player target, int damage, Player source = null);
    
    /// <summary>
    /// 治疗玩家
    /// </summary>
    void HealPlayer(Player target, int amount);
    
    /// <summary>
    /// 为玩家添加效果
    /// </summary>
    void ApplyEffect(Player target, GameEffect effect);
}

/// <summary>
/// 物品注册表接口
/// </summary>
public interface IItemRegistry
{
    /// <summary>
    /// 注册物品
    /// </summary>
    void RegisterItem(string itemId, System.Func<Item> factory);
    
    /// <summary>
    /// 注册骰子
    /// </summary>
    void RegisterDice(string diceId, System.Func<Dice> factory);
    
    /// <summary>
    /// 注册饰品
    /// </summary>
    void RegisterAccessory(string accessoryId, System.Func<Accessory> factory);
    
    /// <summary>
    /// 获取所有已注册的物品ID
    /// </summary>
    IEnumerable<string> GetRegisteredItemIds();
}

/// <summary>
/// UI上下文接口
/// </summary>
public interface IUIContext
{
    /// <summary>
    /// 菜单宽度
    /// </summary>
    int MenuWidth { get; }
    
    /// <summary>
    /// 屏幕宽度
    /// </summary>
    int ScreenWidth { get; }
    
    /// <summary>
    /// 屏幕高度
    /// </summary>
    int ScreenHeight { get; }
    
    /// <summary>
    /// 添加自定义UI面板
    /// </summary>
    void AddCustomPanel(string panelId, IUIPanel panel);
    
    /// <summary>
    /// 显示通知
    /// </summary>
    void ShowNotification(string message, float duration = 3.0f);
}

/// <summary>
/// UI面板接口
/// </summary>
public interface IUIPanel
{
    /// <summary>
    /// 面板是否可见
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// 更新面板逻辑
    /// </summary>
    void Update(float deltaTime);
    
    /// <summary>
    /// 绘制面板
    /// </summary>
    void Draw(object spriteBatch);
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput(object mouseState, object keyboardState);
}
