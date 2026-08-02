namespace LifeOS.Core.Exceptions;

public sealed class ResourceNotFoundException
    : LifeOSException
{
    public ResourceNotFoundException(string message)
        : base(message)
    {
    }
}