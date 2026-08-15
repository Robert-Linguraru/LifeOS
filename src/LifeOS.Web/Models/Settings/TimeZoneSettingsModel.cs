using System.ComponentModel.DataAnnotations;

namespace LifeOS.Web.Models.Settings;

public sealed class TimeZoneSettingsModel
{
    [Required(ErrorMessage = "Select a time zone.")]
    [StringLength(100, ErrorMessage = "Time zone IDs cannot exceed 100 characters.")]
    public string TimeZoneId { get; set; } = "UTC";
}
