using LifeOS.Core.Enums.Tasks;

namespace LifeOS.Core.DTOs.Tasks;

public sealed class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public TimeOnly? DueTime { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Low;

    public TaskCategory Category { get; set; } = TaskCategory.Personal;

    public EstimatedTime EstimatedTime { get; set; } =
        EstimatedTime.Under15Minutes;

    public FrictionLevel FrictionLevel { get; set; } = FrictionLevel.Low;
}