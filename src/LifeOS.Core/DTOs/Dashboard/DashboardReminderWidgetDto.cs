using LifeOS.Core.DTOs.Reminders;

namespace LifeOS.Core.DTOs.Dashboard;

public sealed class DashboardReminderWidgetDto
{
    public IReadOnlyList<ReminderSummaryDto> Reminders { get; init; } =
        Array.Empty<ReminderSummaryDto>();
}
