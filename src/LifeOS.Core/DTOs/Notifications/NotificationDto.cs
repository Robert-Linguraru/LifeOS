using LifeOS.Core.Enums.Notifications;

namespace LifeOS.Core.DTOs.Notifications;

public sealed class NotificationDto
{
    public Guid Id { get; init; }

    public NotificationType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public NotificationSourceType? SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ReadAtUtc { get; init; }

    public DateTimeOffset? DismissedAtUtc { get; init; }

    public bool IsRead => ReadAtUtc is not null || IsDismissed;

    public bool IsDismissed => DismissedAtUtc is not null;
}
