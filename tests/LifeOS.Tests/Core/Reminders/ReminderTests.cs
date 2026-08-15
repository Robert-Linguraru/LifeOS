using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Reminders;

namespace LifeOS.Tests.Core.Reminders;

public sealed class ReminderTests
{
    [Fact]
    public void NewReminder_HasPendingCustomDefaults()
    {
        var reminder = new Reminder();

        Assert.Equal(ReminderSourceType.Custom, reminder.SourceType);
        Assert.Equal(ReminderStatus.Pending, reminder.Status);
        Assert.Equal(string.Empty, reminder.Title);
        Assert.Equal("UTC", reminder.TimeZoneId);
        Assert.Equal(string.Empty, reminder.IdempotencyKey);
        Assert.Equal(0, reminder.Version);
        Assert.Null(reminder.SourceId);
        Assert.Null(reminder.SourceTitle);
        Assert.Null(reminder.Message);
        Assert.Null(reminder.FiredAtUtc);
        Assert.Null(reminder.NotificationId);
    }

    [Fact]
    public void ReminderEnums_HaveFrozenValues()
    {
        Assert.Equal(0, (int)ReminderStatus.Pending);
        Assert.Equal(1, (int)ReminderStatus.Fired);
        Assert.Equal(2, (int)ReminderStatus.Cancelled);
        Assert.Equal(0, (int)ReminderSourceType.Task);
        Assert.Equal(1, (int)ReminderSourceType.Habit);
        Assert.Equal(2, (int)ReminderSourceType.Custom);
    }

    [Fact]
    public void ReminderConstants_HaveFrozenValues()
    {
        Assert.Equal(200, ReminderConstants.TitleMaxLength);
        Assert.Equal(2000, ReminderConstants.MessageMaxLength);
        Assert.Equal(200, ReminderConstants.SourceTitleMaxLength);
        Assert.Equal(100, ReminderConstants.TimeZoneIdMaxLength);
        Assert.Equal(200, ReminderConstants.IdempotencyKeyMaxLength);
        Assert.Equal(100, ReminderConstants.DefaultListLimit);
        Assert.Equal(3, ReminderConstants.DashboardListLimit);
    }
}
