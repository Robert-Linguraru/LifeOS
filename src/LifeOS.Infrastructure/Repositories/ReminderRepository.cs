using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public Task<ReminderFireCommitResult> CommitFireAsync(
        ReminderFireCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Reminder firing is implemented in a later milestone ticket.");
    }
}
