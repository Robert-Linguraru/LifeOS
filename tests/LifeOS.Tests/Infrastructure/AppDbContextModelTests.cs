using LifeOS.Core.Abstractions;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure.Persistence;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void Model_ShouldMapTaskItemToTasksTable()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(TaskItem));

        Assert.NotNull(entityType);
        Assert.Equal("Tasks", entityType.GetTableName());
    }

    [Fact]
    public void Model_ShouldConfigureTaskTitleAndDescriptionLengths()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(TaskItem));

        Assert.NotNull(entityType);

        var titleProperty = entityType.FindProperty(nameof(TaskItem.Title));
        var descriptionProperty = entityType.FindProperty(nameof(TaskItem.Description));

        Assert.NotNull(titleProperty);
        Assert.NotNull(descriptionProperty);

        Assert.Equal(TaskConstants.TitleMaxLength, titleProperty.GetMaxLength());
        Assert.Equal(
            TaskConstants.DescriptionMaxLength,
            descriptionProperty.GetMaxLength());
    }

    [Fact]
    public void Model_ShouldConfigureTaskIndexes()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(TaskItem));

        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes()
            .Select(index => index.Properties
                .Select(property => property.Name)
                .ToArray())
            .ToList();

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(TaskItem.UserId), nameof(TaskItem.Status)]));

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(TaskItem.UserId), nameof(TaskItem.DueDate)]));

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(TaskItem.UserId), nameof(TaskItem.IsDeleted)]));
    }

    [Fact]
    public void Model_ShouldConfigureUserSettingsUserIdIndexAsUnique()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserSettings));

        Assert.NotNull(entityType);

        var index = entityType.GetIndexes()
            .Single(index => index.Properties
                .Select(property => property.Name)
                .SequenceEqual([nameof(UserSettings.UserId)]));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Model_ShouldMapHabitWithDocumentedProperties()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Habit));

        Assert.NotNull(entityType);
        Assert.Equal("Habits", entityType.GetTableName());

        var name = entityType.FindProperty(nameof(Habit.Name));
        var description = entityType.FindProperty(nameof(Habit.Description));
        var targetQuantity = entityType.FindProperty(nameof(Habit.TargetQuantity));
        var targetUnit = entityType.FindProperty(nameof(Habit.TargetUnit));

        Assert.NotNull(name);
        Assert.NotNull(description);
        Assert.NotNull(targetQuantity);
        Assert.NotNull(targetUnit);

        Assert.True(name.IsNullable == false);
        Assert.Equal(HabitConstants.NameMaxLength, name.GetMaxLength());
        Assert.True(description.IsNullable);
        Assert.Equal(
            HabitConstants.DescriptionMaxLength,
            description.GetMaxLength());
        Assert.True(targetQuantity.IsNullable);
        Assert.Equal(18, targetQuantity.GetPrecision());
        Assert.Equal(2, targetQuantity.GetScale());
        Assert.True(targetUnit.IsNullable);
        Assert.Equal(
            HabitConstants.TargetUnitMaxLength,
            targetUnit.GetMaxLength());
    }

    [Fact]
    public void Model_ShouldConfigureHabitIndexesWithoutNameUniqueness()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Habit));

        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes().ToList();

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(Habit.UserId), nameof(Habit.IsActive)]));

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(Habit.UserId), nameof(Habit.IsDeleted)]));

        Assert.DoesNotContain(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Habit.UserId), nameof(Habit.Name)]));
    }

    [Fact]
    public void Model_ShouldMapHabitLogWithUniqueCompletionIndexAndSupportingIndexes()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(HabitLog));

        Assert.NotNull(entityType);
        Assert.Equal("HabitLogs", entityType.GetTableName());

        var indexes = entityType.GetIndexes().ToList();

        var uniqueCompletionIndex = indexes.Single(
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [
                        nameof(HabitLog.UserId),
                        nameof(HabitLog.HabitId),
                        nameof(HabitLog.CompletionDate)
                    ]));

        Assert.True(uniqueCompletionIndex.IsUnique);

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(HabitLog.UserId), nameof(HabitLog.CompletionDate)]));

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(HabitLog.HabitId), nameof(HabitLog.CompletionDate)]));
    }

    [Fact]
    public void Model_ShouldConfigureHabitLogForeignKeyWithoutCascadeDelete()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(HabitLog));

        Assert.NotNull(entityType);

        var foreignKey = entityType.GetForeignKeys().Single(
            key => key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(HabitLog.HabitId)]));

        Assert.Equal(typeof(Habit), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Model_ShouldApplySoftDeleteFilterToAllBaseEntities()
    {
        using var context = CreateContext();

        var baseEntityTypes = context.Model
            .GetEntityTypes()
            .Where(entityType =>
                typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            .ToList();

        Assert.NotEmpty(baseEntityTypes);

        foreach (var entityType in baseEntityTypes)
        {
            Assert.NotNull(entityType.GetQueryFilter());
        }
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=lifeos_model_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(
            options,
            new TestDateTimeProvider());
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public DateOnly GetCurrentDate(string timeZoneId)
        {
            return DateOnly.FromDateTime(UtcNow.UtcDateTime);
        }

        public bool IsValidTimeZone(string timeZoneId)
        {
            return true;
        }
    }
}