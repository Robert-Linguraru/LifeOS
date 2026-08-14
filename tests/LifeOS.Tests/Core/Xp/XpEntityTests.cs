using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Xp;

namespace LifeOS.Tests.Core.Xp;

public sealed class XpEntityTests
{
    [Fact]
    public void UserProgression_ShouldHaveCanonicalDefaults()
    {
        var progression = new UserProgression();

        Assert.Equal(0L, progression.TotalLifetimeXp);
        Assert.Equal(1, progression.CurrentLevel);
        Assert.Equal(Echelon.Iron, progression.CurrentEchelon);
        Assert.Equal(0, progression.DailyQuestXpToday);
        Assert.Null(progression.DailyQuestXpDate);
        Assert.Equal(0L, progression.Version);
    }

    [Fact]
    public void XpTransaction_ShouldHaveCanonicalDefaults()
    {
        var transaction = new XpTransaction();

        Assert.Equal(XpSource.QuestCompletion, transaction.Source);
        Assert.Null(transaction.SourceType);
        Assert.Null(transaction.SourceEntityId);
        Assert.Equal(0, transaction.XpAmount);
        Assert.Null(transaction.IdempotencyKey);
        Assert.Null(transaction.Notes);
    }

    [Fact]
    public void XpEntities_ShouldBeUserOwnedEntities()
    {
        Assert.IsAssignableFrom<UserOwnedEntity>(new XpTransaction());
        Assert.IsAssignableFrom<UserOwnedEntity>(new UserProgression());
    }

    [Fact]
    public void XpEntities_ShouldNotHaveFeatureNavigationProperties()
    {
        var entityTypes = new[]
        {
            typeof(XpTransaction),
            typeof(UserProgression)
        };

        var forbiddenNames = new[]
        {
            "Task",
            "Habit",
            "Progression",
            "Transactions",
            "Notification"
        };

        foreach (var entityType in entityTypes)
        {
            var properties = entityType
                .GetProperties()
                .Select(property => property.Name);

            Assert.DoesNotContain(
                properties,
                propertyName => forbiddenNames.Contains(propertyName));
        }
    }

    [Fact]
    public void XpTransaction_ShouldExposeDocumentedPropertyTypes()
    {
        Assert.Equal(typeof(XpSource), typeof(XpTransaction)
            .GetProperty(nameof(XpTransaction.Source))!.PropertyType);
        Assert.Equal(typeof(XpSourceType?), typeof(XpTransaction)
            .GetProperty(nameof(XpTransaction.SourceType))!.PropertyType);
        Assert.Equal(typeof(Guid?), typeof(XpTransaction)
            .GetProperty(nameof(XpTransaction.SourceEntityId))!.PropertyType);
        Assert.Equal(typeof(int), typeof(XpTransaction)
            .GetProperty(nameof(XpTransaction.XpAmount))!.PropertyType);
        Assert.Equal(typeof(DateTimeOffset), typeof(XpTransaction)
            .GetProperty(nameof(XpTransaction.OccurredAtUtc))!.PropertyType);
        Assert.Equal(typeof(DateOnly), typeof(XpTransaction)
            .GetProperty(nameof(XpTransaction.BusinessDate))!.PropertyType);
    }

    [Fact]
    public void XpAwardResultDto_ShouldRepresentTransitionsAndNormalOutcomes()
    {
        var result = new XpAwardResultDto
        {
            RawXp = 100,
            AwardedXp = 50,
            IsCapConstrained = true,
            PreviousLevel = 9,
            CurrentLevel = 10,
            PreviousEchelon = Echelon.Iron,
            CurrentEchelon = Echelon.Bronze
        };

        Assert.False(result.IsDuplicate);
        Assert.True(result.IsCapConstrained);
        Assert.True(result.LevelChanged);
        Assert.True(result.EchelonChanged);
        Assert.Null(result.TransactionId);
    }
}
