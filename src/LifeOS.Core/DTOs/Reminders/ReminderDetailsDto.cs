using LifeOS.Core.Enums.Reminders;

namespace LifeOS.Core.DTOs.Reminders;

public sealed class ReminderDetailsDto
{
    public Guid Id { get; init; }

    public ReminderSourceType SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public string? SourceTitle { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Message { get; init; }

    public DateOnly ScheduledLocalDate { get; init; }

    public TimeOnly ScheduledLocalTime { get; init; }

    public string TimeZoneId { get; init; } = string.Empty;

    public DateTimeOffset ScheduledForUtc { get; init; }

    public ReminderStatus Status { get; init; }

    public DateTimeOffset? FiredAtUtc { get; init; }

    public Guid? NotificationId { get; init; }

    public long Version { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}
