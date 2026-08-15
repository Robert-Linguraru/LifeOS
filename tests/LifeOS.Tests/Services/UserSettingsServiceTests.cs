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
    private static readonly DateTimeOffset SavedAtUtc =
        new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

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
        Assert.Null(result.TimeZoneConfiguredAtUtc);

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
                    s.TimeZoneId == "UTC" &&
                    s.TimeZoneConfiguredAtUtc == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserSettingsAsync_UnauthenticatedUser_ThrowsAndDoesNotAccessRepository()
    {
        var service = CreateService();

        _currentUser
            .Setup(x => x.IsAuthenticated)
            .Returns(false);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.GetCurrentUserSettingsAsync());

        VerifyRepositoryNotCalled();
    }

    [Fact]
    public async Task GetCurrentUserSettingsAsync_EmptyUserId_ThrowsAndDoesNotAccessRepository()
    {
        var service = CreateService();

        _currentUser
            .Setup(x => x.UserId)
            .Returns(Guid.Empty);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.GetCurrentUserSettingsAsync());

        VerifyRepositoryNotCalled();
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
        _dateTimeProvider
            .Setup(x => x.UtcNow)
            .Returns(SavedAtUtc);

        _repository
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        await service.UpdateTimeZoneAsync("Europe/Bucharest");

        Assert.Equal("Europe/Bucharest", settings.TimeZoneId);
        Assert.Equal(SavedAtUtc, settings.TimeZoneConfiguredAtUtc);

        _repository.Verify(
            x => x.UpdateAsync(settings, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_UnauthenticatedUser_ThrowsAndDoesNotAccessRepository()
    {
        var service = CreateService();

        _currentUser
            .Setup(x => x.IsAuthenticated)
            .Returns(false);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.UpdateTimeZoneAsync("Europe/Bucharest"));

        VerifyRepositoryNotCalled();
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_EmptyUserId_ThrowsAndDoesNotAccessRepository()
    {
        var service = CreateService();

        _currentUser
            .Setup(x => x.UserId)
            .Returns(Guid.Empty);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.UpdateTimeZoneAsync("Europe/Bucharest"));

        VerifyRepositoryNotCalled();
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
    public async Task UpdateTimeZoneAsync_ExplicitUtc_SetsConfirmationTimestamp()
    {
        var settings = new UserSettings
        {
            UserId = UserId,
            TimeZoneId = "UTC"
        };

        _dateTimeProvider
            .Setup(x => x.IsValidTimeZone("UTC"))
            .Returns(true);
        _dateTimeProvider
            .Setup(x => x.UtcNow)
            .Returns(SavedAtUtc);
        _repository
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        await CreateService().UpdateTimeZoneAsync(" UTC ");

        Assert.Equal("UTC", settings.TimeZoneId);
        Assert.Equal(SavedAtUtc, settings.TimeZoneConfiguredAtUtc);
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_RepeatedSave_UpdatesConfirmationTimestamp()
    {
        var settings = new UserSettings
        {
            UserId = UserId,
            TimeZoneId = "UTC",
            TimeZoneConfiguredAtUtc = SavedAtUtc.AddMinutes(-1)
        };

        _dateTimeProvider
            .Setup(x => x.IsValidTimeZone("UTC"))
            .Returns(true);
        _dateTimeProvider
            .Setup(x => x.UtcNow)
            .Returns(SavedAtUtc);
        _repository
            .Setup(x => x.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        await CreateService().UpdateTimeZoneAsync("UTC");

        Assert.Equal(SavedAtUtc, settings.TimeZoneConfiguredAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Invalid")]
    public async Task UpdateTimeZoneAsync_InvalidValue_DoesNotPersist(
        string timeZoneId)
    {
        _dateTimeProvider
            .Setup(x => x.IsValidTimeZone(It.IsAny<string>()))
            .Returns(false);

        await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().UpdateTimeZoneAsync(timeZoneId));

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

    private void VerifyRepositoryNotCalled()
    {
        _repository.Verify(
            x => x.GetByUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<UserSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            x => x.UpdateAsync(
                It.IsAny<UserSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}