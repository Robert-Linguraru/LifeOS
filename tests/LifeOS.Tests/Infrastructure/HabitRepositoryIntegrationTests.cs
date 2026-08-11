using LifeOS.Core.Entities;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class HabitRepositoryIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public HabitRepositoryIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByIdAsync_IsUserScopedAndReturnsArchivedHabit()
    {
        await ResetDatabaseAsync();

        var userOne = Guid.NewGuid();
        var userTwo = Guid.NewGuid();
        var archivedHabit = new Habit
        {
            UserId = userOne,
            Name = "Archived habit",
            IsActive = false
        };
        var otherUserHabit = new Habit
        {
            UserId = userTwo,
            Name = "Other user habit"
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.AddRange(archivedHabit, otherUserHabit);
            await context.SaveChangesAsync();
        }

        var repository = CreateRepository();

        var retrievedArchived = await repository.GetByIdAsync(
            userOne,
            archivedHabit.Id);
        var inaccessible = await repository.GetByIdAsync(
            userOne,
            otherUserHabit.Id);
        var userHabits = await repository.GetAllByUserIdAsync(userOne);

        Assert.NotNull(retrievedArchived);
        Assert.False(retrievedArchived.IsActive);
        Assert.Null(inaccessible);
        Assert.Single(userHabits);
        Assert.Equal(archivedHabit.Id, userHabits[0].Id);
    }

    [Fact]
    public async Task HabitLogQueries_AreUserScoped()
    {
        await ResetDatabaseAsync();

        var userOne = Guid.NewGuid();
        var userTwo = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = userOne,
            Name = "Read"
        };
        var completionDate = new DateOnly(2026, 8, 10);

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.Add(habit);
            await context.SaveChangesAsync();
        }

        var log = new HabitLog
        {
            UserId = userOne,
            HabitId = habit.Id,
            CompletionDate = completionDate,
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                10,
                20,
                0,
                0,
                TimeSpan.Zero)
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.HabitLogs.Add(log);
            await context.SaveChangesAsync();
        }

        var repository = CreateRepository();

        var ownedLog = await repository.GetLogByDateAsync(
            userOne,
            habit.Id,
            completionDate);
        var inaccessibleLog = await repository.GetLogByDateAsync(
            userTwo,
            habit.Id,
            completionDate);
        var logs = await repository.GetLogsByHabitIdAsync(
            userOne,
            habit.Id);
        var dates = await repository.GetCompletionDatesAsync(
            userOne,
            habit.Id);

        Assert.NotNull(ownedLog);
        Assert.Null(inaccessibleLog);
        Assert.Single(logs);
        Assert.Equal(log.Id, logs[0].Id);
        Assert.Single(dates);
        Assert.Equal(completionDate, dates[0]);
    }

    [Fact]
    public async Task TryAddLogAsync_ReturnsTrueForFirstInsertAndFalseForDuplicateCompletion()
    {
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = userId,
            Name = "Meditate"
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.Habits.Add(habit);
            await context.SaveChangesAsync();
        }

        var repository = CreateRepository();
        var completionDate = new DateOnly(2026, 8, 11);
        var firstLog = CreateLog(
            userId,
            habit.Id,
            completionDate);
        var duplicateLog = CreateLog(
            userId,
            habit.Id,
            completionDate);

        var inserted = await repository.TryAddLogAsync(firstLog);
        var duplicate = await repository.TryAddLogAsync(duplicateLog);

        Assert.True(inserted);
        Assert.False(duplicate);
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
