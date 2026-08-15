using LifeOS.Core.Time;

namespace LifeOS.Core.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    bool IsValidTimeZone(string timeZoneId);

    DateOnly GetCurrentDate(string timeZoneId);

    LocalTimeConversionResult ConvertLocalToUtc(
        DateOnly localDate,
        TimeOnly localTime,
        string timeZoneId);

    DateTimeOffset ConvertUtcToLocal(
        DateTimeOffset utcInstant,
        string timeZoneId);
}