using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;

namespace LifeOS.Tests.Core.Notifications;

public sealed class NotificationTests
{
    [Fact]
    public void NewNotification_HasUnreadUndismissedSafeDefaults()
    {
        var notification = new Notification();

        Assert.Equal(NotificationType.ReminderDue, notification.Type);
        Assert.Equal(string.Empty, notification.Title);
        Assert.Equal(string.Empty, notification.Message);
        Assert.Null(notification.SourceType);
        Assert.Null(notification.SourceId);
        Assert.Null(notification.ReadAtUtc);
        Assert.Null(notification.DismissedAtUtc);
        Assert.Equal(string.Empty, notification.IdempotencyKey);
    }

    [Fact]
    public void NotificationEnums_HaveFrozenValues()
    {
        Assert.Equal(0, (int)NotificationType.ReminderDue);
        Assert.Equal(1, (int)NotificationType.LevelUp);
        Assert.Equal(2, (int)NotificationType.EchelonChanged);
        Assert.Equal(0, (int)NotificationSourceType.Reminder);
        Assert.Equal(1, (int)NotificationSourceType.XpTransaction);
    }

    [Fact]
    public void NotificationConstants_HaveFrozenValues()
    {
        Assert.Equal(200, NotificationConstants.TitleMaxLength);
        Assert.Equal(2000, NotificationConstants.MessageMaxLength);
        Assert.Equal(200, NotificationConstants.IdempotencyKeyMaxLength);
        Assert.Equal(100, NotificationConstants.DefaultListLimit);
    }
}
