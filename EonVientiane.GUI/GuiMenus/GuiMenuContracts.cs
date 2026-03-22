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

public sealed record GuiStructuredContentItem(
    string PrimaryText,
    string? SecondaryText = null,
    string? Badge = null,
    string? ActionText = null,
    string? ActionCommand = null);

public sealed record GuiStructuredContentSection(
    string Title,
    IReadOnlyList<GuiStructuredContentItem> Items);

public sealed record GuiStructuredContentDefinition(
    string ModuleId,
    string Title,
    IReadOnlyList<GuiStructuredContentSection> Sections);

public sealed record GuiContentProviderDefinition(
    string ModuleId,
    Type ProviderType);
