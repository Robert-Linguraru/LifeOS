namespace LifeOS.Core.Abstractions.Reminders;

public sealed record ReminderDueCandidate(
    Guid ReminderId,
    Guid UserId,
    long Version,
    DateTimeOffset ScheduledForUtc);
