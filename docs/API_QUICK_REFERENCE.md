# EonVientiane API 快速参考

快速查找API用法的参考手册。

---

## 🎮 插件系统

### 创建插件
```csharp
public class MyPlugin : IGamePlugin
{
    public string Name => "我的插件";
    public string Version => "1.0.0";
    public string Author => "作者名";
    public string Description => "插件描述";
    
    public void Initialize(IGameContext context) { }
    public void Shutdown() { }
    public void Update(float deltaTime) { }
}
```

### 加载插件
```csharp
var pm = new PluginManager(gameContext, "Mods");
pm.LoadAllPlugins();
pm.UpdatePlugins(deltaTime);
```

---

## ⚔️ 战斗系统API

### 事件订阅
```csharp
BattleAPI.BattleStarted += (battle) => { };
BattleAPI.RoundStarted += (battle, round) => { };
BattleAPI.BeforePlayerAction += (battle, player) => { };
BattleAPI.AfterDamageDealt += (attacker, target, damage) => { };
```

### 修改伤害
```csharp
BattleAPI.BeforeDamageCalculation += (attacker, target, baseDamage) =>
{
    return (int)(baseDamage * 1.5f); // 伤害提升50%
};
```

### 自定义规则
```csharp
public class MyRule : IBattleRule
{
    public string RuleName => "规则名";
    public int Priority => 100;
    public void OnRoundStart(Battle battle) { }
    public void OnRoundEnd(Battle battle) { }
    public bool CanPlayerAct(Battle battle, Player player) => true;
    public void ModifyTurnOrder(Battle battle, List<Player> players) { }
}

BattleAPI.AddBattleRule(new MyRule());
```

---

## 👤 玩家系统API

### 扩展方法
```csharp
// 获取装备
var activeDice = player.GetActiveDice();
var passiveDice = player.GetPassiveDice();
var accessories = player.GetAccessories();

// 检查状态
bool lowHP = player.IsLowHealth(0.3f);
bool stunned = player.IsStunned();
bool hasItem = player.HasEquipment("item_id");

// 计算属性
int attack = player.GetTotalAttackPower();
int defense = player.GetTotalDefense();

// 清除效果
player.ClearDebuffs();
player.ClearBuffs();
```

### 构建器
```csharp
var player = new PlayerBuilder()
    .WithId("p1")
    .WithName("玩家")
    .InCamp(PlayerCamp.Team1)
    .WithMaxHP(100)
    .WithEquipment(new D6Dice())
    .Build();
```

---

## 📦 物品系统API

### 注册物品效果
```csharp
ItemAPI.RegisterItemEffect("potion", (item, player) =>
{
    player.Heal(50);
    return true;
});
```

### 查询物品
```csharp
var query = new ItemQueryBuilder(items);
var rareDice = query.Dices().WithQuality(ItemQuality.Rare).ToList();
var stackable = query.Stackable().ToList();
```

### 批量创建
```csharp
var dices = ItemFactoryExtensions.CreateDiceSet("d6", "feathered");
var items = ItemAPI.CreateItems(("potion", 5), ("gold", 100));
```

### 物品修改器
```csharp
public class MyModifier : IItemModifier
{
    public string Name => "修改器";
    public int Priority => 10;
    public Item ModifyItem(Item item) => item;
}

ItemAPI.AddItemModifier(new MyModifier());
```

---

## 🏆 成就系统API

### 创建成就
```csharp
var ach = AchievementAPI.CreateCustomAchievement(
    "ach_001",
    "成就名",
    "描述",
    AchievementSystem.AchievementType.CustomEvent,
    requiredProgress: 10,
    new AchievementSystem.AchievementReward { /*...*/ }
);
```

### 成就链
```csharp
var chain = AchievementAPI.CreateAchievementChain(
    "victories",
    "战斗大师",
    new[] { 10, 50, 100 },
    AchievementSystem.AchievementType.BattleVictories
);
```

### 奖励生成器
```csharp
public class MyRewardGen : IAchievementRewardGenerator
{
    public List<AchievementSystem.AchievementReward> GenerateRewards(
        AchievementSystem.Achievement ach)
    {
        return new List<AchievementSystem.AchievementReward>();
    }
}

AchievementAPI.AddRewardGenerator(new MyRewardGen());
```

---

## 🌐 网络系统API

### 消息处理
```csharp
NetworkAPI.RegisterMessageHandler("custom_msg", (msg) =>
{
    Console.WriteLine($"收到: {msg.Type}");
});
```

### 事件订阅
```csharp
NetworkAPI.ConnectionEstablished += (addr) => { };
NetworkAPI.ConnectionLost += (reason) => { };
NetworkAPI.MessageReceived += (msg) => { };
```

### 消息构建
```csharp
var msg = new NetworkMessageBuilder()
    .WithType("action")
    .AddData("type", "attack")
    .AddData("target", "enemy_1")
    .Build();
```

### 拦截器
```csharp
public class MyInterceptor : IMessageInterceptor
{
    public NetworkMessage OnBeforeSend(NetworkMessage msg) => msg;
    public NetworkMessage OnAfterReceive(NetworkMessage msg) => msg;
}

NetworkAPI.AddMessageInterceptor(new MyInterceptor());
```

---

## 🎨 UI系统API

### 注册UI元素
```csharp
public class MyUI : ICustomUIElement
{
    public string ElementId { get; set; }
    public bool IsVisible { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    
    public void Update(GameTime gt) { }
    public void Draw(SpriteBatch sb) { }
    public bool HandleClick(Vector2 pos) => false;
}

UIAPI.RegisterUIElement("my_ui", new MyUI(), priority: 10);
```

### UI主题
```csharp
var theme = new UITheme
{
    PrimaryColor = Color.Black,
    TextColor = Color.White,
    // ...
};

UIAPI.RegisterTheme("Dark", theme);
UIAPI.SetTheme("Dark");
var current = UIAPI.GetCurrentTheme();
```

### UI事件
```csharp
UIAPI.SubscribeEvent("btn_click", (data) => { });
UIAPI.TriggerEvent("btn_click", "button_1");
```

### 布局构建
```csharp
var layout = new UILayoutBuilder(new Vector2(50, 50), spacing: 10f)
    .AddElement(button1)
    .MoveDown()
    .AddElement(button2)
    .NewRow()
    .AddElement(button3)
    .Build();
```

### 通知系统
```csharp
var notifications = new NotificationSystem(3.0f, 5);
notifications.ShowNotification("消息");
notifications.Update(deltaTime);
notifications.Draw(spriteBatch, font, screenBounds);
```

---

## 🔌 常用模式

### 单例插件
```csharp
public class MySingleton : IGamePlugin
{
    private static MySingleton _instance;
    public static MySingleton Instance => _instance;
    
    public void Initialize(IGameContext ctx)
    {
        _instance = this;
    }
}
```

### 事件订阅模式
```csharp
public void Initialize(IGameContext ctx)
{
    BattleAPI.BattleStarted += OnBattleStart;
}

public void Shutdown()
{
    BattleAPI.BattleStarted -= OnBattleStart;
}
```

### 配置模式
```csharp
public class Config
{
    public float DamageMultiplier { get; set; } = 1.0f;
    public int MaxLevel { get; set; } = 100;
}

private Config _config = new Config();
```

---

## 📊 数据类型

### 常用枚举
```csharp
ItemType: Equipment, Consumable, Material, QuestItem, Currency, Dice, Accessory
ItemQuality: Common, Uncommon, Rare, Epic, Legendary
PlayerCamp: Team1, Team2
EffectType: AttackBoost, DefenseBoost, Poisoned, Stunned, Frozen, etc.
BattleState: Idle, Initialization, RoundStart, PlayerAction, etc.
```

### 常用类
```csharp
Player, Battle, Item, Equipment, Dice, Accessory
GameEffect, ItemStack, Achievement
NetworkMessage, UITheme
```

---

## 🎯 最佳实践

### ✅ DO
- 在Shutdown中取消所有事件订阅
- 使用try-catch处理错误
- 记录详细日志
- 遵循命名规范
- 提供清晰的文档

### ❌ DON'T
- 不要在Update中创建大量对象
- 不要阻塞主线程
- 不要忽略异常
- 不要修改核心游戏文件
- 不要泄漏事件订阅

---

## 🔗 完整文档

- [完整API指南](API_GUIDE.md)
- [插件示例](PLUGIN_EXAMPLES.md)
- [系统架构文档](../docs/)

---

**版本**: 1.0.0  
**最后更新**: 2026-01-14
