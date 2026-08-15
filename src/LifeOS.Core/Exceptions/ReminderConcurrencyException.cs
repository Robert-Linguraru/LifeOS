namespace LifeOS.Core.Exceptions;

public sealed class ReminderConcurrencyException
    : LifeOSException
{
    public ReminderConcurrencyException()
        : base("The reminder was changed by another operation.")
    {
    }
}
