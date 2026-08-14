using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Xp;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace LifeOS.Tests.Infrastructure;

public sealed class XpPersistenceIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public XpPersistenceIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task XpTransaction_ShouldRoundTripPostgreSqlValues()
    {
        var userId = Guid.NewGuid();
        var sourceEntityId = Guid.NewGuid();
        var occurredAtUtc = new DateTimeOffset(
            2026,
            8,
            12,
            14,
            30,
            45,
            TimeSpan.Zero);
        var transaction = new XpTransaction
        {
            UserId = userId,
            Source = XpSource.QuestCompletion,
            SourceType = XpSourceType.Habit,
            SourceEntityId = sourceEntityId,
            XpAmount = 75,
            OccurredAtUtc = occurredAtUtc,
            BusinessDate = new DateOnly(2026, 8, 12),
            IdempotencyKey = $"HabitComplete:{sourceEntityId:D}:2026-08-12",
            Notes = "round-trip"
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.XpTransactions.Add(transaction);
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var persisted = await context.XpTransactions
                .AsNoTracking()
                .SingleAsync(item => item.Id == transaction.Id);

            Assert.Equal(userId, persisted.UserId);
            Assert.Equal(XpSource.QuestCompletion, persisted.Source);
            Assert.Equal(XpSourceType.Habit, persisted.SourceType);
            Assert.Equal(sourceEntityId, persisted.SourceEntityId);
            Assert.Equal(75, persisted.XpAmount);
            Assert.Equal(
                occurredAtUtc.ToUniversalTime(),
                persisted.OccurredAtUtc);
            Assert.Equal(new DateOnly(2026, 8, 12), persisted.BusinessDate);
            Assert.Equal(transaction.IdempotencyKey, persisted.IdempotencyKey);
            Assert.Equal("round-trip", persisted.Notes);
        }
    }

    [Fact]
    public async Task UserProgression_ShouldRoundTrip64BitAndDateValues()
    {
        var userId = Guid.NewGuid();
        var progression = new UserProgression
        {
            UserId = userId,
            TotalLifetimeXp = (long)int.MaxValue + 42,
            CurrentLevel = 100,
            CurrentEchelon = Echelon.Apex,
            DailyQuestXpToday = 400,
            DailyQuestXpDate = new DateOnly(2026, 8, 12),
            Version = long.MaxValue - 1
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.UserProgressions.Add(progression);
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var persisted = await context.UserProgressions
                .AsNoTracking()
                .SingleAsync(item => item.Id == progression.Id);

            Assert.Equal(userId, persisted.UserId);
            Assert.Equal((long)int.MaxValue + 42, persisted.TotalLifetimeXp);
            Assert.Equal(100, persisted.CurrentLevel);
            Assert.Equal(Echelon.Apex, persisted.CurrentEchelon);
            Assert.Equal(400, persisted.DailyQuestXpToday);
            Assert.Equal(new DateOnly(2026, 8, 12), persisted.DailyQuestXpDate);
            Assert.Equal(long.MaxValue - 1, persisted.Version);
        }
    }

    [Fact]
    public async Task XpTransaction_IdempotencyKey_ShouldBeUniquePerUserAndAllowNulls()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        const string sharedKey = "TaskComplete:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

        await using (var context = _fixture.CreateDbContext())
        {
            context.XpTransactions.AddRange(
                CreateTransaction(firstUserId, sharedKey),
                CreateTransaction(secondUserId, sharedKey),
                CreateTransaction(firstUserId, null),
                CreateTransaction(firstUserId, null));

            await context.SaveChangesAsync();
        }

        await using var duplicateContext = _fixture.CreateDbContext();
        duplicateContext.XpTransactions.Add(
            CreateTransaction(firstUserId, sharedKey));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(
            exception.InnerException);
        Assert.Equal(
            "IX_XpTransactions_UserId_IdempotencyKey",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task UserProgression_UserId_ShouldBeUnique()
    {
        var userId = Guid.NewGuid();

        await using (var context = _fixture.CreateDbContext())
        {
            context.UserProgressions.Add(new UserProgression { UserId = userId });
            await context.SaveChangesAsync();
        }

        await using var duplicateContext = _fixture.CreateDbContext();
        duplicateContext.UserProgressions.Add(
            new UserProgression { UserId = userId });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(
            exception.InnerException);
        Assert.Equal(
            "IX_UserProgressions_UserId",
            postgresException.ConstraintName);
    }

    [Theory]
    [InlineData(
        "CK_UserProgressions_TotalLifetimeXp_NonNegative",
        -1L,
        1,
        0,
        0L)]
    [InlineData(
        "CK_UserProgressions_CurrentLevel_AtLeastOne",
        0L,
        0,
        0,
        0L)]
    [InlineData(
        "CK_UserProgressions_DailyQuestXpToday_InRange",
        0L,
        1,
        -1,
        0L)]
    [InlineData(
        "CK_UserProgressions_DailyQuestXpToday_InRange",
        0L,
        1,
        501,
        0L)]
    [InlineData(
        "CK_UserProgressions_Version_NonNegative",
        0L,
        1,
        0,
        -1L)]
    public async Task UserProgression_CheckConstraints_ShouldRejectInvalidValues(
        string constraintName,
        long totalLifetimeXp,
        int currentLevel,
        int dailyQuestXpToday,
        long version)
    {
        await using var context = _fixture.CreateDbContext();
        context.UserProgressions.Add(new UserProgression
        {
            UserId = Guid.NewGuid(),
            TotalLifetimeXp = totalLifetimeXp,
            CurrentLevel = currentLevel,
            CurrentEchelon = Echelon.Iron,
            DailyQuestXpToday = dailyQuestXpToday,
            Version = version
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(
            exception.InnerException);
        Assert.Equal(constraintName, postgresException.ConstraintName);
    }

    [Fact]
    public async Task XpTransaction_ShouldAcceptNegativeAmountAndArbitrarySourceId()
    {
        await using var context = _fixture.CreateDbContext();
        var transaction = CreateTransaction(
            Guid.NewGuid(),
            null,
            Guid.NewGuid());
        transaction.XpAmount = -25;

        context.XpTransactions.Add(transaction);
        await context.SaveChangesAsync();

        var persisted = await context.XpTransactions
            .AsNoTracking()
            .SingleAsync(item => item.Id == transaction.Id);

        Assert.Equal(-25, persisted.XpAmount);
        Assert.Equal(transaction.SourceEntityId, persisted.SourceEntityId);
    }

    [Fact]
    public async Task MigrationChain_ShouldIncludeAddXpProgression()
    {
        await using var context = _fixture.CreateDbContext();

        var migrations = await context.Database
            .GetAppliedMigrationsAsync();

        Assert.Contains(
            migrations,
            migration => migration.EndsWith(
                "_AddXpProgression",
                StringComparison.Ordinal));
    }

    private static XpTransaction CreateTransaction(
        Guid userId,
        string? idempotencyKey,
        Guid? sourceEntityId = null)
    {
        return new XpTransaction
        {
            UserId = userId,
            Source = XpSource.QuestCompletion,
            SourceType = XpSourceType.Task,
            SourceEntityId = sourceEntityId ?? Guid.NewGuid(),
            XpAmount = 100,
            OccurredAtUtc = new DateTimeOffset(
                2026,
                8,
                12,
                12,
                0,
                0,
                TimeSpan.Zero),
            BusinessDate = new DateOnly(2026, 8, 12),
            IdempotencyKey = idempotencyKey
        };
    }
}
