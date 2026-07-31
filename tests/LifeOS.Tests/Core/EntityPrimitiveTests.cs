using LifeOS.Core.Entities;

namespace LifeOS.Tests.Core.Entities;

public sealed class EntityPrimitiveTests
{
    [Fact]
    public void BaseEntity_ShouldGenerateNonEmptyId()
    {
        // Arrange and act
        var entity = new TestEntity();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void BaseEntity_ShouldStartAsNotDeleted()
    {
        // Arrange and act
        var entity = new TestEntity();

        // Assert
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAtUtc);
    }

    [Fact]
    public void UserOwnedEntity_ShouldInheritFromBaseEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var entity = new TestUserOwnedEntity
        {
            UserId = userId
        };

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
        Assert.Equal(userId, entity.UserId);
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    private sealed class TestEntity : BaseEntity
    {
    }

    private sealed class TestUserOwnedEntity : UserOwnedEntity
    {
    }
}