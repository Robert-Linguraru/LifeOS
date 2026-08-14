using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Mappings;

public static class XpMappings
{
    public static XpTransactionDto ToDto(this XpTransaction transaction)
    {
        return new XpTransactionDto
        {
            Id = transaction.Id,
            Source = transaction.Source,
            SourceType = transaction.SourceType,
            SourceEntityId = transaction.SourceEntityId,
            XpAmount = transaction.XpAmount,
            OccurredAtUtc = transaction.OccurredAtUtc,
            BusinessDate = transaction.BusinessDate,
            Notes = transaction.Notes
        };
    }

    public static UserProgressionDto ToDto(this UserProgression progression)
    {
        return new UserProgressionDto
        {
            TotalLifetimeXp = progression.TotalLifetimeXp,
            CurrentLevel = progression.CurrentLevel,
            CurrentEchelon = progression.CurrentEchelon,
            DailyQuestXpToday = progression.DailyQuestXpToday,
            DailyQuestXpDate = progression.DailyQuestXpDate
        };
    }
}
