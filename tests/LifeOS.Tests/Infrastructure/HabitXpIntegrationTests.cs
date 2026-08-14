using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Progression;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LifeOS.Tests.Infrastructure;

public sealed class HabitXpIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public HabitXpIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteHabitAsync_CreatesOneLogAndOneHabitQuestAward()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var habit = await SeedHabitAsync(userId);
        var date = new DateOnly(2026, 8, 12);
        var completedAt = new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);

        var result = await CreateService(userId, date, completedAt).CompleteHabitAsync(habit.Id);

        Assert.True(result.WasNewlyCompleted);
        Assert.NotNull(result.XpAward);
        await using var context = _fixture.CreateDbContext();
        var log = await context.HabitLogs.SingleAsync(item => item.HabitId == habit.Id);
        var transaction = await context.XpTransactions.SingleAsync(item => item.UserId == userId);
        var progression = await context.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(XpSource.QuestCompletion, transaction.Source);
        Assert.Equal(XpSourceType.Habit, transaction.SourceType);
        Assert.Equal(habit.Id, transaction.SourceEntityId);
        Assert.Equal(log.CompletedAtUtc, transaction.OccurredAtUtc);
        Assert.Equal(log.CompletionDate, transaction.BusinessDate);
        Assert.Equal(100L, progression.TotalLifetimeXp);
        Assert.Equal(100, progression.DailyQuestXpToday);
    }

    [Fact]
    public async Task ConcurrentCompletion_CreatesOneLogAndOneXpAward()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var habit = await SeedHabitAsync(userId);
        var date = new DateOnly(2026, 8, 12);
        var first = CreateService(userId, date,
            new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero));
        var second = CreateService(userId, date,
            new DateTimeOffset(2026, 8, 12, 13, 0, 1, TimeSpan.Zero));

        var results = await Task.WhenAll(
            first.CompleteHabitAsync(habit.Id),
            second.CompleteHabitAsync(habit.Id));

        Assert.Single(results, result => result.WasNewlyCompleted);
        Assert.Single(results, result => !result.WasNewlyCompleted);
        Assert.Equal(results[0].CompletedAtUtc, results[1].CompletedAtUtc);
        Assert.Equal(results[0].CompletionDate, results[1].CompletionDate);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.HabitLogs.CountAsync(item => item.HabitId == habit.Id));
        Assert.Equal(1, await context.XpTransactions.CountAsync(item => item.UserId == userId));
        var progression = await context.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(100L, progression.TotalLifetimeXp);
        Assert.Equal(100, progression.DailyQuestXpToday);
        Assert.Equal(1L, progression.Version);
    }

    [Fact]
    public async Task CompletingOnNextDate_CreatesDistinctLogsAndXpEvents()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var habit = await SeedHabitAsync(userId);
        await CreateService(userId, new DateOnly(2026, 8, 12),
            new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero)).CompleteHabitAsync(habit.Id);
        await CreateService(userId, new DateOnly(2026, 8, 13),
            new DateTimeOffset(2026, 8, 13, 13, 0, 0, TimeSpan.Zero)).CompleteHabitAsync(habit.Id);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(2, await context.HabitLogs.CountAsync(item => item.HabitId == habit.Id));
        Assert.Equal(2, await context.XpTransactions.CountAsync(item => item.UserId == userId));
        Assert.Equal(2, await context.XpTransactions.Select(item => item.IdempotencyKey).Distinct().CountAsync());
        Assert.Equal(2, await context.XpTransactions.Select(item => item.BusinessDate).Distinct().CountAsync());
    }

    [Fact]
    public async Task CapExhaustion_PersistsHabitLogWithoutXpTransaction()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var habit = await SeedHabitAsync(userId);
        var date = new DateOnly(2026, 8, 12);
        await using (var context = _fixture.CreateDbContext())
        {
            context.UserProgressions.Add(new UserProgression
            {
                UserId = userId,
                TotalLifetimeXp = 500,
                CurrentLevel = XpRules.CalculateLevel(500),
                CurrentEchelon = XpRules.CalculateEchelon(XpRules.CalculateLevel(500)),
                DailyQuestXpToday = 500,
                DailyQuestXpDate = date
            });
            context.XpTransactions.Add(new XpTransaction
            {
                UserId = userId,
                Source = XpSource.QuestCompletion,
                SourceType = XpSourceType.Task,
                SourceEntityId = Guid.NewGuid(),
                XpAmount = 500,
                OccurredAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero),
                BusinessDate = date,
                IdempotencyKey = "habit-cap-seed"
            });
            await context.SaveChangesAsync();
        }

        var result = await CreateService(userId, date,
            new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero)).CompleteHabitAsync(habit.Id);

        Assert.True(result.WasNewlyCompleted);
        Assert.Equal(0, result.XpAward!.AwardedXp);
        await using var verify = _fixture.CreateDbContext();
        Assert.Single(await verify.HabitLogs.Where(item => item.HabitId == habit.Id).ToListAsync());
        Assert.Equal(1, await verify.XpTransactions.CountAsync(item => item.UserId == userId));
        var progression = await verify.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(500L, progression.TotalLifetimeXp);
        Assert.Equal(500, progression.DailyQuestXpToday);
    }

    private HabitService CreateService(Guid userId, DateOnly date, DateTimeOffset utcNow)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(item => item.IsAuthenticated).Returns(true);
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        var settings = new Mock<IUserSettingsService>();
        settings.Setup(item => item.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { UserId = userId, TimeZoneId = "UTC" });
        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.SetupGet(item => item.UtcNow).Returns(utcNow);
        dateTime.Setup(item => item.GetCurrentDate("UTC")).Returns(date);
        var factory = new TestDbContextFactory(_fixture);
        var xpService = new XpService(new XpRepository(factory), currentUser.Object, settings.Object,
            dateTime.Object, NullLogger<XpService>.Instance);
        return new HabitService(new HabitRepository(factory), currentUser.Object, settings.Object,
            dateTime.Object, xpService, NullLogger<HabitService>.Instance);
    }

    private async Task<Habit> SeedHabitAsync(Guid userId)
    {
        var habit = new Habit
        {
            UserId = userId,
            Name = "Read",
            Frequency = HabitFrequency.Daily,
            TargetType = HabitTargetType.Binary,
            IsActive = true,
            EstimatedTime = EstimatedTime.Between15And30Minutes,
            FrictionLevel = FrictionLevel.Low
        };
        await using var context = _fixture.CreateDbContext();
        context.Habits.Add(habit);
        await context.SaveChangesAsync();
        return habit;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await context.XpTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.UserProgressions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.HabitLogs.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Habits.IgnoreQueryFilters().ExecuteDeleteAsync();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly PostgreSqlContainerFixture _fixture;
        public TestDbContextFactory(PostgreSqlContainerFixture fixture) => _fixture = fixture;
        public AppDbContext CreateDbContext() => _fixture.CreateDbContext();
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_fixture.CreateDbContext());
    }
}
