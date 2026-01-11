using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EonVientiane;

/// <summary>
/// 移动端兼容性集成示例
/// 演示如何在现有游戏中集成触摸输入和平台适配
/// </summary>
public class MobileCompatibilityExample
{
    // 这个文件仅作为示例，展示如何使用新的移动端功能
    // 实际的集成已在 Game1.cs 中完成

    /// <summary>
    /// 示例 1: 基本的触摸输入处理
    /// </summary>
    public static void Example1_BasicTouchInput()
    {
        // 在 Game1.Initialize() 中：
        // InputManager inputManager = new InputManager(graphics);
        // inputManager.TouchInput.GestureDetected += HandleGesture;

        // 在 Game1.Update() 中：
        // inputManager.Update(gameTime);

        // private void HandleGesture(object sender, TouchInputManager.GestureEventArgs args)
        // {
        //     switch (args.Type)
        //     {
        //         case TouchInputManager.GestureType.Tap:
        //             Console.WriteLine($"用户点击了: {args.Position}");
        //             break;
        //         case TouchInputManager.GestureType.Drag:
        //             Console.WriteLine($"用户拖拽了: {args.Delta}");
        //             break;
        //     }
        // }
    }

    /// <summary>
    /// 示例 2: 处理按钮点击（同时支持鼠标和触摸）
    /// </summary>
    public static void Example2_UnifiedButtonHandling()
    {
        // 使用平台适配器获取推荐的按钮大小
        // PlatformAdapter adapter = inputManager.PlatformAdapter;
        // int buttonHeight = adapter.GetRecommendedButtonHeight();
        // int buttonWidth = adapter.GetRecommendedButtonWidth();
        //
        // Rectangle loginButton = new Rectangle(10, 10, buttonWidth, buttonHeight);
        //
        // // 处理点击
        // if (inputManager.IsTouchDevice)
        // {
        //     // 移动设备: 处理触摸
        //     inputManager.TouchInput.GestureDetected += (sender, args) =>
        //     {
        //         if (args.Type == TouchInputManager.GestureType.Tap &&
        //             loginButton.Contains((int)args.Position.X, (int)args.Position.Y))
        //         {
        //             OnLoginButtonPressed();
        //         }
        //     };
        // }
        // else
        // {
        //     // 桌面设备: 处理鼠标
        //     MouseState mouseState = Mouse.GetState();
        //     if (loginButton.Contains(mouseState.Position) &&
        //         mouseState.LeftButton == ButtonState.Pressed)
        //     {
        //         OnLoginButtonPressed();
        //     }
        // }
    }

    /// <summary>
    /// 示例 3: 响应式UI缩放
    /// </summary>
    public static void Example3_ResponsiveUI()
    {
        // PlatformAdapter adapter = inputManager.PlatformAdapter;
        //
        // // 获取虚拟坐标和屏幕坐标之间的映射
        // Vector2 virtualPos = new Vector2(100, 100);
        // Vector2 screenPos = adapter.VirtualToScreen(virtualPos);
        //
        // // 或反向映射
        // Vector2 touchPos = inputManager.TouchInput.GetActiveTouches()[0].Position;
        // Vector2 virtualTouchPos = adapter.ScreenToVirtual(touchPos);
        //
        // // 缩放矩形
        // Rectangle virtualRect = new Rectangle(0, 0, 100, 50);
        // Rectangle scaledRect = adapter.ScaleRectangle(virtualRect);
        //
        // // 获取推荐的字体大小
        // float fontScale = adapter.GetFontScaleFactor();
        // int adjustedFontSize = (int)(20 * fontScale);
    }

    /// <summary>
    /// 示例 4: 文本输入处理（含虚拟键盘）
    /// </summary>
    public static void Example4_TextInputWithVirtualKeyboard()
    {
        // // 在 Game1.LoadContent() 中
        // if (inputManager.PlatformAdapter.NeedsVirtualKeyboard())
        // {
        //     virtualKeyboard = new VirtualKeyboard(font, 40, 40, 2);
        //
        //     // 订阅虚拟键盘事件
        //     virtualKeyboard.CharacterEntered += (c) =>
        //     {
        //         if (activeInputField == InputField.Username)
        //             loginManager.Username += c;
        //     };
        //
        //     virtualKeyboard.BackspacePressed += () =>
        //     {
        //         if (activeInputField == InputField.Username)
        //         {
        //             var text = loginManager.Username;
        //             if (text.Length > 0)
        //                 loginManager.Username = text[..^1];
        //         }
        //     };
        //
        //     // 显示虚拟键盘（用户点击输入框时）
        //     virtualKeyboard.SetVisible(true);
        // }
        //
        // // 在 Game1.Draw() 中
        // if (virtualKeyboard != null && virtualKeyboard IsVisible)
        // {
        //     virtualKeyboard.Draw(spriteBatch, keyTexture);
        // }
    }

    /// <summary>
    /// 示例 5: 平台检测和条件逻辑
    /// </summary>
    public static void Example5_PlatformDetection()
    {
        // PlatformAdapter adapter = inputManager.PlatformAdapter;
        //
        // if (adapter.Platform == PlatformAdapter.DevicePlatform.Mobile)
        // {
        //     // 移动设备特定代码
        //     // - 增大触摸区域
        //     // - 简化UI
        //     // - 优化性能
        // }
        // else if (adapter.Platform == PlatformAdapter.DevicePlatform.Tablet)
        // {
        //     // 平板电脑特定代码
        //     // - 平衡的UI布局
        // }
        // else
        // {
        //     // 桌面特定代码
        //     // - 完整的UI
        //     // - 支持更多功能
        // }
        //
        // // 处理屏幕方向
        // if (adapter.Orientation == PlatformAdapter.ScreenOrientation.Portrait)
        // {
        //     // 竖屏模式
        // }
        // else
        // {
        //     // 横屏模式
        // }
    }

    /// <summary>
    /// 示例 6: 多点触摸处理
    /// </summary>
    public static void Example6_MultiTouchHandling()
    {
        // // 获取所有活跃的触摸点
        // var activeTouches = inputManager.TouchInput.GetActiveTouches();
        //
        // if (activeTouches.Count >= 2)
        // {
        //     // 至少有两个触摸点 - 可以处理手势
        //     var touch1 = activeTouches[0];
        //     var touch2 = activeTouches[1];
        //
        //     // 计算触摸点之间的距离用于缩放
        //     float distance = Vector2.Distance(touch1.Position, touch2.Position);
        //
        //     // 或等待 Pinch 手势事件
        //     inputManager.TouchInput.GestureDetected += (sender, args) =>
        //     {
        //         if (args.Type == TouchInputManager.GestureType.Pinch)
        //         {
        //             // 使用 args.Scale 进行缩放
        //             ZoomLevel *= args.Scale;
        //         }
        //     };
        // }
    }

    /// <summary>
    /// 示例 7: 手势和菜单滚动
    /// </summary>
    public static void Example7_MenuScrolling()
    {
        // // 监听拖拽手势用于菜单滚动
        // inputManager.TouchInput.GestureDetected += (sender, args) =>
        // {
        //     if (args.Type == TouchInputManager.GestureType.Drag)
        //     {
        //         // args.Delta 是相对移动量
        //         int scrollDelta = (int)args.Delta.Y;
        //
        //         // 向下拖拽 (正值) = 向下滚动菜单
        //         // 向上拖拽 (负值) = 向上滚动菜单
        //         menuManager.Scroll(-scrollDelta);
        //     }
        //     else if (args.Type == TouchInputManager.GestureType.Swipe)
        //     {
        //         // 快速滑动 - 可用于页面切换
        //         if (args.Delta.X > 0)
        //         {
        //             // 向右滑动
        //             SwitchToPreviousPage();
        //         }
        //         else
        //         {
        //             // 向左滑动
        //             SwitchToNextPage();
        //         }
        //     }
        // };
    }

    /// <summary>
    /// 示例 8: 性能优化配置
    /// </summary>
    public static void Example8_PerformanceOptimization()
    {
        // // 在 Game1.Initialize() 中根据平台调整性能设置
        // PlatformAdapter adapter = inputManager.PlatformAdapter;
        //
        // if (adapter.Platform == PlatformAdapter.DevicePlatform.Mobile)
        // {
        //     // 移动设备 - 优先考虑电池寿命和热量
        //     this.TargetElapsedTime = TimeSpan.FromSeconds(1/30.0); // 30 FPS
        //     graphics.SynchronizeWithVerticalRetrace = true;         // 启用垂直同步
        // }
        // else if (adapter.Platform == PlatformAdapter.DevicePlatform.Tablet)
        // {
        //     // 平板电脑 - 平衡性能和响应性
        //     this.TargetElapsedTime = TimeSpan.FromSeconds(1/45.0); // 45 FPS
        //     graphics.SynchronizeWithVerticalRetrace = true;
        // }
        // else
        // {
        //     // 桌面 - 追求最高性能
        //     this.TargetElapsedTime = TimeSpan.FromSeconds(1/60.0); // 60 FPS
        //     graphics.SynchronizeWithVerticalRetrace = false;
        // }
    }

    /// <summary>
    /// 示例 9: 调试和日志记录
    /// </summary>
    public static void Example9_DebuggingAndLogging()
    {
        // // 获取设备信息
        // PlatformAdapter adapter = inputManager.PlatformAdapter;
        // string deviceInfo = adapter.GetDeviceInfo();
        // System.Diagnostics.Debug.WriteLine(deviceInfo);
        // // 输出: Platform: Mobile, Orientation: Portrait, Resolution: 540x960, Scale: (0.42, 1.33)
        //
        // // 检查触摸设备
        // if (inputManager.IsTouchDevice)
        // {
        //     System.Diagnostics.Debug.WriteLine("触摸设备已检测");
        // }
        //
        // // 监听活跃触摸
        // inputManager.TouchInput.GestureDetected += (sender, args) =>
        // {
        //     System.Diagnostics.Debug.WriteLine(
        //         $"手势: {args.Type}, 位置: {args.Position}"
        //     );
        // };
        //
        // // 安全区域信息
        // Rectangle safeArea = adapter.GetSafeArea();
        // System.Diagnostics.Debug.WriteLine(
        //     $"安全区域: {safeArea.X}, {safeArea.Y}, " +
        //     $"{safeArea.Width}x{safeArea.Height}"
        // );
    }

    /// <summary>
    /// 示例 10: 完整的集成示例
    /// </summary>
    public static void Example10_CompleteIntegration()
    {
        // 这是一个完整的游戏循环集成示例：
        //
        // public class Game1 : Game
        // {
        //     private InputManager _inputManager;
        //     private PlatformAdapter _platformAdapter;
        //     private VirtualKeyboard _virtualKeyboard;
        //
        //     public Game1()
        //     {
        //         var graphics = new GraphicsDeviceManager(this);
        //         _platformAdapter = new PlatformAdapter(graphics);
        //         
        //         // 根据平台设置初始分辨率
        //         if (_platformAdapter.Platform == PlatformAdapter.DevicePlatform.Mobile)
        //         {
        //             graphics.PreferredBackBufferWidth = 540;
        //             graphics.PreferredBackBufferHeight = 960;
        //         }
        //     }
        //
        //     protected override void Initialize()
        //     {
        //         // 初始化输入管理器
        //         _inputManager = new InputManager(_graphics);
        //
        //         // 订阅触摸事件
        //         _inputManager.TouchInput.GestureDetected += OnGestureDetected;
        //
        //         base.Initialize();
        //     }
        //
        //     protected override void LoadContent()
        //     {
        //         // 加载虚拟键盘（如果需要）
        //         if (_platformAdapter.NeedsVirtualKeyboard())
        //         {
        //             _virtualKeyboard = new VirtualKeyboard(font, 40, 40, 2);
        //             _virtualKeyboard.CharacterEntered += OnKeyboardCharacter;
        //         }
        //     }
        //
        //     protected override void Update(GameTime gameTime)
        //     {
        //         // 更新输入（包括触摸）
        //         _inputManager.Update(gameTime);
        //
        //         // 处理游戏逻辑
        //         if (_inputManager.IsTouchDevice)
        //         {
        //             // 移动设备逻辑
        //         }
        //         else
        //         {
        //             // 桌面设备逻辑（使用键盘/鼠标）
        //         }
        //     }
        //
        //     protected override void Draw(GameTime gameTime)
        //     {
        //         GraphicsDevice.Clear(Color.Black);
        //
        //         _spriteBatch.Begin();
        //
        //         // 绘制游戏内容
        //
        //         // 绘制虚拟键盘（如果可见）
        //         if (_virtualKeyboard != null)
        //         {
        //             _virtualKeyboard.Draw(_spriteBatch, keyTexture);
        //         }
        //
        //         _spriteBatch.End();
        //     }
        //
        //     private void OnGestureDetected(object sender, 
        //         TouchInputManager.GestureEventArgs args)
        //     {
        //         // 处理不同的手势
        //         switch (args.Type)
        //         {
        //             case TouchInputManager.GestureType.Tap:
        //                 HandleMenuClick(args.Position);
        //                 break;
        //             // ... 其他手势处理
        //         }
        //     }
        //
        //     private void OnKeyboardCharacter(char c)
        //     {
        //         // 处理虚拟键盘输入
        //     }
        // }
    }
}

/// <summary>
/// 注意: 这是一个示例文件，展示如何使用新的移动端功能
/// 实际的实现已集成到 Game1.cs 和其他游戏类中
/// 
/// 关键类：
/// - InputManager: 统一的输入管理，支持键盘、鼠标和触摸
/// - TouchInputManager: 专门处理触摸和手势
/// - PlatformAdapter: 自动平台检测和UI缩放
/// - VirtualKeyboard: 移动设备虚拟键盘UI
/// 
/// 使用这些类可以轻松支持桌面和移动平台！
/// </summary>
/// 