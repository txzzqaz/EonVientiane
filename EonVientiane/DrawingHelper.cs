using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EonVientiane;

/// <summary>
/// 绘制辅助类 - 提供常用的绘制方法
/// </summary>
public static class DrawingHelper
{
    /// <summary>
    /// 绘制矩形边框
    /// </summary>
    public static void DrawRectangle(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int lineWidth)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.Left, rect.Top, rect.Width, lineWidth), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Left, rect.Bottom - lineWidth, rect.Width, lineWidth), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Left, rect.Top, lineWidth, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - lineWidth, rect.Top, lineWidth, rect.Height), color);
    }
    
    /// <summary>
    /// 绘制按钮
    /// </summary>
    public static void DrawButton(SpriteBatch spriteBatch, Texture2D texture, MenuButton button, SpriteFont font, Rectangle? customBounds = null)
    {
        Rectangle bounds = customBounds ?? button.Bounds;
        
        MouseState mouseState = Mouse.GetState();
        Point mousePoint = new Point(mouseState.X, mouseState.Y);
        bool isHovered = bounds.Contains(mousePoint);
        
        Color color = isHovered ? button.HoverColor : button.Color;
        
        // 绘制按钮背景
        spriteBatch.Draw(texture, bounds, color);
        
        // 绘制按钮边框
        DrawRectangle(spriteBatch, texture, bounds, Color.White, 2);
        
        // 绘制文字（如果字体已加载）
        if (font != null)
        {
            Vector2 textSize = font.MeasureString(button.Label);
            Vector2 textPosition = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, button.Label, textPosition, Color.White);
        }
    }
}
