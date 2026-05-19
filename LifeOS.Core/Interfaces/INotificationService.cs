using LifeOS.Core.Entities;
using LifeOS.Core.Enums;

namespace LifeOS.Core.Interfaces;

public interface INotificationService
{
    Task<string> ScheduleReminderAsync(
        string userId,
        string title,
        string body,
        DateTime fireAt,
        NotificationSourceType sourceType,
        Guid sourceEntityId);

    Task CancelReminderAsync(string hangfireJobId);

    Task DeliverNotificationAsync(
        string userId,
        string title,
        string body,
        NotificationSourceType sourceType,
        Guid? sourceEntityId);

    Task<List<Notification>> GetUnreadAsync(string userId);
    Task MarkReadAsync(Guid notificationId);
    Task SnoozeAsync(Guid notificationId, int minutes);
}