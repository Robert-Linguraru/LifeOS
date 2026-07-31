namespace LifeOS.Core.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    bool IsValidTimeZone(string timeZoneId);

    DateOnly GetCurrentDate(string timeZoneId);
}