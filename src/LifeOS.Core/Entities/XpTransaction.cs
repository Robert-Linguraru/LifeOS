using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.Entities;

public sealed class XpTransaction : UserOwnedEntity
{
    public XpSource Source { get; set; } = XpSource.QuestCompletion;

    public XpSourceType? SourceType { get; set; }

    public Guid? SourceEntityId { get; set; }

    public int XpAmount { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateOnly BusinessDate { get; set; }

    public string? IdempotencyKey { get; set; }

    public string? Notes { get; set; }
}
