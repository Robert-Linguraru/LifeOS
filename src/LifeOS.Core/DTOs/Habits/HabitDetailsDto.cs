using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;

namespace LifeOS.Core.DTOs.Habits;

public sealed class HabitDetailsDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public HabitFrequency Frequency { get; init; }

    public HabitTargetType TargetType { get; init; }

    public decimal? TargetQuantity { get; init; }

    public string? TargetUnit { get; init; }

    public bool IsActive { get; init; }

    public EstimatedTime EstimatedTime { get; init; }

    public FrictionLevel FrictionLevel { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}
