using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions;

public interface IXpRepository
{
    Task<UserProgression?> GetProgressionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserProgression> GetOrCreateProgressionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<XpTransaction?> FindByIdempotencyKeyAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<int> GetQuestXpSumAsync(
        Guid userId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<XpTransaction>> GetHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<XpAwardCommitResult> CommitAwardAsync(
        XpAwardCommitRequest request,
        CancellationToken cancellationToken = default);
}
