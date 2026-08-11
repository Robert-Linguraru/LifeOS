using LifeOS.Core.DTOs.Habits;

namespace LifeOS.Core.DTOs.Dashboard;

public sealed class DashboardHabitWidgetDto
{
    public DateOnly CurrentDate { get; init; }

    public IReadOnlyList<HabitSummaryDto> ActiveHabits { get; init; } =
        Array.Empty<HabitSummaryDto>();

    public int CompletedCount { get; init; }

    public int TotalActiveCount { get; init; }
}
