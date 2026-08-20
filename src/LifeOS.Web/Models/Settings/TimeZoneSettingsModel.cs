using System.ComponentModel.DataAnnotations;

namespace LifeOS.Web.Models.Settings;

public sealed class TimeZoneSettingsModel
{
    [Required(ErrorMessage = "Select a time zone.")]
    [StringLength(100, ErrorMessage = "Time zone IDs cannot exceed 100 characters.")]
    public string TimeZoneId { get; set; } = "UTC";

    [Required(ErrorMessage = "Select a currency.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency codes must contain three characters.")]
    public string Currency { get; set; } = "USD";
}
