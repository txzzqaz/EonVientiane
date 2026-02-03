using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EonVientiane;

/// <summary>
/// UI系统API - 提供UI扩展和自定义UI元素支持
/// </summary>
public static class UIAPI
{
    /// <summary>
    /// 自定义UI元素注册表
    /// </summary>
    private static readonly Dictionary<string, ICustomUIElement> _customUIElements = new();
    
    /// <summary>
    /// UI主题列表
    /// </summary>
    private static readonly Dictionary<string, UITheme> _themes = new();
    
    /// <summary>
    /// 当前激活的主题
    /// </summary>
    private static string _activeTheme = "Default";
    
    /// <summary>
    /// UI渲染顺序控制
    /// </summary>
    private static readonly List<(int priority, ICustomUIElement element)> _renderQueue = new();
    
    /// <summary>
    /// UI事件总线
    /// </summary>
    private static readonly Dictionary<string, List<Action<object>>> _eventBus = new();
    
    /// <summary>
    /// 注册自定义UI元素
    /// </summary>
    public static void RegisterUIElement(string elementId, ICustomUIElement element, int renderPriority = 0)
    {
        _customUIElements[elementId] = element;
        _renderQueue.Add((renderPriority, element));
        _renderQueue.Sort((a, b) => a.priority.CompareTo(b.priority));
    }
    
    /// <summary>
    /// 移除自定义UI元素
    /// </summary>
    public static void UnregisterUIElement(string elementId)
    {
        if (_customUIElements.TryGetValue(elementId, out var element))
        {
            _customUIElements.Remove(elementId);
            _renderQueue.RemoveAll(item => item.element == element);
        }
    }
    
    /// <summary>
    /// 获取UI元素
    /// </summary>
    public static ICustomUIElement GetUIElement(string elementId)
    {
        return _customUIElements.TryGetValue(elementId, out var element) ? element : null;
    }
    
    /// <summary>
    /// 注册UI主题
    /// </summary>
    public static void RegisterTheme(string themeName, UITheme theme)
    {
        _themes[themeName] = theme;
    }
    
    /// <summary>
    /// 设置当前主题
    /// </summary>
    public static void SetTheme(string themeName)
    {
        if (_themes.ContainsKey(themeName))
        {
            _activeTheme = themeName;
        }
    }
    
    /// <summary>
    /// 获取当前主题
    /// </summary>
    public static UITheme GetCurrentTheme()
    {
        return _themes.TryGetValue(_activeTheme, out var theme) ? theme : UITheme.Default;
    }
    
    /// <summary>
    /// 订阅UI事件
    /// </summary>
    public static void SubscribeEvent(string eventName, Action<object> handler)
    {
        if (!_eventBus.ContainsKey(eventName))
        {
            _eventBus[eventName] = new List<Action<object>>();
        }
        _eventBus[eventName].Add(handler);
    }
    
    /// <summary>
    /// 取消订阅UI事件
    /// </summary>
    public static void UnsubscribeEvent(string eventName, Action<object> handler)
    {
        if (_eventBus.TryGetValue(eventName, out var handlers))
        {
            handlers.Remove(handler);
        }
    }
    
    /// <summary>
    /// 触发UI事件
    /// </summary>
    public static void TriggerEvent(string eventName, object data = null)
    {
        if (_eventBus.TryGetValue(eventName, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler?.Invoke(data);
            }
        }
    }
    
    /// <summary>
    /// 更新所有自定义UI元素
    /// </summary>
    public static void UpdateCustomElements(GameTime gameTime)
    {
        foreach (var (_, element) in _renderQueue)
        {
            if (element.IsVisible)
            {
                element.Update(gameTime);
            }
        }
    }
    
    /// <summary>
    /// 绘制所有自定义UI元素
    /// </summary>
    public static void DrawCustomElements(SpriteBatch spriteBatch)
    {
        foreach (var (_, element) in _renderQueue)
        {
            if (element.IsVisible)
            {
                element.Draw(spriteBatch);
            }
        }
    }
}

/// <summary>
/// 自定义UI元素接口
/// </summary>
public interface ICustomUIElement
{
    /// <summary>
    /// 元素ID
    /// </summary>
    string ElementId { get; }
    
    /// <summary>
    /// 是否可见
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// 位置
    /// </summary>
    Vector2 Position { get; set; }
    
    /// <summary>
    /// 大小
    /// </summary>
    Vector2 Size { get; set; }
    
    /// <summary>
    /// 更新逻辑
    /// </summary>
    void Update(GameTime gameTime);
    
    /// <summary>
    /// 绘制
    /// </summary>
    void Draw(SpriteBatch spriteBatch);
    
    /// <summary>
    /// 处理点击
    /// </summary>
    bool HandleClick(Vector2 mousePosition);
}

/// <summary>
/// UI主题定义
/// </summary>
public class UITheme
{
    public Color PrimaryColor { get; set; }
    public Color SecondaryColor { get; set; }
    public Color BackgroundColor { get; set; }
    public Color TextColor { get; set; }
    public Color AccentColor { get; set; }
    public Color BorderColor { get; set; }
    public Color HoverColor { get; set; }
    public Color DisabledColor { get; set; }
    
    public static UITheme Default => new UITheme
    {
        PrimaryColor = new Color(50, 50, 50),
        SecondaryColor = new Color(100, 100, 100),
        BackgroundColor = new Color(30, 30, 30),
        TextColor = Color.White,
        AccentColor = new Color(0, 120, 215),
        BorderColor = Color.Gray,
        HoverColor = new Color(70, 70, 70),
        DisabledColor = new Color(150, 150, 150)
    };
    
    public static UITheme Light => new UITheme
    {
        PrimaryColor = Color.White,
        SecondaryColor = new Color(240, 240, 240),
        BackgroundColor = new Color(250, 250, 250),
        TextColor = Color.Black,
        AccentColor = new Color(0, 120, 215),
        BorderColor = new Color(200, 200, 200),
        HoverColor = new Color(230, 230, 230),
        DisabledColor = new Color(180, 180, 180)
    };
}

/// <summary>
/// UI构建器 - 用于快速构建UI布局
/// </summary>
public class UILayoutBuilder
{
    private readonly List<ICustomUIElement> _elements = new();
    private Vector2 _currentPosition;
    private readonly float _spacing;
    
    public UILayoutBuilder(Vector2 startPosition, float spacing = 10f)
    {
        _currentPosition = startPosition;
        _spacing = spacing;
    }
    
    public UILayoutBuilder AddElement(ICustomUIElement element)
    {
        element.Position = _currentPosition;
        _elements.Add(element);
        return this;
    }
    
    public UILayoutBuilder MoveDown(float? customSpacing = null)
    {
        float spacing = customSpacing ?? _spacing;
        if (_elements.Count > 0)
        {
            var lastElement = _elements[_elements.Count - 1];
            _currentPosition.Y += lastElement.Size.Y + spacing;
        }
        return this;
    }
    
    public UILayoutBuilder MoveRight(float? customSpacing = null)
    {
        float spacing = customSpacing ?? _spacing;
        if (_elements.Count > 0)
        {
            var lastElement = _elements[_elements.Count - 1];
            _currentPosition.X += lastElement.Size.X + spacing;
        }
        return this;
    }
    
    public UILayoutBuilder NewRow(float? yPosition = null)
    {
        if (yPosition.HasValue)
        {
            _currentPosition.Y = yPosition.Value;
        }
        else
        {
            MoveDown();
        }
        _currentPosition.X = _elements.Count > 0 ? _elements[0].Position.X : 0;
        return this;
    }
    
    public List<ICustomUIElement> Build()
    {
        return new List<ICustomUIElement>(_elements);
    }
}

/// <summary>
/// 通知系统 - 显示临时通知消息
/// </summary>
public class NotificationSystem
{
    private class Notification
    {
        public string Message { get; set; }
        public float RemainingTime { get; set; }
        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }
    }
    
    private readonly List<Notification> _notifications = new();
    private readonly float _notificationDuration;
    private readonly int _maxNotifications;
    
    public NotificationSystem(float defaultDuration = 3.0f, int maxNotifications = 5)
    {
        _notificationDuration = defaultDuration;
        _maxNotifications = maxNotifications;
    }
    
    public void ShowNotification(string message, Color? backgroundColor = null, Color? textColor = null, float? duration = null)
    {
        var notification = new Notification
        {
            Message = message,
            RemainingTime = duration ?? _notificationDuration,
            BackgroundColor = backgroundColor ?? new Color(50, 50, 50, 200),
            TextColor = textColor ?? Color.White
        };
        
        _notifications.Add(notification);
        
        // 保持通知数量限制
        while (_notifications.Count > _maxNotifications)
        {
            _notifications.RemoveAt(0);
        }
    }
    
    public void Update(float deltaTime)
    {
        for (int i = _notifications.Count - 1; i >= 0; i--)
        {
            _notifications[i].RemainingTime -= deltaTime;
            if (_notifications[i].RemainingTime <= 0)
            {
                _notifications.RemoveAt(i);
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Rectangle screenBounds)
    {
        float y = screenBounds.Height - 50;
        
        for (int i = _notifications.Count - 1; i >= 0; i--)
        {
            var notification = _notifications[i];
            var textSize = font.MeasureString(notification.Message);
            var bgRect = new Rectangle(
                screenBounds.Width - (int)textSize.X - 30,
                (int)y - 25,
                (int)textSize.X + 20,
                40
            );
            
            // 绘制背景
            var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Draw(pixel, bgRect, notification.BackgroundColor);
            
            // 绘制文本
            spriteBatch.DrawString(font, notification.Message, 
                new Vector2(bgRect.X + 10, bgRect.Y + 10), 
                notification.TextColor);
            
            y -= 50;
        }
    }
}
