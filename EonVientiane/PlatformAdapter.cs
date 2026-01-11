using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EonVientiane;

/// <summary>
/// 平台适配器 - 处理不同设备的分辨率、屏幕尺寸和布局调整
/// </summary>
public class PlatformAdapter
{
    public enum DevicePlatform
    {
        Desktop,
        Tablet,
        Mobile
    }

    public enum ScreenOrientation
    {
        Portrait,
        Landscape
    }

    private GraphicsDeviceManager _graphics;
    private int _baseWidth = 1280;
    private int _baseHeight = 720;
    private float _scaleX = 1f;
    private float _scaleY = 1f;
    private DevicePlatform _currentPlatform;
    private ScreenOrientation _screenOrientation = ScreenOrientation.Landscape;
    private float _dpiScale = 1f;

    public int VirtualWidth { get; private set; }
    public int VirtualHeight { get; private set; }
    public float ScaleX => _scaleX;
    public float ScaleY => _scaleY;
    public DevicePlatform Platform => _currentPlatform;
    public ScreenOrientation Orientation => _screenOrientation;
    public float DpiScale => _dpiScale;

    public PlatformAdapter(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;
        DetectPlatform();
        CalculateScaling();
    }

    /// <summary>
    /// 检测运行平台
    /// </summary>
    private void DetectPlatform()
    {
        // 通过屏幕尺寸和纵横比检测平台（在构造阶段 GraphicsDevice 可能尚未创建）
        int width = 0;
        int height = 0;

        // 首选使用显卡适配器的当前显示模式（无需 GraphicsDevice）
        try
        {
            var adapterMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            width = adapterMode.Width;
            height = adapterMode.Height;
        }
        catch
        {
            // 忽略，继续尝试其他来源
        }

        // 若仍未获取，尝试从 GraphicsDevice（若已可用）读取
        if ((width <= 0 || height <= 0) && _graphics != null && _graphics.GraphicsDevice != null)
        {
            var dm = _graphics.GraphicsDevice.DisplayMode;
            width = dm.Width;
            height = dm.Height;
        }

        // 仍未获取有效尺寸，则回退到当前的首选缓冲区或基准分辨率
        if (width <= 0 || height <= 0)
        {
            width = _graphics?.PreferredBackBufferWidth > 0 ? _graphics.PreferredBackBufferWidth : _baseWidth;
            height = _graphics?.PreferredBackBufferHeight > 0 ? _graphics.PreferredBackBufferHeight : _baseHeight;
        }

        float aspectRatio = height > 0 ? (float)width / height : (float)_baseWidth / _baseHeight;

        // 移动设备通常尺寸较小或屏幕比例不同
        if (width < 600 || height < 600)
        {
            _currentPlatform = DevicePlatform.Mobile;
            if (height > width)
            {
                _screenOrientation = ScreenOrientation.Portrait;
            }
        }
        else if (width < 1200 && height < 1200)
        {
            _currentPlatform = DevicePlatform.Tablet;
        }
        else
        {
            _currentPlatform = DevicePlatform.Desktop;
        }
    }

    /// <summary>
    /// 计算缩放比例
    /// </summary>
    private void CalculateScaling()
    {
        int currentWidth = _graphics?.PreferredBackBufferWidth ?? 0;
        int currentHeight = _graphics?.PreferredBackBufferHeight ?? 0;

        // 若首选缓冲区尚未设置，回退至适配器显示模式或基准分辨率
        if (currentWidth <= 0 || currentHeight <= 0)
        {
            try
            {
                var adapterMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                currentWidth = adapterMode.Width;
                currentHeight = adapterMode.Height;
            }
            catch
            {
                currentWidth = _baseWidth;
                currentHeight = _baseHeight;
            }
        }

        VirtualWidth = currentWidth;
        VirtualHeight = currentHeight;

        _scaleX = _baseWidth > 0 ? (float)currentWidth / _baseWidth : 1f;
        _scaleY = _baseHeight > 0 ? (float)currentHeight / _baseHeight : 1f;

        // 计算DPI缩放因子(用于字体大小调整)
        _dpiScale = Math.Min(_scaleX, _scaleY);
        if (_dpiScale <= 0f) _dpiScale = 1f;

        AdjustLayoutForPlatform();
    }

    /// <summary>
    /// 根据平台调整布局
    /// </summary>
    private void AdjustLayoutForPlatform()
    {
        switch (_currentPlatform)
        {
            case DevicePlatform.Mobile:
                // 移动设备: 更大的触摸区域，优化竖屏显示
                if (_screenOrientation == ScreenOrientation.Portrait)
                {
                    // 竖屏模式: 调整宽度为高度比例
                    if (VirtualWidth > VirtualHeight * 0.6f)
                    {
                        _scaleX = _scaleY;
                    }
                }
                break;

            case DevicePlatform.Tablet:
                // 平板电脑: 平衡的UI布局
                break;

            case DevicePlatform.Desktop:
                // 桌面: 保持原始比例
                break;
        }
    }

    /// <summary>
    /// 设置屏幕方向
    /// </summary>
    public void SetScreenOrientation(ScreenOrientation orientation)
    {
        _screenOrientation = orientation;
        CalculateScaling();
    }

    /// <summary>
    /// 根据虚拟坐标转换为实际屏幕坐标
    /// </summary>
    public Vector2 VirtualToScreen(Vector2 virtualPos)
    {
        return new Vector2(
            virtualPos.X * _scaleX,
            virtualPos.Y * _scaleY
        );
    }

    /// <summary>
    /// 根据屏幕坐标转换为虚拟坐标
    /// </summary>
    public Vector2 ScreenToVirtual(Vector2 screenPos)
    {
        return new Vector2(
            screenPos.X / _scaleX,
            screenPos.Y / _scaleY
        );
    }

    /// <summary>
    /// 缩放矩形
    /// </summary>
    public Rectangle ScaleRectangle(Rectangle virtualRect)
    {
        return new Rectangle(
            (int)(virtualRect.X * _scaleX),
            (int)(virtualRect.Y * _scaleY),
            (int)(virtualRect.Width * _scaleX),
            (int)(virtualRect.Height * _scaleY)
        );
    }

    /// <summary>
    /// 获取推荐的按钮大小(像素)
    /// </summary>
    public int GetRecommendedButtonHeight()
    {
        return _currentPlatform switch
        {
            DevicePlatform.Mobile => (int)(50 * _dpiScale),
            DevicePlatform.Tablet => (int)(45 * _dpiScale),
            _ => (int)(40 * _dpiScale)
        };
    }

    /// <summary>
    /// 获取推荐的按钮宽度(像素)
    /// </summary>
    public int GetRecommendedButtonWidth()
    {
        return _currentPlatform switch
        {
            DevicePlatform.Mobile => (int)(200 * _dpiScale),
            DevicePlatform.Tablet => (int)(220 * _dpiScale),
            _ => (int)(240 * _dpiScale)
        };
    }

    /// <summary>
    /// 获取推荐的字体大小缩放
    /// </summary>
    public float GetFontScaleFactor()
    {
        return _currentPlatform switch
        {
            DevicePlatform.Mobile => 1.1f * _dpiScale,
            DevicePlatform.Tablet => 1.0f * _dpiScale,
            _ => 1.0f * _dpiScale
        };
    }

    /// <summary>
    /// 获取推荐的UI边距(像素)
    /// </summary>
    public int GetRecommendedMargin()
    {
        return _currentPlatform switch
        {
            DevicePlatform.Mobile => (int)(8 * _dpiScale),
            DevicePlatform.Tablet => (int)(12 * _dpiScale),
            _ => (int)(15 * _dpiScale)
        };
    }

    /// <summary>
    /// 获取推荐的UI内边距(像素)
    /// </summary>
    public int GetRecommendedPadding()
    {
        return _currentPlatform switch
        {
            DevicePlatform.Mobile => (int)(10 * _dpiScale),
            DevicePlatform.Tablet => (int)(15 * _dpiScale),
            _ => (int)(20 * _dpiScale)
        };
    }

    /// <summary>
    /// 获取安全区域(考虑刘海屏、刷新率条等)
    /// </summary>
    public Rectangle GetSafeArea()
    {
        // 在移动设备上，留出顶部和底部安全区域
        int safeMargin = GetRecommendedMargin() * 2;

        return _currentPlatform switch
        {
            DevicePlatform.Mobile => new Rectangle(
                safeMargin,
                safeMargin * 2,
                VirtualWidth - safeMargin * 2,
                VirtualHeight - safeMargin * 4
            ),
            _ => new Rectangle(0, 0, VirtualWidth, VirtualHeight)
        };
    }

    /// <summary>
    /// 检查触摸区域是否足够大(移动设备优化)
    /// </summary>
    public bool IsTouchAreaLargeEnough(Rectangle area)
    {
        int minSize = GetRecommendedButtonHeight();
        return area.Width >= minSize && area.Height >= minSize;
    }

    /// <summary>
    /// 获取是否需要虚拟键盘(移动设备)
    /// </summary>
    public bool NeedsVirtualKeyboard()
    {
        return _currentPlatform == DevicePlatform.Mobile;
    }

    /// <summary>
    /// 更新显示模式
    /// </summary>
    public void UpdateDisplayMode(int width, int height)
    {
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();
        CalculateScaling();
    }

    /// <summary>
    /// 获取设备信息字符串(用于调试)
    /// </summary>
    public string GetDeviceInfo()
    {
        return $"Platform: {_currentPlatform}, " +
               $"Orientation: {_screenOrientation}, " +
               $"Resolution: {VirtualWidth}x{VirtualHeight}, " +
               $"Scale: ({_scaleX:F2}, {_scaleY:F2})";
    }
}
