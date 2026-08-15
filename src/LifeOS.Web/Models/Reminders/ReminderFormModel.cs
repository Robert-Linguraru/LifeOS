using System.ComponentModel.DataAnnotations;
using LifeOS.Core.Enums.Reminders;

namespace LifeOS.Web.Models.Reminders;

public sealed class ReminderFormModel
{
    public ReminderSourceType SourceType { get; set; } = ReminderSourceType.Custom;

    public Guid? SourceId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title must be 200 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Message must be 2000 characters or fewer.")]
    public string? Message { get; set; }

    public DateOnly ScheduledLocalDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    public TimeOnly ScheduledLocalTime { get; set; } = new(9, 0);

    [Required(ErrorMessage = "Time zone is required.")]
    [StringLength(100, ErrorMessage = "Time zone must be 100 characters or fewer.")]
    public string TimeZoneId { get; set; } = string.Empty;

    public long ExpectedVersion { get; set; }
}
