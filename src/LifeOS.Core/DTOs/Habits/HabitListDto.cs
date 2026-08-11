namespace LifeOS.Core.DTOs.Habits;

public sealed class HabitListDto
{
    public DateOnly CurrentDate { get; init; }

    public IReadOnlyList<HabitSummaryDto> Active { get; init; } =
        Array.Empty<HabitSummaryDto>();

    public IReadOnlyList<HabitSummaryDto> Archived { get; init; } =
        Array.Empty<HabitSummaryDto>();
}
