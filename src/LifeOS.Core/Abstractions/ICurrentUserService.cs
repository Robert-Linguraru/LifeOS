namespace LifeOS.Core.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }
}