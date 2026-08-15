using LifeOS.Core.Enums.Notifications;

namespace LifeOS.Core.Abstractions.Notifications;

public sealed class NotificationDraft
{
    public Guid NotificationId { get; init; }

    public Guid UserId { get; init; }

    public NotificationType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public NotificationSourceType? SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;
}
