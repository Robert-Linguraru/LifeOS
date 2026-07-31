using LifeOS.Web.Options;
using LifeOS.Web.Services;
using Microsoft.Extensions.Options;

namespace LifeOS.Tests.Web;

public sealed class DevelopmentCurrentUserServiceTests
{
    [Fact]
    public void ShouldReturnConfiguredUserId()
    {
        // Arrange
        var expected = Guid.NewGuid();

        var options = Options.Create(
            new DevelopmentUserOptions
            {
                UserId = expected
            });

        // Act
        var service = new DevelopmentCurrentUserService(options);

        // Assert
        Assert.Equal(expected, service.UserId);
    }

    [Fact]
    public void ShouldThrow_WhenGuidIsEmpty()
    {
        // Arrange
        var options = Options.Create(
            new DevelopmentUserOptions());

        // Act
        Action action =
            () => new DevelopmentCurrentUserService(options);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }
}