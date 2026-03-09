# EonVientiane

EonVientiane 是一个基于 .NET 与 MonoGame 的多人战斗游戏项目，包含图形客户端、独立服务端、脚本化 CLI 客户端以及共享协议层。

## 项目目标

- 提供可本地运行的多人对战验证环境（服务端 + 多客户端）
- 支持账号系统、房间大厅、对战流程、道具与背包系统
- 支持离线本地数据保存与联机同步能力
- 提供可脚本化 CLI 以便自动化测试服务器接口

## 子项目结构

| 子项目 | 路径 | 作用 |
| --- | --- | --- |
| 图形客户端 | `EonVientiane/` | 主游戏客户端（MonoGame） |
| 服务端 | `EonVientianeServer/` | TCP 游戏服务器，负责认证、房间、背包、战斗编排 |
| CLI 客户端 | `EonVientianeCliClient/` | 命令行测试工具，用于脚本化调用服务端协议 |
| 共享库 | `Shared/` | 网络协议、随机工具、钱包相关共享类型 |

## 环境要求

- Linux / Windows / macOS
- .NET SDK 9.0（客户端、服务端、CLI）
- .NET SDK 8.0（`Shared` 当前目标框架为 `net8.0`）
- OpenGL 运行环境（MonoGame DesktopGL 依赖）

> 说明：当前 `EonVientiane.sln` 仅包含图形客户端项目；服务端与 CLI 需按项目路径单独构建或运行。

## 快速开始（本地联调）

### 1) 构建

```bash
dotnet build Shared/Shared.csproj -c Debug
dotnet build EonVientiane/EonVientiane.csproj -c Debug
dotnet build EonVientianeServer/EonVientianeServer.csproj -c Debug
dotnet build EonVientianeCliClient/EonVientianeCliClient.csproj -c Debug
```

### 2) 启动服务端

```bash
dotnet run --project EonVientianeServer/EonVientianeServer.csproj -- 7777
```

服务端交互命令：
- `status`：查看在线状态
- `help`：查看命令帮助
- `quit` / `exit`：退出服务端

### 3) 启动图形客户端

```bash
dotnet run --project EonVientiane/EonVientiane.csproj
```

### 4) 使用一键本地测试脚本

```bash
chmod +x start_local_test.sh
./start_local_test.sh
```

脚本会：
- 重新构建 Shared / 客户端 / 服务端
- 启动 1 个服务端（默认 `localhost:7777`）
- 启动 3 个客户端进程
- 在 `test_longterm/` 与 `test_client_*` 目录保留本地测试数据

## CLI 客户端用法（自动化测试）

查看帮助：

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- --help
```

常用示例：

```bash
# 注册
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  register --username user1 --password pass1 --email user1@test.local

# 登录
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  login --username user1 --password pass1

# 查询背包
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  inventory --username user1 --password pass1
```

更多命令见 [docs/CLI.md](docs/CLI.md)。

## 数据目录与持久化

- 根目录 `data/`
  - `users/users.json`：用户数据
  - `wallets/`：钱包与密钥文件
- 测试脚本目录
  - `test_longterm/`：服务端长期测试数据目录
  - `test_client_1/2/3/`：客户端测试数据目录（运行脚本后自动创建）
- 客户端本地加密数据（默认）
  - `data/local_player_data/*.edat`

## 文档索引

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)：系统架构与模块关系
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)：开发流程、构建发布与排障
- [docs/CLI.md](docs/CLI.md)：CLI 参数、命令与脚本集成建议

## 许可证

见 [LICENSE](LICENSE)。
