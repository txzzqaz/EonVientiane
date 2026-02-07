# 道具功能描述添加 - 完成报告

## 概述
为项目中的所有骰子（5个）和饰品（6个）添加了详细的功能描述（Function属性）。每个功能描述清晰地说明了该道具的机制和效果。

## 骰子 (Dices)

### 1. D6Dice
- **ID**: d6_dice
- **名称**: D6
- **描述**: Reroll your destiny.
- **功能**: 掷六面骰。主动使用获得ATKP（攻击点数）；被动使用获得DEFP（防御点数）。ATKP ≤ DEFP则完全防御无伤；ATKP > DEFP则受到差值伤害
- **使用类型**: Both (主动/被动)

### 2. GuaShaParquetDice
- **ID**: guasha_parquet
- **名称**: 刮痧师傅
- **描述**: 驽马十驾，功在不舍
- **功能**: 掷六面骰获得ATKP。若对方被动防御未完全格挡伤害：根据造成伤害次数进行(6-MITP)面骰投掷，得到额外ATKP，直接再次攻击（跳过对方PD回合）
- **使用类型**: Active (主动)
- **创作者**: yyzh

### 3. FeatheredDice
- **ID**: feathered_dice
- **名称**: 飞羽
- **描述**: 一小步.
- **功能**: 掷(计数器+ATKP×2)面骰获得AVOP（闪避点数）。ATKP > AVOP则闪避成功无伤；ATKP ≤ AVOP则闪避失败受全部伤害。每次使用后计数器临时+1（游戏结束清空）
- **使用类型**: Passive (被动)
- **特性**: 支持计数器 (ICounterDice)

### 4. SpringBreezeDice
- **ID**: spring_breeze
- **名称**: 春风
- **描述**: 生生不息
- **功能**: 掷四面骰获得SPRP（春风点数）。将下一栏位骰子的计数器修改为（原值-SPRP），允许为负数。仅对支持计数器的骰子生效
- **使用类型**: Active (主动)

### 5. ErrorDice
- **ID**: error_dice
- **名称**: ERROR
- **描述**: Debug
- **功能**: 每次使用前可手动输入点数（无上限），否则按D6规则掷出1-6。支持主动和被动使用（调试专用）
- **使用类型**: Both (主动/被动)
- **特性**: 支持手动投掷 (IManualRollDice)

## 饰品 (Accessories)

### 1. SelfAccessory
- **ID**: self_accessory
- **名称**: 自我
- **描述**: 这就是你自己
- **功能**: 对局开始时提供10点生命值（HP）。若当前不能获得HP则无效
- **槽位消耗**: 2

### 2. ForesightAccessory
- **ID**: foresight
- **名称**: 预见
- **描述**: 指向唯一的胜利
- **功能**: 允许在对方行动完成前提前规划后续主动回合的行动，不占用行动时间。启用提前规划功能
- **槽位消耗**: 3

### 3. WandererHeartAccessory
- **ID**: wanderer_heart
- **名称**: 漫游者之心
- **描述**: 纯粹
- **功能**: 若最慢一步选择时间在1秒内，最终攻击点数倍率根据时间增加：0秒=10倍，1秒=1倍。超过1秒无加成。奖励快速操作
- **槽位消耗**: 3

### 4. AscensionProofAccessory
- **ID**: ascension_proof
- **名称**: 飞升之证
- **描述**: 终局？
- **功能**: 无视所有HP加成，强制HP=0且无法获得HP。每连续赢5场计数器永久+1。对局开始获得等于计数器数量的护盾层，每层可抵挡一次未完全防御的攻击
- **槽位消耗**: 11
- **特性**: 支持计数器和护盾机制

### 5. ConcertedEffortAccessory
- **ID**: concerted_effort
- **名称**: 戮力同心
- **描述**: 运，赢！
- **功能**: 若掷出点数与上一次相同（连号），本回合行动效果提升为 n×n（n为点数）倍。否则效果不变但记录本次点数
- **槽位消耗**: 1
- **创作者**: yyzh

### 6. HolyFireAccessory
- **ID**: holy_fire
- **名称**: 圣火
- **描述**: 沧海桑田，然后永恒
- **功能**: 对局内对手每步选择若超过0.5秒，自动选择跳过。限制对手反应时间
- **槽位消耗**: 5

## 技术细节

### 修改的文件
1. `/EonVientiane/Dices/D6Dice.cs`
2. `/EonVientiane/Dices/GuaShaParquetDice.cs`
3. `/EonVientiane/Dices/FeatheredDice.cs`
4. `/EonVientiane/Dices/SpringBreezeDice.cs`
5. `/EonVientiane/Dices/ErrorDice.cs`
6. `/EonVientiane/Accessories/SelfAccessory.cs`
7. `/EonVientiane/Accessories/ForesightAccessory.cs`
8. `/EonVientiane/Accessories/WandererHeartAccessory.cs`
9. `/EonVientiane/Accessories/AscensionProofAccessory.cs`
10. `/EonVientiane/Accessories/ConcertedEffortAccessory.cs`
11. `/EonVientiane/Accessories/HolyFireAccessory.cs`

### 实现方式
- 为所有骰子类（Dice）的构造函数添加 `function` 参数
- 为所有饰品类（Accessory）的构造函数添加 `function` 参数
- 所有参数都正确传递给父类的构造函数
- 编译验证成功，无新增错误

## 用途
- **游戏内展示**: 在物品详情面板或手册中显示功能说明
- **玩家指引**: 帮助玩家了解每个道具的具体效果
- **UI集成**: 可用于创建交互式的功能说明弹窗
- **数据持久化**: Function属性已集成到Item基类，可自动序列化

## 下一步建议
1. 在UIManager或HandbookPanel中集成功能描述的显示
2. 创建一个道具详情面板，同时显示描述(Description)和功能(Function)
3. 考虑为功能说明添加游戏内的视觉示意图或动画演示
