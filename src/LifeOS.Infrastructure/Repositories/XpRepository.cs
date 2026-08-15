using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Xp;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LifeOS.Infrastructure.Repositories;

public sealed class XpRepository : IXpRepository
{
    private const string ProgressionUserIdIndex =
        "IX_UserProgressions_UserId";

    private const string TransactionIdempotencyIndex =
        "IX_XpTransactions_UserId_IdempotencyKey";

    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public XpRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<UserProgression?> GetProgressionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        return await context.UserProgressions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                progression => progression.UserId == userId,
                cancellationToken);
    }

    public async Task<UserProgression> GetOrCreateProgressionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        var existing = await context.UserProgressions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                progression => progression.UserId == userId,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var progression = new UserProgression
        {
            UserId = userId
        };

        context.UserProgressions.Add(progression);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return progression;
        }
        catch (DbUpdateException exception)
            when (IsExpectedUniqueViolation(
                exception,
                ProgressionUserIdIndex))
        {
            var authoritative = await context.UserProgressions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.UserId == userId,
                    cancellationToken);

            if (authoritative is not null)
            {
                return authoritative;
            }

            throw;
        }
    }

    public async Task<XpTransaction?> FindByIdempotencyKeyAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        return await context.XpTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transaction => transaction.UserId == userId &&
                    transaction.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<int> GetQuestXpSumAsync(
        Guid userId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        return await context.XpTransactions
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.BusinessDate == businessDate &&
                transaction.Source == XpSource.QuestCompletion)
            .SumAsync(transaction => (int?)transaction.XpAmount, cancellationToken)
            ?? 0;
    }

    public async Task<IReadOnlyList<XpTransaction>> GetHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        return await context.XpTransactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId)
            .OrderByDescending(transaction => transaction.OccurredAtUtc)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<XpAwardCommitResult> CommitAwardAsync(
        XpAwardCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCommitRequest(request);

        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);
        await using var aggregateTransaction = await context.Database
            .BeginTransactionAsync(cancellationToken);

        var existingTransaction = await context.XpTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UserId == request.UserId &&
                    item.IdempotencyKey == request.IdempotencyKey,
                cancellationToken);

        if (existingTransaction is not null)
        {
            return new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.Duplicate,
                Transaction = existingTransaction,
                Progression = await context.UserProgressions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.UserId == request.UserId,
                        cancellationToken)
            };
        }

        var progression = await context.UserProgressions
            .SingleOrDefaultAsync(
                item => item.UserId == request.UserId,
                cancellationToken);

        if (progression is null)
        {
            return new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.ConcurrencyConflict
            };
        }

        if (progression.Version != request.ExpectedVersion)
        {
            return new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.ConcurrencyConflict,
                Progression = progression
            };
        }

        progression.TotalLifetimeXp = request.ResultingTotalLifetimeXp;
        progression.CurrentLevel = request.ResultingCurrentLevel;
        progression.CurrentEchelon = request.ResultingCurrentEchelon;
        progression.DailyQuestXpToday = request.ResultingDailyQuestXpToday;
        progression.DailyQuestXpDate = request.ResultingDailyQuestXpDate;
        progression.Version = request.ResultingVersion;

        var transaction = new XpTransaction
        {
            Id = request.XpTransactionId == Guid.Empty
                ? Guid.NewGuid()
                : request.XpTransactionId,
            UserId = request.UserId,
            Source = request.Source,
            SourceType = request.SourceType,
            SourceEntityId = request.SourceEntityId,
            XpAmount = request.XpAmount,
            OccurredAtUtc = request.OccurredAtUtc,
            BusinessDate = request.BusinessDate,
            IdempotencyKey = request.IdempotencyKey,
            Notes = request.Notes
        };

        context.XpTransactions.Add(transaction);
        foreach (var draft in request.Notifications)
        {
            context.Notifications.Add(new Notification
            {
                Id = draft.NotificationId,
                UserId = request.UserId,
                Type = draft.Type,
                Title = draft.Title,
                Message = draft.Message,
                SourceType = draft.SourceType,
                SourceId = draft.SourceId,
                IdempotencyKey = draft.IdempotencyKey
            });
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await aggregateTransaction.CommitAsync(cancellationToken);

            return new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.Committed,
                Transaction = transaction,
                Progression = progression
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            await aggregateTransaction.RollbackAsync(CancellationToken.None);
            return new XpAwardCommitResult
            {
                Status = XpAwardCommitStatus.ConcurrencyConflict,
                Progression = await GetProgressionAsync(
                    request.UserId,
                    cancellationToken)
            };
        }
        catch (DbUpdateException exception)
            when (IsExpectedUniqueViolation(
                exception,
                TransactionIdempotencyIndex))
        {
            await aggregateTransaction.RollbackAsync(CancellationToken.None);
            var duplicate = await FindByIdempotencyKeyAsync(
                request.UserId,
                request.IdempotencyKey,
                cancellationToken);

            if (duplicate is not null)
            {
                return new XpAwardCommitResult
                {
                    Status = XpAwardCommitStatus.Duplicate,
                    Transaction = duplicate,
                    Progression = await GetProgressionAsync(
                        request.UserId,
                        cancellationToken)
                };
            }

            throw;
        }
        catch
        {
            await aggregateTransaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void ValidateCommitRequest(
        XpAwardCommitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);

        if (request.XpAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "An atomic XP award must contain positive XP.");
        }

        if (request.ExpectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Expected progression Version cannot be negative.");
        }

        if (request.ExpectedVersion == long.MaxValue ||
            request.ResultingVersion != request.ExpectedVersion + 1)
        {
            throw new ArgumentException(
                "Resulting progression Version must be exactly one greater than ExpectedVersion.",
                nameof(request));
        }
    }

    private static bool IsExpectedUniqueViolation(
        DbUpdateException exception,
        string constraintName)
    {
        return exception.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
            string.Equals(
                postgresException.ConstraintName,
                constraintName,
                StringComparison.Ordinal);
    }
}
