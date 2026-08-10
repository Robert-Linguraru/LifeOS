using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Mappings;

namespace LifeOS.Tests.Core.Tasks;

public sealed class TaskItemMappingsTests
{
    [Fact]
    public void ToSummaryDto_ShouldMapSummaryFields()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Prepare presentation",
            DueDate = new DateOnly(2026, 8, 10),
            DueTime = new TimeOnly(14, 30),
            Priority = TaskPriority.High,
            Status = TaskItemStatus.Active,
            Category = TaskCategory.Work,
            EstimatedTime = EstimatedTime.Between30And60Minutes,
            FrictionLevel = FrictionLevel.Medium
        };

        // Act
        var result = task.ToSummaryDto();

        // Assert
        Assert.Equal(task.Id, result.Id);
        Assert.Equal(task.Title, result.Title);
        Assert.Equal(task.DueDate, result.DueDate);
        Assert.Equal(task.DueTime, result.DueTime);
        Assert.Equal(task.Priority, result.Priority);
        Assert.Equal(task.Status, result.Status);
        Assert.Equal(task.Category, result.Category);
        Assert.Equal(task.EstimatedTime, result.EstimatedTime);
        Assert.Equal(task.FrictionLevel, result.FrictionLevel);
    }

    [Fact]
    public void ToDetailsDto_ShouldMapAllExposedFields()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Prepare presentation",
            Description = "Prepare final slides and speaker notes.",
            DueDate = new DateOnly(2026, 8, 10),
            DueTime = new TimeOnly(14, 30),
            Priority = TaskPriority.High,
            Status = TaskItemStatus.Completed,
            Category = TaskCategory.Work,
            EstimatedTime = EstimatedTime.Between30And60Minutes,
            FrictionLevel = FrictionLevel.Medium,
            CompletedAtUtc =
                new DateTimeOffset(2026, 8, 10, 12, 45, 0, TimeSpan.Zero),
            CompletedDate = new DateOnly(2026, 8, 10),
            CreatedAtUtc =
                new DateTimeOffset(2026, 8, 7, 8, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc =
                new DateTimeOffset(2026, 8, 10, 12, 45, 0, TimeSpan.Zero)
        };

        // Act
        var result = task.ToDetailsDto();

        // Assert
        Assert.Equal(task.Id, result.Id);
        Assert.Equal(task.Title, result.Title);
        Assert.Equal(task.Description, result.Description);
        Assert.Equal(task.DueDate, result.DueDate);
        Assert.Equal(task.DueTime, result.DueTime);
        Assert.Equal(task.Priority, result.Priority);
        Assert.Equal(task.Status, result.Status);
        Assert.Equal(task.Category, result.Category);
        Assert.Equal(task.EstimatedTime, result.EstimatedTime);
        Assert.Equal(task.FrictionLevel, result.FrictionLevel);
        Assert.Equal(task.CompletedAtUtc, result.CompletedAtUtc);
        Assert.Equal(task.CompletedDate, result.CompletedDate);
        Assert.Equal(task.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(task.UpdatedAtUtc, result.UpdatedAtUtc);
    }
}