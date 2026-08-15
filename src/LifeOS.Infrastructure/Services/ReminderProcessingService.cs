using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Constants;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services;

public sealed class ReminderProcessingService : IReminderProcessingService
{
    private readonly IReminderRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ReminderProcessingService> _logger;

    public ReminderProcessingService(
        IReminderRepository repository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ReminderProcessingService> logger)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ReminderProcessingResult> ProcessDueRemindersAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be positive.");
        }

        var boundedBatchSize = Math.Min(
            batchSize,
            ReminderConstants.DefaultListLimit);
        var cutoffUtc = _dateTimeProvider.UtcNow;
        var candidates = await _repository.GetDueCandidatesAsync(
            cutoffUtc,
            boundedBatchSize,
            cancellationToken);

        var attempted = 0;
        var fired = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var candidate in candidates)
        {
            attempted++;

            try
            {
                var result = await _repository.CommitFireAsync(
                    new ReminderFireCommitRequest
                    {
                        ReminderId = candidate.ReminderId,
                        UserId = candidate.UserId,
                        ExpectedVersion = candidate.Version,
                        DueCutoffUtc = cutoffUtc,
                        FiredAtUtc = cutoffUtc,
                        Notification = new NotificationDraft
                        {
                            NotificationId = candidate.ReminderId,
                            UserId = candidate.UserId,
                            Type = NotificationType.ReminderDue,
                            SourceType = NotificationSourceType.Reminder,
                            SourceId = candidate.ReminderId,
                            IdempotencyKey = $"ReminderFired:{candidate.ReminderId:N}"
                        }
                    },
                    cancellationToken);

                if (result.Status == ReminderFireCommitStatus.Fired)
                {
                    fired++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                _logger.LogError(
                    exception,
                    "Reminder processing failed for reminder {ReminderId} and user {UserId}",
                    candidate.ReminderId,
                    candidate.UserId);
            }
        }

        var processingResult = new ReminderProcessingResult
        {
            Attempted = attempted,
            Fired = fired,
            Skipped = skipped,
            Failed = failed
        };

        if (failed > 0)
        {
            throw new ReminderProcessingBatchException(processingResult);
        }

        return processingResult;
    }
}
