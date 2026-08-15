using LifeOS.Core.Enums.Reminders;

namespace LifeOS.Core.Entities;

public sealed class Reminder : UserOwnedEntity
{
    public ReminderSourceType SourceType { get; set; } = ReminderSourceType.Custom;

    public Guid? SourceId { get; set; }

    public string? SourceTitle { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateOnly ScheduledLocalDate { get; set; }

    public TimeOnly ScheduledLocalTime { get; set; }

    public string TimeZoneId { get; set; } = "UTC";

    public DateTimeOffset ScheduledForUtc { get; set; }

    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

    public DateTimeOffset? FiredAtUtc { get; set; }

    public Guid? NotificationId { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public long Version { get; set; }
}
