using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace LifeOS.Tests.Infrastructure;

public sealed class HabitPersistenceIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public HabitPersistenceIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrationChain_ShouldApplyThroughAddHabits()
    {
        await using var context = _fixture.CreateDbContext();

        var pendingMigrations =
            await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations =
            await context.Database.GetAppliedMigrationsAsync();

        Assert.Empty(pendingMigrations);
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith(
                "_AddHabits",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task HabitAndHabitLog_ShouldRoundTripPostgreSqlValues()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var completionDate = new DateOnly(2026, 8, 10);
        var completedAtUtc = new DateTimeOffset(
            2026,
            8,
            10,
            22,
            45,
            0,
            TimeSpan.Zero);
        var habit = new Habit
        {
            UserId = userId,
            Name = "Run",
            Description = "Run outside.",
            Frequency = HabitFrequency.Daily,
            TargetType = HabitTargetType.Quantity,
            TargetQuantity = 2.50m,
            TargetUnit = "km",
            IsActive = true,
            EstimatedTime = EstimatedTime.Between30And60Minutes,
            FrictionLevel = FrictionLevel.Medium
        };
        var habitLog = new HabitLog
        {
            UserId = userId,
            HabitId = habit.Id,
            CompletionDate = completionDate,
            CompletedAtUtc = completedAtUtc
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.Add(habit);
            await context.SaveChangesAsync();

            context.HabitLogs.Add(habitLog);
            await context.SaveChangesAsync();
        }

        await using var verificationContext = _fixture.CreateDbContext();
        var persistedHabit = await verificationContext.Habits
            .SingleAsync(item => item.Id == habit.Id);
        var persistedLog = await verificationContext.HabitLogs
            .SingleAsync(item => item.Id == habitLog.Id);

        Assert.Equal(userId, persistedHabit.UserId);
        Assert.Equal("Run", persistedHabit.Name);
        Assert.Equal("Run outside.", persistedHabit.Description);
        Assert.Equal(HabitFrequency.Daily, persistedHabit.Frequency);
        Assert.Equal(HabitTargetType.Quantity, persistedHabit.TargetType);
        Assert.Equal(2.50m, persistedHabit.TargetQuantity);
        Assert.Equal("km", persistedHabit.TargetUnit);
        Assert.True(persistedHabit.IsActive);
        Assert.Equal(
            EstimatedTime.Between30And60Minutes,
            persistedHabit.EstimatedTime);
        Assert.Equal(FrictionLevel.Medium, persistedHabit.FrictionLevel);
        Assert.NotEqual(default, persistedHabit.CreatedAtUtc);
        Assert.NotEqual(default, persistedHabit.UpdatedAtUtc);

        Assert.Equal(userId, persistedLog.UserId);
        Assert.Equal(habit.Id, persistedLog.HabitId);
        Assert.Equal(completionDate, persistedLog.CompletionDate);
        Assert.Equal(completedAtUtc, persistedLog.CompletedAtUtc);
    }

    [Fact]
    public async Task HabitLog_ShouldRejectDuplicateCompletionKey()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = userId,
            Name = "Read"
        };
        var completionDate = new DateOnly(2026, 8, 11);

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.Add(habit);
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            context.HabitLogs.Add(CreateLog(
                userId,
                habit.Id,
                completionDate));
            await context.SaveChangesAsync();
        }

        await using var duplicateContext = _fixture.CreateDbContext();
        duplicateContext.HabitLogs.Add(CreateLog(
            userId,
            habit.Id,
            completionDate));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());
    }

    [Fact]
    public async Task HabitService_CompletionArchiveAndHistory_ShouldComposeAgainstPostgreSql()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var currentDate = new DateOnly(2026, 8, 12);
        var completedAtUtc = new DateTimeOffset(
            2026,
            8,
            11,
            23,
            30,
            0,
            TimeSpan.Zero);
        var repository = CreateRepository();
        var currentUser = new Mock<ICurrentUserService>();
        var userSettings = new Mock<IUserSettingsService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var xpService = new Mock<IXpService>();
        var logger = new Mock<ILogger<HabitService>>();

        currentUser.Setup(service => service.UserId).Returns(userId);
        currentUser.Setup(service => service.IsAuthenticated).Returns(true);
        userSettings
            .Setup(service => service.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                UserId = userId,
                TimeZoneId = "UTC"
            });
        dateTimeProvider.Setup(provider => provider.UtcNow)
            .Returns(completedAtUtc);
        dateTimeProvider.Setup(provider => provider.GetCurrentDate("UTC"))
            .Returns(currentDate);

        var service = new HabitService(
            repository,
            currentUser.Object,
            userSettings.Object,
            dateTimeProvider.Object,
            xpService.Object,
            logger.Object);

        var created = await service.CreateHabitAsync(
            new CreateHabitDto
            {
                Name = "Read"
            });

        var firstCompletion = await service.CompleteHabitAsync(created.Id);
        var secondCompletion = await service.CompleteHabitAsync(created.Id);
        var archived = await service.ArchiveHabitAsync(created.Id);
        var history = await service.GetHabitHistoryAsync(created.Id);

        Assert.True(firstCompletion.Habit.IsCompletedToday);
        Assert.True(secondCompletion.Habit.IsCompletedToday);
        Assert.False(archived.IsActive);
        Assert.Single(history.Entries);
        Assert.Equal(currentDate, history.Entries[0].CompletionDate);

        var persistedLogs = await repository.GetLogsByHabitIdAsync(
            userId,
            created.Id);
        var persistedHabit = await repository.GetByIdAsync(
            userId,
            created.Id);

        Assert.Single(persistedLogs);
        Assert.NotNull(persistedHabit);
        Assert.False(persistedHabit.IsActive);
        Assert.False(persistedHabit.IsDeleted);
    }

    [Fact]
    public async Task HabitArchive_ShouldKeepHabitAndLogsWithoutSoftDelete()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = userId,
            Name = "Journal"
        };
        var log = CreateLog(
            userId,
            habit.Id,
            new DateOnly(2026, 8, 13));

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.Add(habit);
            await context.SaveChangesAsync();
            context.HabitLogs.Add(log);
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var persistedHabit = await context.Habits
                .SingleAsync(item => item.Id == habit.Id);
            persistedHabit.IsActive = false;
            await context.SaveChangesAsync();
        }

        await using var verificationContext = _fixture.CreateDbContext();
        var archivedHabit = await verificationContext.Habits
            .SingleAsync(item => item.Id == habit.Id);
        var persistedLogs = await verificationContext.HabitLogs
            .Where(item => item.HabitId == habit.Id)
            .ToListAsync();

        Assert.False(archivedHabit.IsActive);
        Assert.False(archivedHabit.IsDeleted);
        Assert.Single(persistedLogs);
    }

    [Fact]
    public async Task GlobalSoftDeleteFilter_ShouldHideHabitAndHabitLogRows()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = userId,
            Name = "Delete filter"
        };
        var log = CreateLog(
            userId,
            habit.Id,
            new DateOnly(2026, 8, 14));

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.Add(habit);
            await context.SaveChangesAsync();
            context.HabitLogs.Add(log);
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var habitToDelete = await context.Habits
                .SingleAsync(item => item.Id == habit.Id);
            context.Habits.Remove(habitToDelete);
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var logToDelete = await context.HabitLogs
                .SingleAsync(item => item.Id == log.Id);
            context.HabitLogs.Remove(logToDelete);
            await context.SaveChangesAsync();
        }

        await using var verificationContext = _fixture.CreateDbContext();
        Assert.Null(await verificationContext.Habits
            .SingleOrDefaultAsync(item => item.Id == habit.Id));
        Assert.Null(await verificationContext.HabitLogs
            .SingleOrDefaultAsync(item => item.Id == log.Id));

        var deletedHabit = await verificationContext.Habits
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == habit.Id);
        var deletedLog = await verificationContext.HabitLogs
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == log.Id);

        Assert.True(deletedHabit.IsDeleted);
        Assert.True(deletedLog.IsDeleted);
    }

    [Fact]
    public async Task PhysicalHabitDelete_ShouldNotCascadeDeleteHabitLogs()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = userId,
            Name = "Restrictive relationship"
        };
        var log = CreateLog(
            userId,
            habit.Id,
            new DateOnly(2026, 8, 15));

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.Add(habit);
            await context.SaveChangesAsync();
            context.HabitLogs.Add(log);
            await context.SaveChangesAsync();
        }

        await using var deleteContext = _fixture.CreateDbContext();

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => deleteContext.Habits
                .Where(item => item.Id == habit.Id)
                .ExecuteDeleteAsync());

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            exception.SqlState);

        Assert.Equal(
            1,
            await deleteContext.HabitLogs
                .IgnoreQueryFilters()
                .CountAsync(item => item.Id == log.Id));
    }

    private HabitRepository CreateRepository()
    {
        return new HabitRepository(
            new TestDbContextFactory(_fixture));
    }

    private static HabitLog CreateLog(
        Guid userId,
        Guid habitId,
        DateOnly completionDate)
    {
        return new HabitLog
        {
            UserId = userId,
            HabitId = habitId,
            CompletionDate = completionDate,
            CompletedAtUtc = new DateTimeOffset(
                completionDate.ToDateTime(new TimeOnly(12, 0)),
                TimeSpan.Zero)
        };
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = _fixture.CreateDbContext();

        await context.HabitLogs
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await context.Habits
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
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
            return Task.FromResult(
                _fixture.CreateDbContext());
        }
    }
}
