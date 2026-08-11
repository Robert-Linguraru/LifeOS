using LifeOS.Core.DTOs.Dashboard;
using LifeOS.Core.Services;

namespace LifeOS.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ITaskService _taskService;
    private readonly IHabitService _habitService;

    public DashboardService(
        ITaskService taskService,
        IHabitService habitService)
    {
        _taskService = taskService;
        _habitService = habitService;
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

    public async Task<DashboardHabitWidgetDto> GetHabitWidgetAsync(
        CancellationToken cancellationToken = default)
    {
        var habitList = await _habitService.GetHabitListAsync(
            cancellationToken);

        var completedCount = habitList.Active
            .Count(habit => habit.IsCompletedToday);

        return new DashboardHabitWidgetDto
        {
            CurrentDate = habitList.CurrentDate,
            ActiveHabits = habitList.Active,
            CompletedCount = completedCount,
            TotalActiveCount = habitList.Active.Count
        };
    }
}