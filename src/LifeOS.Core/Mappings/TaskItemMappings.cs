using LifeOS.Core.DTOs.Tasks;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Mappings;

public static class TaskItemMappings
{
    public static TaskSummaryDto ToSummaryDto(this TaskItem task)
    {
        return new TaskSummaryDto
        {
            Id = task.Id,
            Title = task.Title,
            DueDate = task.DueDate,
            DueTime = task.DueTime,
            Priority = task.Priority,
            Status = task.Status,
            Category = task.Category,
            EstimatedTime = task.EstimatedTime,
            FrictionLevel = task.FrictionLevel
        };
    }

    public static TaskDetailsDto ToDetailsDto(this TaskItem task)
    {
        return new TaskDetailsDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            DueTime = task.DueTime,
            Priority = task.Priority,
            Status = task.Status,
            Category = task.Category,
            EstimatedTime = task.EstimatedTime,
            FrictionLevel = task.FrictionLevel,
            CompletedAtUtc = task.CompletedAtUtc,
            CompletedDate = task.CompletedDate,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc
        };
    }
}