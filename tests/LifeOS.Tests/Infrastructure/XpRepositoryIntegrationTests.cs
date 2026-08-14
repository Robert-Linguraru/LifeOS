using LifeOS.Core.Abstractions;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Xp;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class XpRepositoryIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public XpRepositoryIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOrCreateProgressionAsync_ShouldCreateOneRowDuringConcurrentInitialization()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => CreateRepository()
                    .GetOrCreateProgressionAsync(userId)));

        Assert.All(results, progression =>
        {
            Assert.Equal(userId, progression.UserId);
            Assert.Equal(0L, progression.TotalLifetimeXp);
            Assert.Equal(1, progression.CurrentLevel);
            Assert.Equal(Echelon.Iron, progression.CurrentEchelon);
        });

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(
            1,
            await context.UserProgressions
                .CountAsync(progression => progression.UserId == userId));
    }

    [Fact]
    public async Task Reads_ShouldBeUserScopedAndQuestSumShouldExcludeOtherSources()
    {
        await ResetDatabaseAsync();

        var userOne = Guid.NewGuid();
        var userTwo = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 8, 12);

        await using (var context = _fixture.CreateDbContext())
        {
            context.UserProgressions.AddRange(
                new UserProgression { UserId = userOne },
                new UserProgression { UserId = userTwo });
            context.XpTransactions.AddRange(
                CreateTransaction(userOne, "one", 75, businessDate),
                CreateTransaction(userOne, "two", 25, businessDate),
                CreateTransaction(
                    userOne,
                    "daily",
                    1000,
                    businessDate,
                    XpSource.DailyScore),
                CreateTransaction(userTwo, "other", 200, businessDate));
            await context.SaveChangesAsync();
        }

        var repository = CreateRepository();

        var progression = await repository.GetProgressionAsync(userOne);
        var otherProgression = await repository.GetProgressionAsync(
            Guid.NewGuid());
        var transaction = await repository.FindByIdempotencyKeyAsync(
            userOne,
            "one");
        var otherTransaction = await repository.FindByIdempotencyKeyAsync(
            userTwo,
            "one");
        var questSum = await repository.GetQuestXpSumAsync(
            userOne,
            businessDate);

        Assert.NotNull(progression);
        Assert.Null(otherProgression);
        Assert.NotNull(transaction);
        Assert.Null(otherTransaction);
        Assert.Equal(100, questSum);
    }

    [Fact]
    public async Task History_ShouldBeUserScopedAndNewestFirstWithIdTieBreaker()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(
            2026,
            8,
            12,
            12,
            0,
            0,
            TimeSpan.Zero);
        var older = CreateTransaction(
            userId,
            "older",
            25,
            new DateOnly(2026, 8, 11),
            occurredAtUtc: timestamp.AddDays(-1));
        var sameTimeFirst = CreateTransaction(
            userId,
            "same-first",
            50,
            new DateOnly(2026, 8, 12),
            occurredAtUtc: timestamp);
        sameTimeFirst.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var sameTimeSecond = CreateTransaction(
            userId,
            "same-second",
            75,
            new DateOnly(2026, 8, 12),
            occurredAtUtc: timestamp);
        sameTimeSecond.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        await using (var context = _fixture.CreateDbContext())
        {
            context.XpTransactions.AddRange(
                older,
                sameTimeFirst,
                sameTimeSecond,
                CreateTransaction(
                    Guid.NewGuid(),
                    "other",
                    100,
                    new DateOnly(2026, 8, 12)));
            await context.SaveChangesAsync();
        }

        var history = await CreateRepository().GetHistoryAsync(userId);

        Assert.Equal(3, history.Count);
        Assert.Equal(sameTimeSecond.Id, history[0].Id);
        Assert.Equal(sameTimeFirst.Id, history[1].Id);
        Assert.Equal(older.Id, history[2].Id);
    }

    [Fact]
    public async Task CommitAwardAsync_ShouldAtomicallyInsertTransactionAndUpdateProgression()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        await CreateRepository().GetOrCreateProgressionAsync(userId);

        var result = await CreateRepository().CommitAwardAsync(
            CreateRequest(userId, "commit", 0, 100, 1));

        Assert.Equal(XpAwardCommitStatus.Committed, result.Status);
        Assert.NotNull(result.Transaction);
        Assert.NotNull(result.Progression);
        Assert.Equal(100, result.Transaction!.XpAmount);
        Assert.Equal(100L, result.Progression!.TotalLifetimeXp);
        Assert.Equal(1L, result.Progression.Version);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(
            1,
            await context.XpTransactions
                .CountAsync(transaction => transaction.UserId == userId));
    }

    [Fact]
    public async Task CommitAwardAsync_DuplicateShouldNotChangeProgression()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        await CreateRepository().GetOrCreateProgressionAsync(userId);

        await using (var context = _fixture.CreateDbContext())
        {
            context.XpTransactions.Add(
                CreateTransaction(
                    userId,
                    "duplicate",
                    100,
                    new DateOnly(2026, 8, 12)));
            await context.SaveChangesAsync();
        }

        var result = await CreateRepository().CommitAwardAsync(
            CreateRequest(userId, "duplicate", 0, 100, 1));

        Assert.Equal(XpAwardCommitStatus.Duplicate, result.Status);
        Assert.NotNull(result.Transaction);
        Assert.NotNull(result.Progression);
        Assert.Equal(0L, result.Progression!.TotalLifetimeXp);
        Assert.Equal(0L, result.Progression.Version);

        await using var verificationContext = _fixture.CreateDbContext();
        Assert.Equal(
            1,
            await verificationContext.XpTransactions
                .CountAsync(transaction => transaction.UserId == userId));
    }

    [Fact]
    public async Task CommitAwardAsync_InvalidProgressionShouldRollBackTransaction()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        await CreateRepository().GetOrCreateProgressionAsync(userId);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            CreateRepository().CommitAwardAsync(
                CreateRequest(
                    userId,
                    "invalid",
                    0,
                    100,
                    1,
                    currentLevel: 0)));

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(
            0,
            await context.XpTransactions
                .CountAsync(transaction => transaction.UserId == userId));

        var progression = await context.UserProgressions
            .SingleAsync(item => item.UserId == userId);
        Assert.Equal(0L, progression.TotalLifetimeXp);
        Assert.Equal(0L, progression.Version);
    }

    [Fact]
    public async Task CommitAwardAsync_DifferentKeysShouldReturnOneCommitAndOneConcurrencyConflict()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        await CreateRepository().GetOrCreateProgressionAsync(userId);

        var results = await Task.WhenAll(
            CreateRepository().CommitAwardAsync(
                CreateRequest(userId, "first", 0, 100, 1)),
            CreateRepository().CommitAwardAsync(
                CreateRequest(userId, "second", 0, 100, 1)));

        Assert.Single(
            results,
            result => result.Status == XpAwardCommitStatus.Committed);
        Assert.Single(
            results,
            result => result.Status == XpAwardCommitStatus.ConcurrencyConflict);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(
            1,
            await context.XpTransactions
                .CountAsync(transaction => transaction.UserId == userId));
        Assert.Equal(
            100L,
            (await context.UserProgressions
                .SingleAsync(item => item.UserId == userId)).TotalLifetimeXp);
    }

    [Fact]
    public async Task CommitAwardAsync_SameKeyShouldCreateOneTransactionAndOneIncrement()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        await CreateRepository().GetOrCreateProgressionAsync(userId);

        var results = await Task.WhenAll(
            CreateRepository().CommitAwardAsync(
                CreateRequest(userId, "same-key", 0, 100, 1)),
            CreateRepository().CommitAwardAsync(
                CreateRequest(userId, "same-key", 0, 100, 1)));

        Assert.Contains(
            results,
            result => result.Status == XpAwardCommitStatus.Committed);
        Assert.Contains(
            results,
            result => result.Status is
                XpAwardCommitStatus.Duplicate or
                XpAwardCommitStatus.ConcurrencyConflict);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(
            1,
            await context.XpTransactions
                .CountAsync(transaction => transaction.UserId == userId));
        var progression = await context.UserProgressions
            .SingleAsync(item => item.UserId == userId);
        Assert.Equal(100L, progression.TotalLifetimeXp);
        Assert.Equal(1L, progression.Version);
    }

    private XpRepository CreateRepository()
    {
        return new XpRepository(new TestDbContextFactory(_fixture));
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await context.XpTransactions
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
        await context.UserProgressions
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
    }

    private static XpAwardCommitRequest CreateRequest(
        Guid userId,
        string idempotencyKey,
        long expectedVersion,
        long resultingTotalLifetimeXp,
        long resultingVersion,
        int currentLevel = 1)
    {
        return new XpAwardCommitRequest
        {
            UserId = userId,
            Source = XpSource.QuestCompletion,
            SourceType = XpSourceType.Task,
            SourceEntityId = Guid.NewGuid(),
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
            IdempotencyKey = idempotencyKey,
            ExpectedVersion = expectedVersion,
            ResultingTotalLifetimeXp = resultingTotalLifetimeXp,
            ResultingCurrentLevel = currentLevel,
            ResultingCurrentEchelon = Echelon.Iron,
            ResultingDailyQuestXpToday = 100,
            ResultingDailyQuestXpDate = new DateOnly(2026, 8, 12),
            ResultingVersion = resultingVersion
        };
    }

    private static XpTransaction CreateTransaction(
        Guid userId,
        string? idempotencyKey,
        int xpAmount,
        DateOnly businessDate,
        XpSource source = XpSource.QuestCompletion,
        DateTimeOffset? occurredAtUtc = null)
    {
        return new XpTransaction
        {
            UserId = userId,
            Source = source,
            SourceType = XpSourceType.Task,
            SourceEntityId = Guid.NewGuid(),
            XpAmount = xpAmount,
            OccurredAtUtc = occurredAtUtc ?? new DateTimeOffset(
                businessDate.ToDateTime(new TimeOnly(12, 0)),
                TimeSpan.Zero),
            BusinessDate = businessDate,
            IdempotencyKey = idempotencyKey
        };
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<AppDbContext>
    {
        private readonly PostgreSqlContainerFixture _fixture;

        public TestDbContextFactory(
            PostgreSqlContainerFixture fixture)
        {
            _fixture = fixture;
        }

        public AppDbContext CreateDbContext()
        {
            return _fixture.CreateDbContext();
        }

        public Task<AppDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_fixture.CreateDbContext());
        }
    }
}
