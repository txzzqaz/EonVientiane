# 插件开发示例集

本文档包含多个完整的插件开发示例，帮助你快速上手 EonVientiane 插件开发。

---

## 示例1: 基础战斗插件

### 功能：在战斗中添加"狂暴"机制

```csharp
using System;
using EonVientiane;
using EonVientiane.PluginSystem;

namespace MyPlugins
{
    public class BerserkPlugin : IBattlePlugin
    {
        public string Name => "狂暴系统";
        public string Version => "1.0.0";
        public string Author => "开发者";
        public string Description => "玩家生命值低于30%时进入狂暴状态";
        
        private IGameContext _context;
        
        public void Initialize(IGameContext context)
        {
            _context = context;
            
            // 订阅伤害事件
            BattleAPI.AfterDamageDealt += OnDamageDealt;
            
            _context.Log($"[{Name}] 插件已加载");
        }
        
        public void Shutdown()
        {
            // 取消订阅
            BattleAPI.AfterDamageDealt -= OnDamageDealt;
            
            _context.Log($"[{Name}] 插件已卸载");
        }
        
        public void Update(float deltaTime)
        {
            // 可选：每帧更新逻辑
        }
        
        public void OnBattleStart(IBattleContext battle)
        {
            battle.AddLog($"⚔️ {Name} 已激活！");
        }
        
        public void OnRoundStart(IBattleContext battle, int roundNumber)
        {
            // 检查所有玩家是否处于低血量
            foreach (var player in battle.AllPlayers)
            {
                if (player.GetHealthPercentage() <= 0.3f && !player.HasEffect(EffectType.AttackBoost))
                {
                    // 进入狂暴状态
                    var berserkEffect = new GameEffect(EffectType.AttackBoost, 20, 999);
                    player.AddEffect(berserkEffect);
                    
                    battle.AddLog($"🔴 {player.PlayerName} 进入狂暴状态！攻击力+20");
                }
            }
        }
        
        private void OnDamageDealt(Player attacker, Player target, int damage)
        {
            // 狂暴状态下额外伤害
            if (attacker.HasEffect(EffectType.AttackBoost) && attacker.GetHealthPercentage() <= 0.3f)
            {
                int bonusDamage = damage / 2;
                target.TakeDamage(bonusDamage);
                _context.Log($"💥 狂暴加成！额外造成{bonusDamage}伤害");
            }
        }
        
        public void OnBeforePlayerAction(IBattleContext battle, Player player) { }
        public void OnAfterPlayerAction(IBattleContext battle, Player player) { }
        
        public void OnBattleEnd(IBattleContext battle, PlayerCamp? winner)
        {
            // 清除所有狂暴效果
            foreach (var player in battle.AllPlayers)
            {
                player.RemoveEffects(EffectType.AttackBoost);
            }
        }
    }
}
```

---

## 示例2: 物品系统插件

### 功能：添加可合成的自定义物品

```csharp
using System;
using System.Collections.Generic;
using EonVientiane;
using EonVientiane.PluginSystem;

namespace MyPlugins
{
    public class CraftingPlugin : IItemPlugin
    {
        public string Name => "合成系统";
        public string Version => "1.0.0";
        public string Author => "开发者";
        public string Description => "添加物品合成功能";
        
        private IGameContext _context;
        private Dictionary<string, CraftingRecipe> _recipes;
        
        public void Initialize(IGameContext context)
        {
            _context = context;
            _recipes = new Dictionary<string, CraftingRecipe>();
            
            // 注册合成配方
            RegisterRecipes();
            
            // 注册自定义物品效果
            RegisterItemEffects();
            
            _context.Log($"[{Name}] 已加载 {_recipes.Count} 个合成配方");
        }
        
        public void Shutdown()
        {
            _recipes.Clear();
        }
        
        public void Update(float deltaTime) { }
        
        public void RegisterItems(IItemRegistry registry)
        {
            // 注册合成台物品
            registry.RegisterItem("crafting_table", () => new Item(
                "crafting_table",
                "合成台",
                "用于合成物品的工作台",
                ItemType.Tool
            ));
            
            // 注册强化石
            registry.RegisterItem("enhancement_stone", () => new Item(
                "enhancement_stone",
                "强化石",
                "用于强化装备",
                ItemType.Material
            ) { Quality = ItemQuality.Rare });
            
            // 注册传奇武器
            registry.RegisterDice("legendary_dice", () => new LegendaryDice());
        }
        
        private void RegisterRecipes()
        {
            // 配方：3个基础骰子 -> 1个强化骰子
            _recipes["enhanced_dice"] = new CraftingRecipe
            {
                ResultItemId = "enhanced_dice",
                ResultQuantity = 1,
                Ingredients = new Dictionary<string, int>
                {
                    { "d6_dice", 3 },
                    { "enhancement_stone", 1 }
                }
            };
            
            // 配方：传奇武器
            _recipes["legendary_dice"] = new CraftingRecipe
            {
                ResultItemId = "legendary_dice",
                ResultQuantity = 1,
                Ingredients = new Dictionary<string, int>
                {
                    { "d6_dice", 5 },
                    { "enhancement_stone", 3 },
                    { "gold", 1000 }
                }
            };
        }
        
        private void RegisterItemEffects()
        {
            ItemAPI.RegisterItemEffect("crafting_table", (item, player) =>
            {
                _context.TriggerEvent("open_crafting_ui", player);
                return true;
            });
        }
        
        public bool OnItemUsed(Item item, Player player)
        {
            if (item.ItemID == "crafting_table")
            {
                _context.Log($"{player.PlayerName} 打开了合成台");
                return true;
            }
            return false;
        }
        
        public void OnItemEquipped(Equipment equipment, Player player)
        {
            if (equipment.ItemID.Contains("legendary"))
            {
                _context.Log($"✨ {player.PlayerName} 装备了传奇物品！");
            }
        }
        
        public void OnItemUnequipped(Equipment equipment, Player player) { }
        
        /// <summary>
        /// 尝试合成物品
        /// </summary>
        public bool TryCraft(string recipeId, InventoryManager inventory)
        {
            if (!_recipes.TryGetValue(recipeId, out var recipe))
            {
                _context.Log($"配方 {recipeId} 不存在");
                return false;
            }
            
            // 检查材料
            foreach (var (ingredientId, requiredAmount) in recipe.Ingredients)
            {
                int available = inventory.CountItem(ingredientId);
                if (available < requiredAmount)
                {
                    _context.Log($"材料不足：需要 {requiredAmount}x {ingredientId}，但只有 {available}");
                    return false;
                }
            }
            
            // 消耗材料
            foreach (var (ingredientId, requiredAmount) in recipe.Ingredients)
            {
                inventory.RemoveItem(ingredientId, requiredAmount);
            }
            
            // 给予结果物品
            var resultItem = ItemFactory.CreateItem(recipe.ResultItemId);
            for (int i = 0; i < recipe.ResultQuantity; i++)
            {
                inventory.AddItem(resultItem);
            }
            
            _context.Log($"✅ 合成成功：{recipe.ResultQuantity}x {recipe.ResultItemId}");
            return true;
        }
        
        private class CraftingRecipe
        {
            public string ResultItemId { get; set; }
            public int ResultQuantity { get; set; }
            public Dictionary<string, int> Ingredients { get; set; }
        }
        
        // 自定义传奇骰子
        private class LegendaryDice : Dice
        {
            public LegendaryDice() : base(
                "legendary_dice",
                "传奇骰子",
                "拥有强大力量的传奇骰子",
                ItemType.Dice)
            {
                IsActivatable = true;
                IsPassive = true;
                Quality = ItemQuality.Legendary;
                MaxValue = 12; // 最大值更高
            }
            
            public override int RollActiveDice(Player owner)
            {
                var random = new Random();
                int value = random.Next(8, MaxValue + 1); // 8-12
                return value;
            }
            
            public override int RollPassiveDice(Player defender, Player attacker, int incomingDamage)
            {
                var random = new Random();
                int value = random.Next(6, MaxValue + 1); // 6-12
                return value;
            }
        }
    }
}
```

---

## 示例3: UI系统插件

### 功能：添加自定义状态显示面板

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EonVientiane;
using EonVientiane.PluginSystem;

namespace MyPlugins
{
    public class StatusPanelPlugin : IUIPlugin
    {
        public string Name => "状态面板";
        public string Version => "1.0.0";
        public string Author => "开发者";
        public string Description => "显示玩家详细状态信息";
        
        private IGameContext _context;
        private StatusPanel _panel;
        
        public void Initialize(IGameContext context)
        {
            _context = context;
            _panel = new StatusPanel();
        }
        
        public void Shutdown() { }
        
        public void Update(float deltaTime)
        {
            _panel.Update(deltaTime);
        }
        
        public void RegisterUIElements(IUIContext uiContext)
        {
            uiContext.AddCustomPanel("status_panel", _panel);
        }
        
        public void OnDraw(IUIContext uiContext)
        {
            // 在此处可以添加额外的绘制逻辑
        }
        
        private class StatusPanel : IUIPanel
        {
            public bool IsVisible { get; set; } = true;
            private float _updateTimer = 0f;
            private Player _currentPlayer;
            
            public void Update(float deltaTime)
            {
                _updateTimer += deltaTime;
                
                // 每0.5秒更新一次
                if (_updateTimer >= 0.5f)
                {
                    _updateTimer = 0f;
                    // 更新逻辑
                }
            }
            
            public void Draw(object spriteBatch)
            {
                if (!IsVisible || _currentPlayer == null) return;
                
                var batch = spriteBatch as SpriteBatch;
                if (batch == null) return;
                
                // 绘制面板背景
                var panelRect = new Rectangle(10, 10, 300, 400);
                var pixel = new Texture2D(batch.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { Color.White });
                
                batch.Draw(pixel, panelRect, new Color(30, 30, 30, 200));
                
                // 这里可以添加更多UI元素绘制
                // 例如：玩家头像、生命条、状态图标等
            }
            
            public void HandleInput(object mouseState, object keyboardState)
            {
                // 处理输入
            }
            
            public void SetPlayer(Player player)
            {
                _currentPlayer = player;
            }
        }
    }
}
```

---

## 示例4: 综合插件（包含多个系统）

### 功能：技能系统

```csharp
using System;
using System.Collections.Generic;
using EonVientiane;
using EonVientiane.PluginSystem;

namespace MyPlugins
{
    /// <summary>
    /// 完整的技能系统插件
    /// </summary>
    public class SkillSystemPlugin : IGamePlugin, IBattlePlugin, IItemPlugin
    {
        public string Name => "技能系统";
        public string Version => "1.0.0";
        public string Author => "开发者";
        public string Description => "为游戏添加技能系统";
        
        private IGameContext _context;
        private Dictionary<string, Skill> _skills;
        private Dictionary<Player, List<Skill>> _playerSkills;
        
        public void Initialize(IGameContext context)
        {
            _context = context;
            _skills = new Dictionary<string, Skill>();
            _playerSkills = new Dictionary<Player, List<Skill>>();
            
            // 注册技能
            RegisterSkills();
            
            // 订阅事件
            BattleAPI.BeforePlayerAction += OnBeforePlayerAction;
            
            _context.Log($"[{Name}] 已加载 {_skills.Count} 个技能");
        }
        
        public void Shutdown()
        {
            BattleAPI.BeforePlayerAction -= OnBeforePlayerAction;
            _skills.Clear();
            _playerSkills.Clear();
        }
        
        public void Update(float deltaTime)
        {
            // 更新所有技能冷却时间
            foreach (var (player, skills) in _playerSkills)
            {
                foreach (var skill in skills)
                {
                    skill.UpdateCooldown(deltaTime);
                }
            }
        }
        
        private void RegisterSkills()
        {
            // 火球术
            _skills["fireball"] = new Skill
            {
                Id = "fireball",
                Name = "火球术",
                Description = "发射火球造成50点伤害",
                Cooldown = 3.0f,
                ManaCost = 20,
                Execute = (caster, battle) =>
                {
                    var enemies = battle.Team1Players.Contains(caster) 
                        ? battle.Team2Players 
                        : battle.Team1Players;
                    
                    if (enemies.Count > 0)
                    {
                        var target = enemies[new Random().Next(enemies.Count)];
                        battle.DealDamage(target, 50, caster);
                        battle.AddLog($"🔥 {caster.PlayerName} 使用火球术！");
                        return true;
                    }
                    return false;
                }
            };
            
            // 治疗术
            _skills["heal"] = new Skill
            {
                Id = "heal",
                Name = "治疗术",
                Description = "恢复30点生命值",
                Cooldown = 5.0f,
                ManaCost = 25,
                Execute = (caster, battle) =>
                {
                    battle.HealPlayer(caster, 30);
                    battle.AddLog($"💚 {caster.PlayerName} 使用治疗术！");
                    return true;
                }
            };
            
            // 护盾术
            _skills["shield"] = new Skill
            {
                Id = "shield",
                Name = "护盾术",
                Description = "为自己添加2层护盾",
                Cooldown = 4.0f,
                ManaCost = 15,
                Execute = (caster, battle) =>
                {
                    caster.ShieldLayers += 2;
                    battle.AddLog($"🛡️ {caster.PlayerName} 使用护盾术！");
                    return true;
                }
            };
        }
        
        /// <summary>
        /// 为玩家学习技能
        /// </summary>
        public void LearnSkill(Player player, string skillId)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
            {
                _context.Log($"技能 {skillId} 不存在");
                return;
            }
            
            if (!_playerSkills.ContainsKey(player))
            {
                _playerSkills[player] = new List<Skill>();
            }
            
            if (!_playerSkills[player].Exists(s => s.Id == skillId))
            {
                _playerSkills[player].Add(skill.Clone());
                _context.Log($"{player.PlayerName} 学会了技能：{skill.Name}");
            }
        }
        
        /// <summary>
        /// 使用技能
        /// </summary>
        public bool UseSkill(Player player, string skillId, IBattleContext battle)
        {
            if (!_playerSkills.TryGetValue(player, out var skills))
                return false;
            
            var skill = skills.Find(s => s.Id == skillId);
            if (skill == null)
            {
                _context.Log($"{player.PlayerName} 没有学会技能 {skillId}");
                return false;
            }
            
            if (!skill.CanUse())
            {
                _context.Log($"技能 {skill.Name} 冷却中");
                return false;
            }
            
            // TODO: 检查法力值
            
            bool success = skill.Execute(player, battle);
            if (success)
            {
                skill.StartCooldown();
            }
            
            return success;
        }
        
        private void OnBeforePlayerAction(Battle battle, Player player)
        {
            // 可以在这里自动使用技能
        }
        
        // IBattlePlugin 实现
        public void OnBattleStart(IBattleContext battle) { }
        public void OnRoundStart(IBattleContext battle, int roundNumber) { }
        public void OnAfterPlayerAction(IBattleContext battle, Player player) { }
        public void OnBattleEnd(IBattleContext battle, PlayerCamp? winner)
        {
            // 重置所有技能冷却
            foreach (var skills in _playerSkills.Values)
            {
                foreach (var skill in skills)
                {
                    skill.ResetCooldown();
                }
            }
        }
        
        // IItemPlugin 实现
        public void RegisterItems(IItemRegistry registry)
        {
            // 注册技能书物品
            registry.RegisterItem("skill_book_fireball", () => new Item(
                "skill_book_fireball",
                "火球术技能书",
                "使用后学会火球术",
                ItemType.Consumable
            ));
        }
        
        public bool OnItemUsed(Item item, Player player)
        {
            if (item.ItemID.StartsWith("skill_book_"))
            {
                string skillId = item.ItemID.Replace("skill_book_", "");
                LearnSkill(player, skillId);
                return true;
            }
            return false;
        }
        
        public void OnItemEquipped(Equipment equipment, Player player) { }
        public void OnItemUnequipped(Equipment equipment, Player player) { }
        
        /// <summary>
        /// 技能类
        /// </summary>
        private class Skill
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public float Cooldown { get; set; }
            public int ManaCost { get; set; }
            public Func<Player, IBattleContext, bool> Execute { get; set; }
            
            private float _currentCooldown = 0f;
            
            public bool CanUse() => _currentCooldown <= 0f;
            
            public void StartCooldown()
            {
                _currentCooldown = Cooldown;
            }
            
            public void UpdateCooldown(float deltaTime)
            {
                if (_currentCooldown > 0f)
                {
                    _currentCooldown -= deltaTime;
                }
            }
            
            public void ResetCooldown()
            {
                _currentCooldown = 0f;
            }
            
            public Skill Clone()
            {
                return new Skill
                {
                    Id = this.Id,
                    Name = this.Name,
                    Description = this.Description,
                    Cooldown = this.Cooldown,
                    ManaCost = this.ManaCost,
                    Execute = this.Execute
                };
            }
        }
    }
}
```

---

## 编译和使用插件

### 1. 创建插件项目

```bash
# 创建新的类库项目
dotnet new classlib -n MyGamePlugin

# 添加对EonVientiane的引用
cd MyGamePlugin
dotnet add reference ../EonVientiane/EonVientiane.csproj
```

### 2. 编译插件

```bash
dotnet build -c Release
```

### 3. 部署插件

将编译好的DLL复制到游戏的 `Mods` 文件夹：

```bash
cp bin/Release/net6.0/MyGamePlugin.dll ../EonVientiane/bin/Release/Mods/
```

### 4. 在游戏中加载插件

```csharp
// 在Game1.cs的Initialize方法中
var pluginManager = new PluginManager(gameContext, "Mods");
pluginManager.LoadAllPlugins();

// 在Update方法中
pluginManager.UpdatePlugins((float)gameTime.ElapsedGameTime.TotalSeconds);
```

---

## 调试技巧

### 1. 启用详细日志

```csharp
public void Initialize(IGameContext context)
{
    _context = context;
    _context.Log($"[{Name}] 开始初始化...");
    
    try
    {
        // 初始化代码
        _context.Log($"[{Name}] 初始化成功");
    }
    catch (Exception ex)
    {
        _context.Log($"[{Name}] 初始化失败: {ex.Message}");
        throw;
    }
}
```

### 2. 使用断点调试

在Visual Studio中附加到游戏进程进行调试。

### 3. 错误处理

```csharp
public void Update(float deltaTime)
{
    try
    {
        // 更新逻辑
    }
    catch (Exception ex)
    {
        _context.Log($"[{Name}] Update错误: {ex.Message}");
        // 不要让异常传播，避免影响主程序
    }
}
```

---

## 更多资源

- [API完整文档](API_GUIDE.md)
- [API参考手册](API_REFERENCE.md)
- [最佳实践](BEST_PRACTICES.md)

---

**提示**: 所有示例代码都可以在游戏源代码中找到更多参考。
