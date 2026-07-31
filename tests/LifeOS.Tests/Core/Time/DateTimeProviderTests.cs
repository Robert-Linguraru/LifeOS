using LifeOS.Infrastructure.Services;

namespace LifeOS.Tests.Core.Time;

public sealed class DateTimeProviderTests
{
    private readonly SystemDateTimeProvider _provider =
        new(TimeProvider.System);

    [Fact]
    public void UtcNow_ShouldReturnUtcOffset()
    {
        // Act
        var result = _provider.UtcNow;

        // Assert
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void IsValidTimeZone_ShouldReturnTrue_ForUtc()
    {
        // Act
        var result = _provider.IsValidTimeZone("UTC");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidTimeZone_ShouldReturnTrue_ForEuropeBucharest()
    {
        // Act
        var result = _provider.IsValidTimeZone("Europe/Bucharest");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidTimeZone_ShouldReturnFalse_ForUnknownTimeZone()
    {
        // Act
        var result = _provider.IsValidTimeZone(
            "Invalid/TimeZone");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void IsValidTimeZone_ShouldReturnFalse_ForBlankValue(
        string timeZoneId)
    {
        // Act
        var result = _provider.IsValidTimeZone(timeZoneId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetCurrentDate_ShouldReturnADate_ForUtc()
    {
        // Act
        var result = _provider.GetCurrentDate("UTC");

        // Assert
        Assert.NotEqual(default, result);
    }

    [Fact]
    public void GetCurrentDate_ShouldThrow_ForUnknownTimeZone()
    {
        // Act
        Action action = () =>
            _provider.GetCurrentDate("Invalid/TimeZone");

        // Assert
        Assert.Throws<ArgumentException>(action);
    }
}