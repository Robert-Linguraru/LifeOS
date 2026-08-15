namespace LifeOS.Core.DTOs.Reminders;

public sealed class ReminderMutationResultDto
{
    public ReminderMutationStatus Status { get; init; }

    public ReminderDetailsDto? Reminder { get; init; }

    public bool IsSuccess =>
        Status is ReminderMutationStatus.Updated or
            ReminderMutationStatus.Cancelled or
            ReminderMutationStatus.AlreadyCancelled;
}
