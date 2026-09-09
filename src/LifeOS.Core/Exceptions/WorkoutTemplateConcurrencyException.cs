namespace LifeOS.Core.Exceptions;

public sealed class WorkoutTemplateConcurrencyException : LifeOSException
{
    public WorkoutTemplateConcurrencyException()
        : base("The workout template was changed by another operation.")
    {
    }
}
