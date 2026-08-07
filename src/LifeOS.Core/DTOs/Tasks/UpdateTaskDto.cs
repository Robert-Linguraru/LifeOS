using LifeOS.Core.Enums.Tasks;

namespace LifeOS.Core.DTOs.Tasks;

public sealed class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public TimeOnly? DueTime { get; set; }

    public TaskPriority Priority { get; set; }

    public TaskCategory Category { get; set; }

    public EstimatedTime EstimatedTime { get; set; }

    public FrictionLevel FrictionLevel { get; set; }
}