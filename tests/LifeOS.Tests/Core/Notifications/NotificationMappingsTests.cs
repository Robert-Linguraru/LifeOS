using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Mappings;

namespace LifeOS.Tests.Core.Notifications;

public sealed class NotificationMappingsTests
{
    [Fact]
    public void ToDto_MapsUnreadNotification()
    {
        var notificationId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var createdAtUtc = new DateTimeOffset(
            2026,
            8,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);
        var notification = new Notification
        {
            Id = notificationId,
            Type = NotificationType.ReminderDue,
            Title = "Reminder due",
            Message = "Review the task",
            SourceType = NotificationSourceType.Reminder,
            SourceId = sourceId,
            CreatedAtUtc = createdAtUtc,
            IdempotencyKey = "ReminderDue:abc"
        };

        var result = notification.ToDto();

        Assert.Equal(notificationId, result.Id);
        Assert.Equal(NotificationType.ReminderDue, result.Type);
        Assert.Equal("Reminder due", result.Title);
        Assert.Equal("Review the task", result.Message);
        Assert.Equal(NotificationSourceType.Reminder, result.SourceType);
        Assert.Equal(sourceId, result.SourceId);
        Assert.Equal(createdAtUtc, result.CreatedAtUtc);
        Assert.Null(result.ReadAtUtc);
        Assert.Null(result.DismissedAtUtc);
        Assert.False(result.IsRead);
        Assert.False(result.IsDismissed);
    }

    [Fact]
    public void ToDto_MapsReadNotification()
    {
        var readAtUtc = new DateTimeOffset(
            2026,
            8,
            15,
            12,
            1,
            0,
            TimeSpan.Zero);
        var notification = new Notification
        {
            ReadAtUtc = readAtUtc
        };

        var result = notification.ToDto();

        Assert.Equal(readAtUtc, result.ReadAtUtc);
        Assert.True(result.IsRead);
        Assert.False(result.IsDismissed);
    }

    [Fact]
    public void ToDto_MapsDismissedNotificationAsReadAndDismissed()
    {
        var dismissedAtUtc = new DateTimeOffset(
            2026,
            8,
            15,
            12,
            2,
            0,
            TimeSpan.Zero);
        var notification = new Notification
        {
            DismissedAtUtc = dismissedAtUtc
        };

        var result = notification.ToDto();

        Assert.Equal(dismissedAtUtc, result.DismissedAtUtc);
        Assert.True(result.IsRead);
        Assert.True(result.IsDismissed);
    }
}
