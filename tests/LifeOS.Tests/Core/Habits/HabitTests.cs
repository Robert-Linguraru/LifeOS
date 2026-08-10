using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;

namespace LifeOS.Tests.Core.Habits;

public sealed class HabitTests
{
    [Fact]
    public void Habit_ShouldInheritFromUserOwnedEntity()
    {
        var habit = new Habit();

        Assert.IsAssignableFrom<UserOwnedEntity>(habit);
    }

    [Fact]
    public void Habit_ShouldUseDocumentedDefaults()
    {
        var habit = new Habit();

        Assert.NotEqual(Guid.Empty, habit.Id);
        Assert.Equal(HabitFrequency.Daily, habit.Frequency);
        Assert.Equal(HabitTargetType.Binary, habit.TargetType);
        Assert.True(habit.IsActive);
        Assert.Equal(
            EstimatedTime.Under15Minutes,
            habit.EstimatedTime);
        Assert.Equal(FrictionLevel.Low, habit.FrictionLevel);
        Assert.Null(habit.Description);
        Assert.Null(habit.TargetQuantity);
        Assert.Null(habit.TargetUnit);
    }

    [Fact]
    public void Habit_ShouldAllowApprovedPropertiesToBeAssigned()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = userId,
            Name = "Drink water",
            Description = "Drink water throughout the day.",
            Frequency = HabitFrequency.Daily,
            TargetType = HabitTargetType.Quantity,
            TargetQuantity = 2m,
            TargetUnit = "liters",
            IsActive = true,
            EstimatedTime = EstimatedTime.Between15And30Minutes,
            FrictionLevel = FrictionLevel.Medium
        };

        Assert.Equal(userId, habit.UserId);
        Assert.Equal("Drink water", habit.Name);
        Assert.Equal(
            "Drink water throughout the day.",
            habit.Description);
        Assert.Equal(2m, habit.TargetQuantity);
        Assert.Equal("liters", habit.TargetUnit);
        Assert.Equal(
            EstimatedTime.Between15And30Minutes,
            habit.EstimatedTime);
        Assert.Equal(FrictionLevel.Medium, habit.FrictionLevel);
    }

    [Fact]
    public void HabitConstants_ShouldUseApprovedLimits()
    {
        Assert.Equal(200, HabitConstants.NameMaxLength);
        Assert.Equal(2000, HabitConstants.DescriptionMaxLength);
        Assert.Equal(50, HabitConstants.TargetUnitMaxLength);
    }

    [Fact]
    public void HabitLog_ShouldStoreCompletionFields()
    {
        var habitId = Guid.NewGuid();
        var completionDate = new DateOnly(2026, 8, 10);
        var completedAtUtc = new DateTimeOffset(
            2026,
            8,
            10,
            21,
            30,
            0,
            TimeSpan.Zero);

        var habitLog = new HabitLog
        {
            HabitId = habitId,
            CompletionDate = completionDate,
            CompletedAtUtc = completedAtUtc
        };

        Assert.NotEqual(Guid.Empty, habitLog.Id);
        Assert.Equal(habitId, habitLog.HabitId);
        Assert.Equal(completionDate, habitLog.CompletionDate);
        Assert.Equal(completedAtUtc, habitLog.CompletedAtUtc);
        Assert.IsAssignableFrom<UserOwnedEntity>(habitLog);
    }
}
