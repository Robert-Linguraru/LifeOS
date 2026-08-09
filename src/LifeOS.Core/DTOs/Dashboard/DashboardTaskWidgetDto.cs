using LifeOS.Core.DTOs.Tasks;

namespace LifeOS.Core.DTOs.Dashboard;

public sealed class DashboardTaskWidgetDto
{
    public DateOnly CurrentDate { get; init; }

    public IReadOnlyList<TaskSummaryDto> Overdue { get; init; } =
        Array.Empty<TaskSummaryDto>();

    public IReadOnlyList<TaskSummaryDto> Today { get; init; } =
        Array.Empty<TaskSummaryDto>();
}