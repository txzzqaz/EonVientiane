using System.Collections.Generic;
using System.Linq;

namespace EonVientianeServer.Achievements;

public static class AchievementCatalog
{
    private static readonly List<IAchievementDefinition> Definitions = new()
    {
        new FirstDefense.FirstDefenseAchievement(),
        new PerfectVictory.PerfectVictoryAchievement(),
        new LongThinking.LongThinkingAchievement(),
        new BlitzVictory.BlitzVictoryAchievement(),
        new WhereAmI.WhereAmIAchievement(),
        new GuashaMaster.GuashaMasterAchievement(),
        new Miracle.MiracleAchievement(),
        new AbsoluteLuck.AbsoluteLuckAchievement()
    };

    public static IReadOnlyDictionary<string, IAchievementDefinition> AllById { get; } =
        Definitions.ToDictionary(d => d.Id, d => d);

    public static IReadOnlyList<string> DefaultIds { get; } = Definitions.Select(d => d.Id).ToList();

    public static bool TryGet(string achievementId, out IAchievementDefinition definition)
    {
        return AllById.TryGetValue(achievementId, out definition!);
    }
}
