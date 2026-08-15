using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Dashboard;
using LifeOS.Core.Services;

namespace LifeOS.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ITaskService _taskService;
    private readonly IHabitService _habitService;
    private readonly IXpService _xpService;
    private readonly IReminderService? _reminderService;

    public DashboardService(
        ITaskService taskService,
        IHabitService habitService,
        IXpService xpService,
        IReminderService? reminderService = null)
    {
        _taskService = taskService;
        _habitService = habitService;
        _xpService = xpService;
        _reminderService = reminderService;
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

    public async Task<DashboardXpWidgetDto> GetXpWidgetAsync(
        CancellationToken cancellationToken = default)
    {
        var progression = await _xpService.GetProgressionAsync(cancellationToken);
        var dailyCap = XpConstants.DailyQuestXpCap;
        var dailyXp = progression.DailyQuestXpToday;
        var remainingXp = Math.Max(0, dailyCap - dailyXp);
        var percentageXp = Math.Clamp(dailyXp, 0, dailyCap);

        return new DashboardXpWidgetDto
        {
            TotalLifetimeXp = progression.TotalLifetimeXp,
            CurrentLevel = progression.CurrentLevel,
            CurrentEchelon = progression.CurrentEchelon,
            DailyQuestXpToday = dailyXp,
            DailyQuestXpCap = dailyCap,
            RemainingQuestXp = remainingXp,
            ProgressPercent = percentageXp * 100 / dailyCap
        };
    }

    public async Task<DashboardReminderWidgetDto> GetReminderWidgetAsync(
        CancellationToken cancellationToken = default)
    {
        if (_reminderService is null)
        {
            throw new InvalidOperationException(
                "The Reminder service is not configured.");
        }

        return new DashboardReminderWidgetDto
        {
            Reminders = await _reminderService.GetPendingAsync(
                cancellationToken,
                ReminderConstants.DashboardListLimit)
        };
    }
}