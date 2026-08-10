using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Tasks;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Enums;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class TaskServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<ITaskRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserSettingsService> _userSettingsService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<ILogger<TaskService>> _logger = new();

    private TaskService CreateService()
    {
        _currentUser
            .Setup(x => x.UserId)
            .Returns(UserId);

        _currentUser
            .Setup(x => x.IsAuthenticated)
            .Returns(true);

        return new TaskService(
            _repository.Object,
            _currentUser.Object,
            _userSettingsService.Object,
            _dateTimeProvider.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CreateTaskAsync_CreatesTaskForCurrentUser()
    {
        var dto = new CreateTaskDto
        {
            Title = "  Prepare presentation  ",
            Description = "  Finish the final slides.  ",
            Priority = TaskPriority.High,
            Category = TaskCategory.Work,
            EstimatedTime =
                EstimatedTime.Between30And60Minutes,
            FrictionLevel = FrictionLevel.Medium
        };

        var service = CreateService();

        var result = await service.CreateTaskAsync(dto);

        Assert.Equal("Prepare presentation", result.Title);
        Assert.Equal(
            "Finish the final slides.",
            result.Description);
        Assert.Equal(TaskItemStatus.Active, result.Status);

        _repository.Verify(
            x => x.AddAsync(
                It.Is<TaskItem>(task =>
                    task.UserId == UserId &&
                    task.Title == "Prepare presentation" &&
                    task.Status == TaskItemStatus.Active),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_WhitespaceDescription_NormalizesToNull()
    {
        var dto = new CreateTaskDto
        {
            Title = "Prepare presentation",
            Description = "   "
        };

        var service = CreateService();

        var result = await service.CreateTaskAsync(dto);

        Assert.Null(result.Description);
    }

    [Fact]
    public async Task CreateTaskAsync_EmptyTitle_ThrowsValidationException()
    {
        var dto = new CreateTaskDto
        {
            Title = "   "
        };

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateTaskAsync(dto));

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTaskAsync_DueTimeWithoutDate_ThrowsValidationException()
    {
        var dto = new CreateTaskDto
        {
            Title = "Prepare presentation",
            DueTime = new TimeOnly(14, 30)
        };

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateTaskAsync(dto));

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTaskAsync_InvalidEnum_ThrowsValidationException()
    {
        var dto = new CreateTaskDto
        {
            Title = "Prepare presentation",
            Priority = (TaskPriority)999
        };

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateTaskAsync(dto));
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesActiveTask()
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Old title",
            Status = TaskItemStatus.Active
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var dto = new UpdateTaskDto
        {
            Title = "  Updated title  ",
            Description = "  Updated description  ",
            Priority = TaskPriority.High,
            Category = TaskCategory.Work,
            EstimatedTime = EstimatedTime.Over60Minutes,
            FrictionLevel = FrictionLevel.High
        };

        var service = CreateService();

        var result =
            await service.UpdateTaskAsync(taskId, dto);

        Assert.Equal("Updated title", result.Title);
        Assert.Equal(
            "Updated description",
            result.Description);
        Assert.Equal(TaskPriority.High, result.Priority);

        _repository.Verify(
            x => x.UpdateAsync(
                task,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Archived)]
    public async Task UpdateTaskAsync_NonActiveTask_ThrowsValidationException(
        TaskItemStatus status)
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Existing task",
            Status = status
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var dto = new UpdateTaskDto
        {
            Title = "Updated task"
        };

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateTaskAsync(taskId, dto));

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTaskAsync_TaskMissing_ThrowsResourceNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.UpdateTaskAsync(
                taskId,
                new UpdateTaskDto
                {
                    Title = "Updated task"
                }));
    }

    [Fact]
    public async Task GetTaskByIdAsync_ReturnsOwnedTask()
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Existing task"
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        var result =
            await service.GetTaskByIdAsync(taskId);

        Assert.Equal(task.Id, result.Id);
        Assert.Equal(task.Title, result.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_TaskMissing_ThrowsResourceNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetTaskByIdAsync(taskId));
    }

    [Fact]
    public async Task GetTaskListAsync_ClassifiesTasksByLocalDateAndStatus()
    {
        var currentDate = new DateOnly(2026, 8, 8);

        _userSettingsService
            .Setup(x => x.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                UserId = UserId,
                TimeZoneId = "Europe/Bucharest"
            });

        _dateTimeProvider
            .Setup(x => x.GetCurrentDate(
                "Europe/Bucharest"))
            .Returns(currentDate);

        var tasks = new List<TaskItem>
        {
            new()
            {
                UserId = UserId,
                Title = "Overdue",
                DueDate = currentDate.AddDays(-1),
                Status = TaskItemStatus.Active
            },
            new()
            {
                UserId = UserId,
                Title = "Today",
                DueDate = currentDate,
                Status = TaskItemStatus.Active
            },
            new()
            {
                UserId = UserId,
                Title = "Upcoming",
                DueDate = currentDate.AddDays(1),
                Status = TaskItemStatus.Active
            },
            new()
            {
                UserId = UserId,
                Title = "Unscheduled",
                DueDate = null,
                Status = TaskItemStatus.Active
            },
            new()
            {
                UserId = UserId,
                Title = "Completed",
                DueDate = currentDate.AddDays(-5),
                Status = TaskItemStatus.Completed,
                CompletedAtUtc =
                    new DateTimeOffset(
                        2026,
                        8,
                        8,
                        10,
                        0,
                        0,
                        TimeSpan.Zero)
            },
            new()
            {
                UserId = UserId,
                Title = "Archived",
                DueDate = currentDate,
                Status = TaskItemStatus.Archived
            }
        };

        _repository
            .Setup(x => x.GetAllByUserIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var service = CreateService();

        var result =
            await service.GetTaskListAsync();

        Assert.Equal(currentDate, result.CurrentDate);

        Assert.Single(result.Overdue);
        Assert.Equal("Overdue", result.Overdue[0].Title);

        Assert.Single(result.Today);
        Assert.Equal("Today", result.Today[0].Title);

        Assert.Single(result.Upcoming);
        Assert.Equal("Upcoming", result.Upcoming[0].Title);

        Assert.Single(result.Unscheduled);
        Assert.Equal(
            "Unscheduled",
            result.Unscheduled[0].Title);

        Assert.Single(result.Completed);
        Assert.Equal(
            "Completed",
            result.Completed[0].Title);

        Assert.Single(result.Archived);
        Assert.Equal(
            "Archived",
            result.Archived[0].Title);
    }

    [Fact]
    public async Task GetTaskListAsync_UsesConfiguredUserTimeZone()
    {
        _userSettingsService
            .Setup(x => x.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                UserId = UserId,
                TimeZoneId = "Europe/Bucharest"
            });

        _dateTimeProvider
            .Setup(x => x.GetCurrentDate(
                "Europe/Bucharest"))
            .Returns(new DateOnly(2026, 8, 8));

        _repository
            .Setup(x => x.GetAllByUserIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TaskItem>());

        var service = CreateService();

        await service.GetTaskListAsync();

        _dateTimeProvider.Verify(
            x => x.GetCurrentDate(
                "Europe/Bucharest"),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_CurrentUserUnavailable_ThrowsException()
    {
        var service = CreateService();

        _currentUser
            .Setup(x => x.IsAuthenticated)
            .Returns(false);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.CreateTaskAsync(
                new CreateTaskDto
                {
                    Title = "Task"
                }));
    }

    [Fact]
    public async Task CompleteTaskAsync_ActiveTask_CompletesTask()
    {
        var taskId = Guid.NewGuid();
        var utcNow =
            new DateTimeOffset(
                2026,
                8,
                8,
                22,
                30,
                0,
                TimeSpan.Zero);

        var localDate =
            new DateOnly(2026, 8, 9);

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Complete me",
            Status = TaskItemStatus.Active
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _userSettingsService
            .Setup(x => x.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                UserId = UserId,
                TimeZoneId = "Europe/Bucharest"
            });

        _dateTimeProvider
            .Setup(x => x.UtcNow)
            .Returns(utcNow);

        _dateTimeProvider
            .Setup(x => x.GetCurrentDate(
                "Europe/Bucharest"))
            .Returns(localDate);

        var service = CreateService();

        var result =
            await service.CompleteTaskAsync(taskId);

        Assert.Equal(
            TaskItemStatus.Completed,
            result.Status);

        Assert.Equal(
            utcNow,
            result.CompletedAtUtc);

        Assert.Equal(
            localDate,
            result.CompletedDate);

        _repository.Verify(
            x => x.UpdateAsync(
                task,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_AlreadyCompleted_IsIdempotent()
    {
        var taskId = Guid.NewGuid();

        var originalCompletedAtUtc =
            new DateTimeOffset(
                2026,
                8,
                7,
                18,
                30,
                0,
                TimeSpan.Zero);

        var originalCompletedDate =
            new DateOnly(2026, 8, 7);

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Already completed",
            Status = TaskItemStatus.Completed,
            CompletedAtUtc = originalCompletedAtUtc,
            CompletedDate = originalCompletedDate
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        var result =
            await service.CompleteTaskAsync(taskId);

        Assert.Equal(
            originalCompletedAtUtc,
            result.CompletedAtUtc);

        Assert.Equal(
            originalCompletedDate,
            result.CompletedDate);

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _userSettingsService.Verify(
            x => x.GetCurrentUserSettingsAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteTaskAsync_ArchivedTask_ThrowsValidationException()
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Archived task",
            Status = TaskItemStatus.Archived
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CompleteTaskAsync(taskId));

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteTaskAsync_TaskMissing_ThrowsResourceNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.CompleteTaskAsync(taskId));

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ArchiveTaskAsync_ActiveTask_ArchivesTask()
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Archive me",
            Status = TaskItemStatus.Active
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        var result =
            await service.ArchiveTaskAsync(taskId);

        Assert.Equal(
            TaskItemStatus.Archived,
            result.Status);

        _repository.Verify(
            x => x.UpdateAsync(
                task,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ArchiveTaskAsync_AlreadyArchived_IsIdempotent()
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Already archived",
            Status = TaskItemStatus.Archived
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        var result =
            await service.ArchiveTaskAsync(taskId);

        Assert.Equal(
            TaskItemStatus.Archived,
            result.Status);

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ArchiveTaskAsync_CompletedTask_ThrowsValidationException()
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Completed task",
            Status = TaskItemStatus.Completed
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.ArchiveTaskAsync(taskId));

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(TaskItemStatus.Active)]
    [InlineData(TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Archived)]
    public async Task DeleteTaskAsync_OwnedTask_DeletesTask(
    TaskItemStatus status)
    {
        var taskId = Guid.NewGuid();

        var task = new TaskItem
        {
            UserId = UserId,
            Title = "Delete me",
            Status = status
        };

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        await service.DeleteTaskAsync(taskId);

        _repository.Verify(
            x => x.DeleteAsync(
                task,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_TaskMissing_ThrowsResourceNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(
                UserId,
                taskId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.DeleteTaskAsync(taskId));

        _repository.Verify(
            x => x.DeleteAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}