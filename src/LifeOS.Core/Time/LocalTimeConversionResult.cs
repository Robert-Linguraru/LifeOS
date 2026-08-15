namespace LifeOS.Core.Time;

public sealed record LocalTimeConversionResult
{
    private LocalTimeConversionResult(
        DateTimeOffset? utcInstant,
        LocalTimeConversionFailure? failure)
    {
        UtcInstant = utcInstant;
        Failure = failure;
    }

    public DateTimeOffset? UtcInstant { get; }

    public LocalTimeConversionFailure? Failure { get; }

    public bool IsSuccess => Failure is null;

    public static LocalTimeConversionResult Success(DateTimeOffset utcInstant)
    {
        return new LocalTimeConversionResult(
            utcInstant.ToUniversalTime(),
            null);
    }

    public static LocalTimeConversionResult Failed(
        LocalTimeConversionFailure failure)
    {
        return new LocalTimeConversionResult(null, failure);
    }
}
