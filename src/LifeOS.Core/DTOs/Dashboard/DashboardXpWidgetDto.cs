using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.DTOs.Dashboard;

public sealed class DashboardXpWidgetDto
{
    public long TotalLifetimeXp { get; init; }

    public int CurrentLevel { get; init; }

    public Echelon CurrentEchelon { get; init; }

    public int DailyQuestXpToday { get; init; }

    public int DailyQuestXpCap { get; init; }

    public int RemainingQuestXp { get; init; }

    public int ProgressPercent { get; init; }
}
