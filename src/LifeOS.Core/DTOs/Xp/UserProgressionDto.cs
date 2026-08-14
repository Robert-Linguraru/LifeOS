using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.DTOs.Xp;

public sealed class UserProgressionDto
{
    public long TotalLifetimeXp { get; init; }

    public int CurrentLevel { get; init; }

    public Echelon CurrentEchelon { get; init; }

    public int DailyQuestXpToday { get; init; }

    public DateOnly? DailyQuestXpDate { get; init; }
}
