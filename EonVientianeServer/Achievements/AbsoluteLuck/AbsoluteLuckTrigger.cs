using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements.AbsoluteLuck;

public sealed class AbsoluteLuckTrigger : IAchievementTrigger
{
    public AchievementTriggerType TriggerType => AchievementTriggerType.Manual;

    public IEnumerable<string> GetEligiblePlayers(AchievementTriggerContext context)
    {
        // 绝对幸运需要跨战斗追踪，需要通过Manual更新
        // 这里返回空，通过UpdateAchievementProgress手动更新
        return Enumerable.Empty<string>();
    }

    public int CalculateProgress(AchievementTriggerContext context, string playerId)
    {
        // 进度由客户端手动更新
        return 0;
    }
}
