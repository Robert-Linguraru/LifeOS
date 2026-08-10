using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Enums;

namespace LifeOS.Core.Entities;

public sealed class TaskItem : UserOwnedEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public TimeOnly? DueTime { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Low;

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Active;

    public TaskCategory Category { get; set; } = TaskCategory.Personal;

    public EstimatedTime EstimatedTime { get; set; } = EstimatedTime.Under15Minutes;

    public FrictionLevel FrictionLevel { get; set; } = FrictionLevel.Low;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateOnly? CompletedDate { get; set; }
}