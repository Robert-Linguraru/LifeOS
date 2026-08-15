using LifeOS.Core.Enums.Notifications;

namespace LifeOS.Core.Entities;

public sealed class Notification : UserOwnedEntity
{
    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationSourceType? SourceType { get; set; }

    public Guid? SourceId { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }

    public DateTimeOffset? DismissedAtUtc { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
}
