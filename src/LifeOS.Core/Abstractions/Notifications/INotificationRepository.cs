using LifeOS.Core.Entities;
using LifeOS.Core.Constants;

namespace LifeOS.Core.Abstractions.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetLatestNonDismissedAsync(
        Guid userId,
        int limit = NotificationConstants.DefaultListLimit,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken = default);

    Task DismissAsync(
        Guid userId,
        Guid notificationId,
        DateTimeOffset dismissedAtUtc,
        CancellationToken cancellationToken = default);
}
