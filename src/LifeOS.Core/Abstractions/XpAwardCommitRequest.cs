using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.Abstractions;

public sealed class XpAwardCommitRequest
{
    public Guid UserId { get; init; }

    public XpSource Source { get; init; }

    public XpSourceType? SourceType { get; init; }

    public Guid? SourceEntityId { get; init; }

    public int XpAmount { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public DateOnly BusinessDate { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public long ExpectedVersion { get; init; }

    public long ResultingTotalLifetimeXp { get; init; }

    public int ResultingCurrentLevel { get; init; }

    public Echelon ResultingCurrentEchelon { get; init; }

    public int ResultingDailyQuestXpToday { get; init; }

    public DateOnly? ResultingDailyQuestXpDate { get; init; }

    public long ResultingVersion { get; init; }
}
