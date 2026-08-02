namespace LifeOS.Core.Exceptions;

public sealed class ValidationException
    : LifeOSException
{
    public ValidationException(string message)
        : base(message)
    {
    }
}