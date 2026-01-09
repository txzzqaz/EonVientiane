using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EonVientiane;

/// <summary>
/// 背包系统输入处理器
/// </summary>
public class InventoryInputHandler
{
    private int _menuWidth;
    private readonly Action<ItemStack> _onEquipRequested;
    private readonly Action<ItemStack> _onUnequipRequested;

    public InventoryInputHandler(int menuWidth, Action<ItemStack> onEquipRequested, Action<ItemStack> onUnequipRequested)
    {
        _menuWidth = menuWidth;
        _onEquipRequested = onEquipRequested;
        _onUnequipRequested = onUnequipRequested;
    }

    /// <summary>
    /// 处理背包界面的输入
    /// </summary>
    public void HandleInput(MouseState mouseState, MouseState previousMouseState, InventoryManager inventoryManager, 
        ref int? selectedInventoryIndex, ref int? selectedEquipmentIndex, int screenHeight)
    {
        int panelX = _menuWidth;
        int panelWidth = 1280 - _menuWidth; // 假设窗口宽度为1280
        int panelHeight = screenHeight;
        int dividerX = panelX + panelWidth / 2;

        Point mousePoint = new Point(mouseState.X, mouseState.Y);

        // 背包区域
        int inventoryX = panelX + 20;
        int inventoryY = 70;
        int inventoryWidth = panelWidth / 2 - 30;
        int inventoryHeight = panelHeight - 80;

        // 装备区域
        int equipmentX = dividerX + 10;
        int equipmentY = 70;

        // 处理背包滚动
        if (mousePoint.X >= inventoryX && mousePoint.X <= inventoryX + inventoryWidth &&
            mousePoint.Y >= inventoryY && mousePoint.Y <= inventoryY + inventoryHeight)
        {
            int scrollDelta = mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                int itemHeight = 60;
                int itemSpacing = 5;
                int availableHeight = inventoryHeight - 40;
                int totalHeight = inventoryManager.InventoryItems.Count * (itemHeight + itemSpacing);
                int maxScroll = Math.Max(0, totalHeight - availableHeight);

                inventoryManager.InventoryScrollOffset -= scrollDelta / 10;
                inventoryManager.InventoryScrollOffset = Math.Clamp(inventoryManager.InventoryScrollOffset, 0, maxScroll);
            }
        }

        // 处理点击
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            int itemStartY = inventoryY + 40;
            int itemHeight = 60;
            int itemSpacing = 5;

            // 检查背包物品点击
            if (mousePoint.X >= inventoryX && mousePoint.X <= inventoryX + inventoryWidth)
            {
                int relativeY = mousePoint.Y - itemStartY + inventoryManager.InventoryScrollOffset;
                if (relativeY >= 0)
                {
                    int itemIndex = relativeY / (itemHeight + itemSpacing);
                    if (itemIndex < inventoryManager.InventoryItems.Count)
                    {
                        // 双击装备物品
                        if (selectedInventoryIndex == itemIndex)
                        {
                                var stack = inventoryManager.InventoryItems[itemIndex];
                                if (stack.Item.IsEquippable)
                                {
                                    _onEquipRequested?.Invoke(stack);
                                    selectedInventoryIndex = null;
                                    System.Diagnostics.Debug.WriteLine($"请求装备: {stack.Item.Name}");
                                }
                        }
                        else
                        {
                            selectedInventoryIndex = itemIndex;
                            selectedEquipmentIndex = null;
                        }
                    }
                }
            }
            // 检查装备槽位点击
            else if (mousePoint.X >= equipmentX && mousePoint.X <= equipmentX + inventoryWidth)
            {
                int slotStartY = equipmentY + 40;
                int slotHeight = 70;
                int slotSpacing = 5;

                int relativeY = mousePoint.Y - slotStartY;
                if (relativeY >= 0)
                {
                    int equipIndex = relativeY / (slotHeight + slotSpacing);

                    if (equipIndex < inventoryManager.EquippedStacks.Count)
                    {
                        // 双击卸下装备
                        if (selectedEquipmentIndex == equipIndex)
                        {
                            var stack = inventoryManager.EquippedStacks[equipIndex];
                            _onUnequipRequested?.Invoke(stack);
                            System.Diagnostics.Debug.WriteLine($"请求卸下装备索引: {equipIndex}");
                            selectedEquipmentIndex = null;
                        }
                        else
                        {
                            selectedEquipmentIndex = equipIndex;
                            selectedInventoryIndex = null;
                        }
                    }
                }
            }
        }
    }
}
