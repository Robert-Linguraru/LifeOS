using Hangfire;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace LifeOS.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(60)]
public sealed class DueReminderJob
{
    private readonly IReminderProcessingService _processingService;
    private readonly IOptions<ReminderProcessingOptions> _options;

    public DueReminderJob(
        IReminderProcessingService processingService,
        IOptions<ReminderProcessingOptions> options)
    {
        _processingService = processingService;
        _options = options;
    }

    public async Task ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        await _processingService.ProcessDueRemindersAsync(
            _options.Value.BatchSize,
            cancellationToken);
    }
}
