namespace LifeOS.Core.Exceptions;

public abstract class LifeOSException : Exception
{
    protected LifeOSException(string message)
        : base(message)
    {
    }
}