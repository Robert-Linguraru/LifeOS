using LifeOS.Core.Abstractions;
using LifeOS.Core.Entities;
using LifeOS.Core.Exceptions;
using LifeOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LifeOS.Tests.Services;

public sealed class UserSettingsServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IUserSettingsRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<ILogger<UserSettingsService>> _logger = new();

    private UserSettingsService CreateService()
    {
        _currentUser.Setup(x => x.UserId).Returns(UserId);
        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);

        return new UserSettingsService(
            _repository.Object,
            _currentUser.Object,
            _dateTimeProvider.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetCurrentUserSettingsAsync_ReturnsExistingSettings()
    {
        var settings = new UserSettings
        {
            UserId = UserId,
            TimeZoneId = "Europe/Bucharest"
        };

        _repository
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        var result = await service.GetCurrentUserSettingsAsync();

        Assert.Equal(UserId, result.UserId);
        Assert.Equal("Europe/Bucharest", result.TimeZoneId);

        _repository.Verify(
            x => x.AddAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserSettingsAsync_CreatesDefaultSettings_WhenMissing()
    {
        _repository
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSettings?)null);

        var service = CreateService();

        var result = await service.GetCurrentUserSettingsAsync();

        Assert.Equal(UserId, result.UserId);
        Assert.Equal("UTC", result.TimeZoneId);

        _repository.Verify(
            x => x.AddAsync(
                It.Is<UserSettings>(s =>
                    s.UserId == UserId &&
                    s.TimeZoneId == "UTC"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_UpdatesTimeZone()
    {
        var settings = new UserSettings
        {
            UserId = UserId,
            TimeZoneId = "UTC"
        };

        _dateTimeProvider
            .Setup(x => x.IsValidTimeZone("Europe/Bucharest"))
            .Returns(true);

        _repository
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        await service.UpdateTimeZoneAsync("Europe/Bucharest");

        Assert.Equal("Europe/Bucharest", settings.TimeZoneId);

        _repository.Verify(
            x => x.UpdateAsync(settings, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_InvalidTimeZone_ThrowsValidationException()
    {
        _dateTimeProvider
            .Setup(x => x.IsValidTimeZone("Invalid"))
            .Returns(false);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateTimeZoneAsync("Invalid"));

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<UserSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_SettingsMissing_ThrowsResourceNotFoundException()
    {
        _dateTimeProvider
            .Setup(x => x.IsValidTimeZone("Europe/Bucharest"))
            .Returns(true);

        _repository
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSettings?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.UpdateTimeZoneAsync("Europe/Bucharest"));
    }
}