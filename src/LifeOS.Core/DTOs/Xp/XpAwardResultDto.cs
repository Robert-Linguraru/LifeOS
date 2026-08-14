using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.DTOs.Xp;

public sealed class XpAwardResultDto
{
    public int RawXp { get; init; }

    public int AwardedXp { get; init; }

    public bool IsDuplicate { get; init; }

    public bool IsCapConstrained { get; init; }

    public Guid? TransactionId { get; init; }

    public UserProgressionDto Progression { get; init; } = new();

    public int PreviousLevel { get; init; }

    public int CurrentLevel { get; init; }

    public Echelon PreviousEchelon { get; init; }

    public Echelon CurrentEchelon { get; init; }

    public bool LevelChanged => PreviousLevel != CurrentLevel;

    public bool EchelonChanged => PreviousEchelon != CurrentEchelon;
}
