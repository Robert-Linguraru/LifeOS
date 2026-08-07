using LifeOS.Core.Enums.Tasks;

namespace LifeOS.Core.DTOs.Tasks;

public sealed class TaskDetailsDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DateOnly? DueDate { get; init; }

    public TimeOnly? DueTime { get; init; }

    public TaskPriority Priority { get; init; }

    public TaskItemStatus Status { get; init; }

    public TaskCategory Category { get; init; }

    public EstimatedTime EstimatedTime { get; init; }

    public FrictionLevel FrictionLevel { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public DateOnly? CompletedDate { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}