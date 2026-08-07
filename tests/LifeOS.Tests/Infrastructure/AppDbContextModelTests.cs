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