using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;

namespace LifeOS.Core.DTOs.Habits;

public sealed class HabitSummaryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public HabitFrequency Frequency { get; init; }

    public HabitTargetType TargetType { get; init; }

    public decimal? TargetQuantity { get; init; }

    public string? TargetUnit { get; init; }

    public bool IsActive { get; init; }

    public EstimatedTime EstimatedTime { get; init; }

    public FrictionLevel FrictionLevel { get; init; }

    public bool IsCompletedToday { get; init; }

    public int CurrentStreak { get; init; }
}
