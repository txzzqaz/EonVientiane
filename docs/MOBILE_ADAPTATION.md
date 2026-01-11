# 📱 EonVientiane 移动端适配指南

## 概述

本文档描述了 EonVientiane 游戏项目中的移动端平台兼容性实现。该项目现已支持 Windows 桌面、iOS、Android 等多个平台。

---

## 🏗️ 架构设计

### 核心组件

#### 1. **PlatformAdapter** - 平台适配层
负责检测运行平台，自动调整UI布局和缩放。

```csharp
// 自动检测平台
var adapter = new PlatformAdapter(graphics);
Console.WriteLine(adapter.Platform); // Mobile, Tablet, 或 Desktop

// 获取推荐的UI尺寸
int buttonHeight = adapter.GetRecommendedButtonHeight();
int padding = adapter.GetRecommendedPadding();
```

**主要功能:**
- 自动平台检测 (Mobile/Tablet/Desktop)
- 分辨率和DPI自适应
- UI元素自动缩放
- 屏幕方向管理

#### 2. **TouchInputManager** - 触摸输入管理
处理移动设备的触摸输入，支持多种手势识别。

```csharp
// 初始化触摸管理器
var touchMgr = new TouchInputManager();

// 订阅手势事件
touchMgr.GestureDetected += (sender, args) =>
{
    switch (args.Type)
    {
        case TouchInputManager.GestureType.Tap:
            // 处理点击
            break;
        case TouchInputManager.GestureType.Drag:
            // 处理拖拽
            break;
        case TouchInputManager.GestureType.Pinch:
            // 处理缩放
            break;
    }
};

// 在Update中更新
touchMgr.Update(gameTime);
```

**支持的手势:**
- **Tap** - 单击
- **DoubleTap** - 双击
- **LongPress** - 长按
- **Drag** - 拖拽
- **Pinch** - 双指缩放
- **Swipe** - 滑动

#### 3. **VirtualKeyboard** - 虚拟键盘
为移动设备提供屏幕键盘输入，特别适用于文本输入场景。

```csharp
// 创建虚拟键盘
var keyboard = new VirtualKeyboard(font, 40, 40, 2);

// 订阅事件
keyboard.CharacterEntered += (c) => inputText += c;
keyboard.BackspacePressed += () => { /* 处理退格 */ };

// 显示/隐藏
keyboard.SetVisible(true);

// 在Draw中绘制
keyboard.Draw(spriteBatch, keyTexture);
```

#### 4. **InputManager** 改进
现已统一处理键盘、鼠标和触摸输入。

```csharp
// 初始化输入管理器（支持平台适配）
var inputMgr = new InputManager(graphics);

// 访问各种输入
inputMgr.TouchInput;         // 触摸管理器
inputMgr.PlatformAdapter;    // 平台适配器
inputMgr.IsTouchDevice;      // 是否触摸设备

// 更新（包括触摸输入）
inputMgr.Update(gameTime);
```

---

## 📱 各平台的特定实现

### Windows/Desktop

**特点:**
- 全分辨率支持 (1280x720)
- 键盘和鼠标输入
- 可调整窗口大小

**初始化:**
```csharp
_graphics.PreferredBackBufferWidth = 1280;
_graphics.PreferredBackBufferHeight = 720;
```

### iOS

**特点:**
- 竖屏优化 (540x960)
- 触摸输入为主
- 安全区域处理（刘海屏等）

**构建命令:**
```bash
# 构建iOS应用
dotnet publish -c Release -f net9.0 -r ios-arm64 /p:TargetRID=ios

# 使用Xcode打开生成的项目进行签名和部署
```

**关键配置:**
```csharp
// Game1.cs
if (_platformAdapter.Platform == PlatformAdapter.DevicePlatform.Mobile)
{
    _graphics.PreferredBackBufferWidth = 540;
    _graphics.PreferredBackBufferHeight = 960;
    _graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
    _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24;
}
```

### Android

**特点:**
- 多分辨率支持
- 触摸输入
- 支持横屏和竖屏

**构建命令:**
```bash
# 构建Android应用
dotnet publish -c Release -f net9.0 -r android-arm64 /p:TargetRID=android
```

**关键配置:**
```csharp
// 设置屏幕方向
_platformAdapter.SetScreenOrientation(PlatformAdapter.ScreenOrientation.Portrait);
```

---

## 🎮 游戏交互适配

### 按钮交互

**桌面版 (鼠标点击):**
```csharp
MouseState mouseState = Mouse.GetState();
if (button.Bounds.Contains(mouseState.Position))
{
    if (mouseState.LeftButton == ButtonState.Pressed)
    {
        OnButtonClicked();
    }
}
```

**移动版 (触摸点击):**
```csharp
var touchInput = _inputManager.TouchInput;
touchInput.GestureDetected += (sender, args) =>
{
    if (args.Type == TouchInputManager.GestureType.Tap)
    {
        if (button.Bounds.Contains((int)args.Position.X, (int)args.Position.Y))
        {
            OnButtonClicked();
        }
    }
};
```

### 文本输入

**移动设备 (虚拟键盘):**
```csharp
if (_platformAdapter.NeedsVirtualKeyboard())
{
    _virtualKeyboard.SetVisible(true);
    _virtualKeyboard.CharacterEntered += (c) => 
    {
        _loginManager.Username += c;
    };
}
```

### 菜单滚动

**触摸拖拽:**
```csharp
touchInput.GestureDetected += (sender, args) =>
{
    if (args.Type == TouchInputManager.GestureType.Drag)
    {
        // 拖拽Y偏移量用于滚动
        int scrollDelta = (int)args.Delta.Y;
        menu.Scroll(scrollDelta);
    }
};
```

---

## 🎨 UI响应式设计

### 自动缩放

`PlatformAdapter` 提供自动缩放功能：

```csharp
// 获取缩放因子
float scaleX = _platformAdapter.ScaleX;
float scaleY = _platformAdapter.ScaleY;

// 缩放矩形
Rectangle scaledButton = _platformAdapter.ScaleRectangle(originalButton);

// 缩放向量
Vector2 scaledPos = _platformAdapter.VirtualToScreen(virtualPos);
```

### 字体大小调整

```csharp
// 获取DPI感知的字体缩放
float fontScale = _platformAdapter.GetFontScaleFactor();

// 调整字体大小
int fontSize = (int)(baseSize * fontScale);
```

### 安全区域

某些移动设备有安全区域限制（如刘海屏）：

```csharp
// 获取安全区域
Rectangle safeArea = _platformAdapter.GetSafeArea();

// 确保UI在安全区域内
if (!safeArea.Contains(uiElement.Bounds))
{
    // 调整UI位置
}
```

---

## 🔧 构建和部署

### 构建配置

#### 为所有平台构建

```bash
# Windows Desktop
dotnet publish -c Release -f net9.0 -r win-x64

# macOS
dotnet publish -c Release -f net9.0 -r osx-arm64
dotnet publish -c Release -f net9.0 -r osx-x64

# Linux
dotnet publish -c Release -f net9.0 -r linux-x64

# iOS (需要Xcode)
dotnet publish -c Release -f net9.0 -r ios-arm64 /p:TargetRID=ios

# Android (需要Android SDK)
dotnet publish -c Release -f net9.0 -r android-arm64 /p:TargetRID=android
```

### 使用构建脚本

查看 [BUILD_SCRIPT_README.md](deployment/BUILD_SCRIPT_README.md) 了解详细的构建流程。

### 项目文件配置

`.csproj` 文件已更新以支持条件编译：

```xml
<PropertyGroup>
  <OutputType Condition="'$(TargetRID)' == 'win-x64'">WinExe</OutputType>
  <OutputType Condition="'$(TargetRID)' == 'ios'">Library</OutputType>
  <OutputType Condition="'$(TargetRID)' == 'android'">Library</OutputType>
</PropertyGroup>
```

---

## 📊 性能优化

### 移动设备优化

1. **纹理优化**
   - 使用较小的纹理分辨率
   - 启用纹理压缩

2. **绘制调用优化**
   - 批量绘制相同纹理
   - 使用精灵图集

3. **内存管理**
   - 及时释放不需要的资源
   - 使用对象池减少GC压力

### 示例配置

```csharp
if (_platformAdapter.Platform == PlatformAdapter.DevicePlatform.Mobile)
{
    // 降低画质以提升性能
    _graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
    _graphics.SynchronizeWithVerticalRetrace = true;
    this.TargetElapsedTime = TimeSpan.FromSeconds(1/30.0); // 30 FPS
}
else
{
    // 桌面版本可以使用更高帧率
    this.TargetElapsedTime = TimeSpan.FromSeconds(1/60.0); // 60 FPS
}
```

---

## 🐛 调试和测试

### 模拟移动设备

在Windows上模拟移动设备分辨率进行测试：

```csharp
// 在Initialize中
_graphics.PreferredBackBufferWidth = 540;  // 手机宽度
_graphics.PreferredBackBufferHeight = 960; // 手机高度
_graphics.ApplyChanges();

// 然后测试触摸输入处理
```

### 日志输出

```csharp
string deviceInfo = _platformAdapter.GetDeviceInfo();
Console.WriteLine(deviceInfo);
// 输出: Platform: Mobile, Orientation: Portrait, Resolution: 540x960, Scale: (0.42, 1.33)
```

### 触摸调试

```csharp
// 显示活跃触摸点
foreach (var touch in _inputManager.TouchInput.GetActiveTouches())
{
    Console.WriteLine($"Touch {touch.Id}: {touch.Position}");
}
```

---

## 📚 常见问题

### Q: 如何在PC上测试触摸功能？

**A:** 使用以下方法：
1. 在Game1中设置移动分辨率
2. 使用鼠标模拟触摸点击
3. 或使用Windows Touch模拟工具

### Q: 虚拟键盘何时显示？

**A:** 虚拟键盘在以下情况显示：
- 平台为移动设备
- 用户点击文本输入框
- 调用 `_virtualKeyboard.SetVisible(true)`

### Q: 如何处理不同的屏幕方向？

**A:** 使用 `PlatformAdapter.SetScreenOrientation()` 方法：
```csharp
_platformAdapter.SetScreenOrientation(
    PlatformAdapter.ScreenOrientation.Portrait
);
```

### Q: 性能不达预期怎么办？

**A:** 检查以下几点：
1. 是否启用了垂直同步
2. 帧率目标是否合理
3. 是否过度使用复杂着色器
4. 是否有内存泄漏

---

## 🔗 相关文档

- [快速参考](QUICK_REFERENCE.md)
- [游戏逻辑文档](systems/GAME_LOGIC.md)
- [UI系统文档](systems/INVENTORY_SYSTEM.md)
- [构建脚本文档](deployment/BUILD_SCRIPT_README.md)

---

## 📝 更新日志

### v1.0.0 (移动端初始支持)

**新增:**
- ✅ PlatformAdapter - 自动平台检测和UI缩放
- ✅ TouchInputManager - 完整的触摸和手势识别
- ✅ VirtualKeyboard - 屏幕虚拟键盘
- ✅ 多平台项目配置
- ✅ iOS 和 Android 构建支持

**改进:**
- ✅ InputManager 统一输入处理
- ✅ Game1 平台感知初始化
- ✅ 响应式UI布局

---

## 💡 最佳实践

1. **始终使用PlatformAdapter进行缩放** - 不要硬编码分辨率
2. **处理多种输入方式** - 同时支持触摸和鼠标
3. **测试各种屏幕尺寸** - 特别是边界情况
4. **优化性能** - 移动设备资源受限
5. **关注安全区域** - 避免刘海屏和系统UI覆盖
6. **提供虚拟键盘反馈** - 视觉或触觉反馈
7. **文档化平台差异** - 记录平台特定的行为

---

**问题报告:** 如有任何问题或建议，请在项目的 Issues 页面提出。
