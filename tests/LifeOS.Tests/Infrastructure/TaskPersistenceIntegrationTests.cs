using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class TaskPersistenceIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public TaskPersistenceIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TaskItem_ShouldPersistPostgreSqlDataTypes()
    {
        // Arrange
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();

        var completedAtUtc =
            new DateTimeOffset(
                2026,
                8,
                9,
                18,
                45,
                0,
                TimeSpan.Zero);

        var task = new TaskItem
        {
            UserId = userId,
            Title = "Persistence test",
            Description = "Verify PostgreSQL types.",
            DueDate = new DateOnly(2026, 8, 10),
            DueTime = new TimeOnly(14, 30),
            Priority = TaskPriority.High,
            Category = TaskCategory.Work,
            EstimatedTime =
                EstimatedTime.Between30And60Minutes,
            FrictionLevel = FrictionLevel.Medium,
            Status = TaskItemStatus.Completed,
            CompletedAtUtc = completedAtUtc,
            CompletedDate =
                new DateOnly(2026, 8, 9)
        };

        await using (var context =
            _fixture.CreateDbContext())
        {
            context.Tasks.Add(task);

            await context.SaveChangesAsync();
        }

        // Act
        await using var verificationContext =
            _fixture.CreateDbContext();

        var persistedTask =
            await verificationContext.Tasks
                .SingleAsync(
                    item => item.Id == task.Id);

        // Assert
        Assert.Equal(
            new DateOnly(2026, 8, 10),
            persistedTask.DueDate);

        Assert.Equal(
            new TimeOnly(14, 30),
            persistedTask.DueTime);

        Assert.Equal(
            completedAtUtc,
            persistedTask.CompletedAtUtc);

        Assert.Equal(
            new DateOnly(2026, 8, 9),
            persistedTask.CompletedDate);
    }

    [Fact]
    public async Task TaskItem_Delete_ShouldSoftDeleteAndBeFiltered()
    {
        // Arrange
        await ResetDatabaseAsync();

        var deleteTime =
            new DateTimeOffset(
                2026,
                8,
                9,
                15,
                0,
                0,
                TimeSpan.Zero);

        var task = new TaskItem
        {
            UserId = Guid.NewGuid(),
            Title = "Delete me"
        };

        await using (var context =
            _fixture.CreateDbContext(deleteTime))
        {
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context =
            _fixture.CreateDbContext(deleteTime))
        {
            var taskToDelete =
                await context.Tasks.SingleAsync(
                    item => item.Id == task.Id);

            context.Tasks.Remove(taskToDelete);

            await context.SaveChangesAsync();
        }

        // Assert - normal query hides it
        await using (var context =
            _fixture.CreateDbContext())
        {
            var visibleTask =
                await context.Tasks
                    .SingleOrDefaultAsync(
                        item => item.Id == task.Id);

            Assert.Null(visibleTask);
        }

        // Assert - physical row still exists
        await using (var context =
            _fixture.CreateDbContext())
        {
            var deletedTask =
                await context.Tasks
                    .IgnoreQueryFilters()
                    .SingleAsync(
                        item => item.Id == task.Id);

            Assert.True(deletedTask.IsDeleted);

            Assert.Equal(
                deleteTime,
                deletedTask.DeletedAtUtc);

            Assert.Equal(
                deleteTime,
                deletedTask.UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task UserSettings_ShouldRejectDuplicateUserId()
    {
        // Arrange
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();

        var settings = new UserSettings
        {
            UserId = userId,
            TimeZoneId = "UTC"
        };

        await using (var context =
            _fixture.CreateDbContext())
        {
            context.UserSettings.Add(settings);

            await context.SaveChangesAsync();
        }

        var duplicateSettings = new UserSettings
        {
            UserId = userId,
            TimeZoneId = "Europe/Bucharest"
        };

        await using (var context =
            _fixture.CreateDbContext())
        {
            context.UserSettings.Add(duplicateSettings);

            // Assert
            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task TaskRepository_ShouldScopeQueriesToUser()
    {
        // Arrange
        await ResetDatabaseAsync();

        var userOne = Guid.NewGuid();
        var userTwo = Guid.NewGuid();

        var userOneTask = new TaskItem
        {
            UserId = userOne,
            Title = "User one task"
        };

        var userTwoTask = new TaskItem
        {
            UserId = userTwo,
            Title = "User two task"
        };

        await using (var context =
            _fixture.CreateDbContext())
        {
            context.Tasks.AddRange(
                userOneTask,
                userTwoTask);

            await context.SaveChangesAsync();
        }

        var contextFactory =
            CreateContextFactory();

        var repository =
            new TaskRepository(
                contextFactory);

        // Act
        var userOneTasks =
            await repository.GetAllByUserIdAsync(
                userOne);

        var inaccessibleTask =
            await repository.GetByIdAsync(
                userOne,
                userTwoTask.Id);

        // Assert
        Assert.Single(userOneTasks);

        Assert.Equal(
            userOneTask.Id,
            userOneTasks[0].Id);

        Assert.Null(inaccessibleTask);
    }

    [Fact]
    public async Task ArchivedTask_ShouldRemainQueryable()
    {
        // Arrange
        await ResetDatabaseAsync();

        var userId = Guid.NewGuid();

        var archivedTask = new TaskItem
        {
            UserId = userId,
            Title = "Archived task",
            Status = TaskItemStatus.Archived
        };

        await using (var context =
            _fixture.CreateDbContext())
        {
            context.Tasks.Add(archivedTask);

            await context.SaveChangesAsync();
        }

        var repository =
            new TaskRepository(
                CreateContextFactory());

        // Act
        var tasks =
            await repository.GetAllByUserIdAsync(
                userId);

        // Assert
        var persisted =
            Assert.Single(tasks);

        Assert.Equal(
            archivedTask.Id,
            persisted.Id);

        Assert.Equal(
            TaskItemStatus.Archived,
            persisted.Status);

        Assert.False(
            persisted.IsDeleted);
    }

    private IDbContextFactory<AppDbContext>
    CreateContextFactory()
    {
        return new TestDbContextFactory(
            _fixture);
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
    private async Task ResetDatabaseAsync()
    {
        await using var context =
            _fixture.CreateDbContext();

        await context.Tasks
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await context.UserSettings
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
    }
}