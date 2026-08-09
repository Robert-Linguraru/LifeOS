using LifeOS.Core.DTOs.Tasks;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Services;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class DashboardServiceTests
{
    private readonly Mock<ITaskService> _taskService = new();

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
                _taskService.Object);

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
                _taskService.Object);

        // Act
        await service.GetTaskWidgetAsync();

        // Assert
        _taskService.Verify(
            service =>
                service.GetTaskListAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
}