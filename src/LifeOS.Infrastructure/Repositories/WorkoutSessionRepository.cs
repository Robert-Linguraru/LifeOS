using LifeOS.Core.Abstractions.WorkoutSessions;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Services;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

    public async Task<WorkoutSessionWriteResult> UpdateAsync(
        Guid userId,
        WorkoutSession session,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var existing = await context.WorkoutSessions
            .Include(item => item.Exercises)
            .ThenInclude(item => item.Sets)
            .SingleOrDefaultAsync(
                item => item.Id == session.Id && item.UserId == userId,
                cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        if (existing.Version != expectedVersion)
        {
            return Conflict();
        }

        context.Entry(existing).Property(item => item.Version).OriginalValue = expectedVersion;
        context.Entry(existing).Property(item => item.Version).CurrentValue = session.Version;

        var existingById = existing.Exercises.ToDictionary(item => item.Id);
        var proposedIds = session.Exercises.Select(item => item.Id).ToHashSet();
        foreach (var removed in existing.Exercises.Where(item => !proposedIds.Contains(item.Id)).ToList())
        {
            context.WorkoutSessionExercises.Remove(removed);
        }

        foreach (var proposed in session.Exercises)
        {
            if (existingById.TryGetValue(proposed.Id, out var current))
            {
                CopyExerciseValues(context.Entry(current), proposed);
            }
            else
            {
                context.WorkoutSessionExercises.Add(proposed);
            }
        }

        var requiresOrderPhase = existing.Exercises.Any(item =>
                proposedIds.Contains(item.Id) &&
                item.SortOrder != session.Exercises.Single(proposed => proposed.Id == item.Id).SortOrder)
            || session.Exercises.Any(item => !existingById.ContainsKey(item.Id));

        try
        {
            if (requiresOrderPhase)
            {
                var active = session.Exercises.ToList();
                for (var index = 0; index < active.Count; index++)
                {
                    var tracked = existingById.TryGetValue(active[index].Id, out var current)
                        ? context.Entry(current)
                        : context.Entry(active[index]);
                    tracked.Property(item => item.SortOrder).CurrentValue = TemporarySortOrderBase + index;
                }

                await context.SaveChangesAsync(cancellationToken);
                await ApplyFinalOrderingAsync(context, active, existingById, cancellationToken);
            }
            else
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Succeeded(existing);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
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

    private static void CopyExerciseValues(
        EntityEntry<WorkoutSessionExercise> entry,
        WorkoutSessionExercise source)
    {
        entry.Property(item => item.SortOrder).CurrentValue = source.SortOrder;
        entry.Property(item => item.OriginalExerciseId).CurrentValue = source.OriginalExerciseId;
        entry.Property(item => item.OriginalExerciseNameSnapshot).CurrentValue = source.OriginalExerciseNameSnapshot;
        entry.Property(item => item.ExerciseId).CurrentValue = source.ExerciseId;
        entry.Property(item => item.ExerciseNameSnapshot).CurrentValue = source.ExerciseNameSnapshot;
        entry.Property(item => item.LoggingModeSnapshot).CurrentValue = source.LoggingModeSnapshot;
        entry.Property(item => item.TargetSetCountSnapshot).CurrentValue = source.TargetSetCountSnapshot;
        entry.Property(item => item.TargetRepMinSnapshot).CurrentValue = source.TargetRepMinSnapshot;
        entry.Property(item => item.TargetRepMaxSnapshot).CurrentValue = source.TargetRepMaxSnapshot;
        entry.Property(item => item.DefaultRestSecondsSnapshot).CurrentValue = source.DefaultRestSecondsSnapshot;
        entry.Property(item => item.IsSkipped).CurrentValue = source.IsSkipped;
    }

    private static async Task ApplyFinalOrderingAsync(
        AppDbContext context,
        IReadOnlyList<WorkoutSessionExercise> exercises,
        IReadOnlyDictionary<Guid, WorkoutSessionExercise> existingById,
        CancellationToken cancellationToken)
    {
        var parameters = new List<NpgsqlParameter>();
        var cases = new List<string>();
        var ids = new List<string>();
        for (var index = 0; index < exercises.Count; index++)
        {
            parameters.Add(new NpgsqlParameter($"p_id_{index}", exercises[index].Id));
            parameters.Add(new NpgsqlParameter($"p_order_{index}", index + 1));
            cases.Add($"WHEN @p_id_{index} THEN @p_order_{index}");
            ids.Add($"@p_id_{index}");
            var tracked = existingById.TryGetValue(exercises[index].Id, out var current)
                ? current
                : exercises[index];
            var property = context.Entry(tracked).Property(item => item.SortOrder);
            property.CurrentValue = index + 1;
            property.OriginalValue = index + 1;
            property.IsModified = false;
        }

        var sql = $"UPDATE \"WorkoutSessionExercises\" SET \"SortOrder\" = CASE \"Id\" {string.Join(' ', cases)} END WHERE \"Id\" IN ({string.Join(", ", ids)});";
        await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        foreach (var exercise in exercises)
        {
            var tracked = existingById.TryGetValue(exercise.Id, out var current)
                ? current
                : exercise;
            context.Entry(tracked).State = EntityState.Detached;
        }
    }

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

    private static WorkoutSessionWriteResult NotFound() =>
        new() { Status = WorkoutSessionWriteStatus.NotFound };

    private const int TemporarySortOrderBase = 1_000_000;
}
