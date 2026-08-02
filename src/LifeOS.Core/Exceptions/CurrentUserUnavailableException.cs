namespace LifeOS.Core.Exceptions;

public sealed class CurrentUserUnavailableException
    : LifeOSException
{
    public CurrentUserUnavailableException()
        : base("The current user is unavailable.")
    {
    }
}