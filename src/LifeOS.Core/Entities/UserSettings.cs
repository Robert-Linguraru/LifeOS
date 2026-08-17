using LifeOS.Core.Constants;

namespace LifeOS.Core.Entities;

public sealed class UserSettings : UserOwnedEntity
{
    public string TimeZoneId { get; set; } = "UTC";

    public DateTimeOffset? TimeZoneConfiguredAtUtc { get; set; }

    public string Currency { get; set; } = FinanceConstants.DefaultCurrency;
}