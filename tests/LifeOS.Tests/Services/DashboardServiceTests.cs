using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.DTOs.Reminders;
using LifeOS.Core.DTOs.Tasks;
using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Constants;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Core.Enums.Habits;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Finance;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Services;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class DashboardServiceTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<IHabitService> _habitService = new();
    private readonly Mock<IXpService> _xpService = new();
    private readonly Mock<IReminderService> _reminderService = new();
    private readonly Mock<IFinanceService> _financeService = new();
    private readonly Mock<IUserSettingsService> _userSettingsService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    [Fact]
    public async Task GetFinanceWidgetAsync_ProjectsCurrentMonthFromFinanceSummary()
    {
        var currentDate = new DateOnly(2026, 9, 15);
        var categoryId = Guid.NewGuid();
        _userSettingsService.Setup(service => service.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { TimeZoneId = "UTC" });
        _dateTimeProvider.Setup(service => service.GetCurrentDate("UTC"))
            .Returns(currentDate);
        _financeService.Setup(service => service.GetMonthlySummaryAsync(
                2026, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceSummaryDto
            {
                TotalIncome = 3000,
                TotalExpenses = 1450,
                ExpenseCategories =
                [
                    new FinanceCategorySummaryDto
                    {
                        CategoryId = categoryId,
                        CategoryName = "Housing/Rent",
                        TotalExpenses = 900
                    },
                    new FinanceCategorySummaryDto
                    {
                        CategoryId = Guid.NewGuid(),
                        CategoryName = "Food",
                        TotalExpenses = 550
                    }
                ],
                Transactions =
                [new FinanceTransactionDto { Id = Guid.NewGuid() }]
            });

        var service = new DashboardService(
            _taskService.Object,
            _habitService.Object,
            _xpService.Object,
            financeService: _financeService.Object,
            userSettingsService: _userSettingsService.Object,
            dateTimeProvider: _dateTimeProvider.Object);

        var result = await service.GetFinanceWidgetAsync();

        Assert.True(result.HasTransactions);
        Assert.Equal(3000, result.TotalIncome);
        Assert.Equal(1450, result.TotalExpenses);
        Assert.Equal(1550, result.NetCashFlow);
        Assert.Equal("Housing/Rent", result.LargestExpenseCategory);
        _financeService.Verify(service => service.GetMonthlySummaryAsync(
            2026, 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFinanceWidgetAsync_ProjectsEmptySummary()
    {
        _userSettingsService.Setup(service => service.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { TimeZoneId = "UTC" });
        _dateTimeProvider.Setup(service => service.GetCurrentDate("UTC"))
            .Returns(new DateOnly(2026, 9, 15));
        _financeService.Setup(service => service.GetMonthlySummaryAsync(
                2026, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceSummaryDto());

        var service = new DashboardService(
            _taskService.Object,
            _habitService.Object,
            _xpService.Object,
            financeService: _financeService.Object,
            userSettingsService: _userSettingsService.Object,
            dateTimeProvider: _dateTimeProvider.Object);

        var result = await service.GetFinanceWidgetAsync();

        Assert.False(result.HasTransactions);
        Assert.Null(result.LargestExpenseCategory);
    }

    [Fact]
    public async Task GetTaskWidgetAsync_ReturnsOnlyOverdueAndTodayTasks()
    {
        // Arrange
        var currentDate =
            new DateOnly(2026, 8, 9);

        var overdueTask =
            new TaskSummaryDto
            {
                Id = Guid.NewGuid(),
                Title = "Overdue task",
                DueDate = currentDate.AddDays(-1)
            };

        var todayTask =
            new TaskSummaryDto
            {
                Id = Guid.NewGuid(),
                Title = "Today task",
                DueDate = currentDate
            };

        var upcomingTask =
            new TaskSummaryDto
            {
                Id = Guid.NewGuid(),
                Title = "Upcoming task",
                DueDate = currentDate.AddDays(1)
            };

        _taskService
            .Setup(service =>
                service.GetTaskListAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TaskListDto
                {
                    CurrentDate = currentDate,
                    Overdue = [overdueTask],
                    Today = [todayTask],
                    Upcoming = [upcomingTask]
                });

        var service =
            new DashboardService(
                _taskService.Object,
                _habitService.Object,
                _xpService.Object);

        // Act
        var result =
            await service.GetTaskWidgetAsync();

        // Assert
        Assert.Equal(
            currentDate,
            result.CurrentDate);

        Assert.Single(result.Overdue);
        Assert.Equal(
            overdueTask.Id,
            result.Overdue[0].Id);

        Assert.Single(result.Today);
        Assert.Equal(
            todayTask.Id,
            result.Today[0].Id);
    }

    [Fact]
    public async Task GetTaskWidgetAsync_UsesTaskService()
    {
        // Arrange
        _taskService
            .Setup(service =>
                service.GetTaskListAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TaskListDto
                {
                    CurrentDate =
                        new DateOnly(2026, 8, 9)
                });

        var service =
            new DashboardService(
                _taskService.Object,
                _habitService.Object,
                _xpService.Object);

        // Act
        await service.GetTaskWidgetAsync();

        // Assert
        _taskService.Verify(
            service =>
                service.GetTaskListAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHabitWidgetAsync_ProjectsActiveHabitsAndCounts()
    {
        var currentDate = new DateOnly(2026, 8, 10);
        var completedHabit = new HabitSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = "Read",
            TargetType = HabitTargetType.Quantity,
            TargetQuantity = 30m,
            TargetUnit = "minutes",
            IsCompletedToday = true,
            CurrentStreak = 4
        };
        var activeHabit = new HabitSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = "Walk",
            IsCompletedToday = false,
            CurrentStreak = 1
        };
        var archivedHabit = new HabitSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = "Archived",
            IsActive = false
        };

        _habitService
            .Setup(service => service.GetHabitListAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HabitListDto
            {
                CurrentDate = currentDate,
                Active = [completedHabit, activeHabit],
                Archived = [archivedHabit]
            });

        var service = new DashboardService(
            _taskService.Object,
            _habitService.Object,
            _xpService.Object);

        var result = await service.GetHabitWidgetAsync();

        Assert.Equal(currentDate, result.CurrentDate);
        Assert.Equal(2, result.TotalActiveCount);
        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(2, result.ActiveHabits.Count);
        Assert.DoesNotContain(
            result.ActiveHabits,
            habit => habit.Id == archivedHabit.Id);
        Assert.True(result.ActiveHabits[0].IsCompletedToday);
        Assert.Equal(4, result.ActiveHabits[0].CurrentStreak);
        Assert.Equal(30m, result.ActiveHabits[0].TargetQuantity);
        Assert.Equal("minutes", result.ActiveHabits[0].TargetUnit);

        _habitService.Verify(
            service => service.GetHabitListAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHabitWidgetAsync_WithNoActiveHabitsReturnsEmptyCounts()
    {
        _habitService
            .Setup(service => service.GetHabitListAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HabitListDto
            {
                CurrentDate = new DateOnly(2026, 8, 10),
                Archived =
                [
                    new HabitSummaryDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Archived",
                        IsActive = false
                    }
                ]
            });

        var service = new DashboardService(
            _taskService.Object,
            _habitService.Object,
            _xpService.Object);

        var result = await service.GetHabitWidgetAsync();

        Assert.Equal(0, result.TotalActiveCount);
        Assert.Equal(0, result.CompletedCount);
        Assert.Empty(result.ActiveHabits);
    }

    [Fact]
    public async Task GetXpWidgetAsync_NewUser_ProjectsDefaultState()
    {
        _xpService.Setup(service => service.GetProgressionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProgressionDto
            {
                TotalLifetimeXp = 0,
                CurrentLevel = 1,
                CurrentEchelon = Echelon.Iron,
                DailyQuestXpToday = 0
            });

        var service = new DashboardService(_taskService.Object, _habitService.Object, _xpService.Object);

        var result = await service.GetXpWidgetAsync();

        Assert.Equal(0L, result.TotalLifetimeXp);
        Assert.Equal(1, result.CurrentLevel);
        Assert.Equal(Echelon.Iron, result.CurrentEchelon);
        Assert.Equal(0, result.DailyQuestXpToday);
        Assert.Equal(500, result.DailyQuestXpCap);
        Assert.Equal(500, result.RemainingQuestXp);
        Assert.Equal(0, result.ProgressPercent);
    }

    [Theory]
    [InlineData(0, 0, 500)]
    [InlineData(1, 0, 499)]
    [InlineData(250, 50, 250)]
    [InlineData(499, 99, 1)]
    [InlineData(500, 100, 0)]
    public async Task GetXpWidgetAsync_ProjectsCapAndPercentage(
        int dailyXp, int expectedPercent, int expectedRemaining)
    {
        _xpService.Setup(service => service.GetProgressionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProgressionDto
            {
                TotalLifetimeXp = 2700,
                CurrentLevel = 10,
                CurrentEchelon = Echelon.Bronze,
                DailyQuestXpToday = dailyXp
            });

        var service = new DashboardService(_taskService.Object, _habitService.Object, _xpService.Object);

        var result = await service.GetXpWidgetAsync();

        Assert.Equal(2700, result.TotalLifetimeXp);
        Assert.Equal(10, result.CurrentLevel);
        Assert.Equal(Echelon.Bronze, result.CurrentEchelon);
        Assert.Equal(500, result.DailyQuestXpCap);
        Assert.Equal(expectedRemaining, result.RemainingQuestXp);
        Assert.Equal(expectedPercent, result.ProgressPercent);
        _xpService.Verify(service => service.GetProgressionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetXpWidgetAsync_ProgressionError_Propagates()
    {
        var exception = new InvalidOperationException("progression failed");
        _xpService.Setup(service => service.GetProgressionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var service = new DashboardService(_taskService.Object, _habitService.Object, _xpService.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetXpWidgetAsync());

        Assert.Same(exception, actual);
    }

    [Fact]
    public async Task GetReminderWidgetAsync_UsesBoundedPendingReminderQuery()
    {
        var reminders = new[]
        {
            new ReminderSummaryDto
            {
                Id = Guid.NewGuid(),
                Title = "First",
                SourceType = ReminderSourceType.Custom
            },
            new ReminderSummaryDto
            {
                Id = Guid.NewGuid(),
                Title = "Second",
                SourceType = ReminderSourceType.Task
            }
        };
        _reminderService
            .Setup(service => service.GetPendingAsync(
                It.IsAny<CancellationToken>(),
                ReminderConstants.DashboardListLimit))
            .ReturnsAsync(reminders);

        var service = new DashboardService(
            _taskService.Object,
            _habitService.Object,
            _xpService.Object,
            _reminderService.Object);

        var result = await service.GetReminderWidgetAsync();

        Assert.Equal(reminders, result.Reminders);
        _reminderService.Verify(reminderService => reminderService.GetPendingAsync(
            It.IsAny<CancellationToken>(),
            ReminderConstants.DashboardListLimit), Times.Once);
    }

    [Fact]
    public async Task GetReminderWidgetAsync_PropagatesReminderQueryFailure()
    {
        var exception = new InvalidOperationException("reminders failed");
        _reminderService
            .Setup(service => service.GetPendingAsync(
                It.IsAny<CancellationToken>(),
                ReminderConstants.DashboardListLimit))
            .ThrowsAsync(exception);

        var service = new DashboardService(
            _taskService.Object,
            _habitService.Object,
            _xpService.Object,
            _reminderService.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetReminderWidgetAsync());

        Assert.Same(exception, actual);
    }
}