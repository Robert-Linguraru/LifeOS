using LifeOS.Core.Abstractions.WorkoutSessions;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LifeOS.Infrastructure.Repositories;

public sealed class WorkoutSessionRepository : IWorkoutSessionRepository
{
    private const string ActiveSessionIndexName = "IX_WorkoutSessions_UserId";
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public WorkoutSessionRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<WorkoutSession?> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryDetails(context.WorkoutSessions)
            .SingleOrDefaultAsync(
                session => session.UserId == userId && session.Status == WorkoutSessionStatus.InProgress,
                cancellationToken);
    }

    public async Task<WorkoutSession?> GetByIdAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryDetails(context.WorkoutSessions)
            .SingleOrDefaultAsync(
                session => session.UserId == userId && session.Id == sessionId,
                cancellationToken);
    }

    public async Task<WorkoutSessionWriteResult> AddAsync(
        WorkoutSession session,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.WorkoutSessions.Add(session);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Succeeded(session);
        }
        catch (DbUpdateException exception) when (IsActiveSessionViolation(exception))
        {
            var active = await QueryDetails(context.WorkoutSessions)
                .SingleOrDefaultAsync(
                    item => item.UserId == session.UserId && item.Status == WorkoutSessionStatus.InProgress,
                    cancellationToken);
            return active is null ? Conflict() : Existing(active);
        }
    }

    private static IQueryable<WorkoutSession> QueryDetails(IQueryable<WorkoutSession> query) =>
        query
            .AsNoTracking()
            .Include(session => session.Exercises)
            .ThenInclude(exercise => exercise.Sets)
            .AsSplitQuery();

    private static bool IsActiveSessionViolation(DbUpdateException exception) =>
        exception.GetBaseException() is PostgresException postgresException &&
        postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
        string.Equals(postgresException.ConstraintName, ActiveSessionIndexName, StringComparison.Ordinal);

    private static WorkoutSessionWriteResult Succeeded(WorkoutSession session) =>
        new() { Status = WorkoutSessionWriteStatus.Succeeded, Session = session };

    private static WorkoutSessionWriteResult Existing(WorkoutSession session) =>
        new() { Status = WorkoutSessionWriteStatus.ActiveSessionAlreadyExists, Session = session };

    private static WorkoutSessionWriteResult Conflict() =>
        new() { Status = WorkoutSessionWriteStatus.ConcurrencyConflict };
}
