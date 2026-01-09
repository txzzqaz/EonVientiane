using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EonVientiane;

/// <summary>
/// 菜单按钮类
/// </summary>
public class MenuButton
{
    public Rectangle Bounds { get; set; }
    public string Label { get; set; }
    public Color Color { get; set; }
    public Color HoverColor { get; set; }
    
    public MenuButton(Rectangle bounds, string label, Color color, Color hoverColor)
    {
        Bounds = bounds;
        Label = label;
        Color = color;
        HoverColor = hoverColor;
    }
    
    public void OnClick()
    {
        // 可以在这里添加点击音效等
        System.Diagnostics.Debug.WriteLine($"Button clicked: {Label}");
    }
}
