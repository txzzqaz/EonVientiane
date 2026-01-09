using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 菜单系统管理器，负责菜单按钮的管理、布局和交互处理
/// </summary>
public class MenuManager
{
    private const int MenuWidth = 150;
    private const int ButtonHeight = 50;
    private const int ButtonMargin = 10;

    // 菜单按钮
    private MenuButton _topButton;
    private MenuButton _bottomButton;
    private List<MenuButton> _middleButtons;

    // 滚动控制
    private float _scrollOffset = 0;
    private float _maxScroll = 0;
    private bool _isDragging = false;
    private int _dragStartY = 0;
    private float _dragStartOffset = 0;

    private GraphicsDeviceManager _graphics;

    public float ScrollOffset => _scrollOffset;
    public float MaxScroll => _maxScroll;
    public MenuButton TopButton => _topButton;
    public MenuButton BottomButton => _bottomButton;
    public List<MenuButton> MiddleButtons => _middleButtons;

    public MenuManager(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;
        _middleButtons = new List<MenuButton>();
    }

    /// <summary>
    /// 初始化菜单按钮
    /// </summary>
    public void InitializeButtons(Texture2D texture, SpriteFont font)
    {
        // 初始化顶部按钮
        _topButton = new MenuButton(
            new Rectangle(ButtonMargin, ButtonMargin, MenuWidth - ButtonMargin * 2, ButtonHeight),
            "主菜单",
            Color.DarkBlue,
            Color.LightBlue
        );

        // 初始化底部按钮
        int bottomY = _graphics.PreferredBackBufferHeight - ButtonHeight - ButtonMargin;
        _bottomButton = new MenuButton(
            new Rectangle(ButtonMargin, bottomY, MenuWidth - ButtonMargin * 2, ButtonHeight),
            "设置",
            Color.DarkGreen,
            Color.LightGreen
        );

        // 初始化中间按钮列表
        string[] buttonLabels = { "联机大厅", "按钮2", "按钮3", "按钮4", "战斗" };
        foreach (var label in buttonLabels)
        {
            AddMiddleButton(label);
        }
    }

    private void UpdateMaxScroll()
    {
        int topAreaHeight = ButtonMargin + ButtonHeight + ButtonMargin;
        int bottomAreaHeight = ButtonHeight + ButtonMargin;
        int availableHeight = _graphics.PreferredBackBufferHeight - topAreaHeight - bottomAreaHeight;

        int totalMiddleButtonsHeight = _middleButtons.Count * (ButtonHeight + ButtonMargin);
        _maxScroll = Math.Max(0, totalMiddleButtonsHeight - availableHeight);
    }

    private void RelayoutMiddleButtons()
    {
        int startY = ButtonMargin + ButtonHeight + ButtonMargin;
        for (int i = 0; i < _middleButtons.Count; i++)
        {
            var b = _middleButtons[i];
            b.Bounds = new Rectangle(
                ButtonMargin,
                startY + i * (ButtonHeight + ButtonMargin),
                MenuWidth - ButtonMargin * 2,
                ButtonHeight
            );
        }
        UpdateMaxScroll();
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
    }

    /// <summary>
    /// 添加中间按钮
    /// </summary>
    public void AddMiddleButton(string label, Color? color = null, Color? hoverColor = null, int? insertIndex = null)
    {
        var btn = new MenuButton(
            new Rectangle(ButtonMargin, 0, MenuWidth - ButtonMargin * 2, ButtonHeight),
            label,
            color ?? Color.DarkSlateGray,
            hoverColor ?? Color.Gray
        );

        if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= _middleButtons.Count)
        {
            _middleButtons.Insert(insertIndex.Value, btn);
        }
        else
        {
            _middleButtons.Add(btn);
        }
        RelayoutMiddleButtons();
    }

    /// <summary>
    /// 移除指定索引的中间按钮
    /// </summary>
    public bool RemoveMiddleButton(int index)
    {
        if (index < 0 || index >= _middleButtons.Count)
            return false;
        _middleButtons.RemoveAt(index);
        RelayoutMiddleButtons();
        return true;
    }

    /// <summary>
    /// 按标签移除中间按钮
    /// </summary>
    public int RemoveMiddleButtonByLabel(string label)
    {
        int removed = _middleButtons.RemoveAll(b => b.Label == label);
        if (removed > 0)
            RelayoutMiddleButtons();
        return removed;
    }

    /// <summary>
    /// 处理菜单输入
    /// </summary>
    public MenuClickResult HandleInput(MouseState mouseState, MouseState previousMouseState)
    {
        var result = new MenuClickResult();

        // 检查顶部和底部按钮点击
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mousePoint = new Point(mouseState.X, mouseState.Y);

            if (_topButton.Bounds.Contains(mousePoint))
            {
                _topButton.OnClick();
                result.TopButtonClicked = true;
            }

            if (_bottomButton.Bounds.Contains(mousePoint))
            {
                _bottomButton.OnClick();
                result.BottomButtonClicked = true;
            }

            // 检查中间按钮点击
            foreach (var button in _middleButtons)
            {
                Rectangle adjustedBounds = new Rectangle(
                    button.Bounds.X,
                    button.Bounds.Y - (int)_scrollOffset,
                    button.Bounds.Width,
                    button.Bounds.Height
                );

                int topLimit = ButtonMargin + ButtonHeight + ButtonMargin;
                int bottomLimit = _graphics.PreferredBackBufferHeight - ButtonHeight - ButtonMargin;

                if (adjustedBounds.Y >= topLimit && adjustedBounds.Y + adjustedBounds.Height <= bottomLimit)
                {
                    if (adjustedBounds.Contains(mousePoint))
                    {
                        button.OnClick();
                        int index = _middleButtons.IndexOf(button);
                        result.MiddleButtonClicked = true;
                        result.ClickedButtonIndex = index;
                        result.ClickedButtonLabel = button.Label;
                    }
                }
            }

            // 开始拖动检测
            if (mouseState.X < MenuWidth)
            {
                _isDragging = true;
                _dragStartY = mouseState.Y;
                _dragStartOffset = _scrollOffset;
            }
        }

        // 处理拖动
        if (_isDragging)
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                int deltaY = mouseState.Y - _dragStartY;
                _scrollOffset = _dragStartOffset - deltaY;
                _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
            }
            else
            {
                _isDragging = false;
            }
        }

        // 鼠标滚轮滚动（仅在菜单区域）
        if (mouseState.X < MenuWidth)
        {
            int scrollDelta = mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;
            _scrollOffset -= scrollDelta * 0.1f;
            _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
        }

        return result;
    }

    /// <summary>
    /// 绘制菜单
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, GraphicsDevice graphicsDevice)
    {
        // 绘制菜单背景
        spriteBatch.Draw(texture, new Rectangle(0, 0, MenuWidth, graphicsDevice.Viewport.Height), Color.Black * 0.8f);

        // 绘制顶部按钮
        DrawingHelper.DrawButton(spriteBatch, texture, _topButton, font);

        // 绘制底部按钮
        DrawingHelper.DrawButton(spriteBatch, texture, _bottomButton, font);

        // 设置剪裁区域绘制中间按钮
        int topLimit = ButtonMargin + ButtonHeight + ButtonMargin;
        int bottomLimit = graphicsDevice.Viewport.Height - ButtonHeight - ButtonMargin;

        // 结束当前批次以设置剪裁
        spriteBatch.End();

        // 使用剪裁区域
        Rectangle scissorRect = new Rectangle(0, topLimit, MenuWidth, bottomLimit - topLimit);
        RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };

        spriteBatch.Begin(rasterizerState: rasterizerState);
        graphicsDevice.ScissorRectangle = scissorRect;

        // 绘制中间按钮
        foreach (var button in _middleButtons)
        {
            Rectangle adjustedBounds = new Rectangle(
                button.Bounds.X,
                button.Bounds.Y - (int)_scrollOffset,
                button.Bounds.Width,
                button.Bounds.Height
            );

            // 只绘制可见区域内的按钮
            if (adjustedBounds.Y + adjustedBounds.Height >= topLimit && adjustedBounds.Y <= bottomLimit)
            {
                DrawingHelper.DrawButton(spriteBatch, texture, button, font, adjustedBounds);
            }
        }

        spriteBatch.End();

        // 绘制滚动条（如果需要）
        if (_maxScroll > 0)
        {
            spriteBatch.Begin();

            int scrollBarX = MenuWidth - 5;
            int scrollBarHeight = bottomLimit - topLimit;
            float scrollBarThumbHeight = Math.Max(20, scrollBarHeight * (scrollBarHeight / (float)(scrollBarHeight + _maxScroll)));
            float scrollBarThumbY = topLimit + (_scrollOffset / _maxScroll) * (scrollBarHeight - scrollBarThumbHeight);

            spriteBatch.Draw(texture,
                new Rectangle(scrollBarX, (int)scrollBarThumbY, 3, (int)scrollBarThumbHeight),
                Color.White * 0.5f);

            spriteBatch.End();
        }

        // 重新开始一个新的批次用于后续绘制
        spriteBatch.Begin();
    }

    /// <summary>
    /// 获取菜单宽度
    /// </summary>
    public static int GetMenuWidth() => 150;

    /// <summary>
    /// 获取按钮高度
    /// </summary>
    public static int GetButtonHeight() => 50;
}

/// <summary>
/// 菜单点击结果
/// </summary>
public class MenuClickResult
{
    public bool TopButtonClicked { get; set; }
    public bool BottomButtonClicked { get; set; }
    public bool MiddleButtonClicked { get; set; }
    public int ClickedButtonIndex { get; set; }
    public string ClickedButtonLabel { get; set; }
}
