using EonVientiane.GUI.GuiMenus;

namespace EonVientiane.GUI.InventoryMenu;

/// <summary>
/// 背包模块的 GUI 菜单扩展。
/// 编译后 DLL 会被 post-build 脚本复制到 gui-modules/ 目录，
/// GUI 启动时自动扫描并加载此菜单卡片。
/// </summary>
public sealed class InventoryGuiMenuModule : IGuiMenuModule
{
    public GuiMenuDefinition GetMenu() => new(
        ModuleId: "inventory",
        Title: "📦 背包",
        Layout: GuiMenuLayout.Vertical,
        Order: 10,
        Buttons:
        [
            // inv 输出同时包含背包物品与已穿戴装备两个区块
            new GuiMenuButton("查看背包", "inv"),
        ]);
}
