using LifeOS.Core.DTOs.Dashboard;

namespace LifeOS.Core.Services;

public interface IDashboardService
{
    Task<DashboardTaskWidgetDto> GetTaskWidgetAsync(
        CancellationToken cancellationToken = default);
}