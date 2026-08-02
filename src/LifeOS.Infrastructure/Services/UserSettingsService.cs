using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.Entities;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services;

public sealed class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UserSettingsService> _logger;

    public UserSettingsService(
        IUserSettingsRepository repository,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<UserSettingsService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<UserSettingsDto> GetCurrentUserSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetByUserIdAsync(
            _currentUser.UserId,
            cancellationToken);

        if (settings is null)
        {
            settings = new UserSettings
            {
                UserId = _currentUser.UserId,
                TimeZoneId = "UTC"
            };

            await _repository.AddAsync(
                settings,
                cancellationToken);

            _logger.LogInformation(
                "Created default user settings for user {UserId}",
                settings.UserId);
        }

        return new UserSettingsDto
        {
            UserId = settings.UserId,
            TimeZoneId = settings.TimeZoneId
        };
    }

    public async Task UpdateTimeZoneAsync(
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        if (!_dateTimeProvider.IsValidTimeZone(timeZoneId))
        {
            throw new ValidationException(
               "The supplied time zone is invalid.");
        }

        var settings = await _repository.GetByUserIdAsync(
            _currentUser.UserId,
            cancellationToken);

        if (settings is null)
        {
            throw new ResourceNotFoundException(
                "User settings were not found.");
        }

        settings.TimeZoneId = timeZoneId;

        await _repository.UpdateAsync(
            settings,
            cancellationToken);

        _logger.LogInformation(
            "Updated time zone for user {UserId} to {TimeZoneId}",
            settings.UserId,
            settings.TimeZoneId);
    }

}