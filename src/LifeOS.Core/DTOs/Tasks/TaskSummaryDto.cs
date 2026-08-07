using LifeOS.Core.Enums.Tasks;

namespace LifeOS.Core.DTOs.Tasks;

public sealed class TaskSummaryDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public DateOnly? DueDate { get; init; }

    public TimeOnly? DueTime { get; init; }

    public TaskPriority Priority { get; init; }

    public TaskItemStatus Status { get; init; }

    public TaskCategory Category { get; init; }

    public EstimatedTime EstimatedTime { get; init; }

    public FrictionLevel FrictionLevel { get; init; }
}