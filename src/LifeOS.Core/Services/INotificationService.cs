using LifeOS.Core.DTOs.Notifications;

namespace LifeOS.Core.Services;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task DismissAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
