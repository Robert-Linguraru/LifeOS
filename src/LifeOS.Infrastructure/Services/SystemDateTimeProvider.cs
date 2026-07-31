using LifeOS.Core.Abstractions;

namespace LifeOS.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    private readonly TimeProvider _timeProvider;

    public SystemDateTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public bool IsValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    public DateOnly GetCurrentDate(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new ArgumentException(
                "A time-zone identifier is required.",
                nameof(timeZoneId));
        }

        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException(
                $"The time-zone identifier '{timeZoneId}' was not found.",
                nameof(timeZoneId),
                exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException(
                $"The time-zone identifier '{timeZoneId}' is invalid.",
                nameof(timeZoneId),
                exception);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var localDateTime = TimeZoneInfo.ConvertTime(utcNow, timeZone);

        return DateOnly.FromDateTime(localDateTime.DateTime);
    }
}