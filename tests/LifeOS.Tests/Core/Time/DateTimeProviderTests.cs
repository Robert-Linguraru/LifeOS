using LifeOS.Core.Time;
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

    [Fact]
    public void ConvertLocalToUtc_WinterTime_UsesStandardOffset()
    {
        var result = _provider.ConvertLocalToUtc(
            new DateOnly(2026, 1, 15),
            new TimeOnly(12, 0),
            "Europe/Bucharest");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
            result.UtcInstant);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void ConvertLocalToUtc_SummerTime_UsesDaylightOffset()
    {
        var result = _provider.ConvertLocalToUtc(
            new DateOnly(2026, 7, 15),
            new TimeOnly(12, 0),
            "Europe/Bucharest");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero),
            result.UtcInstant);
    }

    [Fact]
    public void ConvertLocalToUtc_SpringForwardGap_ReturnsInvalidLocalTime()
    {
        var result = _provider.ConvertLocalToUtc(
            new DateOnly(2026, 3, 29),
            new TimeOnly(3, 30),
            "Europe/Bucharest");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            LocalTimeConversionFailure.InvalidLocalTime,
            result.Failure);
        Assert.Null(result.UtcInstant);
    }

    [Fact]
    public void ConvertLocalToUtc_FallBackOverlap_ReturnsAmbiguousLocalTime()
    {
        var result = _provider.ConvertLocalToUtc(
            new DateOnly(2026, 10, 25),
            new TimeOnly(3, 30),
            "Europe/Bucharest");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            LocalTimeConversionFailure.AmbiguousLocalTime,
            result.Failure);
        Assert.Null(result.UtcInstant);
    }

    [Fact]
    public void ConvertLocalToUtc_Utc_LeavesWallTimeUnchanged()
    {
        var result = _provider.ConvertLocalToUtc(
            new DateOnly(2026, 6, 1),
            new TimeOnly(12, 34, 56),
            "UTC");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 1, 12, 34, 56, TimeSpan.Zero),
            result.UtcInstant);
    }

    [Fact]
    public void ConvertLocalToUtc_UnknownTimeZone_ReturnsInvalidTimeZone()
    {
        var result = _provider.ConvertLocalToUtc(
            new DateOnly(2026, 6, 1),
            new TimeOnly(12, 0),
            "Invalid/TimeZone");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            LocalTimeConversionFailure.InvalidTimeZone,
            result.Failure);
        Assert.Null(result.UtcInstant);
    }

    [Fact]
    public void ConvertLocalToUtc_BlankTimeZone_ReturnsInvalidTimeZone()
    {
        var result = _provider.ConvertLocalToUtc(
            new DateOnly(2026, 6, 1),
            new TimeOnly(12, 0),
            " ");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            LocalTimeConversionFailure.InvalidTimeZone,
            result.Failure);
        Assert.Null(result.UtcInstant);
    }

    [Fact]
    public void ConvertUtcToLocal_UsesExplicitTimeZone()
    {
        var local = _provider.ConvertUtcToLocal(
            new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero),
            "Europe/Bucharest");

        Assert.Equal(
            new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.FromHours(3)),
            local);
    }
}