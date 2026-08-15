using LifeOS.Core.Constants;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions.Reminders;

public interface IReminderRepository
{
    Task AddAsync(
        Reminder reminder,
        CancellationToken cancellationToken = default);

    Task<Reminder?> GetByIdAsync(
        Guid userId,
        Guid reminderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reminder>> GetPendingAsync(
        Guid userId,
        int limit = ReminderConstants.DefaultListLimit,
        CancellationToken cancellationToken = default);

    Task UpdatePendingAsync(
        Guid userId,
        Reminder reminder,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task CancelPendingAsync(
        Guid userId,
        Guid reminderId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReminderDueCandidate>> GetDueCandidatesAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<ReminderFireCommitResult> CommitFireAsync(
        ReminderFireCommitRequest request,
        CancellationToken cancellationToken = default);
}
