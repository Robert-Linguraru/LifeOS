namespace LifeOS.Core.Abstractions.Reminders;

public sealed class ReminderFireCommitResult
{
    public ReminderFireCommitStatus Status { get; init; }

    public Guid? NotificationId { get; init; }

    public bool IsSuccess => Status == ReminderFireCommitStatus.Fired;
}
