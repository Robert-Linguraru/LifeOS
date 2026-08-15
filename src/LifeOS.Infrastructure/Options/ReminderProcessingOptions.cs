using System.ComponentModel.DataAnnotations;

namespace LifeOS.Infrastructure.Options;

public sealed class ReminderProcessingOptions
{
    public const string SectionName = "ReminderProcessing";

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    [Range(0, int.MaxValue)]
    public int AutomaticRetryAttempts { get; set; } = 3;
}
