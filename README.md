# Eon Vientiane

## 当前定位

本仓库当前采用“服务端签发加密逻辑包 + 客户端动态加载 DLL”的模块化架构。

文档以当前共识为准：

- 所有功能模块与道具模块都应是**平级独立项目**。
- 服务端负责为玩家签发加密 DLL。
- 客户端只依赖**统一入口约定**，不依赖道具内部实现。
- **任何组件之间均不存在必要依赖**。
- 不允许通过 `BaseItem`、`BaseDice`、`BaseAccessory` 一类强制基类建立耦合。

## 当前已实现的核心架构

### 1. 远程运行时总契约

客户端顶层运行时使用统一契约 `IRemoteGameRuntime`，定义于 [EonVientiane.Core/Models/RemoteRuntimeContract.cs](EonVientiane.Core/Models/RemoteRuntimeContract.cs#L1-L15)。

该契约仅用于“顶层运行时”，不要求每个道具项目都实现它。

### 2. 逻辑包封装与加载

- 逻辑包信封定义于 [EonVientiane.Core/Models/LogicPackageEnvelope.cs](EonVientiane.Core/Models/LogicPackageEnvelope.cs#L1-L29)
- 逻辑包加载服务位于 [EonVientiane.Core/Services/LogicPackageService.cs](EonVientiane.Core/Services/LogicPackageService.cs)
- 模块同步服务位于 [EonVientiane.Core/Services/ModuleSyncService.cs](EonVientiane.Core/Services/ModuleSyncService.cs)

当前流程为：

1. 服务端生成 DLL 逻辑包。
2. 使用用户公钥加密 AES 密钥。
3. 对逻辑包整体签名。
4. 客户端下载后校验签名、解密内容并热加载。

### 3. 当前模块发现方式

当前模块之间主要依赖“反射约定”，而不是编译期强依赖。

例如：

- `CanHandleCommand(string command)`
- `ExecuteCommand(IDictionary<string, object> state, string command, string[] args)`
- `GetHelpText()`

相关实现可见：

- [EonVientiane.PlayerModule/PlayerRuntime.cs](EonVientiane.PlayerModule/PlayerRuntime.cs#L194-L244)
- [EonVientiane.InventoryModule/InventoryRuntime.cs](EonVientiane.InventoryModule/InventoryRuntime.cs#L1-L175)
- [EonVientiane.EquipmentModule/EquipmentCatalog.cs](EonVientiane.EquipmentModule/EquipmentCatalog.cs#L1-L70)
- [EonVientiane.LevelModule/LevelCatalog.cs](EonVientiane.LevelModule/LevelCatalog.cs)

## 当前项目结构

### 已存在项目

- `EonVientiane.Core`
- `EonVientiane.CLI`
- `EonVientiane.Server`
- `EonVientiane.PlayerModule`
- `EonVientiane.InventoryModule`
- `EonVientiane.EquipmentModule`
- `EonVientiane.LevelModule`
- `EonVientiane.EffectModule`
- `EonVientiane.BattleModule`
- `EonVientiane.AchievementModule`
- `EonVientiane.AchievementConnectionModule`
- `EonVientiane.AchievementStatusModule`

### 当前职责概览

| 项目 | 职责 |
|------|------|
| [EonVientiane.Core](EonVientiane.Core) | 逻辑包模型、远程运行时契约、账户与加密服务 |
| [EonVientiane.Server](EonVientiane.Server) | 模块签发、签名、加密、同步接口 |
| [EonVientiane.PlayerModule](EonVientiane.PlayerModule) | 当前客户端主运行时、共享状态、模块分发 |
| [EonVientiane.InventoryModule](EonVientiane.InventoryModule) | 背包状态与展示 |
| [EonVientiane.EquipmentModule](EonVientiane.EquipmentModule) | 装备命令转发 |
| [EonVientiane.LevelModule](EonVientiane.LevelModule) | 关卡命令 |
| [EonVientiane.EffectModule](EonVientiane.EffectModule) | 战斗效果存储区读写与作用域键管理 |
| [EonVientiane.BattleModule](EonVientiane.BattleModule) | 战斗生命周期、回合与伤害结算宿主 |
| [EonVientiane.AchievementModule](EonVientiane.AchievementModule) | 成就相关逻辑 |

## 战斗系统的目标规则

以下规则为当前确认后的战斗设计目标。

### 1. 战斗共享变量

战斗过程中，只有以下变量属于公共战斗约定：

- `HP`
- `ATKP`

除此以外的变量均视为**道具自己的内部变量**，战斗宿主不直接理解其含义。

### 2. 效果系统

除 `HP` 与 `ATKP` 之外，还需要一个“效果系统”。

效果系统的本质是：**战斗过程中允许道具跨回合读写的变量存储区**。

该系统用于：

- 保存饰品或骰子在本场战斗中写入的状态
- 支持跨回合持续效果
- 支持延迟结算效果
- 支持道具之间通过宿主转交共享状态

效果系统本身不解释变量语义，只负责提供稳定的存取区域。

也就是说：

- `HP`、`ATKP` 是公共战斗变量
- 效果存储区是公共宿主能力
- 具体效果键值的业务含义仍由道具自己决定

### 3. 玩家基础状态

战斗前，玩家没有固有属性。

玩家在战斗中的任何属性，都应由已装备道具提供。

### 4. 道具分类

道具只分为两类：

- 饰品
- 骰子

骰子再分为：

- `AD`：主动骰子
- `PD`：被动骰子

一个骰子可以同时具备 `AD` 与 `PD` 两种能力。

### 5. 战斗流程

1. 随机决定先后手。
2. 加载所有饰品的战前效果。
3. 当前行动方在自己的回合可使用 `AD`。
4. 所有可指定对象的行动均可指定任意对象，包括自身。
5. 若某单位被施加 `ATKP`，则该单位进入被动回合。
6. 该单位可使用 `PD` 对 `ATKP` 进行处理。
7. `PD` 处理后得到伤害值。
8. 所有效果均可在结算过程中读写效果存储区。
9. 结算伤害并检查失败。

### 6. 失败条件

任一单位满足以下任一条件即失败：

- 在受到伤害前不存在 `HP`
- 在受到伤害后 `HP <= 0`

## 架构约束

### 1. 道具必须是独立项目

每个饰品或骰子都应作为与其他模块平级的独立项目存在。

示例：

- `EonVientiane.Item.Accessory.BasicHp`
- `EonVientiane.Item.Accessory.Thorns`
- `EonVientiane.Item.Dice.D6`
- `EonVientiane.Item.Dice.CounterShield`

### 2. 不允许存在强制基类

以下方向不符合当前架构原则：

- 必须继承 `BaseItem`
- 必须继承 `BaseDice`
- 必须继承 `BaseAccessory`
- 必须引用单独的“物品 SDK”后才能被宿主识别

原因：这些设计会形成“必要依赖”，违反组件独立规则。

### 3. 允许的耦合形式

允许的只有“约定耦合”：

- 约定的程序集命名
- 约定的导出类型名
- 约定的静态方法名
- 约定的输入输出数据结构

宿主只检查这些约定是否存在，不关心内部实现。

## 推荐的后续模块划分

### 必需模块

#### `EonVientiane.BattleModule`

职责：

- 战斗初始化
- 先后手随机
- 饰品战前效果加载
- 效果存储区生命周期管理
- `AD` 行动处理
- `PD` 响应处理
- `ATKP` 到伤害的结算
- 失败判定

#### `EonVientiane.EffectModule`

职责：

- 提供战斗效果存储区
- 提供跨回合变量读写能力
- 提供作用域管理（如战斗级、单位级、来源道具级）
- 提供效果清理时机

该模块不解释效果内容，只管理存储和访问约定。

#### `EonVientiane.BattleHostModule` 或对 `PlayerModule` 扩展

职责：

- 扫描当前已加载的道具 DLL
- 根据约定识别“饰品模块”和“骰子模块”
- 在正确阶段调用正确入口
- 将战斗公共变量与效果存储区传递给道具模块

建议优先独立为 `BattleHostModule`，避免把 [EonVientiane.PlayerModule/PlayerRuntime.cs](EonVientiane.PlayerModule/PlayerRuntime.cs) 继续扩大。

#### 服务端物品注册/签发能力

当前服务端采用硬编码签发模块，见 [EonVientiane.Server/Program.cs](EonVientiane.Server/Program.cs#L16-L91)。

后续需要增加以下能力之一：

- 物品注册表
- 配置式物品清单
- 自动扫描并登记物品 DLL

否则无法支持“一个物品一个独立项目”的发放模型。

### 独立道具项目

每个具体道具都是一个独立项目，由服务端签发给客户端。

宿主只要求它们满足同一组导出约定。

## 最小接口约定方向

当前尚未落地正式战斗道具约定，但建议保持以下原则：

### 饰品模块建议暴露的能力

- `GetMetadata()`
- `OnBattleStart(...)`
- `OnOwnerTurnStart(...)`
- `OnOwnerTurnEnd(...)`
- `OnBeforeDamageTaken(...)`
- `ReadEffect(...)`
- `WriteEffect(...)`

### 骰子模块建议暴露的能力

- `GetMetadata()`
- `CanUseActive(...)`
- `ExecuteActive(...)`
- `CanUsePassive(...)`
- `ExecutePassive(...)`
- `ReadEffect(...)`
- `WriteEffect(...)`

### 效果系统建议能力

效果系统建议至少支持以下访问方式：

- 按战斗读取
- 按单位读取
- 按来源道具读取
- 设置值
- 删除值
- 枚举指定作用域下的键

建议效果键使用字符串，值使用可序列化对象或 JSON 字符串。

### 返回值原则

建议统一返回可序列化数据：

- `string`
- `bool`
- `Dictionary<string, object>`
- JSON 字符串

这样更适合当前基于反射与动态加载的宿主结构。

## 当前状态总结

### 已完成

- 账户与加密逻辑包体系
- 服务端签发与客户端热加载
- 基础模块化命令分发
- 成就模块下发流程
- 独立战斗模块（`EonVientiane.BattleModule`）
- 独立效果模块（`EonVientiane.EffectModule`）

### 尚未完成

- 独立物品发现机制
- 物品级别的服务端注册与签发
- 战斗用统一约定数据结构

## 构建

### 构建整个解决方案

```bash
dotnet build EonVientiane.slnx
```

### 运行 CLI

```bash
dotnet run --project EonVientiane.CLI/EonVientiane.CLI.csproj
```

### 运行 Server

```bash
dotnet run --project EonVientiane.Server/EonVientiane.Server.csproj
```

## 文档说明

旧的说明文档已移除，当前仓库仅保留本文件作为最新架构说明。

后续若开始实现战斗系统，应继续在本文件基础上更新，不再新增相互冲突的并行说明文档。
