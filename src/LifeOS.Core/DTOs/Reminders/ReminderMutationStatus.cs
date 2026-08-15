namespace LifeOS.Core.DTOs.Reminders;

public enum ReminderMutationStatus
{
    Updated = 0,
    Cancelled = 1,
    AlreadyCancelled = 2,
    Terminal = 3,
    StaleVersion = 4
}
