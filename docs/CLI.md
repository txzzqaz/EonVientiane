# CLI 客户端文档

`EonVientianeCliClient` 用于脚本化调用服务端协议，适合 CI、回归脚本与快速验收。

## 1. 运行入口

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- --help
```

## 2. 全局参数

大多数命令支持：

- `--host <host>`：服务端地址（默认 `127.0.0.1`）
- `--port <port>`：服务端端口（默认 `7777`）
- `--timeout <sec>`：请求超时秒数（默认 `15`）

返回约定：

- 标准输出为 JSON
- 退出码 `0` 表示成功，非 `0` 表示失败

## 3. 命令列表

### 3.1 注册

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  register --username <u> --password <p> [--email <e>]
```

### 3.2 登录

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  login --username <u> --password <p>
```

### 3.3 获取背包

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  inventory --username <u> --password <p>
```

### 3.4 获取初始背包

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  get-initial --username <u> --password <p>
```

### 3.5 装备道具

两种指定方式：

- 直接指定堆叠：`--stack-id <id>`
- 按物品 ID 选择第 N 个：`--item-id <item> [--nth <n>]`

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  equip --username <u> --password <p> --stack-id <stackId>
```

### 3.6 卸下道具

```bash
dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj -- \
  unequip --username <u> --password <p> --stack-id <stackId>
```

## 4. 自动化脚本示例

```bash
#!/usr/bin/env bash
set -euo pipefail

cli='dotnet run --project EonVientianeCliClient/EonVientianeCliClient.csproj --'

$cli register --username ci_user --password ci_pass --email ci_user@test.local || true
$cli login --username ci_user --password ci_pass
$cli inventory --username ci_user --password ci_pass
```

## 5. 失败排查

- 返回 `unknown_command`：检查命令拼写。
- 返回登录失败：确认账号是否已注册、服务端地址端口是否正确。
- 返回超时：确认服务端已启动并可访问，必要时调大 `--timeout`。
