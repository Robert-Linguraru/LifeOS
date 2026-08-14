using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Tasks;
using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Mappings;
using LifeOS.Core.Services;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IXpService _xpService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository repository,
        ICurrentUserService currentUser,
        IUserSettingsService userSettingsService,
        IDateTimeProvider dateTimeProvider,
        IXpService xpService,
        ILogger<TaskService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _userSettingsService = userSettingsService;
        _dateTimeProvider = dateTimeProvider;
        _xpService = xpService;
        _logger = logger;
    }

    public async Task<TaskDetailsDto> CreateTaskAsync(
        CreateTaskDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = GetCurrentUserId();

        var normalized = ValidateAndNormalize(
            dto.Title,
            dto.Description,
            dto.DueDate,
            dto.DueTime,
            dto.Priority,
            dto.Category,
            dto.EstimatedTime,
            dto.FrictionLevel);

        var task = new TaskItem
        {
            UserId = userId,
            Title = normalized.Title,
            Description = normalized.Description,
            DueDate = dto.DueDate,
            DueTime = dto.DueTime,
            Priority = dto.Priority,
            Category = dto.Category,
            EstimatedTime = dto.EstimatedTime,
            FrictionLevel = dto.FrictionLevel,
            Status = TaskItemStatus.Active
        };

        await _repository.AddAsync(
            task,
            cancellationToken);

        _logger.LogInformation(
            "Created task {TaskId} for user {UserId}",
            task.Id,
            userId);

        return task.ToDetailsDto();
    }

    public async Task<TaskDetailsDto> UpdateTaskAsync(
        Guid taskId,
        UpdateTaskDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = GetCurrentUserId();

        var task = await _repository.GetByIdAsync(
            userId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            throw new ResourceNotFoundException(
                "Task was not found.");
        }

        if (task.Status != TaskItemStatus.Active)
        {
            throw new ValidationException(
                "Only active tasks can be edited.");
        }

        var normalized = ValidateAndNormalize(
            dto.Title,
            dto.Description,
            dto.DueDate,
            dto.DueTime,
            dto.Priority,
            dto.Category,
            dto.EstimatedTime,
            dto.FrictionLevel);

        task.Title = normalized.Title;
        task.Description = normalized.Description;
        task.DueDate = dto.DueDate;
        task.DueTime = dto.DueTime;
        task.Priority = dto.Priority;
        task.Category = dto.Category;
        task.EstimatedTime = dto.EstimatedTime;
        task.FrictionLevel = dto.FrictionLevel;

        await _repository.UpdateAsync(
            task,
            cancellationToken);

        _logger.LogInformation(
            "Updated task {TaskId} for user {UserId}",
            task.Id,
            userId);

        return task.ToDetailsDto();
    }

    public async Task<TaskDetailsDto> GetTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var task = await _repository.GetByIdAsync(
            userId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            throw new ResourceNotFoundException(
                "Task was not found.");
        }

        return task.ToDetailsDto();
    }

    public async Task<TaskListDto> GetTaskListAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var settings =
            await _userSettingsService.GetCurrentUserSettingsAsync(
                cancellationToken);

        var currentDate =
            _dateTimeProvider.GetCurrentDate(settings.TimeZoneId);

        var tasks = await _repository.GetAllByUserIdAsync(
            userId,
            cancellationToken);

        var activeTasks = tasks
            .Where(task => task.Status == TaskItemStatus.Active)
            .ToList();

        var overdue = OrderDatedTasks(
                activeTasks.Where(
                    task =>
                        task.DueDate.HasValue &&
                        task.DueDate.Value < currentDate))
            .Select(task => task.ToSummaryDto())
            .ToList();

        var today = OrderDatedTasks(
                activeTasks.Where(
                    task => task.DueDate == currentDate))
            .Select(task => task.ToSummaryDto())
            .ToList();

        var upcoming = OrderDatedTasks(
                activeTasks.Where(
                    task =>
                        task.DueDate.HasValue &&
                        task.DueDate.Value > currentDate))
            .Select(task => task.ToSummaryDto())
            .ToList();

        var unscheduled = activeTasks
            .Where(task => !task.DueDate.HasValue)
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.Title)
            .Select(task => task.ToSummaryDto())
            .ToList();

        var completed = tasks
            .Where(task => task.Status == TaskItemStatus.Completed)
            .OrderByDescending(task => task.CompletedAtUtc)
            .ThenBy(task => task.Title)
            .Select(task => task.ToSummaryDto())
            .ToList();

        var archived = tasks
            .Where(task => task.Status == TaskItemStatus.Archived)
            .OrderByDescending(task => task.UpdatedAtUtc)
            .ThenBy(task => task.Title)
            .Select(task => task.ToSummaryDto())
            .ToList();

        return new TaskListDto
        {
            CurrentDate = currentDate,
            Overdue = overdue,
            Today = today,
            Upcoming = upcoming,
            Unscheduled = unscheduled,
            Completed = completed,
            Archived = archived
        };
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.UserId == Guid.Empty)
        {
            throw new CurrentUserUnavailableException();
        }

        return _currentUser.UserId;
    }

    private static (
        string Title,
        string? Description)
        ValidateAndNormalize(
            string title,
            string? description,
            DateOnly? dueDate,
            TimeOnly? dueTime,
            TaskPriority priority,
            TaskCategory category,
            EstimatedTime estimatedTime,
            FrictionLevel frictionLevel)
    {
        var normalizedTitle = title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            throw new ValidationException(
                "Task title is required.");
        }

        if (normalizedTitle.Length >
            TaskConstants.TitleMaxLength)
        {
            throw new ValidationException(
                $"Task title cannot exceed " +
                $"{TaskConstants.TitleMaxLength} characters.");
        }

        var normalizedDescription =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        if (normalizedDescription?.Length >
            TaskConstants.DescriptionMaxLength)
        {
            throw new ValidationException(
                $"Task description cannot exceed " +
                $"{TaskConstants.DescriptionMaxLength} characters.");
        }

        if (dueTime.HasValue && !dueDate.HasValue)
        {
            throw new ValidationException(
                "A due date is required when a due time is provided.");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new ValidationException(
                "Task priority is invalid.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new ValidationException(
                "Task category is invalid.");
        }

        if (!Enum.IsDefined(estimatedTime))
        {
            throw new ValidationException(
                "Estimated time is invalid.");
        }

        if (!Enum.IsDefined(frictionLevel))
        {
            throw new ValidationException(
                "Friction level is invalid.");
        }

        return (
            normalizedTitle,
            normalizedDescription);
    }

    private static IOrderedEnumerable<TaskItem> OrderDatedTasks(
        IEnumerable<TaskItem> tasks)
    {
        return tasks
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.DueTime.HasValue ? 0 : 1)
            .ThenBy(task => task.DueTime)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title);
    }

    public async Task<TaskCompletionResultDto> CompleteTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var task = await _repository.GetByIdAsync(
            userId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            throw new ResourceNotFoundException(
                "Task was not found.");
        }

        if (task.Status == TaskItemStatus.Completed)
        {
            return CreateCompletionResult(task, false);
        }

        if (task.Status != TaskItemStatus.Active)
        {
            throw new ValidationException(
                "Only active tasks can be completed.");
        }

        var settings =
            await _userSettingsService.GetCurrentUserSettingsAsync(
                cancellationToken);

        var completion = await _repository.CompleteAsync(
            userId,
            taskId,
            _dateTimeProvider.UtcNow,
            _dateTimeProvider.GetCurrentDate(settings.TimeZoneId),
            cancellationToken);

        if (completion.Status == TaskCompletionWriteStatus.NotFound)
        {
            throw new ResourceNotFoundException("Task was not found.");
        }

        if (completion.Status == TaskCompletionWriteStatus.Archived)
        {
            throw new ValidationException("Only active tasks can be completed.");
        }

        var authoritativeTask = completion.Task ??
            throw new InvalidOperationException("Task completion did not return the authoritative task.");

        if (completion.Status == TaskCompletionWriteStatus.AlreadyCompleted)
        {
            return CreateCompletionResult(authoritativeTask, false);
        }

        XpAwardResultDto? xpAward = null;
        var xpAwardFailed = false;

        try
        {
            xpAward = await _xpService.AwardQuestXpAsync(
                new AwardQuestXpDto
                {
                    SourceType = XpSourceType.Task,
                    SourceEntityId = authoritativeTask.Id,
                    OccurredAtUtc = authoritativeTask.CompletedAtUtc ??
                        throw new InvalidOperationException("Completed task timestamp is missing."),
                    BusinessDate = authoritativeTask.CompletedDate ??
                        throw new InvalidOperationException("Completed task date is missing."),
                    EstimatedTime = authoritativeTask.EstimatedTime,
                    FrictionLevel = authoritativeTask.FrictionLevel
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            xpAwardFailed = true;
            _logger.LogError(
                exception,
                "XP award failed after completing task {TaskId} for user {UserId}",
                authoritativeTask.Id,
                userId);
        }

        _logger.LogInformation(
            "Completed task {TaskId} for user {UserId}",
            task.Id,
            userId);

        return CreateCompletionResult(
            authoritativeTask,
            true,
            xpAward,
            xpAwardFailed);
    }

    private static TaskCompletionResultDto CreateCompletionResult(
        TaskItem task,
        bool wasNewlyCompleted,
        XpAwardResultDto? xpAward = null,
        bool xpAwardFailed = false)
    {
        return new TaskCompletionResultDto
        {
            Task = task.ToDetailsDto(),
            WasNewlyCompleted = wasNewlyCompleted,
            XpAward = xpAward,
            XpAwardFailed = xpAwardFailed
        };
    }

    public async Task<TaskDetailsDto> ArchiveTaskAsync(
    Guid taskId,
    CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var task = await _repository.GetByIdAsync(
            userId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            throw new ResourceNotFoundException(
                "Task was not found.");
        }

        if (task.Status == TaskItemStatus.Archived)
        {
            return task.ToDetailsDto();
        }

        if (task.Status != TaskItemStatus.Active)
        {
            throw new ValidationException(
                "Only active tasks can be archived.");
        }

        task.Status = TaskItemStatus.Archived;

        await _repository.UpdateAsync(
            task,
            cancellationToken);

        _logger.LogInformation(
            "Archived task {TaskId} for user {UserId}",
            task.Id,
            userId);

        return task.ToDetailsDto();
    }

    public async Task DeleteTaskAsync(
    Guid taskId,
    CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var task = await _repository.GetByIdAsync(
            userId,
            taskId,
            cancellationToken);

        if (task is null)
        {
            throw new ResourceNotFoundException(
                "Task was not found.");
        }

        await _repository.DeleteAsync(
            task,
            cancellationToken);

        _logger.LogInformation(
            "Deleted task {TaskId} for user {UserId}",
            task.Id,
            userId);
    }
}