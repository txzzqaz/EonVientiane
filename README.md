# Eon Vientiane - 游戏重构项目

## 项目概述

这是对 Eon Vientiane 游戏的彻底重构，建立了一个基于 CLI（命令行界面）的游戏客户端。项目采用分层架构，分为核心逻辑层和表现层。

## 项目结构

```
EonVientiane/
├── EonVientiane.Core/          # 核心业务逻辑层
│   ├── Models/                 # 数据模型
│   │   ├── Item.cs            # 基础物品类
│   │   ├── Equipment.cs       # 装备类
│   │   ├── Inventory.cs       # 背包管理系统
│   │   ├── Level.cs           # 关卡类
│   │   └── GameState.cs       # 游戏状态管理
│   └── Services/              # 业务服务
│       ├── GameService.cs     # 游戏主服务
│       └── InventoryService.cs # 背包服务
│
├── EonVientiane.CLI/           # CLI 表现层
│   ├── CommandParser.cs       # 命令解析器
│   ├── GameEngine.cs          # 游戏引擎
│   └── Program.cs             # 程序入口
│
└── EonVientiane.slnx          # 解决方案文件
```

## 核心功能

### 1. 背包系统 (Inventory)
- ✅ **查看背包** - `inv` 命令
- ✅ **穿戴装备** - `equip 物品名` 命令
- ✅ **卸下装备** - `unequip 物品名` 命令
- 支持8个装备槽位（头、胸、腿、脚、手、主手、副手、饰品）
- 自动处理装备互换逻辑

### 2. 关卡系统 (Level)
- ✅ **加载关卡** - `loadlevel 关卡ID` 命令
- ✅ **查看可用关卡** - `levels` 命令
- ✅ **卸载关卡** - `unloadlevel` 命令
- 包含4个示例关卡：test、forest、castle、dragon

### 3. 物品系统 (Item/Equipment)
- 基础物品支持（药水、金币等）
- 装备系统支持（剑、盾、甲等）
- 物品稀有度系统（普通、非凡、稀有、史诗、传奇）
- 装备属性系统（防御、攻击加成）

### 4. 游戏状态管理 (GameState)
- 玩家信息管理
- 等级和经验跟踪
- 游戏状态追踪（空闲、在关卡、战斗中、暂停）

## 命令列表

### 关卡命令
```bash
loadlevel <关卡ID>    # 加载关卡，如: loadlevel test
levels                # 查看可用关卡列表
unloadlevel          # 卸载当前关卡
```

### 背包命令
```bash
inv                   # 查看背包和已穿戴装备
equip <物品名>       # 穿戴装备，如: equip 铁剑
unequip <物品名>     # 卸下装备，如: unequip 铁剑
```

### 游戏命令
```bash
status                # 查看当前游戏状态
help                  # 显示帮助信息
clear                 # 清屏
exit / quit          # 退出游戏
```

## 运行项目

### 构建项目
```bash
cd EonVientiane
dotnet build -c Debug
```

### 运行游戏
```bash
dotnet run --project EonVientiane.CLI/EonVientiane.CLI.csproj -c Debug
```

### 运行测试脚本
```bash
chmod +x test_cli.sh
./test_cli.sh
```

## 示例游戏流程

```
$ dotnet run --project EonVientiane.CLI

欢迎来到 Eon Vientiane 游戏

[等待中]> levels
=== 可用关卡 ===
  • test       - 测试关卡 (难度: 1)
  • forest     - 森林 (难度: 5)
  ...

[等待中]> loadlevel test
✓ 已加载关卡: 测试关卡

[测试关卡]> inv
=== 背包 (6/20) ===
  • 生命药水 x3
  • 金币 x100
  • 铁剑 [MainHand] (攻击+5)
  ...

[测试关卡]> equip 铁剑
✓ 已穿戴: 铁剑

[测试关卡]> inv
=== 背包 (5/20) ===
  • 生命药水 x3
  • 金币 x100
  
=== 已穿戴装备 (1) ===
  • [MainHand] 铁剑 (攻击+5)

[测试关卡]> exit
感謝遊玩! 再見!
```

## 架构设计

### 分层架构
```
┌─────────────────────────────┐
│     CLI 表现层              │
│  (EonVientiane.CLI)         │
├─────────────────────────────┤
│  • CommandParser            │
│  • GameEngine               │
│  • Program                  │
└────────────┬────────────────┘
             │
┌────────────▼────────────────┐
│     核心业务逻辑层          │
│  (EonVientiane.Core)        │
├─────────────────────────────┤
│  Services:                  │
│  • GameService              │
│  • InventoryService         │
│                             │
│  Models:                    │
│  • GameState                │
│  • Inventory                │
│  • Level, Item, Equipment   │
└─────────────────────────────┘
```

### 设计模式
- **服务层模式** - 业务逻辑与表现层分离
- **状态管理模式** - 集中式游戏状态管理
- **命令模式** - 命令解析和执行
- **工厂模式** - 物品和装备创建

## 后续扩展方向

1. **战斗系统** - 敌人、战斗逻辑、掉落奖励
2. **任务系统** - 主线、支线、日常任务
3. **角色属性** - 血量、魔法、属性点
4. **保存/加载** - 游戏存档系统
5. **NPC交互** - 对话、商店、任务发放
6. **成就系统** - 成就解锁和追踪
7. **数据持久化** - 使用数据库或JSON
8. **高级UI** - 改进的控制台界面显示

## 技术栈

- **语言**: C# 10+
- **框架**: .NET 10
- **.NET 标准库** - 无外部依赖
- **开发模式**: 分层架构、SOLID原则

## 文件详情

### 核心模型文件

| 文件 | 职责 |
|------|------|
| [Item.cs](EonVientiane.Core/Models/Item.cs) | 基础物品类，包含稀有度系统 |
| [Equipment.cs](EonVientiane.Core/Models/Equipment.cs) | 装备类，扩展Item，支持槽位和属性 |
| [Inventory.cs](EonVientiane.Core/Models/Inventory.cs) | 背包管理系统，处理物品和装备 |
| [Level.cs](EonVientiane.Core/Models/Level.cs) | 关卡定义类 |
| [GameState.cs](EonVientiane.Core/Models/GameState.cs) | 游戏状态集中管理 |

### 核心服务文件

| 文件 | 职责 |
|------|------|
| [GameService.cs](EonVientiane.Core/Services/GameService.cs) | 游戏主服务，管理关卡和游戏流程 |
| [InventoryService.cs](EonVientiane.Core/Services/InventoryService.cs) | 背包服务，提供背包操作接口 |

### CLI 文件

| 文件 | 职责 |
|------|------|
| [CommandParser.cs](EonVientiane.CLI/CommandParser.cs) | 解析和验证用户命令 |
| [GameEngine.cs](EonVientiane.CLI/GameEngine.cs) | 游戏引擎，执行命令并管理游戏循环 |
| [Program.cs](EonVientiane.CLI/Program.cs) | 程序入口，初始化游戏 |

## 许可证

此项目是 Eon Vientiane 游戏的一部分。

---

**项目状态**: ✅ 基础框架完成，可用于进一步开发
