namespace LifeOS.Core.DTOs;

public sealed class UserSettingsDto
{
    public Guid UserId { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public DateTimeOffset? TimeZoneConfiguredAtUtc { get; set; }
}