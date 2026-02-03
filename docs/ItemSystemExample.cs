using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 物品系统使用示例
/// 展示如何使用新的物品API
/// </summary>
public class ItemSystemExample
{
    /// <summary>
    /// 示例1: 基础初始化和创建
    /// </summary>
    public void Example1_BasicInitialization()
    {
        // 在应用启动时初始化工厂
        ItemFactory.Initialize();
        
        // 创建单个物品
        var d6 = ItemFactory.Create("d6_dice");
        Console.WriteLine($"创建物品: {d6.Name} (ID: {d6.Id})");
        
        // 创建物品堆栈
        var goldStack = ItemFactory.CreateItemStack("gold_coin", 100);
        Console.WriteLine($"创建堆栈: {goldStack.Item.Name} x{goldStack.Quantity}");
    }
    
    /// <summary>
    /// 示例2: 查询可用物品
    /// </summary>
    public void Example2_QueryAvailableItems()
    {
        ItemFactory.Initialize();
        
        // 获取所有骰子ID
        var diceIds = ItemFactory.GetAllDiceIds();
        Console.WriteLine("可用的骰子:");
        foreach (var id in diceIds)
        {
            var dice = ItemFactory.Create(id) as Dice;
            Console.WriteLine($"  - {id}: {dice.Name} ({dice.UsageType})");
        }
        
        // 获取所有饰品ID
        var accessoryIds = ItemFactory.GetAllAccessoryIds();
        Console.WriteLine("\n可用的饰品:");
        foreach (var id in accessoryIds)
        {
            var accessory = ItemFactory.Create(id) as Accessory;
            Console.WriteLine($"  - {id}: {accessory.Name}");
        }
    }
    
    /// <summary>
    /// 示例3: 创建玩家起始装备
    /// </summary>
    public void Example3_CreatePlayerEquipment()
    {
        ItemFactory.Initialize();
        
        // 为新玩家创建起始装备
        var startingDices = ItemFactory.CreateStarterDices();
        var startingAccessories = ItemFactory.CreateStarterAccessories();
        
        Console.WriteLine("玩家起始骰子:");
        foreach (var dice in startingDices)
        {
            Console.WriteLine($"  - {dice.Name}: {dice.Description}");
        }
        
        Console.WriteLine("\n玩家起始饰品:");
        foreach (var accessory in startingAccessories)
        {
            Console.WriteLine($"  - {accessory.Name}: {accessory.Description}");
        }
    }
    
    /// <summary>
    /// 示例4: 使用骰子进行攻击和防守
    /// </summary>
    public void Example4_DiceActions(Player attacker, List<Player> defenders, Player defender)
    {
        ItemFactory.Initialize();
        
        // 创建D6骰子
        var d6 = new D6Dice();
        
        // 执行攻击
        var attackResult = d6.ExecuteActiveAction(attacker, defenders);
        if (attackResult.Success)
        {
            Console.WriteLine($"攻击成功: {attackResult.Message}");
            Console.WriteLine($"攻击力: {attackResult.AttackPower}");
        }
        
        // 执行防守
        var defenseResult = d6.ExecutePassiveAction(defender, attackResult.AttackPower);
        Console.WriteLine($"防守结果: {defenseResult.Message}");
        Console.WriteLine($"实际伤害: {defenseResult.ActualDamage}");
    }
    
    /// <summary>
    /// 示例5: 使用饰品的战斗效果
    /// </summary>
    public void Example5_AccessoryEffects()
    {
        ItemFactory.Initialize();
        
        // 创建饰品
        var selfAccessory = new SelfAccessory();
        var ascensionProof = new AscensionProofAccessory();
        var wandererHeart = new WandererHeartAccessory();
        
        // 创建战斗上下文
        var context = new BattleContext();
        
        // 自我饰品效果：增加HP
        Console.WriteLine("=== 自我饰品效果 ===");
        selfAccessory.OnBattleStart(context);
        Console.WriteLine($"获得HP: {selfAccessory.Health}");
        Console.WriteLine($"当前玩家HP: {context.PlayerHP}");
        
        // 飞升之证效果：获得护盾
        Console.WriteLine("\n=== 飞升之证效果 ===");
        ascensionProof.OnWin();
        ascensionProof.OnWin();
        ascensionProof.OnWin();
        ascensionProof.OnWin();
        ascensionProof.OnWin();  // 连续5场胜利
        
        var context2 = new BattleContext();
        ascensionProof.OnBattleStart(context2);
        Console.WriteLine($"护盾层数: {context2.ShieldLayers}");
        Console.WriteLine($"状态: {ascensionProof.GetStatusDescription()}");
        
        // 漫游者之心效果：攻击倍率
        Console.WriteLine("\n=== 漫游者之心效果 ===");
        var fastTime = TimeSpan.FromSeconds(0.3);
        var slowTime = TimeSpan.FromSeconds(1.5);
        
        double multiplier1 = wandererHeart.GetAttackMultiplier(fastTime);
        double multiplier2 = wandererHeart.GetAttackMultiplier(slowTime);
        
        Console.WriteLine($"快速操作(0.3秒): 攻击倍率 {multiplier1:F1}倍");
        Console.WriteLine($"缓慢操作(1.5秒): 攻击倍率 {multiplier2:F1}倍");
    }
    
    /// <summary>
    /// 示例6: 骰子的特殊机制
    /// </summary>
    public void Example6_SpecialDiceMechanics()
    {
        ItemFactory.Initialize();
        
        // 飞羽骰的计数器机制
        Console.WriteLine("=== 飞羽骰的计数器机制 ===");
        var feathered = new FeatheredDice();
        Console.WriteLine($"初始计数器: {feathered.Counter}");
        
        int avoidance = feathered.RollWithATKP(5);  // 模拟5点攻击
        Console.WriteLine($"使用后的闪避值: {avoidance}");
        Console.WriteLine($"使用后的计数器: {feathered.Counter}");
        
        // 刮痧师傅的多轮伤害
        Console.WriteLine("\n=== 刮痧师傅骰 ===");
        var guasha = new GuaShaParquetDice();
        int roll = guasha.Roll();
        Console.WriteLine($"掷出的攻击力: {roll}");
        
        // 春风骰的下一骰影响
        Console.WriteLine("\n=== 春风骰的影响机制 ===");
        var springBreeze = new SpringBreezeDice();
        int springRoll = springBreeze.Roll();
        Console.WriteLine($"春风掷出: {springRoll}点");
        Console.WriteLine($"这会减少下一个骰子的计数器{springRoll}点");
    }
    
    /// <summary>
    /// 示例7: 检查物品是否存在
    /// </summary>
    public void Example7_CheckItemRegistration()
    {
        ItemFactory.Initialize();
        
        var testIds = new[] { "d6_dice", "my_custom_dice", "health_potion" };
        
        Console.WriteLine("物品注册状态:");
        foreach (var id in testIds)
        {
            bool isRegistered = ItemFactory.IsItemRegistered(id);
            Console.WriteLine($"  {id}: {(isRegistered ? "已注册" : "未注册")}");
        }
    }
    
    /// <summary>
    /// 示例8: 物品克隆
    /// </summary>
    public void Example8_ItemCloning()
    {
        ItemFactory.Initialize();
        
        // 创建原始物品
        var original = ItemFactory.Create("d6_dice") as Dice;
        original.Attack = 10;
        
        // 克隆物品
        var cloned = original.Clone() as Dice;
        
        Console.WriteLine($"原始物品攻击力: {original.Attack}");
        Console.WriteLine($"克隆物品攻击力: {cloned.Attack}");
        
        // 修改克隆品
        cloned.Attack = 20;
        
        Console.WriteLine($"修改后的原始物品攻击力: {original.Attack}");
        Console.WriteLine($"修改后的克隆物品攻击力: {cloned.Attack}");
    }
    
    /// <summary>
    /// 示例9: 物品堆栈操作
    /// </summary>
    public void Example9_ItemStackOperations()
    {
        ItemFactory.Initialize();
        
        // 创建堆栈
        var stack = ItemFactory.CreateItemStack("gold_coin", 50);
        
        Console.WriteLine($"初始数量: {stack.Quantity}");
        Console.WriteLine($"堆叠上限: {stack.Item.MaxStackSize}");
        
        // 添加物品
        int remaining = stack.AddQuantity(100);  // 尝试添加100个
        Console.WriteLine($"添加100个后的数量: {stack.Quantity}");
        Console.WriteLine($"无法添加的数量: {remaining}");
        
        // 移除物品
        bool success = stack.RemoveQuantity(30);
        Console.WriteLine($"移除30个成功: {success}");
        Console.WriteLine($"移除后的数量: {stack.Quantity}");
        
        // 检查是否可以继续堆叠
        Console.WriteLine($"可以继续堆叠: {stack.CanStack}");
    }
    
    /// <summary>
    /// 示例10: 创建自定义物品（扩展示例）
    /// </summary>
    public void Example10_CustomItemCreation()
    {
        // 这个示例展示如何添加新物品
        // 步骤 1: 创建自定义骰子类 (参考 Dices/D6Dice.cs)
        
        /*
        // 创建 Dices/MySpecialDice.cs:
        public class MySpecialDice : Dice
        {
            public MySpecialDice() : base("my_special_dice", "我的特殊骰", "描述", DiceUsageType.Active)
            {
                DisplayColor = Color.Purple;
            }
            
            public override int Roll() { return new Random().Next(1, 9); }
            public override ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders) { /* ... */ }
            public override Item Clone() { return new MySpecialDice(); }
        }
        */
        
        // 步骤 2: 在 InventoryManager.cs 中注册
        // _registry.RegisterItem("my_special_dice", () => new MySpecialDice());
        
        // 步骤 3: 使用
        // ItemFactory.Initialize();
        // var myDice = ItemFactory.Create("my_special_dice");
        
        Console.WriteLine("查看 Example10_CustomItemCreation 方法中的注释来了解如何添加自定义物品");
    }
    
    /// <summary>
    /// 运行所有示例
    /// </summary>
    public void RunAllExamples()
    {
        Console.WriteLine("========== 物品系统示例 ==========\n");
        
        Console.WriteLine("示例1: 基础初始化");
        Console.WriteLine("------------------------");
        Example1_BasicInitialization();
        
        Console.WriteLine("\n示例2: 查询可用物品");
        Console.WriteLine("------------------------");
        Example2_QueryAvailableItems();
        
        Console.WriteLine("\n示例3: 创建玩家装备");
        Console.WriteLine("------------------------");
        Example3_CreatePlayerEquipment();
        
        Console.WriteLine("\n示例7: 检查物品注册");
        Console.WriteLine("------------------------");
        Example7_CheckItemRegistration();
        
        Console.WriteLine("\n示例8: 物品克隆");
        Console.WriteLine("------------------------");
        Example8_ItemCloning();
        
        Console.WriteLine("\n示例9: 物品堆栈操作");
        Console.WriteLine("------------------------");
        Example9_ItemStackOperations();
        
        Console.WriteLine("\n===============================");
        Console.WriteLine("所有示例运行完成！");
    }
}
