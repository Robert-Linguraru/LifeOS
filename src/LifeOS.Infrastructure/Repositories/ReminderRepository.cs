using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LifeOS.Infrastructure.Repositories;

public sealed class ReminderRepository : IReminderRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ReminderRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(
        Reminder reminder,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Reminder?> GetByIdAsync(
        Guid userId,
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Reminders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                reminder =>
                    reminder.Id == reminderId &&
                    reminder.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetPendingAsync(
        Guid userId,
        int limit = ReminderConstants.DefaultListLimit,
        CancellationToken cancellationToken = default)
    {
        var boundedLimit = Math.Clamp(
            limit,
            1,
            ReminderConstants.DefaultListLimit);

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Reminders
            .AsNoTracking()
            .Where(reminder =>
                reminder.UserId == userId &&
                reminder.Status == ReminderStatus.Pending)
            .OrderBy(reminder => reminder.ScheduledForUtc)
            .ThenBy(reminder => reminder.Id)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdatePendingAsync(
        Guid userId,
        Reminder reminder,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Reminders
            .Where(existing =>
                existing.Id == reminder.Id &&
                existing.UserId == userId &&
                existing.Status == ReminderStatus.Pending &&
                existing.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(existing => existing.SourceType, reminder.SourceType)
                    .SetProperty(existing => existing.SourceId, reminder.SourceId)
                    .SetProperty(existing => existing.SourceTitle, reminder.SourceTitle)
                    .SetProperty(existing => existing.Title, reminder.Title)
                    .SetProperty(existing => existing.Message, reminder.Message)
                    .SetProperty(existing => existing.ScheduledLocalDate, reminder.ScheduledLocalDate)
                    .SetProperty(existing => existing.ScheduledLocalTime, reminder.ScheduledLocalTime)
                    .SetProperty(existing => existing.TimeZoneId, reminder.TimeZoneId)
                    .SetProperty(existing => existing.ScheduledForUtc, reminder.ScheduledForUtc)
                    .SetProperty(existing => existing.UpdatedAtUtc, reminder.UpdatedAtUtc)
                    .SetProperty(existing => existing.Version, existing => existing.Version + 1),
                cancellationToken);
    }

    public async Task CancelPendingAsync(
        Guid userId,
        Guid reminderId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;

        await context.Reminders
            .Where(reminder =>
                reminder.Id == reminderId &&
                reminder.UserId == userId &&
                reminder.Status == ReminderStatus.Pending &&
                reminder.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        reminder => reminder.Status,
                        ReminderStatus.Cancelled)
                    .SetProperty(
                        reminder => reminder.UpdatedAtUtc,
                        utcNow)
                    .SetProperty(
                        reminder => reminder.Version,
                        reminder => reminder.Version + 1),
                cancellationToken);
    }

    public async Task<IReadOnlyList<ReminderDueCandidate>> GetDueCandidatesAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var boundedBatchSize = Math.Max(1, batchSize);

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Reminders
            .AsNoTracking()
            .Where(reminder =>
                reminder.Status == ReminderStatus.Pending &&
                reminder.ScheduledForUtc <= utcNow)
            .OrderBy(reminder => reminder.ScheduledForUtc)
            .ThenBy(reminder => reminder.Id)
            .Take(boundedBatchSize)
            .Select(reminder => new ReminderDueCandidate(
                reminder.Id,
                reminder.UserId,
                reminder.Version,
                reminder.ScheduledForUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReminderFireCommitResult> CommitFireAsync(
        ReminderFireCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var reminder = await context.Reminders
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == request.ReminderId &&
                        candidate.UserId == request.UserId,
                    cancellationToken);

            var initialOutcome = GetPreFireOutcome(reminder, request);
            if (initialOutcome is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return initialOutcome;
            }

            var draft = request.Notification;
            var notification = new Notification
            {
                Id = draft.NotificationId,
                UserId = reminder!.UserId,
                Type = NotificationType.ReminderDue,
                Title = reminder.Title,
                Message = reminder.Message ?? string.Empty,
                SourceType = NotificationSourceType.Reminder,
                SourceId = reminder.Id,
                IdempotencyKey = draft.IdempotencyKey
            };

            context.Notifications.Add(notification);
            reminder.Status = ReminderStatus.Fired;
            reminder.FiredAtUtc = request.FiredAtUtc;
            reminder.NotificationId = notification.Id;
            reminder.Version++;
            reminder.UpdatedAtUtc = request.FiredAtUtc;

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.Fired,
                NotificationId = notification.Id
            };
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return await ResolveDuplicateFireAsync(request, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static ReminderFireCommitResult? GetPreFireOutcome(
        Reminder? reminder,
        ReminderFireCommitRequest request)
    {
        if (reminder is null)
        {
            return new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.Missing
            };
        }

        if (reminder.Status == ReminderStatus.Fired)
        {
            return new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.AlreadyFired,
                NotificationId = reminder.NotificationId
            };
        }

        if (reminder.Status == ReminderStatus.Cancelled)
        {
            return new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.Cancelled
            };
        }

        if (reminder.ScheduledForUtc > request.DueCutoffUtc)
        {
            return new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.NotDue
            };
        }

        if (reminder.Version != request.ExpectedVersion)
        {
            return new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.ConcurrencyLost
            };
        }

        return null;
    }

    private async Task<ReminderFireCommitResult> ResolveDuplicateFireAsync(
        ReminderFireCommitRequest request,
        CancellationToken cancellationToken)
    {
        var authoritative = await GetByIdAsync(
            request.UserId,
            request.ReminderId,
            cancellationToken);

        if (authoritative?.Status == ReminderStatus.Fired)
        {
            return new ReminderFireCommitResult
            {
                Status = ReminderFireCommitStatus.AlreadyFired,
                NotificationId = authoritative.NotificationId
            };
        }

        throw new DbUpdateException(
            "A notification uniqueness conflict occurred before the reminder became fired.");
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
