using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Xp;

namespace LifeOS.Core.DTOs.Xp;

public sealed class AwardQuestXpDto
{
    public XpSourceType SourceType { get; set; }

    public Guid SourceEntityId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateOnly BusinessDate { get; set; }

    public EstimatedTime EstimatedTime { get; set; }

    public FrictionLevel FrictionLevel { get; set; }
}
