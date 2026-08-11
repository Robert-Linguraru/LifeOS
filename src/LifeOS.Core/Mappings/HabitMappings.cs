using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Mappings;

public static class HabitMappings
{
    public static HabitDetailsDto ToDetailsDto(this Habit habit)
    {
        return new HabitDetailsDto
        {
            Id = habit.Id,
            Name = habit.Name,
            Description = habit.Description,
            Frequency = habit.Frequency,
            TargetType = habit.TargetType,
            TargetQuantity = habit.TargetQuantity,
            TargetUnit = habit.TargetUnit,
            IsActive = habit.IsActive,
            EstimatedTime = habit.EstimatedTime,
            FrictionLevel = habit.FrictionLevel,
            CreatedAtUtc = habit.CreatedAtUtc,
            UpdatedAtUtc = habit.UpdatedAtUtc
        };
    }

    public static HabitSummaryDto ToSummaryDto(
        this Habit habit,
        bool isCompletedToday,
        int currentStreak)
    {
        return new HabitSummaryDto
        {
            Id = habit.Id,
            Name = habit.Name,
            Frequency = habit.Frequency,
            TargetType = habit.TargetType,
            TargetQuantity = habit.TargetQuantity,
            TargetUnit = habit.TargetUnit,
            IsActive = habit.IsActive,
            EstimatedTime = habit.EstimatedTime,
            FrictionLevel = habit.FrictionLevel,
            IsCompletedToday = isCompletedToday,
            CurrentStreak = currentStreak
        };
    }

    public static HabitHistoryEntryDto ToHistoryEntryDto(
        this HabitLog habitLog)
    {
        return new HabitHistoryEntryDto
        {
            CompletionDate = habitLog.CompletionDate,
            CompletedAtUtc = habitLog.CompletedAtUtc
        };
    }

    public static HabitHistoryDto ToHistoryDto(
        this Habit habit,
        IEnumerable<HabitLog> habitLogs)
    {
        return new HabitHistoryDto
        {
            HabitId = habit.Id,
            Name = habit.Name,
            IsActive = habit.IsActive,
            Entries = habitLogs
                .Select(log => log.ToHistoryEntryDto())
                .ToList()
        };
    }
}
