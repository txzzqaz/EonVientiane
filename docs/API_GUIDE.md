# EonVientiane 游戏扩展API完整文档

## 📋 目录

1. [插件系统](#插件系统)
2. [战斗系统API](#战斗系统api)
3. [玩家系统API](#玩家系统api)
4. [物品系统API](#物品系统api)
5. [成就系统API](#成就系统api)
6. [网络系统API](#网络系统api)
7. [UI系统API](#ui系统api)
8. [完整示例](#完整示例)

---

## 插件系统

### 核心接口

#### IGamePlugin
所有插件的基础接口。

```csharp
public interface IGamePlugin
{
    string Name { get; }
    string Version { get; }
    string Author { get; }
    string Description { get; }
    
    void Initialize(IGameContext context);
    void Shutdown();
    void Update(float deltaTime);
}
```

#### 专用插件接口

- **IBattlePlugin** - 扩展战斗系统
- **IItemPlugin** - 扩展物品系统
- **IUIPlugin** - 扩展UI系统

### PluginManager

```csharp
// 创建插件管理器
var pluginManager = new PluginManager(gameContext, "Mods");

// 加载所有插件
pluginManager.LoadAllPlugins();

// 加载单个插件
pluginManager.LoadPluginFromFile("MyPlugin.dll");

// 获取插件
var battlePlugins = pluginManager.GetPlugins<IBattlePlugin>();

// 卸载插件
pluginManager.UnloadPlugin("MyPlugin");

// 更新所有插件（每帧调用）
pluginManager.UpdatePlugins(deltaTime);
```

### 创建插件示例

```csharp
using EonVientiane.PluginSystem;

public class MyBattlePlugin : IBattlePlugin
{
    public string Name => "超级战斗插件";
    public string Version => "1.0.0";
    public string Author => "Your Name";
    public string Description => "为战斗添加特殊效果";
    
    private IGameContext _context;
    
    public void Initialize(IGameContext context)
    {
        _context = context;
        _context.Log($"{Name} 已加载");
    }
    
    public void Shutdown()
    {
        _context.Log($"{Name} 已卸载");
    }
    
    public void Update(float deltaTime)
    {
        // 每帧更新逻辑
    }
    
    public void OnBattleStart(IBattleContext battle)
    {
        battle.AddLog("🎮 特殊战斗规则已激活！");
    }
    
    public void OnRoundStart(IBattleContext battle, int roundNumber)
    {
        if (roundNumber % 5 == 0)
        {
            // 每5回合触发特殊事件
            battle.AddLog($"💫 第{roundNumber}回合特殊效果！");
        }
    }
    
    public void OnBeforePlayerAction(IBattleContext battle, Player player) { }
    public void OnAfterPlayerAction(IBattleContext battle, Player player) { }
    public void OnBattleEnd(IBattleContext battle, PlayerCamp? winner) { }
}
```

---

## 战斗系统API

### BattleAPI 事件系统

```csharp
// 订阅战斗事件
BattleAPI.BattleStarted += (battle) => 
{
    Console.WriteLine("战斗开始！");
};

BattleAPI.RoundStarted += (battle, round) => 
{
    Console.WriteLine($"回合 {round} 开始");
};

BattleAPI.BeforePlayerAction += (battle, player) => 
{
    Console.WriteLine($"{player.PlayerName} 准备行动");
};

// 修改伤害计算
BattleAPI.BeforeDamageCalculation += (attacker, target, baseDamage) =>
{
    // 增加暴击系统
    var random = new Random();
    if (random.Next(100) < 20) // 20% 暴击率
    {
        Console.WriteLine("💥 暴击！");
        return baseDamage * 2;
    }
    return baseDamage;
};

// 伤害后事件
BattleAPI.AfterDamageDealt += (attacker, target, damage) =>
{
    if (target.CurrentHP <= 0)
    {
        Console.WriteLine($"⚰️ {target.PlayerName} 被击败！");
    }
};

// 治疗前修改
BattleAPI.BeforeHeal += (target, baseHeal) =>
{
    // 根据生命值百分比调整治疗量
    if (target.GetHealthPercentage() < 0.3f)
    {
        return (int)(baseHeal * 1.5f); // 低血量时治疗加成
    }
    return baseHeal;
};
```

### 自定义战斗规则

```csharp
public class WeatherEffectRule : IBattleRule
{
    public string RuleName => "天气系统";
    public int Priority => 100;
    
    private WeatherType _currentWeather;
    
    public void OnRoundStart(Battle battle)
    {
        // 每3回合切换天气
        if (battle.CurrentRound % 3 == 0)
        {
            _currentWeather = (WeatherType)new Random().Next(3);
            battle.BattleLog.Add($"🌤️ 天气变为: {_currentWeather}");
        }
    }
    
    public void OnRoundEnd(Battle battle) { }
    
    public bool CanPlayerAct(Battle battle, Player player)
    {
        // 暴风雪天气下冻结状态的玩家无法行动
        if (_currentWeather == WeatherType.Blizzard && player.IsFrozen())
        {
            return false;
        }
        return true;
    }
    
    public void ModifyTurnOrder(Battle battle, List<Player> players)
    {
        // 顺风时提升速度快的玩家优先级
        if (_currentWeather == WeatherType.Windy)
        {
            players.Sort((a, b) => a.TurnOrder.CompareTo(b.TurnOrder));
        }
    }
}

// 添加规则
BattleAPI.AddBattleRule(new WeatherEffectRule());
```

---

## 玩家系统API

### PlayerAPI 扩展方法

```csharp
// 获取玩家装备信息
var activeDice = player.GetActiveDice();
var passiveDice = player.GetPassiveDice();
var accessories = player.GetAccessories();

// 检查装备和效果
bool hasSword = player.HasEquipment("legendary_sword");
bool isPoisoned = player.HasEffect(EffectType.Poisoned);

// 获取特定效果
var attackBoosts = player.GetEffects(EffectType.AttackBoost);

// 计算总属性
int totalAttack = player.GetTotalAttackPower();
int totalDefense = player.GetTotalDefense();

// 状态检查
bool isLowHP = player.IsLowHealth(0.3f); // 生命值 < 30%
bool cannotAct = player.IsStunned() || player.IsFrozen();

// 清除效果
player.ClearDebuffs(); // 清除所有负面效果
player.ClearBuffs();   // 清除所有正面效果
player.RemoveEffects(EffectType.Poisoned); // 移除特定效果
```

### PlayerBuilder 构建器模式

```csharp
// 快速创建配置好的玩家
var player = new PlayerBuilder()
    .WithId("player001")
    .WithName("勇者")
    .InCamp(PlayerCamp.Team1)
    .WithMaxHP(150)
    .WithCurrentHP(150)
    .WithShield(2)
    .WithEquipment(new D6Dice())
    .WithEquipment(new SelfAccessory())
    .WithEffect(new GameEffect(EffectType.AttackBoost, 10, 3))
    .Build();
```

### PlayerAPI 事件

```csharp
// 监听玩家属性变化
PlayerAPI.PropertyChanged += (player, propertyName, oldValue, newValue) =>
{
    Console.WriteLine($"{player.PlayerName}的{propertyName}从{oldValue}变为{newValue}");
};

// 玩家死亡事件
PlayerAPI.PlayerDied += (player) =>
{
    Console.WriteLine($"💀 {player.PlayerName} 死亡");
    // 可以在这里添加复活逻辑
};

// 装备变化事件
PlayerAPI.EquipmentChanged += (player, equipment, equipped) =>
{
    string action = equipped ? "装备了" : "卸下了";
    Console.WriteLine($"{player.PlayerName} {action} {equipment.Name}");
};
```

---

## 物品系统API

### ItemAPI 自定义物品效果

```csharp
// 注册自定义物品效果
ItemAPI.RegisterItemEffect("healing_potion", (item, player) =>
{
    int healAmount = 50;
    player.Heal(healAmount);
    Console.WriteLine($"{player.PlayerName} 使用了 {item.Name}，恢复 {healAmount} HP");
    return true; // 返回true表示成功使用
});

ItemAPI.RegisterItemEffect("mystery_box", (item, player) =>
{
    var random = new Random();
    var rewards = new[] { "gold", "gem", "rare_dice" };
    var reward = rewards[random.Next(rewards.Length)];
    Console.WriteLine($"🎁 获得: {reward}");
    return true;
});

// 执行物品效果
bool success = ItemAPI.ExecuteItemEffect(item, player);
```

### 物品修改器

```csharp
public class EnchantmentModifier : IItemModifier
{
    public string Name => "附魔系统";
    public int Priority => 10;
    
    public Item ModifyItem(Item item)
    {
        // 为稀有物品添加附魔
        if (item.Quality >= ItemQuality.Rare && item is Equipment equipment)
        {
            // 添加附魔效果
            item.Description += "\n✨ [附魔: +10攻击力]";
        }
        return item;
    }
}

// 添加修改器
ItemAPI.AddItemModifier(new EnchantmentModifier());

// 创建物品时自动应用修改器
var item = ItemFactoryExtensions.CreateItemWithModifiers("legendary_sword");
```

### ItemQueryBuilder 查询物品

```csharp
// 创建查询
var query = new ItemQueryBuilder(inventoryManager.GetAllItems());

// 查找所有稀有品质的骰子
var rareDice = query
    .Dices()
    .WithQuality(ItemQuality.Rare)
    .ToList();

// 查找可堆叠的消耗品
var consumables = query
    .OfType(ItemType.Consumable)
    .Stackable()
    .ToList();

// 查找名称包含"药水"的物品
var potions = query
    .WithNameContaining("药水")
    .ToList();

// 统计数量
int equipmentCount = query.Equipments().Count();
```

### 批量创建物品

```csharp
// 使用扩展方法批量创建
var diceSet = ItemFactoryExtensions.CreateDiceSet(
    "d6_dice",
    "feathered_dice",
    "guasha_dice"
);

var accessorySet = ItemFactoryExtensions.CreateAccessorySet(
    "self_accessory",
    "ascension_proof"
);

// 批量创建带数量
var items = ItemAPI.CreateItems(
    ("health_potion", 5),
    ("mana_potion", 3),
    ("gold", 100)
);
```

### ItemCategorizer 物品分类

```csharp
var categorizer = new ItemCategorizer();

// 自动按类型分类
categorizer.CategorizeByType(inventoryManager.GetAllItems());

// 自动按品质分类
categorizer.CategorizeByQuality(inventoryManager.GetAllItems());

// 手动分类
categorizer.AddCategory("战斗物品");
categorizer.AddItemToCategory("战斗物品", sword);
categorizer.AddItemToCategory("战斗物品", shield);

// 获取分类内容
var battleItems = categorizer.GetItemsInCategory("战斗物品");
var allCategories = categorizer.GetAllCategories();
```

---

## 成就系统API

### AchievementAPI 自定义成就

```csharp
// 创建自定义成就
var achievement = AchievementAPI.CreateCustomAchievement(
    id: "custom_achievement_001",
    name: "传奇战士",
    description: "在单场战斗中击败10个敌人",
    type: AchievementSystem.AchievementType.CustomEvent,
    requiredProgress: 10,
    new AchievementSystem.AchievementReward
    {
        Type = AchievementSystem.RewardType.Item,
        ItemId = "legendary_sword",
        Quantity = 1
    }
);

// 添加到成就系统
achievementSystem.AddCustomAchievement(achievement);

// 注册自定义触发器
AchievementAPI.RegisterTrigger("custom_achievement_001", (ach) =>
{
    // 自定义完成条件
    return ach.Progress >= ach.RequiredProgress;
});
```

### 成就链

```csharp
// 创建渐进式成就链
var chain = AchievementAPI.CreateAchievementChain(
    chainId: "battle_victories",
    baseName: "战斗大师",
    progressMilestones: new[] { 10, 50, 100, 500, 1000 },
    type: AchievementSystem.AchievementType.BattleVictories
);

foreach (var achievement in chain)
{
    achievementSystem.AddCustomAchievement(achievement);
}
```

### 奖励生成器

```csharp
public class CustomRewardGenerator : IAchievementRewardGenerator
{
    public List<AchievementSystem.AchievementReward> GenerateRewards(
        AchievementSystem.Achievement achievement)
    {
        var rewards = new List<AchievementSystem.AchievementReward>();
        
        // 根据成就类型给予不同奖励
        switch (achievement.Type)
        {
            case AchievementSystem.AchievementType.BattleVictories:
                rewards.Add(new AchievementSystem.AchievementReward
                {
                    Type = AchievementSystem.RewardType.Currency,
                    ItemId = "gold",
                    Quantity = achievement.RequiredProgress * 10
                });
                break;
                
            case AchievementSystem.AchievementType.ItemsCollected:
                rewards.Add(new AchievementSystem.AchievementReward
                {
                    Type = AchievementSystem.RewardType.Item,
                    ItemId = "mystery_box",
                    Quantity = 1
                });
                break;
        }
        
        return rewards;
    }
}

// 添加奖励生成器
AchievementAPI.AddRewardGenerator(new CustomRewardGenerator());
```

### AchievementProgressTracker

```csharp
var tracker = new AchievementProgressTracker();

// 追踪复杂成就进度
tracker.IncrementCustomProgress("combo_master", "max_combo", 15);
tracker.IncrementCustomProgress("combo_master", "perfect_rounds", 1);

// 获取进度
int maxCombo = tracker.GetCustomProgress("combo_master", "max_combo");
int perfectRounds = tracker.GetCustomProgress("combo_master", "perfect_rounds");

// 检查是否完成
if (maxCombo >= 20 && perfectRounds >= 5)
{
    achievementSystem.UpdateProgress("combo_master", 1);
    tracker.ResetProgress("combo_master");
}
```

---

## 网络系统API

### NetworkAPI 自定义消息处理

```csharp
// 注册自定义消息处理器
NetworkAPI.RegisterMessageHandler("custom_event", (message) =>
{
    string eventType = message.Data["event_type"] as string;
    Console.WriteLine($"收到自定义事件: {eventType}");
});

// 订阅网络事件
NetworkAPI.ConnectionEstablished += (serverAddress) =>
{
    Console.WriteLine($"已连接到服务器: {serverAddress}");
};

NetworkAPI.ConnectionLost += (reason) =>
{
    Console.WriteLine($"连接断开: {reason}");
};

NetworkAPI.MessageReceived += (message) =>
{
    Console.WriteLine($"收到消息: {message.Type}");
};
```

### 消息拦截器

```csharp
public class EncryptionInterceptor : IMessageInterceptor
{
    public NetworkMessage OnBeforeSend(NetworkMessage message)
    {
        // 发送前加密
        if (message.Data.ContainsKey("password"))
        {
            message.Data["password"] = Encrypt(message.Data["password"] as string);
        }
        return message;
    }
    
    public NetworkMessage OnAfterReceive(NetworkMessage message)
    {
        // 接收后解密
        if (message.Data.ContainsKey("password"))
        {
            message.Data["password"] = Decrypt(message.Data["password"] as string);
        }
        return message;
    }
    
    private string Encrypt(string data) { /* 加密逻辑 */ return data; }
    private string Decrypt(string data) { /* 解密逻辑 */ return data; }
}

// 添加拦截器
NetworkAPI.AddMessageInterceptor(new EncryptionInterceptor());
```

### NetworkMessageBuilder

```csharp
// 快速构建消息
var message = new NetworkMessageBuilder()
    .WithType("player_action")
    .AddData("action", "attack")
    .AddData("target", "enemy_01")
    .AddData("damage", 50)
    .Build();

// 发送消息
networkClient.SendMessageAsync(message);
```

### MessageLogger 消息日志

```csharp
var logger = new MessageLogger(maxLogSize: 1000);
NetworkAPI.AddMessageInterceptor(logger);

// 查看消息历史
foreach (var (timestamp, message, isSent) in logger.MessageLog)
{
    string direction = isSent ? "发送" : "接收";
    Console.WriteLine($"[{timestamp:HH:mm:ss}] {direction}: {message.Type}");
}

// 清除日志
logger.ClearLog();
```

---

## UI系统API

### UIAPI 自定义UI元素

```csharp
public class CustomButton : ICustomUIElement
{
    public string ElementId { get; set; }
    public bool IsVisible { get; set; } = true;
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    
    private Color _currentColor;
    private readonly Color _normalColor = Color.Gray;
    private readonly Color _hoverColor = Color.LightGray;
    
    public void Update(GameTime gameTime)
    {
        // 更新逻辑
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // 绘制按钮
        var rect = new Rectangle((int)Position.X, (int)Position.Y, 
                                 (int)Size.X, (int)Size.Y);
        // ... 绘制代码
    }
    
    public bool HandleClick(Vector2 mousePosition)
    {
        var bounds = new Rectangle((int)Position.X, (int)Position.Y,
                                   (int)Size.X, (int)Size.Y);
        if (bounds.Contains(mousePosition))
        {
            UIAPI.TriggerEvent("button_clicked", ElementId);
            return true;
        }
        return false;
    }
}

// 注册UI元素
var button = new CustomButton 
{ 
    ElementId = "my_button",
    Position = new Vector2(100, 100),
    Size = new Vector2(200, 50)
};

UIAPI.RegisterUIElement("my_button", button, renderPriority: 10);

// 订阅UI事件
UIAPI.SubscribeEvent("button_clicked", (data) =>
{
    Console.WriteLine($"按钮被点击: {data}");
});
```

### UI主题系统

```csharp
// 创建自定义主题
var darkTheme = new UITheme
{
    PrimaryColor = new Color(20, 20, 20),
    SecondaryColor = new Color(40, 40, 40),
    BackgroundColor = new Color(10, 10, 10),
    TextColor = Color.White,
    AccentColor = new Color(0, 150, 200),
    BorderColor = new Color(80, 80, 80),
    HoverColor = new Color(60, 60, 60),
    DisabledColor = new Color(100, 100, 100)
};

// 注册并应用主题
UIAPI.RegisterTheme("Dark", darkTheme);
UIAPI.SetTheme("Dark");

// 获取当前主题
var currentTheme = UIAPI.GetCurrentTheme();
```

### UILayoutBuilder 布局构建器

```csharp
// 快速构建UI布局
var layout = new UILayoutBuilder(
    startPosition: new Vector2(50, 50),
    spacing: 10f
);

layout
    .AddElement(new CustomButton { Size = new Vector2(200, 40) })
    .MoveDown()
    .AddElement(new CustomLabel { Size = new Vector2(200, 30) })
    .MoveDown()
    .AddElement(new CustomTextField { Size = new Vector2(200, 35) })
    .NewRow(yPosition: 200)
    .AddElement(new CustomButton { Size = new Vector2(95, 40) })
    .MoveRight()
    .AddElement(new CustomButton { Size = new Vector2(95, 40) });

var elements = layout.Build();
```

### NotificationSystem 通知系统

```csharp
var notifications = new NotificationSystem(
    defaultDuration: 3.0f,
    maxNotifications: 5
);

// 显示通知
notifications.ShowNotification("游戏保存成功！");

notifications.ShowNotification(
    "获得传奇装备！",
    backgroundColor: new Color(200, 150, 0, 200),
    textColor: Color.White,
    duration: 5.0f
);

// 在Update中更新
notifications.Update(deltaTime);

// 在Draw中绘制
notifications.Draw(spriteBatch, font, screenBounds);
```

---

## 完整示例

### 示例1: 创建完整的插件

```csharp
using EonVientiane;
using EonVientiane.PluginSystem;

public class ComboSystemPlugin : IBattlePlugin, IUIPlugin
{
    public string Name => "连击系统";
    public string Version => "1.0.0";
    public string Author => "Game Dev";
    public string Description => "添加连击系统和UI显示";
    
    private IGameContext _gameContext;
    private int _comboCount = 0;
    private Player _comboPlayer;
    
    public void Initialize(IGameContext context)
    {
        _gameContext = context;
        
        // 订阅战斗API事件
        BattleAPI.AfterDamageDealt += OnDamageDealt;
        BattleAPI.RoundEnded += OnRoundEnded;
        
        _gameContext.Log("连击系统已启动");
    }
    
    public void Shutdown()
    {
        BattleAPI.AfterDamageDealt -= OnDamageDealt;
        BattleAPI.RoundEnded -= OnRoundEnded;
    }
    
    public void Update(float deltaTime) { }
    
    private void OnDamageDealt(Player attacker, Player target, int damage)
    {
        if (_comboPlayer == attacker)
        {
            _comboCount++;
        }
        else
        {
            _comboPlayer = attacker;
            _comboCount = 1;
        }
        
        if (_comboCount >= 3)
        {
            // 连击加成
            int bonusDamage = _comboCount * 5;
            target.TakeDamage(bonusDamage);
            _gameContext.Log($"🔥 {_comboCount}连击！造成额外{bonusDamage}伤害！");
        }
    }
    
    private void OnRoundEnded(Battle battle, int round)
    {
        // 回合结束重置连击
        _comboCount = 0;
        _comboPlayer = null;
    }
    
    public void OnBattleStart(IBattleContext battle) { }
    public void OnRoundStart(IBattleContext battle, int roundNumber) { }
    public void OnBeforePlayerAction(IBattleContext battle, Player player) { }
    public void OnAfterPlayerAction(IBattleContext battle, Player player) { }
    public void OnBattleEnd(IBattleContext battle, PlayerCamp? winner) { }
    
    public void RegisterUIElements(IUIContext uiContext)
    {
        // 注册连击显示UI
        var comboDisplay = new ComboDisplayUI();
        uiContext.AddCustomPanel("combo_display", comboDisplay);
    }
    
    public void OnDraw(IUIContext uiContext)
    {
        // UI绘制逻辑
    }
}
```

### 示例2: 创建自定义物品

```csharp
// 自定义物品修改器
public class LevelScalingModifier : IItemModifier
{
    public string Name => "等级缩放";
    public int Priority => 5;
    
    private readonly int _playerLevel;
    
    public LevelScalingModifier(int playerLevel)
    {
        _playerLevel = playerLevel;
    }
    
    public Item ModifyItem(Item item)
    {
        if (item is Equipment equipment)
        {
            // 根据玩家等级调整物品属性
            int levelBonus = _playerLevel * 2;
            item.Description += $"\n📈 等级加成: +{levelBonus}";
        }
        return item;
    }
}

// 使用
ItemAPI.AddItemModifier(new LevelScalingModifier(playerLevel: 10));

// 创建自定义物品效果
ItemAPI.RegisterItemEffect("teleport_scroll", (item, player) =>
{
    // 传送玩家到安全位置
    player.CurrentHP = player.MaxHP;
    player.ClearDebuffs();
    Console.WriteLine($"✨ {player.PlayerName} 使用传送卷轴！");
    return true;
});
```

### 示例3: 完整的战斗扩展

```csharp
// 添加天气系统
BattleAPI.AddBattleRule(new WeatherSystemRule());

// 添加伤害公式修改
BattleAPI.BeforeDamageCalculation += (attacker, target, baseDamage) =>
{
    // 克制关系
    int finalDamage = baseDamage;
    
    // 攻击者有火焰效果
    if (attacker.HasEffect(EffectType.Burning))
    {
        finalDamage = (int)(finalDamage * 1.5f);
    }
    
    // 防御者有护盾
    if (target.GetTotalDefense() > 0)
    {
        finalDamage = Math.Max(1, finalDamage - target.GetTotalDefense());
    }
    
    return finalDamage;
};

// 添加回合结束特效
BattleAPI.RoundEnded += (battle, round) =>
{
    // 每5回合全员恢复生命
    if (round % 5 == 0)
    {
        foreach (var player in battle.AllPlayers)
        {
            if (!player.IsDead)
            {
                player.Heal(10);
                battle.BattleLog.Add($"💚 {player.PlayerName} 自然恢复10HP");
            }
        }
    }
};
```

---

## 最佳实践

### 1. 插件开发

- 始终在 `Initialize` 中订阅事件，在 `Shutdown` 中取消订阅
- 使用 `IGameContext.Log` 记录重要信息
- 错误处理要完善，避免插件崩溃影响主程序

### 2. 事件使用

- 避免在事件处理器中执行耗时操作
- 注意事件的执行顺序
- 及时取消不需要的事件订阅

### 3. 性能优化

- 使用对象池减少内存分配
- 避免在 `Update` 中频繁创建新对象
- 合理使用优先级控制执行顺序

### 4. 扩展性设计

- 优先使用接口而不是具体类
- 保持API简单易用
- 提供清晰的文档和示例

---

## 下一步

1. 查看 [PLUGIN_EXAMPLES.md](PLUGIN_EXAMPLES.md) 了解更多插件示例
2. 查看 [API_REFERENCE.md](API_REFERENCE.md) 获取完整API参考
3. 加入开发者社区讨论扩展开发

---

**最后更新**: 2026-01-14  
**API版本**: 1.0.0
