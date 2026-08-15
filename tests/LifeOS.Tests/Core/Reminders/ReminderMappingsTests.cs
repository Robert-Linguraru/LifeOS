using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Core.Mappings;

namespace LifeOS.Tests.Core.Reminders;

public sealed class ReminderMappingsTests
{
    [Fact]
    public void ToSummaryDto_PreservesSchedulingIntentAndSourceSnapshot()
    {
        var reminder = CreateReminder();

        var result = reminder.ToSummaryDto();

        Assert.Equal(reminder.Id, result.Id);
        Assert.Equal(reminder.SourceType, result.SourceType);
        Assert.Equal(reminder.SourceId, result.SourceId);
        Assert.Equal(reminder.SourceTitle, result.SourceTitle);
        Assert.Equal(reminder.Title, result.Title);
        Assert.Equal(reminder.ScheduledLocalDate, result.ScheduledLocalDate);
        Assert.Equal(reminder.ScheduledLocalTime, result.ScheduledLocalTime);
        Assert.Equal(reminder.TimeZoneId, result.TimeZoneId);
        Assert.Equal(reminder.ScheduledForUtc, result.ScheduledForUtc);
        Assert.Equal(reminder.Status, result.Status);
        Assert.Equal(reminder.Version, result.Version);
    }

    [Fact]
    public void ToDetailsDto_PreservesLifecycleAndAuditFields()
    {
        var reminder = CreateReminder();
        reminder.Status = ReminderStatus.Fired;
        reminder.FiredAtUtc = new DateTimeOffset(
            2026,
            8,
            15,
            12,
            1,
            0,
            TimeSpan.Zero);
        reminder.NotificationId = Guid.NewGuid();
        reminder.CreatedAtUtc = new DateTimeOffset(
            2026,
            8,
            15,
            11,
            0,
            0,
            TimeSpan.Zero);
        reminder.UpdatedAtUtc = new DateTimeOffset(
            2026,
            8,
            15,
            12,
            1,
            0,
            TimeSpan.Zero);

        var result = reminder.ToDetailsDto();

        Assert.Equal(reminder.Message, result.Message);
        Assert.Equal(reminder.Status, result.Status);
        Assert.Equal(reminder.FiredAtUtc, result.FiredAtUtc);
        Assert.Equal(reminder.NotificationId, result.NotificationId);
        Assert.Equal(reminder.Version, result.Version);
        Assert.Equal(reminder.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(reminder.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    private static Reminder CreateReminder()
    {
        return new Reminder
        {
            SourceType = ReminderSourceType.Task,
            SourceId = Guid.NewGuid(),
            SourceTitle = "Task title",
            Title = "Reminder title",
            Message = "Reminder message",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = "Europe/Bucharest",
            ScheduledForUtc = new DateTimeOffset(
                2026,
                8,
                20,
                11,
                30,
                0,
                TimeSpan.Zero),
            Version = 2
        };
    }
}
