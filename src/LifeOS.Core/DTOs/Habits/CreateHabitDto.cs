using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Habits;

namespace LifeOS.Core.DTOs.Habits;

public sealed class CreateHabitDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

    public HabitTargetType TargetType { get; set; } = HabitTargetType.Binary;

    public decimal? TargetQuantity { get; set; }

    public string? TargetUnit { get; set; }

    public EstimatedTime EstimatedTime { get; set; } =
        EstimatedTime.Under15Minutes;

    public FrictionLevel FrictionLevel { get; set; } = FrictionLevel.Low;
}
