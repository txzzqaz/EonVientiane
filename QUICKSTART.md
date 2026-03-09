# Eon Vientiane CLI 快速参考指南

## 快速开始

### 编译和运行
```bash
# 编译项目
dotnet build -c Debug

# 运行游戏
dotnet run --project EonVientiane.CLI -c Debug

# 或直接运行可执行文件
./EonVientiane.CLI/bin/Debug/net10.0/EonVientiane.CLI
```

## 核心命令快速查询

| 命令 | 参数 | 说明 | 示例 |
|------|------|------|------|
| `help` | - | 显示帮助信息 | `help` |
| `status` | - | 查看玩家和游戏状态 | `status` |
| `clear` | - | 清屏 | `clear` |
| **关卡命令** | | | |
| `loadlevel` | `<关卡ID>` | 加载指定关卡 | `loadlevel test` |
| `unloadlevel` | - | 卸载当前关卡 | `unloadlevel` |
| `levels` | - | 查看所有可用关卡 | `levels` |
| **背包命令** | | | |
| `inv` | - | 查看背包和穿戴的装备 | `inv` |
| `equip` | `<物品名>` | 穿戴装备 | `equip 铁剑` |
| `unequip` | `<物品名>` | 卸下装备 | `unequip 铁剑` |
| **退出** | | | |
| `exit` / `quit` | - | 退出游戏 | `exit` |

## 默认物品和装备

### 初始物品
- **生命药水** x3 - 基础恢复药物
- **魔法药水** x2 - 魔法恢复药物
- **金币** x100 - 货币

### 初始装备
- **铁剑** - 主手武器，攻击+5
- **木盾** - 副手武器，防御+3
- **布甲** - 胸甲，防御+2

## 游戏流程示例

```
1. 启动游戏
   $ dotnet run --project EonVientiane.CLI

2. 查看帮助
   > help

3. 查看游戏状态
   > status

4. 查看可用关卡
   > levels

5. 加载一个关卡
   > loadlevel test

6. 查看背包
   > inv

7. 穿戴装备
   > equip 铁剑
   > equip 木盾

8. 再次查看背包（查看穿戴的装备）
   > inv

9. 卸下装备
   > unequip 铁剑

10. 退出游戏
    > exit
```

## 可用关卡列表

| 关卡ID | 名称 | 难度 | 描述 |
|--------|------|------|------|
| `test` | 测试关卡 | 1 | 一个用于测试的基础关卡 |
| `forest` | 森林 | 5 | 一片神秘的森林 |
| `castle` | 城堡 | 10 | 一座古老的城堡 |
| `dragon` | 龙巢 | 15 | 龙的巢穴，充满危险 |

## 装备槽位说明

游戏支持以下8个装备槽位：

- **Head** - 头部（头盔、王冠等）
- **Chest** - 胸部（甲胄、衣服等）
- **Legs** - 腿部（护腿等）
- **Feet** - 脚部（靴子等）
- **Hands** - 手部（手套等）
- **MainHand** - 主手（武器等）
- **OffHand** - 副手（盾牌、副武器等）
- **Accessory** - 饰品（戒指、项链等）

## 项目结构概览

```
EonVientiane/
├── EonVientiane.Core/           # 核心游戏逻辑
│   ├── Models/                  # 数据模型
│   │   ├── Item.cs             # 物品基类
│   │   ├── Equipment.cs        # 装备类
│   │   ├── Inventory.cs        # 背包系统
│   │   ├── Level.cs            # 关卡定义
│   │   └── GameState.cs        # 游戏状态
│   └── Services/               # 业务服务
│       ├── GameService.cs      # 游戏主服务
│       └── InventoryService.cs # 背包服务
│
├── EonVientiane.CLI/           # CLI 表现层
│   ├── CommandParser.cs        # 命令解析器
│   ├── GameEngine.cs           # 游戏引擎
│   └── Program.cs              # 入口点
│
├── README.md                    # 详细文档
└── test_cli.sh                 # 自动化测试脚本
```

## 常见操作

### 穿戴所有初始装备
```
equip 铁剑
equip 木盾
equip 布甲
```

### 查看已穿戴装备
```
inv
```
（在输出中的"已穿戴装备"部分查看）

### 卸下所有装备
```
unequip 铁剑
unequip 木盾
unequip 布甲
```

## 故障排除

### 无法穿戴装备
确保：
- 物品名称拼写正确（区分大小写）
- 物品存在于背包中
- 物品是装备（非普通物品）

### 关卡加载失败
确保使用正确的关卡ID（test、forest、castle、dragon）

### 命令未识别
输入 `help` 查看所有可用命令

## 开发信息

- **语言**: C# (.NET 10)
- **架构**: 分层架构（表现层 + 业务逻辑层）
- **模式**: 服务层、状态管理、命令模式
- **编译**: `dotnet build -c Debug`
- **运行**: `dotnet run --project EonVientiane.CLI -c Debug`

---

**提示**: 当前版本提供了基础的CLI游戏框架。未来可以扩展为战斗系统、任务系统、保存/加载等功能。
