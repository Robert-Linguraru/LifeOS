namespace LifeOS.Core.Abstractions.Reminders;

public sealed class ReminderProcessingResult
{
    public int Attempted { get; init; }

    public int Fired { get; init; }

    public int Skipped { get; init; }

    public int Failed { get; init; }
}
