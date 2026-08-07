namespace LifeOS.Core.DTOs.Tasks;

public sealed class TaskListDto
{
    public DateOnly CurrentDate { get; init; }

    public IReadOnlyList<TaskSummaryDto> Overdue { get; init; } =
        Array.Empty<TaskSummaryDto>();

    public IReadOnlyList<TaskSummaryDto> Today { get; init; } =
        Array.Empty<TaskSummaryDto>();

    public IReadOnlyList<TaskSummaryDto> Upcoming { get; init; } =
        Array.Empty<TaskSummaryDto>();

    public IReadOnlyList<TaskSummaryDto> Unscheduled { get; init; } =
        Array.Empty<TaskSummaryDto>();

    public IReadOnlyList<TaskSummaryDto> Completed { get; init; } =
        Array.Empty<TaskSummaryDto>();

    public IReadOnlyList<TaskSummaryDto> Archived { get; init; } =
        Array.Empty<TaskSummaryDto>();
}