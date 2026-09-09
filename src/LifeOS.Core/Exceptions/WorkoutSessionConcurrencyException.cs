namespace LifeOS.Core.Exceptions;

public sealed class WorkoutSessionConcurrencyException : LifeOSException
{
    public WorkoutSessionConcurrencyException()
        : base("The workout session was changed by another operation.")
    {
    }
}
