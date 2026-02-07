using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.FirstDefense;

public sealed class FirstDefenseTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.BattleEnd;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        if (context.Battle == null)
            return Enumerable.Empty<string>();

        // 简化版本：所有参与战斗的玩家都符合条件
        // 实际的防御检查需要通过战斗日志或计数器实现
        return context.Battle.GetAllPlayers().Select(p => p.PlayerId).ToList();
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 第一次防御就完成，进度为1
        return 1;
    }
}
