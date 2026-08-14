using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Mappings;

namespace LifeOS.Tests.Core.Xp;

public sealed class XpMappingsTests
{
    [Fact]
    public void XpTransaction_ToDto_ShouldPreserveLedgerValues()
    {
        var transactionId = Guid.NewGuid();
        var sourceEntityId = Guid.NewGuid();
        var occurredAtUtc = new DateTimeOffset(
            2026,
            8,
            12,
            14,
            30,
            0,
            TimeSpan.Zero);
        var transaction = new XpTransaction
        {
            Id = transactionId,
            UserId = Guid.NewGuid(),
            Source = XpSource.QuestCompletion,
            SourceType = XpSourceType.Habit,
            SourceEntityId = sourceEntityId,
            XpAmount = 75,
            OccurredAtUtc = occurredAtUtc,
            BusinessDate = new DateOnly(2026, 8, 12),
            IdempotencyKey = "internal-key",
            Notes = "completed"
        };

        var dto = transaction.ToDto();

        Assert.Equal(transactionId, dto.Id);
        Assert.Equal(XpSource.QuestCompletion, dto.Source);
        Assert.Equal(XpSourceType.Habit, dto.SourceType);
        Assert.Equal(sourceEntityId, dto.SourceEntityId);
        Assert.Equal(75, dto.XpAmount);
        Assert.Equal(occurredAtUtc, dto.OccurredAtUtc);
        Assert.Equal(new DateOnly(2026, 8, 12), dto.BusinessDate);
        Assert.Equal("completed", dto.Notes);
        Assert.Null(typeof(XpTransactionDto)
            .GetProperty(nameof(transaction.IdempotencyKey)));
    }

    [Fact]
    public void UserProgression_ToDto_ShouldPreserveCurrentState()
    {
        var progression = new UserProgression
        {
            UserId = Guid.NewGuid(),
            TotalLifetimeXp = 12600,
            CurrentLevel = 25,
            CurrentEchelon = Echelon.Silver,
            DailyQuestXpToday = 225,
            DailyQuestXpDate = new DateOnly(2026, 8, 12),
            Version = 4
        };

        var dto = progression.ToDto();

        Assert.Equal(12600, dto.TotalLifetimeXp);
        Assert.Equal(25, dto.CurrentLevel);
        Assert.Equal(Echelon.Silver, dto.CurrentEchelon);
        Assert.Equal(225, dto.DailyQuestXpToday);
        Assert.Equal(new DateOnly(2026, 8, 12), dto.DailyQuestXpDate);
    }
}
