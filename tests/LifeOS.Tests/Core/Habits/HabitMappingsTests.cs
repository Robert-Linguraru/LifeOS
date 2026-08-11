using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;
using LifeOS.Core.Mappings;

namespace LifeOS.Tests.Core.Habits;

public sealed class HabitMappingsTests
{
    [Fact]
    public void ToDetailsDto_ShouldMapDefinitionAndAuditFields()
    {
        var habit = new Habit
        {
            Name = "Read",
            Description = "Read before bed.",
            Frequency = HabitFrequency.Daily,
            TargetType = HabitTargetType.Binary,
            IsActive = true,
            EstimatedTime = EstimatedTime.Between15And30Minutes,
            FrictionLevel = FrictionLevel.Medium,
            CreatedAtUtc = new DateTimeOffset(
                2026,
                8,
                1,
                8,
                0,
                0,
                TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(
                2026,
                8,
                2,
                8,
                0,
                0,
                TimeSpan.Zero)
        };

        var result = habit.ToDetailsDto();

        Assert.Equal(habit.Id, result.Id);
        Assert.Equal(habit.Name, result.Name);
        Assert.Equal(habit.Description, result.Description);
        Assert.Equal(habit.Frequency, result.Frequency);
        Assert.Equal(habit.TargetType, result.TargetType);
        Assert.Equal(habit.TargetQuantity, result.TargetQuantity);
        Assert.Equal(habit.TargetUnit, result.TargetUnit);
        Assert.Equal(habit.IsActive, result.IsActive);
        Assert.Equal(habit.EstimatedTime, result.EstimatedTime);
        Assert.Equal(habit.FrictionLevel, result.FrictionLevel);
        Assert.Equal(habit.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(habit.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [Fact]
    public void ToSummaryDto_ShouldUseSuppliedDailyState()
    {
        var habit = new Habit
        {
            Name = "Exercise",
            TargetType = HabitTargetType.Quantity,
            TargetQuantity = 30m,
            TargetUnit = "minutes"
        };

        var result = habit.ToSummaryDto(
            isCompletedToday: true,
            currentStreak: 7);

        Assert.Equal(habit.Id, result.Id);
        Assert.Equal(habit.Name, result.Name);
        Assert.Equal(habit.TargetType, result.TargetType);
        Assert.Equal(habit.TargetQuantity, result.TargetQuantity);
        Assert.Equal(habit.TargetUnit, result.TargetUnit);
        Assert.True(result.IsCompletedToday);
        Assert.Equal(7, result.CurrentStreak);
    }

    [Fact]
    public void ToHistoryEntryDto_ShouldMapCompletionFields()
    {
        var habitLog = new HabitLog
        {
            CompletionDate = new DateOnly(2026, 8, 10),
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                10,
                20,
                30,
                0,
                TimeSpan.Zero)
        };

        var result = habitLog.ToHistoryEntryDto();

        Assert.Equal(habitLog.CompletionDate, result.CompletionDate);
        Assert.Equal(habitLog.CompletedAtUtc, result.CompletedAtUtc);
    }

    [Fact]
    public void ToHistoryDto_ShouldMapHabitIdentityAndEntriesWithoutSorting()
    {
        var habit = new Habit
        {
            Name = "Journal",
            IsActive = false
        };

        var firstLog = new HabitLog
        {
            CompletionDate = new DateOnly(2026, 8, 8),
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                8,
                20,
                0,
                0,
                TimeSpan.Zero)
        };

        var secondLog = new HabitLog
        {
            CompletionDate = new DateOnly(2026, 8, 9),
            CompletedAtUtc = new DateTimeOffset(
                2026,
                8,
                9,
                20,
                0,
                0,
                TimeSpan.Zero)
        };

        var result = habit.ToHistoryDto([firstLog, secondLog]);

        Assert.Equal(habit.Id, result.HabitId);
        Assert.Equal(habit.Name, result.Name);
        Assert.False(result.IsActive);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(
            firstLog.CompletionDate,
            result.Entries[0].CompletionDate);
        Assert.Equal(
            secondLog.CompletionDate,
            result.Entries[1].CompletionDate);
    }
}
