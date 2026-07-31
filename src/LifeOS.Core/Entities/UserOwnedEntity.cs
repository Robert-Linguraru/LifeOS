namespace LifeOS.Core.Entities;

public abstract class UserOwnedEntity : BaseEntity
{
    public Guid UserId { get; set; }
}