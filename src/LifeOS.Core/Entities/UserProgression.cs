using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.Entities;

public sealed class UserProgression : UserOwnedEntity
{
    public long TotalLifetimeXp { get; set; }

    public int CurrentLevel { get; set; } = 1;

    public Echelon CurrentEchelon { get; set; } = Echelon.Iron;

    public int DailyQuestXpToday { get; set; }

    public DateOnly? DailyQuestXpDate { get; set; }

    public long Version { get; set; }
}
