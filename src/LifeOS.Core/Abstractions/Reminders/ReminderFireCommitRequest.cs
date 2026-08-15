using LifeOS.Core.Abstractions.Notifications;

namespace LifeOS.Core.Abstractions.Reminders;

public sealed class ReminderFireCommitRequest
{
    public Guid ReminderId { get; init; }

    public Guid UserId { get; init; }

    public long ExpectedVersion { get; init; }

    public DateTimeOffset DueCutoffUtc { get; init; }

    public DateTimeOffset FiredAtUtc { get; init; }

    public NotificationDraft Notification { get; init; } = new();
}
