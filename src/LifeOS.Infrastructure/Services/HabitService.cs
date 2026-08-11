using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
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
    private readonly ILogger<HabitService> _logger;

    public HabitService(
        IHabitRepository repository,
        ICurrentUserService currentUser,
        ILogger<HabitService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
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

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.UserId == Guid.Empty)
        {
            throw new CurrentUserUnavailableException();
        }

        return _currentUser.UserId;
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
