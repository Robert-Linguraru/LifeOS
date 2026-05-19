using LifeOS.Core.Enums;

namespace LifeOS.Core.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public NotificationSourceType SourceType { get; set; }
    public Guid? SourceEntityId { get; set; }

    public DateTime? SnoozedUntil { get; set; }
    public string? HangfireJobId { get; set; } // needed for snooze/dismiss

    public ApplicationUser? User { get; set; }
}