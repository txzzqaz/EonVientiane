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
- `EonVientiane.NetworkBattleModule`
- `EonVientiane.Item.Accessory.Self`
- `EonVientiane.Item.Dice.D6`
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
| [EonVientiane.NetworkBattleModule](EonVientiane.NetworkBattleModule) | 局域网房间、进房准备、分组与 PVP 启动编排 |
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
3. 当前行动方进入主动回合，需选择一个 `AD` 骰子并指定目标。
4. 所有可指定对象的行动均可指定任意对象，包括自身。
5. 若某单位被施加 `ATKP`，则该单位进入被动回合。
6. 被攻击单位在被动回合可选择一个 `PD` 骰子处理 `ATKP`，或使用 `battle pass` 直接承受 `ATKP` 伤害。
7. 被动处理后得到伤害值。
8. 被动结算后，该单位直接进入主动回合。
9. 所有效果均可在结算过程中读写效果存储区。
10. 结算伤害并检查失败。

战斗内命令约定（当前实现）：

- `battle active` 为统一行动指令：
	- 主动回合：`battle active <目标> <主动骰子名>`
	- 被动回合：`battle active <被动骰子名>`
- `battle pass`：
	- 主动回合：跳过当前回合。
	- 被动回合：不使用 `PD`，直接将 `ATKP` 转化为伤害。

### 6. 失败条件

任一单位满足以下任一条件即失败：

- 在受到伤害前不存在 `HP`
- 在受到伤害后 `HP <= 0`

### 8. 局域网房间流程（新增）

网络对战通过独立模块 `EonVientiane.NetworkBattleModule` 提供房间流程，命令入口为 `lan`：

- `lan create [房间名] [阵型]`：创建局域网房间
- `lan join <房间ID>`：进入房间
- `lan ready [on|off]`：准备 / 取消准备
- `lan group <组号>`：设置分组（如 `A/B`）
- `lan start`：由房主在“全员已准备 + 至少两个分组”时启动 `pvp`

启动时模块会将房间分组映射为阵型（如 `2v2`），并通过 `BattleApi.StartSession(state, "pvp", formation)` 进入战斗。

### 7. 对战接口统一原则（已落地）

- 战斗由业务模块发起，不再由用户直接使用 `battle start`。
- 统一启动接口为 `BattleApi.StartSession(IDictionary<string, object> state, string mode, string? formation)`。
- 例如 `LevelModule` 在 `loadlevel` 时调用 `BattleApi.StartSession(..., "level", formation)` 进入战斗。
- 关卡敌人被视作“特殊账号”，具备独立公开战斗变量（如 `HP`、`ATKP`）与自动行动条件。
- 本地关卡与网络对战的核心差异仅在敌方决策来源：
	- `level`：敌方依据关卡条件自动判定行动
	- `pvp`：敌方由远端玩家自主选择行动

在可见性边界上，客户端对敌方遵循最小可见原则：

- 仅可读取敌方公开战斗变量（尤其 `HP`）与效果区结果
- 不可读取敌方道具表
- 仅可写入敌方效果区或发送 `ATKP`

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

### 3.1 禁止使用高度耦合方法（新增注意事项）

以下实现被视为高度耦合，明确禁止：

- 在战斗宿主中内置“策略决策”逻辑（例如默认选骰、按固定规则自动补攻击值、按模式硬编码行为差异）
- 宿主通过读取某个关卡或道具的私有业务字段来决定行动
- 为某个具体关卡或道具增加专用分支判断（特判）

应采用的方式：

- 宿主只负责流程推进、接口调用、结果合并与结算
- 所有行动决策（选目标、选骰、是否跳过、攻击值策略）必须由行动主体自身 Runtime 提供
- 宿主与模块间仅通过约定输入输出交互，不共享业务内部状态模型

### 4. 装备数据传递约束（新增）

装备数据在模块间传递时，必须使用“可序列化字典/JSON 对象”约定（如 `Dictionary<string, object>` 或 JSON 对象数组），不得定义或依赖跨模块共享的固定装备类型（DTO/record/class）。

约束目标：

- 防止 `InventoryModule`、`EquipmentModule`、`BattleModule` 之间形成编译期类型耦合
- 允许各模块独立演进字段（新增/缺省字段不破坏加载）
- 保持“宿主仅检查约定字段，不依赖内部实现”的原则

当前约定字段（最小集合）建议包含：`Id`、`Name`、`Slot`；可选字段可按需扩展（如 `Kind`、`AccessorySlotCost`）。

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

### 虚拟玩家行动约定（新增）

战斗宿主不负责行动决策；非本地行动方应由外部 Runtime 作为“虚拟玩家”给出行动指令。

推荐导出：

- `DecideBattleAction(Dictionary<string, object> context)`
- 兼容别名：`GetBattleAction(...)`、`DecideAction(...)`

返回值为 `Dictionary<string, object>`，建议字段：

- `action`: `active` 或 `pass`
- `target`: 目标 `unitId` 或显示名（可选）
- `requestedDiceName` / `dice` / `diceName` / `itemId`: 回合要使用的骰子标识（主动/被动回合均可使用）
- `attack` 或 `ATKP`: 直接给定本回合攻击值（可选）

建议同时读取 `context["phase"]`（`active` / `passive`）来决定选择何种骰子。

若未提供有效行动指令，宿主将视为“自动方本回合跳过”。

### 通用 Hook 约定（新增）

为避免“某个道具专属特判”，战斗宿主支持道具通过反射注册通用 Hook：

- `OnBattleHook(Dictionary<string, object> context)`
- 或 `OnHook(Dictionary<string, object> context)`（兼容别名）

当前宿主已接入“函数调用级”钩子事件：

- `hook.eventName = "function.invoke"`
- `hook.stage = "before" | "after"`
- `hook.elapsedMs = 当前行动方本回合已过去毫秒`
- `targetCall.methodName = 被调用函数名`
- `targetCall.itemId/name/kind = 目标道具信息（若存在）`
- `arguments = 调用参数数组`

其中命令闸门也统一映射为“合成函数调用”：

- `BattleCommand.active`
- `BattleCommand.pass`

道具 Hook 返回值建议为 `Dictionary<string, object>`，可包含：

- `cancel: bool`：取消本次命令
- `forcePass: bool`：强制将当前命令转为“跳过回合”
- `skipOriginal: bool`（或 `skip`）：跳过原函数执行
- `result` / `overrideResult`：覆写函数返回值
- `message: string`：写入战斗日志
- `effects: []`：按效果模块约定写入效果存储区

这意味着“对方每步超时自动跳过”应由道具在 Hook 中根据 `elapsedMs` 自主决策，而不是由宿主内置固定道具逻辑。

此外，宿主还暴露了“假设目标道具函数 Hook”入口：

- `BattleApi.InvokeAssumedItemFunctionHook(...)`

可用于“假设对方存在某道具，并 Hook 到该道具某函数”的场景；上下文会在 `extra.assumedTarget` 中标记假设目标。

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
- 独立网络对战模块（`EonVientiane.NetworkBattleModule`，支持房间/准备/分组）
- 独立效果模块（`EonVientiane.EffectModule`）
- 函数调用级通用 Hook（before/after，支持返回值覆写与跳过原函数）
- 假设目标道具函数 Hook 入口（用于虚拟/假设目标拦截）
- 统一战斗入口（`mirror / level / pvp`）
- 关卡敌方账号化（关卡敌人以特殊账号形式参与同一战斗接口）
- 战斗上下文中敌方道具表默认隐藏，仅暴露公开战斗变量与效果能力
- 战斗结束后自动上报服务端签验并返回客户端本地存储
- 成就签验请求可携带客户端近几场已签验战斗记录，服务端会进行哈希与签名复核后读取用于条件认证
- 背包不再设容量上限（仅展示当前数量）
- 装备规则已落地：最多装备 `8` 个骰子；饰品采用槽位制，默认最多 `12` 槽，且允许饰品使用负槽位（提供额外槽位）
- 装备跨模块传递改为字典/JSON 对象约定，已移除固定装备类型依赖
- 首个独立骰子道具模块 `EonVientiane.Item.Dice.D6`（`D6`）已实现并接入服务端签发链路
- 首个独立饰品道具模块 `EonVientiane.Item.Accessory.Self`（`自我`，饰品槽消耗 `2`，战斗开始提供 `10HP`）已实现并接入服务端签发链路

### 战斗签验链路（当前实现）

1. `BattleModule` 在战斗结束时生成战斗过程记录（含 battleId、回合、单位快照、日志）。
2. `PlayerModule` 读取该记录并调用服务端 `POST /api/logic/battle/verify`。
3. 服务端对记录进行基础结构校验，计算记录哈希并使用服务端私钥签名。
4. 服务端返回签验结果（记录原文 + 哈希 + 签名），并在服务端 `logic-store/battle-records/<userId>/` 留存。
5. 客户端将签验结果落盘到本地 `.../LogicPackages/battle-records/` 目录。

### 成就签验读取战斗记录链路（当前实现）

1. 客户端发起成就签验时，会从本地 `.../LogicPackages/battle-records/` 读取近几场战斗签验结果并随请求上送。
2. 服务端仅接受与当前 `userId` 一致且可通过“记录哈希 + 服务端公钥验签”的战斗记录。
3. 通过复核的记录可被服务端用于成就条件认证（当前能力已就绪，具体规则可在后续成就模块中扩展）。

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
