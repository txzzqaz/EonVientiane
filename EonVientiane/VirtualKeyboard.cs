using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 虚拟键盘 - 为移动设备提供屏幕键盘输入
/// </summary>
public class VirtualKeyboard
{
    public class VirtualKey
    {
        public string Label { get; set; }
        public Rectangle Bounds { get; set; }
        public char? Character { get; set; }
        public KeyType Type { get; set; }
        public bool IsPressed { get; set; }

        public enum KeyType
        {
            Character,
            Backspace,
            Space,
            Shift,
            Enter,
            Tab
        }
    }

    private List<VirtualKey> _keys = new();
    private Dictionary<int, VirtualKey> _touchMap = new();
    private int _keyWidth = 40;
    private int _keyHeight = 40;
    private int _keyMargin = 2;
    private Vector2 _position;
    private bool _isVisible = false;
    private bool _isShiftActive = false;
    private SpriteFont _font;
    private Color _keyColor = Color.Gray;
    private Color _keyPressedColor = Color.DarkGray;
    private Color _textColor = Color.White;

    public event Action<char> CharacterEntered;
    public event Action BackspacePressed;
    public event Action EnterPressed;

    public VirtualKeyboard(SpriteFont font, int width, int height, int margin)
    {
        _font = font;
        _keyWidth = width;
        _keyHeight = height;
        _keyMargin = margin;
        _position = Vector2.Zero;
        
        InitializeKeyboard();
    }

    /// <summary>
    /// 初始化虚拟键盘布局
    /// </summary>
    private void InitializeKeyboard()
    {
        // QWERTY 键盘布局
        string[] rows = new[]
        {
            "1234567890",
            "qwertyuiop",
            "asdfghjkl",
            "zxcvbnm"
        };

        float xPos = _position.X;
        float yPos = _position.Y;

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            xPos = _position.X;
            string row = rows[rowIndex];

            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                var key = new VirtualKey
                {
                    Label = c.ToString(),
                    Character = c,
                    Type = VirtualKey.KeyType.Character,
                    Bounds = new Rectangle(
                        (int)xPos,
                        (int)yPos,
                        _keyWidth,
                        _keyHeight
                    )
                };
                _keys.Add(key);
                xPos += _keyWidth + _keyMargin;
            }

            yPos += _keyHeight + _keyMargin;
        }

        // 添加特殊键
        // Backspace
        _keys.Add(new VirtualKey
        {
            Label = "⌫",
            Type = VirtualKey.KeyType.Backspace,
            Bounds = new Rectangle(
                (int)_position.X,
                (int)(yPos + _keyHeight + _keyMargin),
                _keyWidth * 2,
                _keyHeight
            )
        });

        // Space
        _keys.Add(new VirtualKey
        {
            Label = "Space",
            Character = ' ',
            Type = VirtualKey.KeyType.Space,
            Bounds = new Rectangle(
                (int)(_position.X + _keyWidth * 2 + _keyMargin * 2),
                (int)(yPos + _keyHeight + _keyMargin),
                _keyWidth * 4,
                _keyHeight
            )
        });

        // Enter
        _keys.Add(new VirtualKey
        {
            Label = "Enter",
            Type = VirtualKey.KeyType.Enter,
            Bounds = new Rectangle(
                (int)(_position.X + _keyWidth * 6 + _keyMargin * 6),
                (int)(yPos + _keyHeight + _keyMargin),
                _keyWidth * 2,
                _keyHeight
            )
        });
    }

    /// <summary>
    /// 处理触摸输入
    /// </summary>
    public void HandleTouchInput(Vector2 touchPosition)
    {
        if (!_isVisible)
            return;

        for (int i = 0; i < _keys.Count; i++)
        {
            var key = _keys[i];
            if (key.Bounds.Contains((int)touchPosition.X, (int)touchPosition.Y))
            {
                OnKeyPressed(key);
                break;
            }
        }
    }

    /// <summary>
    /// 键盘键被按下
    /// </summary>
    private void OnKeyPressed(VirtualKey key)
    {
        switch (key.Type)
        {
            case VirtualKey.KeyType.Character:
                CharacterEntered?.Invoke(key.Character ?? ' ');
                break;
            case VirtualKey.KeyType.Backspace:
                BackspacePressed?.Invoke();
                break;
            case VirtualKey.KeyType.Space:
                CharacterEntered?.Invoke(' ');
                break;
            case VirtualKey.KeyType.Enter:
                EnterPressed?.Invoke();
                break;
            case VirtualKey.KeyType.Shift:
                _isShiftActive = !_isShiftActive;
                break;
        }
    }

    /// <summary>
    /// 显示/隐藏虚拟键盘
    /// </summary>
    public void SetVisible(bool visible)
    {
        _isVisible = visible;
    }

    /// <summary>
    /// 绘制虚拟键盘
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D keyTexture)
    {
        if (!_isVisible)
            return;

        foreach (var key in _keys)
        {
            // 绘制键盘按钮背景
            Color buttonColor = key.IsPressed ? _keyPressedColor : _keyColor;
            spriteBatch.Draw(
                keyTexture,
                key.Bounds,
                buttonColor
            );

            // 绘制文本
            Vector2 textSize = _font.MeasureString(key.Label);
            Vector2 textPosition = new Vector2(
                key.Bounds.X + (key.Bounds.Width - textSize.X) / 2,
                key.Bounds.Y + (key.Bounds.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(_font, key.Label, textPosition, _textColor);
        }
    }

    /// <summary>
    /// 设置键盘位置
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        float offsetX = position.X - _position.X;
        float offsetY = position.Y - _position.Y;

        _position = position;

        foreach (var key in _keys)
        {
            key.Bounds = new Rectangle(
                (int)(key.Bounds.X + offsetX),
                (int)(key.Bounds.Y + offsetY),
                key.Bounds.Width,
                key.Bounds.Height
            );
        }
    }

    /// <summary>
    /// 获取键盘总高度
    /// </summary>
    public int GetHeight()
    {
        return _keys.Count > 0 
            ? (int)_position.Y + _keyHeight * 5 + _keyMargin * 5
            : 0;
    }

    /// <summary>
    /// 获取键盘总宽度
    /// </summary>
    public int GetWidth()
    {
        return _keyWidth * 10 + _keyMargin * 9;
    }

    /// <summary>
    /// 清空所有键的按下状态
    /// </summary>
    public void ClearPressedState()
    {
        foreach (var key in _keys)
        {
            key.IsPressed = false;
        }
    }
}
