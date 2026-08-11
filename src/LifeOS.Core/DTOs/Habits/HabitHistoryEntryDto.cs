namespace LifeOS.Core.DTOs.Habits;

public sealed class HabitHistoryEntryDto
{
    public DateOnly CompletionDate { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; }
}
