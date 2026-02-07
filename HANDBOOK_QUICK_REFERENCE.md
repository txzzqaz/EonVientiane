# 图鉴功能快速参考

## 菜单按钮结构

```
┌─ 主菜单 (顶部)
├─ 联机大厅
├─ 背包
├─ 按钮3 (对战历史)
├─ 按钮4 (成就)
├─ 图鉴 ◄━━ 新增
├─ 战斗
└─ 设置 (底部)
```

## 快速访问

| 操作 | 效果 |
|------|------|
| 点击"图鉴" | 进入图鉴界面 |
| 鼠标滚轮 | 滚动物品列表 |
| 左键点击 | 选中物品，显示详情 |
| 切换其他界面 | 保持图鉴数据 |

## 图鉴界面布局

```
┌──────────────────────────────────────────┐
│ 图鉴                  拥有物品: XX种      │
├──────────────────────────────────────────┤
│                                          │
│ ┌──────────────────────────────────────┐ │
│ │ 物品1 (选中)                 数量: 5  │ │
│ ├──────────────────────────────────────┤ │
│ │ 物品2                        数量: 3  │ │
│ ├──────────────────────────────────────┤ │
│ │ 物品3                        数量: 1  │ │
│ │ ...                                  │ │
│ └──────────────────────────────────────┘↕ │
│                                          │
├──────────────────────────────────────────┤
│ 物品详情                                 │
├──────────────────────────────────────────┤
│ 名称: XXX                                │
│ ID: xxx_xxx_xxx                          │
│ 类型: 装备                               │
│ 数量: 5/99                               │
│ 描述: 这是一个很厉害的装备               │
│ 属性: 攻+10 防+5 速+3                     │
└──────────────────────────────────────────┘
```

## 关键方法

### Game1.cs
```csharp
// 处理图鉴输入
private void HandleHandbookInput(MouseState mouseState, MouseState previousMouseState)

// 成员变量
private int _handbookScrollOffset = 0;
private int? _selectedHandbookItemIndex = null;
```

### UIManager.cs (HandbookPanel.cs)
```csharp
// 绘制主面板
public void DrawHandbookPanel(SpriteBatch spriteBatch, InventoryManager inventoryManager, 
                              int scrollOffset = 0, int? selectedItemIndex = null)

// 绘制物品列表
private void DrawHandbookItemList(SpriteBatch spriteBatch, int x, int y, int width, int height,
                                  List<ItemStack> items, int scrollOffset, int? selectedIndex)

// 绘制详情面板
private void DrawHandbookItemDetail(SpriteBatch spriteBatch, ItemStack itemStack, 
                                    int x, int y, int width)
```

## ContentView 枚举映射

| 值 | 意义 |
|----|------|
| 0 | None |
| 1 | Button1 (联机大厅) |
| 2 | Button2 (背包) |
| 3 | Button3 (对战历史) |
| 4 | Button4 (成就) |
| 5 | Button5 (图鉴) |
| 6 | Battle |
| 7 | Settings |

## 修改的文件清单

- ✅ [GameEnums.cs](EonVientiane/GameEnums.cs) - ContentView 枚举
- ✅ [MenuManager.cs](EonVientiane/MenuManager.cs) - 按钮列表
- ✅ [Game1.cs](EonVientiane/Game1.cs) - 逻辑和绘制
- ✅ [UIManager.cs](EonVientiane/UIManager.cs) - partial 类
- ✅ [HandbookPanel.cs](EonVientiane/HandbookPanel.cs) - 新文件（图鉴实现）

## 编译命令

```bash
# 构建客户端
dotnet build EonVientiane/EonVientiane.csproj

# 运行测试
./start_local_test.sh
```

## 排查指南

### 问题：按钮不显示
→ 检查 MenuManager.cs 中的 buttonLabels 数组

### 问题：点击无反应
→ 检查 Game1.cs 中的 HandleGameInput() 是否调用了 HandleHandbookInput()

### 问题：物品列表为空
→ 确认背包中有物品，检查 InventoryManager.InventoryItems

### 问题：编译错误
→ 确保 UIManager.cs 声明为 `partial class`

### 问题：详情面板显示错误
→ 检查 Item 类的属性（Name, Description, Type）
→ 检查 ItemStack 的 Quantity 属性

## 性能优化

- ✅ 使用剪裁区域 (ScissorRectangle) 避免绘制列表外的内容
- ✅ 根据滚动位置动态计算可见的物品，只绘制可见部分
- ✅ 使用高效的列表迭代
- ✅ 滚动条仅在需要时显示

## 兼容性

- ✅ 与现有的成就系统兼容
- ✅ 与现有的背包系统兼容
- ✅ 与菜单系统兼容
- ✅ 支持不同分辨率（动态布局）
