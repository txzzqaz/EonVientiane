using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EonVientiane;

/// <summary>
/// 图鉴面板扩展方法 - 分离到单独文件以简化UIManager
/// </summary>
public partial class UIManager
{
    /// <summary>
    /// 绘制图鉴面板
    /// </summary>
    public void DrawHandbookPanel(SpriteBatch spriteBatch, InventoryManager inventoryManager, int scrollOffset = 0, int? selectedItemIndex = null)
    {
        int panelX = _menuWidth;
        int panelY = 0;
        int panelWidth = _graphics.PreferredBackBufferWidth - _menuWidth;
        int panelHeight = _graphics.PreferredBackBufferHeight;

        // 获取背包中的物品列表
        var items = inventoryManager.InventoryItems.ToList();

        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

        spriteBatch.Begin();

        // 背景
        spriteBatch.Draw(_buttonTexture, panelRect, Color.DarkSlateGray * 0.6f);

        // 标题
        string title = "图鉴";
        if (_buttonFont != null)
        {
            spriteBatch.DrawString(_buttonFont, title, new Vector2(panelX + 30, panelY + 20), Color.Gold, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

            // 物品种类统计
            string statsText = $"拥有物品: {items.Count}种";
            spriteBatch.DrawString(_buttonFont, statsText, new Vector2(panelX + 30, panelY + 50), Color.LightCyan, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();

        // 绘制物品列表（带剪裁）
        DrawHandbookItemList(spriteBatch, panelX + 20, 80, panelWidth - 40, panelHeight - 110,
            items, scrollOffset, selectedItemIndex);

        // 绘制物品详情面板
        if (selectedItemIndex.HasValue && selectedItemIndex.Value >= 0 && selectedItemIndex.Value < items.Count)
        {
            DrawHandbookItemDetail(spriteBatch, items[selectedItemIndex.Value], panelX + 30, panelHeight - 280, panelWidth - 60);
        }
    }

    /// <summary>
    /// 绘制图鉴物品列表
    /// </summary>
    private void DrawHandbookItemList(SpriteBatch spriteBatch, int x, int y, int width, int height,
        List<ItemStack> items, int scrollOffset, int? selectedIndex)
    {
        const int itemHeight = 80;
        const int itemSpacing = 10;
        int maxVisibleItems = height / (itemHeight + itemSpacing);

        // 设置剪裁区域
        Rectangle scissorRect = new Rectangle(x, y, width, height);
        RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };

        spriteBatch.Begin(rasterizerState: rasterizerState);
        spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

        for (int i = 0; i < items.Count; i++)
        {
            int itemY = y + i * (itemHeight + itemSpacing) - scrollOffset;

            if (itemY + itemHeight < y || itemY > y + height)
                continue;

            var itemStack = items[i];
            var item = itemStack.Item;
            Rectangle itemRect = new Rectangle(x, itemY, width, itemHeight);

            // 背景色（选中高亮）
            Color bgColor = i == selectedIndex ? Color.Gold * 0.3f : Color.DarkSlateGray * 0.8f;
            spriteBatch.Draw(_buttonTexture, itemRect, bgColor);
            DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, itemRect, Color.Gray, 1);

            if (_buttonFont != null)
            {
                int contentX = itemRect.X + 15;
                int contentY = itemRect.Y + 8;

                // 物品名称
                string itemName = item.Name;
                Color nameColor = Color.White;
                spriteBatch.DrawString(_buttonFont, itemName, new Vector2(contentX, contentY), nameColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

                // 物品类型 - 区分骰子类型和饰品
                string itemType = GetItemTypeLabel(item);
                spriteBatch.DrawString(_buttonFont, $"类型: {itemType}", 
                    new Vector2(contentX, contentY + 25), Color.LightGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                // 物品功能
                string function = item.Function ?? "无功能说明";
                spriteBatch.DrawString(_buttonFont, function, 
                    new Vector2(contentX, contentY + 50), Color.LightGray, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.End();

        // 绘制滚动条
        if (items.Count > maxVisibleItems)
        {
            int totalHeight = items.Count * (itemHeight + itemSpacing);
            int scrollBarWidth = 8;
            int scrollBarX = x + width - scrollBarWidth - 2;
            float scrollBarThumbHeight = Math.Max(20, height * (height / (float)totalHeight));
            float maxScrollOffset = totalHeight - height;
            float scrollBarThumbY = y + (scrollOffset / maxScrollOffset) * (height - scrollBarThumbHeight);

            spriteBatch.Begin();

            // 滚动条背景
            spriteBatch.Draw(_buttonTexture, new Rectangle(scrollBarX, y, scrollBarWidth, height), Color.Black * 0.3f);

            // 滚动条滑块
            spriteBatch.Draw(_buttonTexture, new Rectangle(scrollBarX, (int)scrollBarThumbY, scrollBarWidth, (int)scrollBarThumbHeight), Color.White * 0.6f);

            spriteBatch.End();
        }
    }

    /// <summary>
    /// 绘制图鉴物品详情面板
    /// </summary>
    private void DrawHandbookItemDetail(SpriteBatch spriteBatch, ItemStack itemStack, int x, int y, int width)
    {
        int detailHeight = 260;
        Rectangle detailRect = new Rectangle(x, y, width, detailHeight);

        var item = itemStack.Item;

        spriteBatch.Begin();

        // 详情面板背景
        spriteBatch.Draw(_buttonTexture, detailRect, Color.DarkSlateBlue * 0.5f);
        DrawingHelper.DrawRectangle(spriteBatch, _buttonTexture, detailRect, Color.SteelBlue, 3);

        if (_buttonFont != null)
        {
            int detailX = x + 20;
            int detailY = y + 15;
            int lineHeight = 30;

            // 详情标题
            spriteBatch.DrawString(_buttonFont, "物品详情", new Vector2(detailX, detailY), Color.Gold, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);
            detailY += 40;

            // 物品名称
            spriteBatch.DrawString(_buttonFont, $"名称: {item.Name}", 
                new Vector2(detailX, detailY), Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 物品ID
            spriteBatch.DrawString(_buttonFont, $"ID: {item.Id}", 
                new Vector2(detailX, detailY), Color.LightGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 物品类型 - 骰子（AD/PD/AD-PD）或饰品
            string itemType = GetItemTypeLabel(item);
            spriteBatch.DrawString(_buttonFont, $"类型: {itemType}", 
                new Vector2(detailX, detailY), Color.LightCyan, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 描述
            string description = item.Description ?? "无描述";
            spriteBatch.DrawString(_buttonFont, $"描述: {description}", 
                new Vector2(detailX, detailY), Color.LightGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 功能
            string function = item.Function ?? "无功能说明";
            spriteBatch.DrawString(_buttonFont, $"功能: {function}", 
                new Vector2(detailX, detailY), Color.LightGreen, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            detailY += lineHeight;

            // 创作者
            spriteBatch.DrawString(_buttonFont, $"创作者: {item.Creator}", 
                new Vector2(detailX, detailY), Color.LightYellow, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }

    /// <summary>
    /// 获取物品类型标签
    /// </summary>
    private string GetItemTypeLabel(Item item)
    {
        if (item is Dice dice)
        {
            return dice.UsageType switch
            {
                DiceUsageType.Active => "骰子（AD）",
                DiceUsageType.Passive => "骰子（PD）",
                DiceUsageType.Both => "骰子（AD/PD）",
                _ => "骰子"
            };
        }
        else if (item is Accessory)
        {
            return "饰品";
        }
        else
        {
            return item.Type.ToString();
        }
    }
}
