using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.DTOs.Xp;

public sealed class XpTransactionDto
{
    public Guid Id { get; init; }

    public XpSource Source { get; init; }

    public XpSourceType? SourceType { get; init; }

    public Guid? SourceEntityId { get; init; }

    public int XpAmount { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public DateOnly BusinessDate { get; init; }

    public string? Notes { get; init; }
}
