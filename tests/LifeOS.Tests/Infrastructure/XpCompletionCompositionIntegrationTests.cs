using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
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

public sealed class XpCompletionCompositionIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateOnly BusinessDate = new(2026, 8, 12);
    private static readonly DateTimeOffset CompletedAtUtc =
        new(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainerFixture _fixture;

    public XpCompletionCompositionIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TaskAndHabitCompletionsShareOneDailyCap()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var task = await SeedTaskAsync(userId);
        var habit = await SeedHabitAsync(userId);
        await SeedProgressionAndExistingQuestXpAsync(userId, 300);

        var taskResult = await CreateTaskService(userId)
            .CompleteTaskAsync(task.Id);
        var habitResult = await CreateHabitService(userId)
            .CompleteHabitAsync(habit.Id);

        Assert.Equal(150, taskResult.XpAward!.AwardedXp);
        Assert.Equal(50, habitResult.XpAward!.AwardedXp);
        Assert.True(habitResult.WasNewlyCompleted);

        await using var context = _fixture.CreateDbContext();
        var transactions = await context.XpTransactions
            .Where(item => item.UserId == userId)
            .ToListAsync();
        var progression = await context.UserProgressions
            .SingleAsync(item => item.UserId == userId);

        Assert.Equal(3, transactions.Count);
        Assert.Equal(500, transactions.Sum(item => item.XpAmount));
        Assert.Equal(500L, progression.TotalLifetimeXp);
        Assert.Equal(500, progression.DailyQuestXpToday);
        Assert.Equal(3L, progression.Version);
        Assert.Equal(150, transactions.Single(item => item.SourceEntityId == task.Id).XpAmount);
        Assert.Equal(50, transactions.Single(item => item.SourceEntityId == habit.Id).XpAmount);
        Assert.Single(await context.HabitLogs
            .Where(item => item.HabitId == habit.Id)
            .ToListAsync());
        Assert.Equal(TaskItemStatus.Completed,
            (await context.Tasks.SingleAsync(item => item.Id == task.Id)).Status);
    }

    [Fact]
    public async Task ConcurrentTaskAndHabitCompletionsNearCapDoNotExceedCap()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var task = await SeedTaskAsync(userId);
        var habit = await SeedHabitAsync(userId);
        await SeedProgressionAndExistingQuestXpAsync(userId, 450);

        var taskCompletion = CreateTaskService(userId).CompleteTaskAsync(task.Id);
        var habitCompletion = CreateHabitService(userId).CompleteHabitAsync(habit.Id);
        await Task.WhenAll(taskCompletion, habitCompletion);

        Assert.True(taskCompletion.Result.WasNewlyCompleted);
        Assert.True(habitCompletion.Result.WasNewlyCompleted);
        Assert.Equal(50, taskCompletion.Result.XpAward!.AwardedXp +
            habitCompletion.Result.XpAward!.AwardedXp);

        await using var context = _fixture.CreateDbContext();
        var progression = await context.UserProgressions
            .SingleAsync(item => item.UserId == userId);
        var transactions = await context.XpTransactions
            .Where(item => item.UserId == userId)
            .ToListAsync();

        Assert.Equal(2, transactions.Count);
        Assert.Equal(500, transactions.Sum(item => item.XpAmount));
        Assert.Equal(500L, progression.TotalLifetimeXp);
        Assert.Equal(500, progression.DailyQuestXpToday);
        Assert.Equal(2L, progression.Version);
        Assert.DoesNotContain(transactions, item => item.XpAmount == 0);
        Assert.Single(await context.HabitLogs
            .Where(item => item.HabitId == habit.Id)
            .ToListAsync());
        Assert.Equal(TaskItemStatus.Completed,
            (await context.Tasks.SingleAsync(item => item.Id == task.Id)).Status);
    }

    private TaskService CreateTaskService(Guid userId)
    {
        var dependencies = CreateDependencies(userId);
        return new TaskService(
            new TaskRepository(dependencies.Factory),
            dependencies.CurrentUser.Object,
            dependencies.Settings.Object,
            dependencies.DateTime.Object,
            new XpService(
                new XpRepository(dependencies.Factory),
                dependencies.CurrentUser.Object,
                dependencies.Settings.Object,
                dependencies.DateTime.Object,
                NullLogger<XpService>.Instance),
            NullLogger<TaskService>.Instance);
    }

    private HabitService CreateHabitService(Guid userId)
    {
        var dependencies = CreateDependencies(userId);
        return new HabitService(
            new HabitRepository(dependencies.Factory),
            dependencies.CurrentUser.Object,
            dependencies.Settings.Object,
            dependencies.DateTime.Object,
            new XpService(
                new XpRepository(dependencies.Factory),
                dependencies.CurrentUser.Object,
                dependencies.Settings.Object,
                dependencies.DateTime.Object,
                NullLogger<XpService>.Instance),
            NullLogger<HabitService>.Instance);
    }

    private (TestDbContextFactory Factory,
        Mock<ICurrentUserService> CurrentUser,
        Mock<IUserSettingsService> Settings,
        Mock<IDateTimeProvider> DateTime) CreateDependencies(Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(item => item.IsAuthenticated).Returns(true);
        currentUser.SetupGet(item => item.UserId).Returns(userId);

        var settings = new Mock<IUserSettingsService>();
        settings.Setup(item => item.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                UserId = userId,
                TimeZoneId = "UTC"
            });

        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.SetupGet(item => item.UtcNow).Returns(CompletedAtUtc);
        dateTime.Setup(item => item.GetCurrentDate("UTC"))
            .Returns(BusinessDate);

        return (new TestDbContextFactory(_fixture), currentUser, settings, dateTime);
    }

    private async Task<TaskItem> SeedTaskAsync(Guid userId)
    {
        var task = new TaskItem
        {
            UserId = userId,
            Title = "Composition task",
            EstimatedTime = EstimatedTime.Between30And60Minutes,
            FrictionLevel = FrictionLevel.Low,
            Status = TaskItemStatus.Active
        };

        await using var context = _fixture.CreateDbContext();
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return task;
    }

    private async Task<Habit> SeedHabitAsync(Guid userId)
    {
        var habit = new Habit
        {
            UserId = userId,
            Name = "Composition habit",
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

    private async Task SeedProgressionAndExistingQuestXpAsync(
        Guid userId,
        int amount)
    {
        await using var context = _fixture.CreateDbContext();
        context.UserProgressions.Add(new UserProgression
        {
            UserId = userId,
            TotalLifetimeXp = amount,
            CurrentLevel = XpRules.CalculateLevel(amount),
            CurrentEchelon = XpRules.CalculateEchelon(
                XpRules.CalculateLevel(amount)),
            DailyQuestXpToday = amount,
            DailyQuestXpDate = BusinessDate,
            Version = 1
        });
        context.XpTransactions.Add(new XpTransaction
        {
            UserId = userId,
            Source = XpSource.QuestCompletion,
            SourceType = XpSourceType.Task,
            SourceEntityId = Guid.NewGuid(),
            XpAmount = amount,
            OccurredAtUtc = CompletedAtUtc,
            BusinessDate = BusinessDate,
            IdempotencyKey = "composition-seed"
        });
        await context.SaveChangesAsync();
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await context.XpTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.UserProgressions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.HabitLogs.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Habits.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Tasks.IgnoreQueryFilters().ExecuteDeleteAsync();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly PostgreSqlContainerFixture _fixture;

        public TestDbContextFactory(PostgreSqlContainerFixture fixture)
        {
            _fixture = fixture;
        }

        public AppDbContext CreateDbContext() => _fixture.CreateDbContext();

        public Task<AppDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_fixture.CreateDbContext());
    }
}
