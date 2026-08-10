using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Tasks;

namespace LifeOS.Tests.Core.Tasks;

public sealed class TaskItemTests
{
    [Fact]
    public void TaskItem_ShouldInheritFromUserOwnedEntity()
    {
        // Arrange and act
        var task = new TaskItem();

        // Assert
        Assert.IsAssignableFrom<UserOwnedEntity>(task);
    }

    [Fact]
    public void TaskItem_ShouldGenerateNonEmptyId()
    {
        // Arrange and act
        var task = new TaskItem();

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
    }

    [Fact]
    public void TaskItem_ShouldDefaultToActiveStatus()
    {
        // Arrange and act
        var task = new TaskItem();

        // Assert
        Assert.Equal(TaskItemStatus.Active, task.Status);
    }

    [Fact]
    public void TaskItem_ShouldUseDocumentedDefaults()
    {
        // Arrange and act
        var task = new TaskItem();

        // Assert
        Assert.Equal(TaskPriority.Low, task.Priority);
        Assert.Equal(TaskCategory.Personal, task.Category);
        Assert.Equal(EstimatedTime.Under15Minutes, task.EstimatedTime);
        Assert.Equal(FrictionLevel.Low, task.FrictionLevel);
    }

    [Fact]
    public void TaskItem_ShouldStartWithoutCompletionValues()
    {
        // Arrange and act
        var task = new TaskItem();

        // Assert
        Assert.Null(task.CompletedAtUtc);
        Assert.Null(task.CompletedDate);
    }

    [Fact]
    public void TaskItem_ShouldStartWithoutDueDateOrTime()
    {
        // Arrange and act
        var task = new TaskItem();

        // Assert
        Assert.Null(task.DueDate);
        Assert.Null(task.DueTime);
    }
}