using Avalonia.Controls;

namespace EonVientiane.GUI.GuiMenus;

public enum GuiMenuLayout
{
    Vertical,
    TwoColumns
}

public sealed record GuiMenuButton(
    string Text,
    string Command,
    /// <summary>
    /// 点击按钮后是否激活本模块的内容面板（取代默认日志视图）。
    /// 若为 false，则按钮仅执行命令并在日志区显示输出。
    /// </summary>
    bool ActivatesContent = false);

public sealed record GuiMenuDefinition(
    string ModuleId,
    string Title,
    GuiMenuLayout Layout,
    int Order,
    IReadOnlyList<GuiMenuButton> Buttons);

public interface IGuiMenuModule
{
    GuiMenuDefinition GetMenu();
}

/// <summary>
/// 可选接口：模块同时实现此接口时，可向 GUI 中间区域注入自定义面板。
/// 面板内容由模块完全控制（可以是表格、卡片、战斗动画区等任意 Avalonia Control）。
/// 当用户点击该模块中标注了 ActivatesContent = true 的按钮时，面板会替换默认日志视图显示。
/// 点击其他模块按钮或顶部「clear」按钮时，会切回日志视图。
/// </summary>
public interface IGuiContentModule
{
    string ModuleId { get; }

    /// <summary>
    /// 返回一个 Avalonia Control，GUI 会将其放入中间内容区。    
    /// 此方法每次激活时都会被调用，模块可根据需要返回新实例或复用同一实例。
    /// </summary>
    Control CreateContentPanel();
}
