# 游戏主体系统说明

## 概述
已实现完整的游戏主体回合制对战系统，支持两个队伍的玩家对战，包含饰品效果初始化、随机回合顺序、行动与防御、伤害计算及效果处理。

## 核心类架构

### 1. Player.cs - 玩家类
代表单个游戏玩家，管理其属性和装备。

**关键属性：**
- `PlayerId`: 玩家唯一标识
- `PlayerName`: 玩家名称
- `Camp`: 所属阵营（Team1 或 Team2）
- `CurrentHP / MaxHP`: 当前/最大生命值
- `ShieldLayers`: 护盾层数（可以抵挡一次伤害）
- `EquippedItems`: 装备的物品（骰子、饰品等）
- `ActiveEffects`: 当前生效的增益减益效果
- `TurnOrder`: 回合顺序（越小越先行动）
- `HasActedThisRound`: 是否已在本回合行动过

**关键方法：**
- `AddEquipment(equipment)`: 添加装备
- `GetEquippedDice()`: 获取所有装备的骰子
- `GetEquippedAccessories()`: 获取所有装备的饰品
- `AddEffect(effect)`: 添加效果
- `UpdateEffects()`: 更新所有效果的持续时间
- `TakeDamage(damage)`: 受到伤害（护盾优先抵挡）
- `Heal(amount)`: 恢复生命值
- `IsDead`: 检查是否已死亡

### 2. GameEffect.cs - 效果系统
表示游戏中的各种增益/减益效果。

**效果基类 GameEffect：**
- `Name / Description`: 效果名称和描述
- `EffectType`: 效果类型（增益/减益）
- `DurationRemaining`: 剩余持续时间（回合数）
- `IsExpired`: 是否已过期
- `Update()`: 每回合更新，减少持续时间
- `ApplyEffect(player)`: 应用效果到玩家

**内置效果类型：**

1. **DamageOverTimeEffect** - 持续伤害
   - 每回合造成指定伤害

2. **HealOverTimeEffect** - 持续治疗
   - 每回合恢复指定生命值

3. **ShieldEffect** - 护盾效果
   - 增加护盾层数
   - 移除时减少护盾层数

4. **StatBoostEffect** - 属性增强
   - 临时增加攻击/防御/速度

5. **StatDebuffEffect** - 属性削弱
   - 临时降低属性

6. **StunEffect** - 眩晕效果
   - 标记使玩家无法行动

7. **PoisonEffect** - 毒性效果
   - 每回合伤害递增

8. **ImmunityEffect** - 免疫效果
   - 免疫指定类型的效果

### 3. Item.cs - 骰子战斗逻辑补充

**Dice 基类扩展：**
```csharp
public virtual ActionResult ExecuteActiveAction(Player attacker, List<Player> defenders)
public virtual DefenseResult ExecutePassiveAction(Player defender, int attackDamage)
```

**D6 骰子（六面骰子）：**
- **主动使用 (AD)**：
  - Roll出1-6，作为 ATKP（攻击点数）
  - 随机选择一个对方玩家作为目标
  
- **被动使用 (PD)**：
  - Roll出1-6，作为 DEFP（防御点数）
  - 如果 ATKP ≤ DEFP：完全防御，实际伤害 = 0
  - 如果 ATKP > DEFP：实际伤害 = ATKP - DEFP

**飞羽（Feathered Dice）：**
- **仅被动使用 (PD)**：
  - 为 (Counter + ATKP × 2) 面的骰子
  - Roll出1至该数值，作为 AVOP（闪避点数）
  - 如果 ATKP > AVOP：闪避成功，实际伤害 = 0
  - 如果 ATKP ≤ AVOP：闪避失败，实际伤害 = ATKP（全部伤害）
  - 每次使用后 Counter + 1（临时增加）
  - 战斗结束后 Counter 重置

### 4. Battle.cs - 战斗管理器

核心游戏逻辑控制中心。

**战斗状态 (BattleState)：**
- `Idle`: 空闲
- `Initialization`: 初始化阶段
- `RoundStart`: 回合开始
- `PlayerAction`: 玩家行动
- `DefenseResponse`: 防守响应
- `EffectCalculation`: 效果计算
- `RoundEnd`: 回合结束
- `BattleEnd`: 战斗结束

**关键属性：**
- `AllPlayers`: 所有参与者
- `Team1Players / Team2Players`: 按阵营的玩家列表
- `CurrentState`: 当前战斗状态
- `CurrentRound`: 当前回合数
- `BattleLog`: 战斗日志
- `IsBattleOver`: 是否战斗结束
- `WinnerCamp`: 赢家阵营

## 游戏流程详解

### 初始化阶段 (Initialization)

1. **应用饰品效果**
   ```
   对每个玩家：
   - 获取所有装备的饰品
   - 调用每个饰品的 OnBattleStart() 方法
   - 饰品可以修改 BattleContext（HP、护盾等）
   - 根据 BattleContext 设置玩家的 MaxHP 和初始HP
   ```

2. **随机决定回合顺序**
   ```
   - 使用 Fisher-Yates 洗牌算法
   - 为每个玩家分配 TurnOrder（0-N）
   - 按 TurnOrder 排序生成 _turnOrder 列表
   ```

### 每个回合的流程

#### Step 1: 回合开始 (RoundStart)
```
- 输出 "第X回合开始" 日志
- 重置所有玩家的 HasActedThisRound 和 IsWaitingForDefense
```

#### Step 2: 玩家行动 (PlayerAction)
```
对 _turnOrder 中的每个玩家：
1. 检查是否被眩晕（HasStunEffect）
   - 如果是，输出 "XXX被眩晕，无法行动" 并跳过

2. 检查是否已死亡
   - 如果是，输出 "XXX已死亡，无法行动" 并跳过

3. 获取对方玩家列表（过滤已死亡）

4. 执行行动：
   a. 获取第一个可用的主动骰子 (AD)
   b. 如果没有，输出 "XXX没有可用的主动骰子" 并跳过
   c. 调用骰子的 ExecuteActiveAction()：
      - D6: Roll出 ATKP，随机选择目标
      - 返回 ActionResult (目标玩家、攻击点数)
   
   d. 对被指定玩家执行防御：
      - 获取第一个可用的被动骰子 (PD)
      - 如果没有，直接受到全部伤害
      - 调用骰子的 ExecutePassiveAction()：
         * D6: Roll出 DEFP，计算 实际伤害 = ATKP - DEFP
         * Feathered: Roll出 AVOP，基于对比计算闪避
      - 返回 DefenseResult (实际伤害)
   
   e. 造成伤害：
      - 调用目标玩家的 TakeDamage(actualDamage)
      - 护盾优先抵挡（护盾层数 -1）
      - 没有护盾则直接扣血
      - 如果 HP ≤ 0，标记为已死亡
```

#### Step 3: 效果计算 (EffectCalculation)
```
对所有玩家：
1. 遍历其 ActiveEffects 列表
2. 对每个效果调用 ApplyEffect(player)
   - 持续伤害：扣血
   - 持续治疗：加血
   - 毒性：递增伤害
   - 其他特殊效果
3. 调用 UpdateEffects()：
   - 对每个效果调用 Update()
   - DurationRemaining--
   - 移除已过期的效果
```

#### Step 4: 回合结束 (RoundEnd)
```
1. 检查是否有阵营全灭：
   - 如果 Team1 全灭 → Team2 获胜 → 结束战斗
   - 如果 Team2 全灭 → Team1 获胜 → 结束战斗
2. 否则进入下一回合
```

### 战斗结束
```
- 设置 IsBattleOver = true
- 记录赢家阵营
- 更新飞升之证饰品的计数器：
  * 赢家计数器 +1（每5场胜利+1）
  * 输家重置连续胜利计数
```

## 饰品效果详解

### 自我 (SelfAccessory)
```
对局开始时提供100HP
- 检查 BattleContext.CanGainHP
- 如果为 true，PlayerHP += 100
- 某些饰品可能禁用HP获取（如飞升之证）
```

### 飞升之证 (AscensionProofAccessory)
```
1. 初始化：
   - 强制 HP = 0
   - 设置 CanGainHP = false（禁用其他HP获取）
   - 获得 Counter 数量的护盾层数

2. 战斗过程：
   - 每层护盾可以抵挡一次伤害
   - 护盾层数递减

3. 战斗结束：
   - 连续5场胜利 → Counter + 1
   - 失败 → 重置连续胜利计数
```

## 代码示例

### 创建玩家及初始化战斗
```csharp
var battle = new Battle();

// 创建玩家
var player1 = new Player("p1", "玩家1", PlayerCamp.Team1);
var player2 = new Player("p2", "玩家2", PlayerCamp.Team2);

// 添加装备
player1.AddEquipment(new D6Dice(DiceUsageType.Both));
player1.AddEquipment(new SelfAccessory());

player2.AddEquipment(new FeatheredDice());
player2.AddEquipment(new AscensionProofAccessory());

// 添加到战斗
battle.AddPlayer(player1);
battle.AddPlayer(player2);

// 初始化并启动
battle.InitializeBattle();

// 每帧更新
while (!battle.IsBattleOver)
{
    battle.Update();
}
```

### 添加自定义效果
```csharp
// 持续伤害效果（3回合，每回合10点伤害）
var dotEffect = new DamageOverTimeEffect("中毒", "受到持续伤害", 10, 3);
player.AddEffect(dotEffect);

// 属性增强效果
var boostEffect = new StatBoostEffect("狂暴", "攻击力提升", 20, 0, 0, 2);
player.AddEffect(boostEffect);
```

## 与 Game1 的集成

### 战斗菜单
- 在左侧菜单中添加"战斗"按钮
- 点击触发 `InitializeBattle()`
- 创建两个队伍各2个测试玩家

### 战斗 UI 显示
- 战斗回合数
- 战斗日志（实时滚动）
- 所有玩家的实时状态（HP、护盾、已死亡标记）
- 战斗结束时显示胜负结果

### 集成点
- `Update()`: 每帧调用 `_currentBattle.Update()`
- `Draw()`: 调用 `DrawBattlePanel()` 绘制战斗UI
- 日志滚动：支持鼠标滚轮滚动查看历史日志

## 扩展指南

### 添加新的骰子类型
1. 继承 `Dice` 类
2. 实现 `Roll()` 方法
3. 覆写 `ExecuteActiveAction()` 或 `ExecutePassiveAction()`
4. 添加到 `AddPlayerEquipment()` 中

### 添加新的饰品效果
1. 继承 `Accessory` 类
2. 实现 `OnBattleStart(BattleContext context)` 
3. 可选：覆写 `GetProvidedHP()` 或其他方法
4. 添加到测试玩家中

### 添加新的游戏效果
1. 继承 `GameEffect` 类
2. 实现 `ApplyEffect(Player player)` 方法
3. 可选：覆写 `Update()` 或 `OnRemove()`
4. 在战斗逻辑中创建和应用

### 自定义行动规则
修改 `Battle.ExecutePlayerAction()` 或 `ExecuteDefense()` 方法，可以：
- 改变目标选择逻辑
- 添加跳过条件
- 实现其他行动类型

## 已知限制与改进空间

### 当前实现
- 自动选择第一个可用骰子（不支持玩家选择）
- 自动随机选择目标（不支持指定目标）
- 基于 D6 和飞羽的简单对抗系统

### 可优化的方向
1. **玩家交互**：实现骰子和目标的选择菜单
2. **高级骰子**：实现更多复杂骰子类型
3. **技能系统**：添加非骰子的特殊技能
4. **队伍配置**：允许玩家自定义队伍和装备
5. **AI**：实现 AI 对手的智能决策
6. **战斗存档**：保存和恢复战斗状态
7. **多人模式**：支持真正的玩家对玩家对战

## 文件列表

- `Player.cs` - 玩家类，管理属性和装备
- `GameEffect.cs` - 效果系统，包括8种内置效果
- `Item.cs` - 骰子类的战斗逻辑补充
- `Battle.cs` - 战斗管理器，核心游戏循环
- `Game1.cs` - 集成战斗系统到主游戏窗口
- `GameEnums.cs` - 添加 ContentView.Battle 枚举

## 战斗日志示例

```
=== 战斗开始 ===
应用饰品效果...
玩家1的自我发动效果
玩家1 HP: 100, 护盾: 0
玩家2的飞升之证发动效果
玩家2 HP: 0, 护盾: 5
回合顺序已随机
  玩家1 - 顺序1
  玩家2 - 顺序2

=== 第1回合开始 ===
玩家1使用D6攻击玩家2
  攻击点数: 4
D6掷出2点，闪避失败！受到全部4点伤害
玩家2受到4点伤害，当前HP: -4
玩家2已被击败！

=== 战斗结束 ===
Team1阵营获胜！
```
