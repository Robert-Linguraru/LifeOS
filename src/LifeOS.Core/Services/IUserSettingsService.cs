using LifeOS.Core.DTOs;

namespace LifeOS.Core.Services;

public interface IUserSettingsService
{
    Task<UserSettingsDto> GetCurrentUserSettingsAsync(
        CancellationToken cancellationToken = default);

    Task UpdateTimeZoneAsync(
        string timeZoneId,
        CancellationToken cancellationToken = default);

    Task UpdateCurrencyAsync(
        string currency,
        CancellationToken cancellationToken = default);
}