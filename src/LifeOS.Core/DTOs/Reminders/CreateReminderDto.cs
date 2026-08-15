using LifeOS.Core.Enums.Reminders;

namespace LifeOS.Core.DTOs.Reminders;

public sealed class CreateReminderDto
{
    public ReminderSourceType SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Message { get; init; }

    public DateOnly ScheduledLocalDate { get; init; }

    public TimeOnly ScheduledLocalTime { get; init; }

    public string TimeZoneId { get; init; } = string.Empty;
}
