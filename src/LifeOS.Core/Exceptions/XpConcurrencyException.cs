namespace LifeOS.Core.Exceptions;

public sealed class XpConcurrencyException : LifeOSException
{
    public XpConcurrencyException()
        : base("The XP award could not be completed after the bounded concurrency retries.")
    {
    }
}
