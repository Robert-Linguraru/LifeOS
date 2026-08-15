using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Abstractions.Tasks;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Reminders;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Mappings;
using LifeOS.Core.Services;
using LifeOS.Core.Time;

namespace LifeOS.Infrastructure.Services;

public sealed class ReminderService : IReminderService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReminderRepository _repository;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IHabitRepository _habitRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReminderService(
        ICurrentUserService currentUser,
        IReminderRepository repository,
        IUserSettingsRepository userSettingsRepository,
        ITaskRepository taskRepository,
        IHabitRepository habitRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUser = currentUser;
        _repository = repository;
        _userSettingsRepository = userSettingsRepository;
        _taskRepository = taskRepository;
        _habitRepository = habitRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ReminderDetailsDto> CreateAsync(
        CreateReminderDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var userId = GetCurrentUserId();
        await RequireConfirmedTimeZoneAsync(userId, cancellationToken);

        var schedule = await ValidateScheduleAsync(
            dto.SourceType,
            dto.SourceId,
            dto.Title,
            dto.Message,
            dto.ScheduledLocalDate,
            dto.ScheduledLocalTime,
            dto.TimeZoneId,
            userId,
            cancellationToken);

        var reminderId = Guid.NewGuid();
        var reminder = new Reminder
        {
            Id = reminderId,
            UserId = userId,
            SourceType = dto.SourceType,
            SourceId = schedule.SourceId,
            SourceTitle = schedule.SourceTitle,
            Title = schedule.Title,
            Message = schedule.Message,
            ScheduledLocalDate = dto.ScheduledLocalDate,
            ScheduledLocalTime = dto.ScheduledLocalTime,
            TimeZoneId = schedule.TimeZoneId,
            ScheduledForUtc = schedule.ScheduledForUtc,
            Status = ReminderStatus.Pending,
            Version = 0,
            IdempotencyKey = $"ReminderFired:{reminderId:N}"
        };

        await _repository.AddAsync(reminder, cancellationToken);
        return reminder.ToDetailsDto();
    }

    public async Task<ReminderDetailsDto> GetDetailsAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(reminderId);
        var reminder = await _repository.GetByIdAsync(
            GetCurrentUserId(),
            reminderId,
            cancellationToken);

        if (reminder is null)
        {
            throw new ResourceNotFoundException("Reminder was not found.");
        }

        return reminder.ToDetailsDto();
    }

    public async Task<IReadOnlyList<ReminderSummaryDto>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var reminders = await _repository.GetPendingAsync(
            GetCurrentUserId(),
            ReminderConstants.DefaultListLimit,
            cancellationToken);

        return reminders
            .Select(reminder => reminder.ToSummaryDto())
            .ToList();
    }

    public async Task<ReminderMutationResultDto> UpdateAsync(
        Guid reminderId,
        UpdateReminderDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateId(reminderId);
        var userId = GetCurrentUserId();
        await RequireConfirmedTimeZoneAsync(userId, cancellationToken);

        var existing = await _repository.GetByIdAsync(
            userId,
            reminderId,
            cancellationToken);

        if (existing is null)
        {
            throw new ResourceNotFoundException("Reminder was not found.");
        }

        if (existing.Status != ReminderStatus.Pending)
        {
            return new ReminderMutationResultDto
            {
                Status = ReminderMutationStatus.Terminal,
                Reminder = existing.ToDetailsDto()
            };
        }

        var schedule = await ValidateScheduleAsync(
            dto.SourceType,
            dto.SourceId,
            dto.Title,
            dto.Message,
            dto.ScheduledLocalDate,
            dto.ScheduledLocalTime,
            dto.TimeZoneId,
            userId,
            cancellationToken);

        var update = new Reminder
        {
            Id = existing.Id,
            UserId = existing.UserId,
            SourceType = dto.SourceType,
            SourceId = schedule.SourceId,
            SourceTitle = schedule.SourceTitle,
            Title = schedule.Title,
            Message = schedule.Message,
            ScheduledLocalDate = dto.ScheduledLocalDate,
            ScheduledLocalTime = dto.ScheduledLocalTime,
            TimeZoneId = schedule.TimeZoneId,
            ScheduledForUtc = schedule.ScheduledForUtc,
            UpdatedAtUtc = _dateTimeProvider.UtcNow
        };

        await _repository.UpdatePendingAsync(
            userId,
            update,
            dto.ExpectedVersion,
            cancellationToken);

        return await ResolveUpdateOutcomeAsync(
            userId,
            reminderId,
            dto.ExpectedVersion,
            cancellationToken);
    }

    public async Task<ReminderMutationResultDto> CancelAsync(
        Guid reminderId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ValidateId(reminderId);
        var userId = GetCurrentUserId();
        await _repository.CancelPendingAsync(
            userId,
            reminderId,
            expectedVersion,
            cancellationToken);

        var authoritative = await _repository.GetByIdAsync(
            userId,
            reminderId,
            cancellationToken);

        if (authoritative is null)
        {
            throw new ResourceNotFoundException("Reminder was not found.");
        }

        if (authoritative.Status == ReminderStatus.Cancelled)
        {
            return new ReminderMutationResultDto
            {
                Status = expectedVersion == authoritative.Version - 1
                    ? ReminderMutationStatus.Cancelled
                    : ReminderMutationStatus.AlreadyCancelled,
                Reminder = authoritative.ToDetailsDto()
            };
        }

        if (authoritative.Status != ReminderStatus.Pending)
        {
            return new ReminderMutationResultDto
            {
                Status = ReminderMutationStatus.Terminal,
                Reminder = authoritative.ToDetailsDto()
            };
        }

        throw new ReminderConcurrencyException();
    }

    private async Task<ReminderMutationResultDto> ResolveUpdateOutcomeAsync(
        Guid userId,
        Guid reminderId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var authoritative = await _repository.GetByIdAsync(
            userId,
            reminderId,
            cancellationToken);

        if (authoritative is null)
        {
            throw new ResourceNotFoundException("Reminder was not found.");
        }

        if (authoritative.Status != ReminderStatus.Pending)
        {
            return new ReminderMutationResultDto
            {
                Status = ReminderMutationStatus.Terminal,
                Reminder = authoritative.ToDetailsDto()
            };
        }

        if (authoritative.Version != expectedVersion + 1)
        {
            throw new ReminderConcurrencyException();
        }

        return new ReminderMutationResultDto
        {
            Status = ReminderMutationStatus.Updated,
            Reminder = authoritative.ToDetailsDto()
        };
    }

    private async Task<ValidatedSchedule> ValidateScheduleAsync(
        ReminderSourceType sourceType,
        Guid? sourceId,
        string title,
        string? message,
        DateOnly localDate,
        TimeOnly localTime,
        string timeZoneId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length == 0 ||
            normalizedTitle.Length > ReminderConstants.TitleMaxLength)
        {
            throw new ValidationException("Reminder title is invalid.");
        }

        var normalizedMessage = string.IsNullOrWhiteSpace(message)
            ? null
            : message.Trim();
        if (normalizedMessage?.Length > ReminderConstants.MessageMaxLength)
        {
            throw new ValidationException("Reminder message is too long.");
        }

        var normalizedTimeZone = timeZoneId.Trim();
        if (normalizedTimeZone.Length == 0 ||
            normalizedTimeZone.Length > ReminderConstants.TimeZoneIdMaxLength ||
            !_dateTimeProvider.IsValidTimeZone(normalizedTimeZone))
        {
            throw new ValidationException("Reminder time zone is invalid.");
        }

        if (localTime.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ValidationException(
                "Reminder time must use minute precision.");
        }

        var conversion = _dateTimeProvider.ConvertLocalToUtc(
            localDate,
            localTime,
            normalizedTimeZone);
        if (!conversion.IsSuccess || conversion.UtcInstant is null)
        {
            throw new ValidationException(
                conversion.Failure switch
                {
                    LocalTimeConversionFailure.InvalidTimeZone => "Reminder time zone is invalid.",
                    LocalTimeConversionFailure.InvalidLocalTime => "Reminder local time does not exist.",
                    LocalTimeConversionFailure.AmbiguousLocalTime => "Reminder local time is ambiguous.",
                    _ => "Reminder local time could not be converted."
                });
        }

        var now = _dateTimeProvider.UtcNow;
        if (conversion.UtcInstant <= now)
        {
            throw new ValidationException(
                "Reminder schedule must be in the future.");
        }

        var source = await ResolveSourceAsync(
            sourceType,
            sourceId,
            userId,
            cancellationToken);

        return new ValidatedSchedule(
            normalizedTitle,
            normalizedMessage,
            normalizedTimeZone,
            conversion.UtcInstant.Value,
            source.SourceId,
            source.SourceTitle);
    }

    private async Task<(Guid? SourceId, string? SourceTitle)> ResolveSourceAsync(
        ReminderSourceType sourceType,
        Guid? sourceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        switch (sourceType)
        {
            case ReminderSourceType.Custom:
                if (sourceId is not null)
                {
                    throw new ValidationException(
                        "Custom reminders cannot have a source.");
                }

                return (null, null);

            case ReminderSourceType.Task:
                if (sourceId is null)
                {
                    throw new ValidationException("Task reminders require a task.");
                }

                var task = await _taskRepository.GetByIdAsync(
                    userId,
                    sourceId.Value,
                    cancellationToken);
                if (task is null || task.Status != TaskItemStatus.Active)
                {
                    throw new ResourceNotFoundException("Task was not found.");
                }

                return (task.Id, task.Title);

            case ReminderSourceType.Habit:
                if (sourceId is null)
                {
                    throw new ValidationException("Habit reminders require a habit.");
                }

                var habit = await _habitRepository.GetByIdAsync(
                    userId,
                    sourceId.Value,
                    cancellationToken);
                if (habit is null || !habit.IsActive)
                {
                    throw new ResourceNotFoundException("Habit was not found.");
                }

                return (habit.Id, habit.Name);

            default:
                throw new ValidationException("Reminder source type is invalid.");
        }
    }

    private async Task RequireConfirmedTimeZoneAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var settings = await _userSettingsRepository.GetByUserIdAsync(
            userId,
            cancellationToken);
        if (settings?.TimeZoneConfiguredAtUtc is null)
        {
            throw new ValidationException(
                "Configure and confirm a time zone before scheduling reminders.");
        }
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

    private static void ValidateId(Guid reminderId)
    {
        if (reminderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reminder ID cannot be empty.",
                nameof(reminderId));
        }
    }

    private sealed record ValidatedSchedule(
        string Title,
        string? Message,
        string TimeZoneId,
        DateTimeOffset ScheduledForUtc,
        Guid? SourceId,
        string? SourceTitle);
}
