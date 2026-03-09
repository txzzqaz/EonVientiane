# 系统架构

## 1. 总览

EonVientiane 采用“客户端 + 服务端 + 共享协议层”的结构：

- `EonVientiane/`：MonoGame 图形客户端，负责 UI、输入、战斗表现与本地状态。
- `EonVientianeServer/`：TCP 服务端，负责账号认证、房间管理、库存与战斗协同。
- `Shared/`：跨端共享协议、消息类型与基础工具。
- `EonVientianeCliClient/`：轻量命令行客户端，用于自动化回归与接口调试。

## 2. 模块关系

```text
EonVientiane (Client)  --> Shared
EonVientianeServer     --> Shared + EonVientiane
EonVientianeCliClient  --> Shared
```

说明：服务端引用了客户端项目中的部分游戏定义（例如物品/玩法相关类型），这使玩法逻辑能够快速对齐，但也意味着服务端与客户端存在较高耦合。

## 3. 网络通信模型

### 3.1 传输层

- 服务端使用 `TcpListener` 监听端口（默认 `7777`）。
- 每个连接对应一个 `ConnectedClient`，由服务端异步消息循环处理。

### 3.2 协议层

- 协议类型定义于 `Shared/NetworkProtocol.cs`。
- 消息通过 `MessageType` 区分业务类型（登录、注册、房间、背包、战斗等）。
- CLI 与图形客户端均通过同一协议与服务端通信。

### 3.3 典型流程

1. 客户端连接服务端
2. 发送注册/登录消息完成认证
3. 获取房间列表、创建或加入房间
4. 在房间内准备/组队后进入战斗流程
5. 可在会话中请求背包、装备/卸下道具

## 4. 数据与状态

## 4.1 服务端数据

- `data/users/users.json`：账号数据
- `data/wallets/`：钱包与密钥数据
- `EonVientianeServer` 启动时初始化钱包与库存存储组件

## 4.2 客户端本地数据

- `LocalDataManager` 使用基于用户账密派生的密钥进行本地加密存储
- 默认目录：`data/local_player_data/`
- 支持数据版本号与哈希用于冲突检测/完整性校验

## 5. 核心能力分层

- 表现层：`UIManager`、菜单/输入处理、画面渲染
- 玩法层：战斗系统、PVE 挑战、成就系统、道具系统
- 网络层：本地网络管理、房间协同、消息分发
- 持久层：本地加密数据、服务端用户与钱包存储

## 6. 运行拓扑（本地测试）

`start_local_test.sh` 提供一键本地联调：

- 启动 1 个服务端实例
- 启动 3 个图形客户端实例
- 使用独立目录隔离每个客户端的数据
- 服务端长期数据落在 `test_longterm/`

该拓扑适合验证：多账号登录、房间交互、背包变更、战斗回放等场景。
