# 🎮 EonVientiane

一个基于 MonoGame 框架开发的 C# 回合制战斗游戏，支持单机和联机对战。

[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![MonoGame](https://img.shields.io/badge/MonoGame-3.8-orange)](https://www.monogame.net/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 📖 目录

- [功能特性](#-功能特性)
- [快速开始](#-快速开始)
- [项目结构](#-项目结构)
- [构建和部署](#-构建和部署)
- [游戏系统](#-游戏系统)
- [开发指南](#-开发指南)
- [文档](#-文档)
- [技术栈](#-技术栈)

---

## ✨ 功能特性

### 🎯 核心功能

- **回合制战斗系统** - 基于骰子机制的策略战斗
- **物品栏系统** - 完整的装备和道具管理
- **用户系统** - 注册、登录、用户配置管理
- **联机对战** - 基于 TCP 的多人游戏支持
- **大厅系统** - 房间创建、加入、匹配功能
- **插件系统** - 支持 Mod 扩展

### 🎨 界面特性

- **响应式 UI** - 自适应窗口大小
- **滚动菜单** - 支持鼠标滚动和拖拽
- **实时反馈** - 战斗日志、状态提示
- **多状态管理** - 游戏、背包、登录、大厅界面切换

---

## 🚀 快速开始

### 前置要求

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- Linux / Windows / macOS
- MonoGame 3.8+

### 1. 克隆项目

```bash
git clone https://github.com/yourusername/EonVientiane.git
cd EonVientiane
```

### 2. 构建项目

#### Linux

```bash
# 构建所有项目（客户端 + 服务端）
./build_all.sh

# 仅构建服务端
./build_server.sh
```

#### Windows

```powershell
# 使用 Visual Studio 打开解决方案
start EonVientiane.sln

# 或使用命令行构建
dotnet build EonVientiane.sln
```

### 3. 运行游戏

#### 单机模式

```bash
cd EonVientiane
dotnet run
```

#### 联机模式

**启动服务端：**

```bash
# 使用脚本启动（推荐）
./start_server.sh

# 或手动启动
cd EonVientianeServer
dotnet run
```

**启动客户端：**

```bash
cd EonVientiane
dotnet run
```

在游戏中选择"联机对战"即可连接服务器。

---

## 📂 项目结构

```
EonVientiane/
├── EonVientiane/              # 游戏客户端
│   ├── Game1.cs               # 游戏主类
│   ├── MenuManager.cs         # 菜单管理器
│   ├── BattleManager.cs       # 战斗管理器
│   ├── InventoryManager.cs    # 物品栏管理器
│   ├── LoginManager.cs        # 登录管理器
│   ├── UIManager.cs           # UI管理器
│   ├── InputManager.cs        # 输入管理器
│   ├── Network/               # 网络模块
│   │   ├── NetworkClient.cs   # TCP客户端
│   │   └── LobbyManager.cs    # 大厅逻辑
│   ├── Content/               # 游戏资源
│   └── PluginSystem/          # 插件系统
│
├── EonVientianeServer/        # 游戏服务端
│   ├── Program.cs             # 服务端入口
│   ├── GameServer.cs          # 服务器核心
│   ├── GameRoom.cs            # 房间管理
│   ├── ConnectedClient.cs     # 客户端连接
│   ├── UserManager.cs         # 用户管理
│   └── InventoryStore.cs      # 物品存储
│
├── Shared/                    # 共享库
│   └── NetworkProtocol.cs     # 网络协议定义
│
├── docs/                      # 项目文档
│   ├── INDEX.md               # 文档索引
│   ├── multiplayer/           # 多人游戏文档
│   ├── deployment/            # 部署文档
│   ├── systems/               # 系统设计文档
│   └── refactoring/           # 重构记录
│
├── build_all.sh               # 全平台构建脚本
├── build_server.sh            # 服务端构建脚本
├── start_server.sh            # 服务端启动脚本
└── EonVientiane.sln           # Visual Studio 解决方案
```

---

## 🔨 构建和部署

### 自动化构建

项目提供了自动化构建脚本，支持多平台发布：

```bash
# 构建所有平台（Linux + Windows）
./build_all.sh

# 输出目录
build_output/
├── published/
│   ├── EonVientiane-Linux/      # Linux 客户端
│   ├── EonVientiane-Windows/    # Windows 客户端
│   └── EonVientianeServer-Windows/  # Windows 服务端
```

### 手动构建

```bash
# 构建客户端
dotnet publish EonVientiane/EonVientiane.csproj -c Release -r linux-x64 --self-contained

# 构建服务端
dotnet publish EonVientianeServer/EonVientianeServer.csproj -c Release -r linux-x64 --self-contained
```

详细部署说明请参考 [部署文档](docs/deployment/SERVER_DEPLOYMENT.md)。

---

## 🎮 游戏系统

### 战斗系统

- **骰子战斗机制** - 通过投掷骰子决定攻击力
- **装备影响** - 武器和防具影响战斗属性
- **战斗日志** - 实时显示战斗过程
- **AI 对手** - 智能电脑对手

### 物品栏系统

- **背包管理** - 存储和管理道具
- **装备槽位** - 武器、防具、饰品栏
- **拖拽装备** - 直观的装备操作
- **物品同步** - 服务端物品状态同步

### 联机系统

- **房间系统** - 创建/加入游戏房间
- **玩家匹配** - 自动匹配在线玩家
- **实时同步** - 游戏状态实时同步
- **断线重连** - 网络异常处理

详细系统说明请查看 [游戏逻辑文档](docs/systems/GAME_LOGIC.md)。

---

## 👨‍💻 开发指南

### 代码架构

项目采用**管理器模式**进行架构设计，主要管理器包括：

- **MenuManager** - 处理菜单逻辑和渲染
- **BattleManager** - 管理战斗流程
- **InventoryManager** - 管理物品和装备
- **LoginManager** - 处理用户认证
- **UIManager** - 统一的 UI 绘制
- **InputManager** - 集中的输入处理

### 代码规范

- 遵循 C# 命名约定
- 使用有意义的变量名
- 添加必要的注释
- 保持方法简洁（单一职责）

### 扩展开发

项目支持插件系统，可以通过 Mod 方式扩展游戏功能：

```csharp
// 插件示例（位于 EonVientiane/PluginSystem/）
public interface IGamePlugin
{
    void Initialize(Game1 game);
    void Update(GameTime gameTime);
}
```

---

## 📚 文档

完整文档位于 [docs/](docs/) 目录：

### 快速导航

| 文档 | 说明 |
|------|------|
| [📋 文档索引](docs/INDEX.md) | 所有文档的导航页面 |
| [🎮 游戏逻辑](docs/systems/GAME_LOGIC.md) | 游戏核心系统说明 |
| [🎒 物品栏系统](docs/systems/INVENTORY_SYSTEM.md) | 物品栏设计文档 |
| [👥 多人游戏指南](docs/multiplayer/MULTIPLAYER_README.md) | 联机功能完整指南 |
| [🚀 服务器部署](docs/deployment/SERVER_DEPLOYMENT.md) | 服务端部署教程 |
| [🔄 重构记录](docs/refactoring/REFACTORING_COMPLETE.md) | 代码重构历史 |

### 快速参考

- **新开发者？** 先阅读本 README
- **了解架构？** 查看 [重构文档](docs/refactoring/REFACTORING_COMPLETE.md)
- **部署服务器？** 参考 [部署指南](docs/deployment/SERVER_DEPLOYMENT.md)
- **开发联机功能？** 阅读 [多人游戏文档](docs/multiplayer/MULTIPLAYER_README.md)

---

## 🛠️ 技术栈

### 客户端

- **框架**: MonoGame 3.8 (DesktopGL)
- **语言**: C# (.NET 9.0)
- **图形**: SpriteBatch 2D 渲染
- **网络**: System.Net.Sockets (TCP)

### 服务端

- **语言**: C# (.NET 9.0)
- **网络**: TcpListener 异步模型
- **架构**: 多线程房间管理
- **协议**: 自定义二进制协议

### 共享库

- **协议**: 统一的网络消息格式
- **数据**: JSON 序列化
- **兼容**: 跨平台支持

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

### 开发流程

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

---

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

---

## 📞 联系方式

- **项目主页**: https://github.com/yourusername/EonVientiane
- **问题反馈**: https://github.com/yourusername/EonVientiane/issues

---

## 🎯 更新日志

### v1.0.0 (2026-01-09)

- ✅ 完整的回合制战斗系统
- ✅ 物品栏和装备系统
- ✅ 用户注册和登录
- ✅ 联机对战功能
- ✅ 大厅和房间系统
- ✅ 跨平台支持（Linux/Windows）
- ✅ 完整的构建和部署脚本
- ✅ 代码重构和优化

---

**享受游戏，快乐编码！** 🚀
