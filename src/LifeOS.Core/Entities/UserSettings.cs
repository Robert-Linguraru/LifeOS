namespace LifeOS.Core.Entities;

public sealed class UserSettings : UserOwnedEntity
{
    public string TimeZoneId { get; set; } = "UTC";

    public DateTimeOffset? TimeZoneConfiguredAtUtc { get; set; }
}