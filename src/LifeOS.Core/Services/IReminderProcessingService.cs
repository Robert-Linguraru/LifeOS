using LifeOS.Core.Abstractions.Reminders;

namespace LifeOS.Core.Services;

public interface IReminderProcessingService
{
    Task<ReminderProcessingResult> ProcessDueRemindersAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}
