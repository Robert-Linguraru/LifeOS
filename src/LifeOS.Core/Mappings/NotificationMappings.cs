using LifeOS.Core.DTOs.Notifications;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Mappings;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            SourceType = notification.SourceType,
            SourceId = notification.SourceId,
            CreatedAtUtc = notification.CreatedAtUtc,
            ReadAtUtc = notification.ReadAtUtc,
            DismissedAtUtc = notification.DismissedAtUtc
        };
    }
}
