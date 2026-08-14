using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Habits;
using LifeOS.Core.Entities;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LifeOS.Infrastructure.Repositories;

public sealed class HabitRepository : IHabitRepository
{
    private const string HabitLogCompletionUniqueIndex =
        "IX_HabitLogs_UserId_HabitId_CompletionDate";

    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public HabitRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Habit?> GetByIdAsync(
        Guid userId,
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Habits
            .AsNoTracking()
            .SingleOrDefaultAsync(
                habit => habit.Id == habitId && habit.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Habits
            .AsNoTracking()
            .Where(habit => habit.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Habit habit,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Habits.Add(habit);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Habit habit,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Habits.Update(habit);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<HabitLog?> GetLogByDateAsync(
        Guid userId,
        Guid habitId,
        DateOnly completionDate,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.HabitLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                log => log.UserId == userId &&
                    log.HabitId == habitId &&
                    log.CompletionDate == completionDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<HabitLog>> GetLogsByHabitIdAsync(
        Guid userId,
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.HabitLogs
            .AsNoTracking()
            .Where(log =>
                log.UserId == userId &&
                log.HabitId == habitId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DateOnly>> GetCompletionDatesAsync(
        Guid userId,
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.HabitLogs
            .AsNoTracking()
            .Where(log =>
                log.UserId == userId &&
                log.HabitId == habitId)
            .Select(log => log.CompletionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Guid HabitId, DateOnly CompletionDate)>>
        GetCompletionDatesByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var completionDates = await context.HabitLogs
            .AsNoTracking()
            .Where(log => log.UserId == userId)
            .Select(log => new
            {
                log.HabitId,
                log.CompletionDate
            })
            .ToListAsync(cancellationToken);

        return completionDates
            .Select(item => (item.HabitId, item.CompletionDate))
            .ToList();
    }

    public async Task<HabitLogWriteResult> TryAddLogAsync(
        HabitLog habitLog,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.HabitLogs.Add(habitLog);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return new HabitLogWriteResult
            {
                WasInserted = true,
                Log = habitLog
            };
        }
        catch (DbUpdateException exception)
            when (IsExpectedDuplicateCompletion(exception))
        {
            var existing = await context.HabitLogs
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    log => log.UserId == habitLog.UserId &&
                        log.HabitId == habitLog.HabitId &&
                        log.CompletionDate == habitLog.CompletionDate,
                    cancellationToken);

            return new HabitLogWriteResult
            {
                WasInserted = false,
                Log = existing
            };
        }
    }

    private static bool IsExpectedDuplicateCompletion(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
            string.Equals(
                postgresException.ConstraintName,
                HabitLogCompletionUniqueIndex,
                StringComparison.Ordinal);
    }
}
