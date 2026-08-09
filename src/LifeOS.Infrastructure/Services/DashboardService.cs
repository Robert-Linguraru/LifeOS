using LifeOS.Core.DTOs.Dashboard;
using LifeOS.Core.Services;

namespace LifeOS.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ITaskService _taskService;

    public DashboardService(
        ITaskService taskService)
    {
        _taskService = taskService;
    }

    public async Task<DashboardTaskWidgetDto> GetTaskWidgetAsync(
        CancellationToken cancellationToken = default)
    {
        var taskList =
            await _taskService.GetTaskListAsync(
                cancellationToken);

        return new DashboardTaskWidgetDto
        {
            CurrentDate = taskList.CurrentDate,
            Overdue = taskList.Overdue,
            Today = taskList.Today
        };
    }
}