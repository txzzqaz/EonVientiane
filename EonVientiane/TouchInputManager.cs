using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EonVientiane;

/// <summary>
/// 触摸输入管理器 - 处理移动设备的触摸输入
/// </summary>
public class TouchInputManager
{
    public class TouchInput
    {
        public Vector2 Position { get; set; }
        public TouchLocationState State { get; set; }
        public int Id { get; set; }
        public long Timestamp { get; set; }
        
        /// <summary>
        /// 检查触摸是否在指定区域内
        /// </summary>
        public bool IsWithinBounds(Rectangle bounds)
        {
            return bounds.Contains((int)Position.X, (int)Position.Y);
        }
    }

    /// <summary>
    /// 触摸手势事件参数
    /// </summary>
    public class GestureEventArgs : EventArgs
    {
        public GestureType Type { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Delta { get; set; }
        public float Scale { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public enum GestureType
    {
        Tap,           // 点击
        DoubleTap,     // 双击
        LongPress,     // 长按
        Drag,          // 拖拽
        Pinch,         // 双指缩放
        Swipe          // 滑动
    }

    private Dictionary<int, TouchInput> _activeTouches = new();
    private Dictionary<int, Stopwatch> _touchTimers = new();
    private Dictionary<int, Vector2> _touchStartPositions = new();
    
    // 手势检测参数
    private const float SWIPE_THRESHOLD = 50f;      // 滑动距离阈值
    private const float DRAG_THRESHOLD = 10f;       // 拖拽距离阈值
    private const long LONG_PRESS_DURATION = 500;   // 长按时长(毫秒)
    private const long DOUBLE_TAP_WINDOW = 300;     // 双击时间窗口(毫秒)
    
    private long _lastTapTime = 0;
    private Vector2 _lastTapPosition = Vector2.Zero;

    public event EventHandler<GestureEventArgs> GestureDetected;

    public TouchInputManager()
    {
        // 在移动设备上启用手势识别
        try
        {
            // MonoGame 3.8 支持: Tap, DoubleTap, FreeDrag, DragComplete, Pinch, PinchComplete
            TouchPanel.EnabledGestures = 
                Microsoft.Xna.Framework.Input.Touch.GestureType.Tap |
                Microsoft.Xna.Framework.Input.Touch.GestureType.DoubleTap |
                Microsoft.Xna.Framework.Input.Touch.GestureType.FreeDrag |
                Microsoft.Xna.Framework.Input.Touch.GestureType.Pinch;
        }
        catch
        {
            // 非触摸设备上可能不支持
        }
    }

    /// <summary>
    /// 更新触摸输入状态
    /// </summary>
    public void Update(GameTime gameTime)
    {
        UpdateTouchLocations();
        DetectGestures(gameTime);
    }

    /// <summary>
    /// 更新触摸位置
    /// </summary>
    private void UpdateTouchLocations()
    {
        try
        {
            TouchCollection touchLocations = TouchPanel.GetState();
            var currentTouchIds = new HashSet<int>();

            foreach (TouchLocation touch in touchLocations)
            {
                currentTouchIds.Add(touch.Id);

                var touchInput = new TouchInput
                {
                    Position = touch.Position,
                    State = touch.State,
                    Id = touch.Id,
                    Timestamp = Stopwatch.GetTimestamp()
                };

                if (!_activeTouches.ContainsKey(touch.Id))
                {
                    // 新的触摸开始
                    _activeTouches[touch.Id] = touchInput;
                    _touchStartPositions[touch.Id] = touch.Position;
                    _touchTimers[touch.Id] = Stopwatch.StartNew();
                }
                else
                {
                    // 更新现有触摸
                    _activeTouches[touch.Id] = touchInput;
                }
            }

            // 移除已结束的触摸
            var endedTouches = new List<int>();
            foreach (var touchId in _activeTouches.Keys)
            {
                if (!currentTouchIds.Contains(touchId))
                {
                    endedTouches.Add(touchId);
                }
            }

            foreach (var touchId in endedTouches)
            {
                _activeTouches.Remove(touchId);
                _touchTimers.Remove(touchId);
                _touchStartPositions.Remove(touchId);
            }
        }
        catch
        {
            // 在非移动设备上，触摸API可能不可用
            // 安全地处理异常
        }
    }

    /// <summary>
    /// 检测手势
    /// </summary>
    private void DetectGestures(GameTime gameTime)
    {
        while (TouchPanel.IsGestureAvailable)
        {
            try
            {
                GestureSample gesture = TouchPanel.ReadGesture();

                // MonoGame 3.8 支持的手势类型
                switch (gesture.GestureType)
                {
                    case Microsoft.Xna.Framework.Input.Touch.GestureType.Tap:
                        HandleTap(gesture.Position, gameTime);
                        break;

                    case Microsoft.Xna.Framework.Input.Touch.GestureType.DoubleTap:
                        GestureDetected?.Invoke(this, new GestureEventArgs
                        {
                            Type = GestureType.DoubleTap,
                            Position = gesture.Position
                        });
                        break;

                    case Microsoft.Xna.Framework.Input.Touch.GestureType.FreeDrag:
                        GestureDetected?.Invoke(this, new GestureEventArgs
                        {
                            Type = GestureType.Drag,
                            Position = gesture.Position,
                            Delta = gesture.Delta
                        });
                        break;

                    case Microsoft.Xna.Framework.Input.Touch.GestureType.DragComplete:
                        // 拖拽完成 - 作为Swipe手势处理
                        GestureDetected?.Invoke(this, new GestureEventArgs
                        {
                            Type = GestureType.Swipe,
                            Position = gesture.Position,
                            Delta = gesture.Delta
                        });
                        break;

                    case Microsoft.Xna.Framework.Input.Touch.GestureType.Pinch:
                    case Microsoft.Xna.Framework.Input.Touch.GestureType.PinchComplete:
                        GestureDetected?.Invoke(this, new GestureEventArgs
                        {
                            Type = GestureType.Pinch,
                            Position = gesture.Position,
                            Scale = 1.0f
                        });
                        break;
                }
            }
            catch
            {
                // 安全地处理手势读取错误
                break;
            }
        }
    }

    /// <summary>
    /// 处理点击手势
    /// </summary>
    private void HandleTap(Vector2 position, GameTime gameTime)
    {
        long currentTime = gameTime.TotalGameTime.Ticks / TimeSpan.TicksPerMillisecond;
        long timeSinceLastTap = currentTime - _lastTapTime;
        bool isDoubleTap = timeSinceLastTap < DOUBLE_TAP_WINDOW && 
                          Vector2.Distance(position, _lastTapPosition) < DRAG_THRESHOLD;

        if (isDoubleTap)
        {
            GestureDetected?.Invoke(this, new GestureEventArgs
            {
                Type = GestureType.DoubleTap,
                Position = position
            });
            _lastTapTime = 0; // 重置以防止三击
        }
        else
        {
            GestureDetected?.Invoke(this, new GestureEventArgs
            {
                Type = GestureType.Tap,
                Position = position
            });
            _lastTapTime = currentTime;
        }

        _lastTapPosition = position;
    }

    /// <summary>
    /// 获取所有活跃的触摸点
    /// </summary>
    public IReadOnlyCollection<TouchInput> GetActiveTouches()
    {
        return _activeTouches.Values;
    }

    /// <summary>
    /// 获取特定触摸点
    /// </summary>
    public TouchInput GetTouch(int touchId)
    {
        return _activeTouches.TryGetValue(touchId, out var touch) ? touch : null;
    }

    /// <summary>
    /// 检查是否有触摸在指定区域内
    /// </summary>
    public bool IsTouchInBounds(Rectangle bounds)
    {
        foreach (var touch in _activeTouches.Values)
        {
            if (touch.IsWithinBounds(bounds))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取在指定区域内的触摸点
    /// </summary>
    public TouchInput GetTouchInBounds(Rectangle bounds)
    {
        foreach (var touch in _activeTouches.Values)
        {
            if (touch.IsWithinBounds(bounds))
                return touch;
        }
        return null;
    }

    /// <summary>
    /// 清空所有触摸状态
    /// </summary>
    public void Clear()
    {
        _activeTouches.Clear();
        _touchTimers.Clear();
        _touchStartPositions.Clear();
    }

    /// <summary>
    /// 获取活跃触摸数量
    /// </summary>
    public int GetActiveTouchCount() => _activeTouches.Count;
}
