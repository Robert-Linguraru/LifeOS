using LifeOS.Core.DTOs.Dashboard;

namespace LifeOS.Core.Services;

public interface IDashboardService
{
    Task<DashboardTaskWidgetDto> GetTaskWidgetAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardHabitWidgetDto> GetHabitWidgetAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardXpWidgetDto> GetXpWidgetAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardReminderWidgetDto> GetReminderWidgetAsync(
        CancellationToken cancellationToken = default);
}