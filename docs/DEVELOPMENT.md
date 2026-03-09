# 开发指南

## 1. 构建策略

当前仓库包含多个可执行项目，推荐按下列顺序构建：

```bash
dotnet build Shared/Shared.csproj -c Debug
dotnet build EonVientiane/EonVientiane.csproj -c Debug
dotnet build EonVientianeServer/EonVientianeServer.csproj -c Debug
dotnet build EonVientianeCliClient/EonVientianeCliClient.csproj -c Debug
```

`EonVientiane.sln` 目前只包含图形客户端项目；若需完整验证，请使用上面的分项目命令。

## 2. 常用运行命令

### 2.1 启动服务端

```bash
dotnet run --project EonVientianeServer/EonVientianeServer.csproj -- 7777
```

### 2.2 启动图形客户端

```bash
dotnet run --project EonVientiane/EonVientiane.csproj
```

### 2.3 启动 CLI 客户端

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- --help
```

## 3. 本地联调建议

### 3.1 一键联调

```bash
chmod +x start_local_test.sh
./start_local_test.sh
```

脚本会自动构建并拉起多进程，适合快速验证多人场景。

### 3.2 手工联调

1. 启动服务端（端口 `7777`）
2. 启动 1~N 个客户端
3. 如需自动化操作，使用 CLI 执行注册/登录/背包操作

## 4. 发布输出

仓库中包含历史/当前发布产物目录：

- `build_output/published/EonVientiane-Linux/`
- `build_output/published/EonVientiane-Windows/`
- `build_output/published/EonVientianeServer-Linux/`
- `build_output/published/EonVientianeServer-Windows/`

如需重新发布，可按目标平台使用 `dotnet publish`：

```bash
# 示例：Linux x64 服务端
dotnet publish EonVientianeServer/EonVientianeServer.csproj -c Release -r linux-x64 --self-contained false
```

## 5. 代码组织建议

- `Shared/` 优先放“纯协议与纯类型”，避免引入平台依赖。
- 新增网络消息时，同步更新：
  1) `Shared` 消息定义
  2) 服务端消息处理分支
  3) 客户端调用与 UI 回显
  4) CLI 命令（如需自动化覆盖）
- 玩法逻辑优先通过现有 `Manager`/`API` 分层扩展，保持职责单一。

## 6. 常见问题排查

- **端口占用**：服务端启动失败时先检查 `7777` 是否被占用。
- **资源加载失败**：确认客户端 `Content/` 与图标资源已复制到输出目录。
- **终端拉起失败**：`start_local_test.sh` 依次尝试 `gnome-terminal`、`xterm`、`konsole`，无图形终端时会退化为后台执行。
- **框架版本问题**：确认本机已安装 .NET 9（以及 `Shared` 需要的 .NET 8）。

## 7. 最小回归清单

每次核心改动后建议至少验证：

1. 账号注册/登录可用
2. 房间创建/加入/准备可用
3. 背包查询与装备/卸下可用
4. 多客户端对局流程可进入并完成
