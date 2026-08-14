using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.DTOs.Xp;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
using LifeOS.Core.Enums.Xp;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Mappings;
using LifeOS.Core.Services;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services;

public sealed class HabitService : IHabitService
{
    private const decimal MaximumTargetQuantity =
        9999999999999999.99m;

    private readonly IHabitRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IXpService _xpService;
    private readonly ILogger<HabitService> _logger;

    public HabitService(
        IHabitRepository repository,
        ICurrentUserService currentUser,
        IUserSettingsService userSettingsService,
        IDateTimeProvider dateTimeProvider,
        IXpService xpService,
        ILogger<HabitService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _userSettingsService = userSettingsService;
        _dateTimeProvider = dateTimeProvider;
        _xpService = xpService;
        _logger = logger;
    }

    public async Task<HabitDetailsDto> CreateHabitAsync(
        CreateHabitDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = GetCurrentUserId();
        var normalized = ValidateAndNormalize(
            dto.Name,
            dto.Description,
            dto.Frequency,
            dto.TargetType,
            dto.TargetQuantity,
            dto.TargetUnit,
            dto.EstimatedTime,
            dto.FrictionLevel);

        var habit = new Habit
        {
            UserId = userId,
            Name = normalized.Name,
            Description = normalized.Description,
            Frequency = normalized.Frequency,
            TargetType = normalized.TargetType,
            TargetQuantity = normalized.TargetQuantity,
            TargetUnit = normalized.TargetUnit,
            IsActive = true,
            EstimatedTime = normalized.EstimatedTime,
            FrictionLevel = normalized.FrictionLevel
        };

        await _repository.AddAsync(habit, cancellationToken);

        _logger.LogInformation(
            "Created habit {HabitId} for user {UserId}",
            habit.Id,
            userId);

        return habit.ToDetailsDto();
    }

    public async Task<HabitDetailsDto> UpdateHabitAsync(
        Guid habitId,
        UpdateHabitDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = GetCurrentUserId();
        var normalized = ValidateAndNormalize(
            dto.Name,
            dto.Description,
            dto.Frequency,
            dto.TargetType,
            dto.TargetQuantity,
            dto.TargetUnit,
            dto.EstimatedTime,
            dto.FrictionLevel);

        var habit = await _repository.GetByIdAsync(
            userId,
            habitId,
            cancellationToken);

        if (habit is null)
        {
            throw new ResourceNotFoundException(
                "Habit was not found.");
        }

        if (!habit.IsActive)
        {
            throw new ValidationException(
                "Only active habits can be edited.");
        }

        habit.Name = normalized.Name;
        habit.Description = normalized.Description;
        habit.Frequency = normalized.Frequency;
        habit.TargetType = normalized.TargetType;
        habit.TargetQuantity = normalized.TargetQuantity;
        habit.TargetUnit = normalized.TargetUnit;
        habit.EstimatedTime = normalized.EstimatedTime;
        habit.FrictionLevel = normalized.FrictionLevel;

        await _repository.UpdateAsync(habit, cancellationToken);

        _logger.LogInformation(
            "Updated habit {HabitId} for user {UserId}",
            habit.Id,
            userId);

        return habit.ToDetailsDto();
    }

    public async Task<HabitDetailsDto> GetHabitByIdAsync(
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var habit = await _repository.GetByIdAsync(
            userId,
            habitId,
            cancellationToken);

        if (habit is null)
        {
            throw new ResourceNotFoundException(
                "Habit was not found.");
        }

        return habit.ToDetailsDto();
    }

    public async Task<HabitDetailsDto> ArchiveHabitAsync(
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var habit = await _repository.GetByIdAsync(
            userId,
            habitId,
            cancellationToken);

        if (habit is null)
        {
            throw new ResourceNotFoundException(
                "Habit was not found.");
        }

        if (!habit.IsActive)
        {
            return habit.ToDetailsDto();
        }

        habit.IsActive = false;

        await _repository.UpdateAsync(habit, cancellationToken);

        _logger.LogInformation(
            "Archived habit {HabitId} for user {UserId}",
            habit.Id,
            userId);

        return habit.ToDetailsDto();
    }

    public async Task<HabitListDto> GetHabitListAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var (currentDate, _) = await GetCurrentUserDateAsync(
            cancellationToken);

        var habits = await _repository.GetAllByUserIdAsync(
            userId,
            cancellationToken);

        var completionDates = habits.Count == 0
            ? Array.Empty<(Guid HabitId, DateOnly CompletionDate)>()
            : await _repository.GetCompletionDatesByUserIdAsync(
                userId,
                cancellationToken);

        var completionDatesByHabit = completionDates
            .GroupBy(item => item.HabitId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.CompletionDate));

        var active = habits
            .Where(habit => habit.IsActive)
            .Select(habit =>
            {
                completionDatesByHabit.TryGetValue(
                    habit.Id,
                    out var dates);

                var habitDates = dates ?? Enumerable.Empty<DateOnly>();

                return habit.ToSummaryDto(
                    habitDates.Contains(currentDate),
                    CalculateCurrentStreak(habitDates, currentDate));
            })
            .OrderBy(summary => summary.IsCompletedToday ? 1 : 0)
            .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var archived = habits
            .Where(habit => !habit.IsActive)
            .OrderByDescending(habit => habit.UpdatedAtUtc)
            .ThenBy(habit => habit.Name, StringComparer.OrdinalIgnoreCase)
            .Select(habit => habit.ToSummaryDto(false, 0))
            .ToList();

        return new HabitListDto
        {
            CurrentDate = currentDate,
            Active = active,
            Archived = archived
        };
    }

    public async Task<HabitCompletionResultDto> CompleteHabitAsync(
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var habit = await _repository.GetByIdAsync(
            userId,
            habitId,
            cancellationToken);

        if (habit is null)
        {
            throw new ResourceNotFoundException(
                "Habit was not found.");
        }

        if (!habit.IsActive)
        {
            throw new ValidationException(
                "Archived habits cannot be completed.");
        }

        var (currentDate, utcNow) = await GetCurrentUserDateAsync(
            cancellationToken);

        var existingLog = await _repository.GetLogByDateAsync(
            userId,
            habitId,
            currentDate,
            cancellationToken);

        if (existingLog is not null)
        {
            var existingDates = await _repository.GetCompletionDatesAsync(
                userId,
                habitId,
                cancellationToken);

            return CreateCompletionResult(
                habit.ToSummaryDto(
                    true,
                    CalculateCurrentStreak(existingDates, currentDate)),
                false,
                completionLog: existingLog);
        }

        var habitLog = new HabitLog
        {
            UserId = userId,
            HabitId = habitId,
            CompletionDate = currentDate,
            CompletedAtUtc = utcNow
        };

        var writeResult = await _repository.TryAddLogAsync(
            habitLog,
            cancellationToken);

        var authoritativeLog = writeResult.Log;
        if (authoritativeLog is null)
        {
            throw new ResourceNotFoundException(
                "The completed Habit state could not be retrieved.");
        }

        var completionDates = await _repository.GetCompletionDatesAsync(
            userId,
            habitId,
            cancellationToken);

        _logger.LogInformation(
            "Completed habit {HabitId} for user {UserId}",
            habitId,
            userId);

        var summary = habit.ToSummaryDto(
            true,
            CalculateCurrentStreak(completionDates, authoritativeLog.CompletionDate));

        if (!writeResult.WasInserted)
        {
            return CreateCompletionResult(
                summary,
                false,
                completionLog: authoritativeLog);
        }

        XpAwardResultDto? xpAward = null;
        var xpAwardFailed = false;

        try
        {
            xpAward = await _xpService.AwardQuestXpAsync(
                new AwardQuestXpDto
                {
                    SourceType = XpSourceType.Habit,
                    SourceEntityId = habit.Id,
                    OccurredAtUtc = authoritativeLog.CompletedAtUtc,
                    BusinessDate = authoritativeLog.CompletionDate,
                    EstimatedTime = habit.EstimatedTime,
                    FrictionLevel = habit.FrictionLevel
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            xpAwardFailed = true;
            _logger.LogError(
                exception,
                "XP award failed after completing habit {HabitId} with log {HabitLogId} for user {UserId}",
                habit.Id,
                authoritativeLog.Id,
                userId);
        }

        return CreateCompletionResult(summary, true, xpAward, xpAwardFailed, authoritativeLog);
    }

    private static HabitCompletionResultDto CreateCompletionResult(
        HabitSummaryDto habit,
        bool wasNewlyCompleted,
        XpAwardResultDto? xpAward = null,
        bool xpAwardFailed = false,
        HabitLog? completionLog = null)
    {
        return new HabitCompletionResultDto
        {
            Habit = habit,
            CompletedAtUtc = completionLog?.CompletedAtUtc,
            CompletionDate = completionLog?.CompletionDate,
            WasNewlyCompleted = wasNewlyCompleted,
            XpAward = xpAward,
            XpAwardFailed = xpAwardFailed
        };
    }

    public async Task<HabitHistoryDto> GetHabitHistoryAsync(
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var habit = await _repository.GetByIdAsync(
            userId,
            habitId,
            cancellationToken);

        if (habit is null)
        {
            throw new ResourceNotFoundException(
                "Habit was not found.");
        }

        var logs = await _repository.GetLogsByHabitIdAsync(
            userId,
            habitId,
            cancellationToken);

        var orderedLogs = logs
            .OrderByDescending(log => log.CompletionDate)
            .ThenByDescending(log => log.CompletedAtUtc)
            .ToList();

        return habit.ToHistoryDto(orderedLogs);
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

    private async Task<(DateOnly CurrentDate, DateTimeOffset UtcNow)>
        GetCurrentUserDateAsync(
            CancellationToken cancellationToken)
    {
        var settings = await _userSettingsService
            .GetCurrentUserSettingsAsync(cancellationToken);

        var utcNow = _dateTimeProvider.UtcNow;
        var currentDate = _dateTimeProvider.GetCurrentDate(
            settings.TimeZoneId);

        return (currentDate, utcNow);
    }

    private static int CalculateCurrentStreak(
        IEnumerable<DateOnly> completionDates,
        DateOnly currentDate)
    {
        var dates = completionDates
            .Where(date => date <= currentDate)
            .ToHashSet();

        DateOnly anchorDate;

        if (dates.Contains(currentDate))
        {
            anchorDate = currentDate;
        }
        else
        {
            var yesterday = currentDate.AddDays(-1);

            if (!dates.Contains(yesterday))
            {
                return 0;
            }

            anchorDate = yesterday;
        }

        var streak = 0;

        while (dates.Contains(anchorDate))
        {
            streak++;
            anchorDate = anchorDate.AddDays(-1);
        }

        return streak;
    }

    private static NormalizedHabit ValidateAndNormalize(
        string name,
        string? description,
        HabitFrequency frequency,
        HabitTargetType targetType,
        decimal? targetQuantity,
        string? targetUnit,
        EstimatedTime estimatedTime,
        FrictionLevel frictionLevel)
    {
        var normalizedName = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ValidationException(
                "Habit name is required.");
        }

        if (normalizedName.Length > HabitConstants.NameMaxLength)
        {
            throw new ValidationException(
                $"Habit name cannot exceed " +
                $"{HabitConstants.NameMaxLength} characters.");
        }

        var normalizedDescription =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        if (normalizedDescription?.Length >
            HabitConstants.DescriptionMaxLength)
        {
            throw new ValidationException(
                $"Habit description cannot exceed " +
                $"{HabitConstants.DescriptionMaxLength} characters.");
        }

        if (!Enum.IsDefined(frequency) ||
            frequency != HabitFrequency.Daily)
        {
            throw new ValidationException(
                "Only daily habits are supported.");
        }

        if (!Enum.IsDefined(targetType))
        {
            throw new ValidationException(
                "Habit target type is invalid.");
        }

        var normalizedQuantity = targetQuantity;
        var normalizedUnit =
            string.IsNullOrWhiteSpace(targetUnit)
                ? null
                : targetUnit.Trim();

        if (targetType == HabitTargetType.Binary)
        {
            normalizedQuantity = null;
            normalizedUnit = null;
        }
        else
        {
            if (!normalizedQuantity.HasValue ||
                normalizedQuantity.Value <= 0)
            {
                throw new ValidationException(
                    "A positive target quantity is required for quantity habits.");
            }

            if (normalizedQuantity.Value > MaximumTargetQuantity ||
                normalizedQuantity.Value !=
                decimal.Round(normalizedQuantity.Value, 2))
            {
                throw new ValidationException(
                    "Target quantity must fit the supported two-decimal precision.");
            }

            if (string.IsNullOrWhiteSpace(normalizedUnit))
            {
                throw new ValidationException(
                    "A target unit is required for quantity habits.");
            }

            if (normalizedUnit.Length > HabitConstants.TargetUnitMaxLength)
            {
                throw new ValidationException(
                    $"Target unit cannot exceed " +
                    $"{HabitConstants.TargetUnitMaxLength} characters.");
            }
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

        return new NormalizedHabit(
            normalizedName,
            normalizedDescription,
            frequency,
            targetType,
            normalizedQuantity,
            normalizedUnit,
            estimatedTime,
            frictionLevel);
    }

    private sealed record NormalizedHabit(
        string Name,
        string? Description,
        HabitFrequency Frequency,
        HabitTargetType TargetType,
        decimal? TargetQuantity,
        string? TargetUnit,
        EstimatedTime EstimatedTime,
        FrictionLevel FrictionLevel);
}
