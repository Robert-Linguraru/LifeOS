using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Tasks;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Tasks;
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

public sealed class TaskCompletionIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateOnly BusinessDate = new(2026, 8, 12);
    private readonly PostgreSqlContainerFixture _fixture;

    public TaskCompletionIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteTaskAsync_PersistsTaskAndOneQuestXpAward()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var task = await SeedTaskAsync(userId);
        var completedAt = new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);
        var service = CreateService(userId, completedAt);

        var result = await service.CompleteTaskAsync(task.Id);

        Assert.True(result.WasNewlyCompleted);
        Assert.False(result.XpAwardFailed);
        Assert.Equal(TaskItemStatus.Completed, result.Task.Status);
        Assert.NotNull(result.XpAward);
        await using var context = _fixture.CreateDbContext();
        var persistedTask = await context.Tasks.SingleAsync(item => item.Id == task.Id);
        var transaction = await context.XpTransactions.SingleAsync(item => item.UserId == userId);
        var progression = await context.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(persistedTask.CompletedAtUtc, transaction.OccurredAtUtc);
        Assert.Equal(persistedTask.CompletedDate, transaction.BusinessDate);
        Assert.Equal(XpSource.QuestCompletion, transaction.Source);
        Assert.Equal(XpSourceType.Task, transaction.SourceType);
        Assert.Equal(task.Id, transaction.SourceEntityId);
        Assert.Equal(100L, progression.TotalLifetimeXp);
        Assert.Equal(100, progression.DailyQuestXpToday);
    }

    [Fact]
    public async Task ConcurrentCompletion_HasOneWinnerAndOneXpTransaction()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var task = await SeedTaskAsync(userId);
        var first = CreateService(userId,
            new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero));
        var second = CreateService(userId,
            new DateTimeOffset(2026, 8, 12, 13, 0, 1, TimeSpan.Zero));

        var results = await Task.WhenAll(
            first.CompleteTaskAsync(task.Id),
            second.CompleteTaskAsync(task.Id));

        Assert.Single(results, result => result.WasNewlyCompleted);
        Assert.Single(results, result => !result.WasNewlyCompleted);
        Assert.Equal(results[0].Task.CompletedAtUtc, results[1].Task.CompletedAtUtc);
        Assert.Equal(results[0].Task.CompletedDate, results[1].Task.CompletedDate);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.XpTransactions.CountAsync(item => item.UserId == userId));
        var progression = await context.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(100L, progression.TotalLifetimeXp);
        Assert.Equal(100, progression.DailyQuestXpToday);
        Assert.Equal(1L, progression.Version);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenQuestCapIsExhausted_CompletesWithoutXpTransaction()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var task = await SeedTaskAsync(userId);
        await using (var context = _fixture.CreateDbContext())
        {
            context.UserProgressions.Add(new UserProgression
            {
                UserId = userId,
                TotalLifetimeXp = 500,
                CurrentLevel = XpRules.CalculateLevel(500),
                CurrentEchelon = XpRules.CalculateEchelon(XpRules.CalculateLevel(500)),
                DailyQuestXpToday = 500,
                DailyQuestXpDate = BusinessDate
            });
            context.XpTransactions.Add(new XpTransaction
            {
                UserId = userId,
                Source = XpSource.QuestCompletion,
                SourceType = XpSourceType.Task,
                SourceEntityId = Guid.NewGuid(),
                XpAmount = 500,
                OccurredAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero),
                BusinessDate = BusinessDate,
                IdempotencyKey = "cap-seed"
            });
            await context.SaveChangesAsync();
        }

        var result = await CreateService(userId,
            new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero))
            .CompleteTaskAsync(task.Id);

        Assert.True(result.WasNewlyCompleted);
        Assert.False(result.XpAwardFailed);
        Assert.NotNull(result.XpAward);
        Assert.Equal(0, result.XpAward!.AwardedXp);
        Assert.True(result.XpAward.IsCapConstrained);
        await using var verify = _fixture.CreateDbContext();
        Assert.Equal(1, await verify.XpTransactions.CountAsync(item => item.UserId == userId));
        var progression = await verify.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(500L, progression.TotalLifetimeXp);
        Assert.Equal(500, progression.DailyQuestXpToday);
    }

    private TaskService CreateService(Guid userId, DateTimeOffset utcNow)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(item => item.IsAuthenticated).Returns(true);
        currentUser.SetupGet(item => item.UserId).Returns(userId);
        var settings = new Mock<IUserSettingsService>();
        settings.Setup(item => item.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { UserId = userId, TimeZoneId = "UTC" });
        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.SetupGet(item => item.UtcNow).Returns(utcNow);
        dateTime.Setup(item => item.GetCurrentDate("UTC")).Returns(BusinessDate);
        var factory = new TestDbContextFactory(_fixture);
        var taskRepository = new TaskRepository(factory);
        var xpRepository = new XpRepository(factory);
        var xpService = new XpService(xpRepository, currentUser.Object, settings.Object, dateTime.Object,
            NullLogger<XpService>.Instance);
        return new TaskService(taskRepository, currentUser.Object, settings.Object, dateTime.Object,
            xpService, NullLogger<TaskService>.Instance);
    }

    private async Task<TaskItem> SeedTaskAsync(Guid userId)
    {
        var task = new TaskItem
        {
            UserId = userId,
            Title = "Complete for XP",
            EstimatedTime = EstimatedTime.Between15And30Minutes,
            FrictionLevel = FrictionLevel.Low,
            Status = TaskItemStatus.Active
        };
        await using var context = _fixture.CreateDbContext();
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return task;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await context.XpTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.UserProgressions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Tasks.IgnoreQueryFilters().ExecuteDeleteAsync();
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
