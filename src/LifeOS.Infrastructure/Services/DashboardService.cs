using LifeOS.Core.Abstractions;
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
    private readonly IFinanceService? _financeService;
    private readonly IUserSettingsService? _userSettingsService;
    private readonly IDateTimeProvider? _dateTimeProvider;

    public DashboardService(
        ITaskService taskService,
        IHabitService habitService,
        IXpService xpService,
        IReminderService? reminderService = null,
        IFinanceService? financeService = null,
        IUserSettingsService? userSettingsService = null,
        IDateTimeProvider? dateTimeProvider = null)
    {
        _taskService = taskService;
        _habitService = habitService;
        _xpService = xpService;
        _reminderService = reminderService;
        _financeService = financeService;
        _userSettingsService = userSettingsService;
        _dateTimeProvider = dateTimeProvider;
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

    public async Task<DashboardFinanceWidgetDto> GetFinanceWidgetAsync(
        CancellationToken cancellationToken = default)
    {
        if (_financeService is null ||
            _userSettingsService is null ||
            _dateTimeProvider is null)
        {
            throw new InvalidOperationException(
                "The Finance dashboard capability is not configured.");
        }

        var settings = await _userSettingsService.GetCurrentUserSettingsAsync(
            cancellationToken);
        var currentDate = _dateTimeProvider.GetCurrentDate(settings.TimeZoneId);
        var summary = await _financeService.GetMonthlySummaryAsync(
            currentDate.Year,
            currentDate.Month,
            cancellationToken);
        var largestCategory = summary.ExpenseCategories
            .OrderByDescending(category => category.TotalExpenses)
            .ThenBy(category => category.CategoryName)
            .FirstOrDefault();

        return new DashboardFinanceWidgetDto
        {
            HasTransactions = summary.Transactions.Count > 0,
            TotalIncome = summary.TotalIncome,
            TotalExpenses = summary.TotalExpenses,
            LargestExpenseCategory = largestCategory?.CategoryName
        };
    }
}