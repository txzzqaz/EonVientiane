# 多人对战血条重叠问题修复

## 问题描述
在进行多人对战时，尤其是一队有多个玩家的情况下，所有玩家的血条都显示在同一位置，导致严重的重叠，玩家无法清楚地看到各个对手的血量，也无法自由选择攻击目标。

## 根本原因
原始代码在绘制血条时，只显示每队的第一个玩家：
```csharp
var leftPlayer = _currentBattle.Team1Players.FirstOrDefault();
var rightPlayer = _currentBattle.Team2Players.FirstOrDefault();

DrawPlayerHealthBar(spriteBatch, texture, font, panelX, leftPlayer, barW, barH, barTop, true);
DrawPlayerHealthBar(spriteBatch, texture, font, panelX + panelWidth, rightPlayer, barW, barH, barTop, false);
```

当一队有多个活着的玩家时，其他玩家的血条完全没有显示。

## 修复方案

### 修改1：增强 `DrawPlayerHealthBar` 方法
**文件**：[EonVientiane/BattleManager.cs](EonVientiane/BattleManager.cs)

添加 `verticalOffset` 参数以支持垂直堆叠血条：

```csharp
private void DrawPlayerHealthBar(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font, 
    int xPosition, Player player, int barW, int barH, int barTop, bool isLeft, int verticalOffset = 0)
{
    if (player == null)
        return;

    int adjustedBarTop = barTop + verticalOffset;
    // ... 血条绘制逻辑使用 adjustedBarTop
}
```

### 修改2：更新主绘制方法显示所有玩家
**位置**：Draw 方法中的血条绘制部分

```csharp
int barSpacing = 35;  // 血条间距

// 显示所有Team1玩家的血条
var team1AlivePlayer = _currentBattle.Team1Players.Where(p => !p.IsDead).ToList();
for (int i = 0; i < team1AlivePlayer.Count; i++)
{
    int verticalOffset = i * barSpacing;
    DrawPlayerHealthBar(spriteBatch, texture, font, panelX, team1AlivePlayer[i], 
        barW, barH, barTop, true, verticalOffset);
}

// 显示所有Team2玩家的血条
var team2AlivePlayer = _currentBattle.Team2Players.Where(p => !p.IsDead).ToList();
for (int i = 0; i < team2AlivePlayer.Count; i++)
{
    int verticalOffset = i * barSpacing;
    DrawPlayerHealthBar(spriteBatch, texture, font, panelX + panelWidth, team2AlivePlayer[i], 
        barW, barH, barTop, false, verticalOffset);
}
```

### 修改3：更新目标选择处理函数
**位置**：HandleOpponentSelection 方法

现在支持选择所有可攻击的对手：

```csharp
private void HandleOpponentSelection(MouseState mouseState, MouseState previousMouseState, int panelX, int panelWidth)
{
    _opponentRects.Clear();
    var opponents = _currentBattle.AvailableOpponents;
    int barSpacing = 35;

    // 添加所有Team1的可攻击对手到碰撞检测列表
    var team1Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team1).ToList();
    for (int i = 0; i < team1Opponents.Count; i++)
    {
        int verticalOffset = i * barSpacing;
        var rect = new Rectangle(panelX + 20 - 10, topY - 10 + verticalOffset, barW + 20, barH + 40);
        _opponentRects.Add((team1Opponents[i], rect));
    }

    // 添加所有Team2的可攻击对手到碰撞检测列表
    var team2Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team2).ToList();
    for (int i = 0; i < team2Opponents.Count; i++)
    {
        int verticalOffset = i * barSpacing;
        var rect = new Rectangle(panelX + panelWidth - 20 - barW - 10, topY - 10 + verticalOffset, barW + 20, barH + 40);
        _opponentRects.Add((team2Opponents[i], rect));
    }

    // 点击检测逻辑...
}
```

### 修改4：更新目标选择可视化
**位置**：DrawBattleActions 方法中的目标高亮部分

在玩家选择骰子后，高亮显示所有可以攻击的对手：

```csharp
if (_pendingSelectedDice != null)
{
    _opponentRects.Clear();
    var opponents = _currentBattle.AvailableOpponents;
    int barSpacing = 35;

    // 高亮所有Team1的可攻击对手
    var team1Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team1).ToList();
    for (int i = 0; i < team1Opponents.Count; i++)
    {
        int verticalOffset = i * barSpacing;
        var rect = new Rectangle(panelX + 20 - 10, barTop - 10 + verticalOffset, barW + 20, barH + 40);
        DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.Yellow, 3);
        _opponentRects.Add((team1Opponents[i], rect));
    }

    // 高亮所有Team2的可攻击对手
    var team2Opponents = opponents.Where(p => p.Camp == PlayerCamp.Team2).ToList();
    for (int i = 0; i < team2Opponents.Count; i++)
    {
        int verticalOffset = i * barSpacing;
        var rect = new Rectangle(panelX + panelWidth - 20 - barW - 10, barTop - 10 + verticalOffset, barW + 20, barH + 40);
        DrawingHelper.DrawRectangle(spriteBatch, texture, rect, Color.Yellow, 3);
        _opponentRects.Add((team2Opponents[i], rect));
    }
}
```

## 改进效果

### 修复前
- ❌ 多个玩家的血条重叠在同一位置
- ❌ 无法清楚看到各个对手的血量
- ❌ 只有第一个玩家可以被点击选中
- ❌ 其他玩家无法选择为攻击目标

### 修复后
- ✅ 所有活着的玩家血条垂直堆叠显示，间距为 35px
- ✅ 每个玩家的血量清晰可见，包括名称、HP、护盾
- ✅ 所有活着的对手都可以被点击选中
- ✅ 可以自由选择攻击任何一个活着的对手
- ✅ 选择骰子后，所有可攻击的对手都会被黄色高亮显示

## 技术细节

### 血条间距计算
- 基础间距：35 像素（包含血条高度 20px + 间隙 15px）
- 垂直偏移：`i * barSpacing`，其中 i 是队伍内的玩家索引

### 碰撞检测
- 每个血条的点击区域：`barW + 20` 宽度，`barH + 40` 高度
- 支持垂直偏移后的正确碰撞检测

### 可用玩家过滤
- 只显示和检测活着的玩家（`!p.IsDead`）
- 确保死亡玩家不占用UI空间

## 编译验证
- ✅ 客户端编译成功：`dotnet build EonVientiane/EonVientiane.csproj`
- ✅ 服务器编译成功：`dotnet build EonVientianeServer/EonVientianeServer.csproj`
- ✅ 无编译错误，仅有预期的警告

## 测试建议
1. 创建多人对战场景，确保一队有多个玩家
2. 启动对战，验证所有玩家血条是否清晰显示
3. 尝试点击不同对手的血条，验证是否能正确选择
4. 选择AD骰子后，验证所有对手是否被黄色高亮
5. 验证死亡的玩家血条是否隐藏

## 相关文件
- [EonVientiane/BattleManager.cs](EonVientiane/BattleManager.cs) - 主要修改文件
- [EonVientiane/Battle.cs](EonVientiane/Battle.cs) - 战斗数据模型（无需修改）
- [EonVientiane/Player.cs](EonVientiane/Player.cs) - 玩家模型（无需修改）
