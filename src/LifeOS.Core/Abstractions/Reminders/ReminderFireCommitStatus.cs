namespace LifeOS.Core.Abstractions.Reminders;

public enum ReminderFireCommitStatus
{
    Fired = 0,
    AlreadyFired = 1,
    Cancelled = 2,
    NotDue = 3,
    Missing = 4,
    ConcurrencyLost = 5
}
