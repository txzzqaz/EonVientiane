# Game1.cs 重构说明

## 概述
将臃肿的Game1.cs文件拆分成4个专注于单一职责的管理器类，提高代码的可维护性和可读性。

## 拆分结果

### 1. **MenuManager.cs** - 菜单系统管理
负责所有菜单相关的功能：
- 菜单按钮的创建和管理（顶部、底部、中间按钮）
- 菜单滚动控制
- 菜单输入处理（鼠标点击、拖拽、滚轮）
- 菜单绘制

**主要类：**
- `MenuManager` - 菜单管理器
- `MenuClickResult` - 菜单点击结果

**关键方法：**
```csharp
public void InitializeButtons(...)          // 初始化菜单按钮
public void AddMiddleButton(...)            // 添加中间按钮
public MenuClickResult HandleInput(...)     // 处理菜单输入
public void Draw(...)                       // 绘制菜单
```

---

### 2. **BattleManager.cs** - 战斗系统管理
负责所有战斗相关的功能：
- 战斗初始化和设置
- 战斗输入处理（骰子选择、目标选择等）
- 战斗绘制（HP条、战斗日志、骰子按钮等）
- 战斗更新逻辑

**主要类：**
- `BattleManager` - 战斗管理器

**关键方法：**
```csharp
public void InitializeBattle()              // 初始化战斗
public void Update()                        // 更新战斗逻辑
public void HandleInput(...)                // 处理战斗输入
public void Draw(...)                       // 绘制战斗界面
public void EndBattle()                     // 结束战斗
```

---

### 3. **InventoryInputHandler.cs** - 背包输入处理
负责背包界面的输入处理：
- 背包物品选择
- 装备槽位选择
- 背包滚动
- 装备/卸装逻辑

**主要类：**
- `InventoryInputHandler` - 背包输入处理器

**关键方法：**
```csharp
public void HandleInput(...)                // 处理背包输入
```

---

### 4. **LoginInputHandler.cs** - 登录界面输入处理
负责登录界面的输入处理：
- 用户名/密码输入框处理
- 键盘输入处理（退格、Tab、Enter等）
- 登录/取消按钮处理

**主要类：**
- `LoginInputHandler` - 登录输入处理器
- `LoginInputResult` - 登录输入结果

**关键方法：**
```csharp
public LoginInputResult HandleInput(...)    // 处理登录输入
```

---

### 5. **Game1.cs（简化版）** - 核心游戏类
保留了游戏的核心循环和状态管理：
- 初始化所有管理器
- 加载资源
- 主游戏更新循环
- 主游戏绘制循环
- UI状态管理

**职责：**
- 管理各个系统的生命周期
- 协调UI状态切换
- 调用各管理器的输入/更新/绘制方法

---

## 文件大小对比

**之前：** Game1.cs ~1200 行
**之后：**
- Game1.cs: ~250 行
- MenuManager.cs: ~350 行
- BattleManager.cs: ~400 行
- InventoryInputHandler.cs: ~80 行
- LoginInputHandler.cs: ~150 行

总行数基本保持不变，但每个文件现在更加专注和易于理解。

---

## 使用示例

### 添加菜单按钮
```csharp
_game1.AddMiddleButton("新按钮");
_game1.AddMiddleButton("自定义按钮", Color.Red, Color.Pink);
```

### 初始化战斗
```csharp
_battleManager.InitializeBattle();
```

### 处理菜单输入
```csharp
var result = _menuManager.HandleInput(mouseState, previousMouseState);
if (result.TopButtonClicked) { ... }
```

---

## 好处

1. **代码组织更清晰** - 每个类有明确的单一职责
2. **更易维护** - 改动某个系统只需修改对应的管理器
3. **代码复用** - 管理器类可以在其他项目中复用
4. **测试更容易** - 可以独立测试每个管理器
5. **新人上手快** - 代码结构清晰，容易理解
6. **并行开发** - 多人可以同时开发不同的管理器

---

## 注意事项

- `MenuManager.GetMenuWidth()` 和 `MenuManager.GetButtonHeight()` 是静态方法，用于获取常数
- 各管理器通过构造函数接收必要的依赖（如 `_graphics`, `_inventoryManager` 等）
- Game1.cs 中保留了公共接口方法（如 `AddMiddleButton`），以保持向后兼容性
- 所有原始功能都被完整保留，没有功能删除或改变
