using LifeOS.Core.Abstractions.Reminders;

namespace LifeOS.Infrastructure.Exceptions;

public sealed class ReminderProcessingBatchException
    : Exception
{
    public ReminderProcessingBatchException(ReminderProcessingResult result)
        : base($"Reminder processing failed for {result.Failed} item(s).")
    {
        Result = result;
    }

    public ReminderProcessingResult Result { get; }
}
