namespace LifeOS.Core.DTOs.Habits;

public sealed class HabitHistoryDto
{
    public Guid HabitId { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public IReadOnlyList<HabitHistoryEntryDto> Entries { get; init; } =
        Array.Empty<HabitHistoryEntryDto>();
}
