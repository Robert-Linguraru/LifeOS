using LifeOS.Core.DTOs.Reminders;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Mappings;

public static class ReminderMappings
{
    public static ReminderSummaryDto ToSummaryDto(this Reminder reminder)
    {
        return new ReminderSummaryDto
        {
            Id = reminder.Id,
            SourceType = reminder.SourceType,
            SourceId = reminder.SourceId,
            SourceTitle = reminder.SourceTitle,
            Title = reminder.Title,
            ScheduledLocalDate = reminder.ScheduledLocalDate,
            ScheduledLocalTime = reminder.ScheduledLocalTime,
            TimeZoneId = reminder.TimeZoneId,
            ScheduledForUtc = reminder.ScheduledForUtc,
            Status = reminder.Status,
            Version = reminder.Version
        };
    }

    public static ReminderDetailsDto ToDetailsDto(this Reminder reminder)
    {
        return new ReminderDetailsDto
        {
            Id = reminder.Id,
            SourceType = reminder.SourceType,
            SourceId = reminder.SourceId,
            SourceTitle = reminder.SourceTitle,
            Title = reminder.Title,
            Message = reminder.Message,
            ScheduledLocalDate = reminder.ScheduledLocalDate,
            ScheduledLocalTime = reminder.ScheduledLocalTime,
            TimeZoneId = reminder.TimeZoneId,
            ScheduledForUtc = reminder.ScheduledForUtc,
            Status = reminder.Status,
            FiredAtUtc = reminder.FiredAtUtc,
            NotificationId = reminder.NotificationId,
            Version = reminder.Version,
            CreatedAtUtc = reminder.CreatedAtUtc,
            UpdatedAtUtc = reminder.UpdatedAtUtc
        };
    }
}
