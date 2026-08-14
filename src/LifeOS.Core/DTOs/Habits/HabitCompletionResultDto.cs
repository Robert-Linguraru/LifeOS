using LifeOS.Core.DTOs.Xp;

namespace LifeOS.Core.DTOs.Habits;

public sealed class HabitCompletionResultDto
{
    public HabitSummaryDto Habit { get; init; } = new();

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public DateOnly? CompletionDate { get; init; }

    public bool WasNewlyCompleted { get; init; }

    public XpAwardResultDto? XpAward { get; init; }

    public bool XpAwardFailed { get; init; }
}
