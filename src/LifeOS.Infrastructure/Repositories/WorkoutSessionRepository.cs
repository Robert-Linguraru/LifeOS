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
        context.Entry(existing).Property(item => item.Status).CurrentValue = session.Status;
        context.Entry(existing).Property(item => item.CompletedAtUtc).CurrentValue = session.CompletedAtUtc;
        context.Entry(existing).Property(item => item.DiscardedAtUtc).CurrentValue = session.DiscardedAtUtc;
        context.Entry(existing).Property(item => item.SessionFeeling).CurrentValue = session.SessionFeeling;
        context.Entry(existing).Property(item => item.RestTimerDurationSeconds).CurrentValue = session.RestTimerDurationSeconds;
        context.Entry(existing).Property(item => item.RestTimerEndsAtUtc).CurrentValue = session.RestTimerEndsAtUtc;
        context.Entry(existing).Property(item => item.RestTimerPausedRemainingSeconds).CurrentValue = session.RestTimerPausedRemainingSeconds;

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
                ReconcileSets(context, current, proposed);
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

        var requiresSetOrderPhase = ReconcileSetOrdering(context, existing, session);

        try
        {
            if (requiresOrderPhase || requiresSetOrderPhase)
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

                if (requiresSetOrderPhase)
                {
                    await context.SaveChangesAsync(cancellationToken);
                    await ApplyFinalSetOrderingAsync(context, session, cancellationToken);
                }
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

    public async Task<WorkoutSession?> GetCompletedByIdAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await QueryDetails(context.WorkoutSessions)
            .Where(session => session.UserId == userId
                && session.Id == sessionId
                && session.Status == WorkoutSessionStatus.Completed)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkoutHistoryPageDto> GetCompletedHistoryAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var sessions = context.WorkoutSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId && session.Status == WorkoutSessionStatus.Completed);
        var totalCount = await sessions.CountAsync(cancellationToken);
        var items = await sessions
            .OrderByDescending(session => session.WorkoutDate)
            .ThenByDescending(session => session.CompletedAtUtc)
            .ThenByDescending(session => session.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(session => new WorkoutSessionSummaryDto(
                session.Id,
                session.NameSnapshot,
                session.WorkoutDate,
                session.Status,
                session.StartedAtUtc,
                session.CompletedAtUtc,
                session.CompletedAtUtc - session.StartedAtUtc,
                session.Exercises.Count,
                session.Exercises.Count(exercise => exercise.Sets.Any(set => set.CompletedAtUtc != null)),
                session.Exercises.SelectMany(exercise => exercise.Sets)
                    .Count(set => set.CompletedAtUtc != null && set.Kind == WorkoutSetKind.Working),
                session.SessionFeeling,
                session.Version))
            .ToListAsync(cancellationToken);

        return new WorkoutHistoryPageDto(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<PreviousPerformanceDto>> GetPreviousPerformancesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> exerciseIds,
        Guid currentSessionId,
        CancellationToken cancellationToken = default)
    {
        if (exerciseIds.Count == 0)
        {
            return [];
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var occurrences = await context.WorkoutSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId
                && session.Id != currentSessionId
                && session.Status == WorkoutSessionStatus.Completed
                && session.CompletedAtUtc != null)
            .SelectMany(session => session.Exercises
                .Where(exercise => exerciseIds.Contains(exercise.ExerciseId)
                    && !exercise.IsSkipped
                    && exercise.Sets.Any(set => set.CompletedAtUtc != null && set.Kind == WorkoutSetKind.Working))
                .Select(exercise => new PreviousPerformanceDto(
                    exercise.ExerciseId,
                    exercise.ExerciseNameSnapshot,
                    session.Id,
                    session.NameSnapshot,
                    session.WorkoutDate,
                    session.CompletedAtUtc!.Value,
                    exercise.SortOrder,
                    exercise.Sets
                        .Where(set => set.CompletedAtUtc != null && set.Kind == WorkoutSetKind.Working)
                        .OrderBy(set => set.SortOrder)
                        .Select(set => new WorkoutSetDto(
                            set.Id,
                            set.SortOrder,
                            set.Kind,
                            set.WeightKg,
                            set.Repetitions,
                            set.DurationSeconds,
                            set.CompletedAtUtc))
                        .ToList())))
            .ToListAsync(cancellationToken);

        return occurrences
            .GroupBy(item => item.ExerciseId)
            .SelectMany(group => group
                .OrderByDescending(item => item.CompletedAtUtc)
                .ThenByDescending(item => item.SessionId)
                .ThenBy(item => item.SessionExerciseSortOrder)
                .Take(1))
            .OrderBy(item => item.ExerciseId)
            .ToList();
    }

    public async Task<ExerciseHistoryDto> GetExerciseHistoryAsync(
        Guid userId,
        Guid exerciseId,
        string exerciseNameSnapshot,
        ExerciseLoggingMode loggingMode,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var occurrences = context.WorkoutSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId && session.Status == WorkoutSessionStatus.Completed)
            .SelectMany(session => session.Exercises
                .Where(exercise => exercise.ExerciseId == exerciseId
                    && !exercise.IsSkipped
                    && exercise.Sets.Any(set => set.CompletedAtUtc != null))
                .Select(exercise => new
                {
                    SessionExerciseId = exercise.Id,
                    exercise.ExerciseId,
                    exerciseNameSnapshot = exercise.ExerciseNameSnapshot,
                    SessionId = session.Id,
                    SessionNameSnapshot = session.NameSnapshot,
                    session.WorkoutDate,
                    CompletedAtUtc = session.CompletedAtUtc!.Value,
                    SessionExerciseSortOrder = exercise.SortOrder
                }));
        var totalCount = await occurrences.CountAsync(cancellationToken);
        var occurrencePage = await occurrences
            .OrderByDescending(item => item.CompletedAtUtc)
            .ThenByDescending(item => item.SessionId)
            .ThenBy(item => item.SessionExerciseSortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var sessionExerciseIds = occurrencePage.Select(item => item.SessionExerciseId).ToArray();
        var setRows = await context.WorkoutSessionExercises
            .AsNoTracking()
            .Where(exercise => sessionExerciseIds.Contains(exercise.Id))
            .SelectMany(exercise => exercise.Sets
                .Where(set => set.CompletedAtUtc != null)
                .Select(set => new
                {
                    SessionExerciseId = exercise.Id,
                    Set = new WorkoutSetDto(
                        set.Id,
                        set.SortOrder,
                        set.Kind,
                        set.WeightKg,
                        set.Repetitions,
                        set.DurationSeconds,
                        set.CompletedAtUtc)
                }))
            .ToListAsync(cancellationToken);
        var setsByOccurrence = setRows
            .GroupBy(item => item.SessionExerciseId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WorkoutSetDto>)group
                    .Select(item => item.Set)
                    .OrderBy(set => set.SortOrder)
                    .ToList());
        var sessions = occurrencePage
            .Select(item => new PreviousPerformanceDto(
                item.ExerciseId,
                item.exerciseNameSnapshot,
                item.SessionId,
                item.SessionNameSnapshot,
                item.WorkoutDate,
                item.CompletedAtUtc,
                item.SessionExerciseSortOrder,
                setsByOccurrence[item.SessionExerciseId]))
            .ToList();

        return new ExerciseHistoryDto(
            exerciseId,
            exerciseNameSnapshot,
            loggingMode,
            sessions,
            page,
            pageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<StrengthSetEvidence>> GetStrengthRecordEvidenceAsync(
        Guid userId,
        Guid exerciseId,
        Guid currentSessionId,
        Guid candidateSetId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await context.WorkoutSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId
                && ((session.Status == WorkoutSessionStatus.Completed && session.Id != currentSessionId)
                    || (session.Id == currentSessionId && session.Status == WorkoutSessionStatus.InProgress)))
            .SelectMany(session => session.Exercises
                .Where(exercise => exercise.ExerciseId == exerciseId && !exercise.IsSkipped)
                .SelectMany(exercise => exercise.Sets
                    .Where(set => set.Id != candidateSetId
                        && set.CompletedAtUtc != null
                        && set.Kind == WorkoutSetKind.Working)
                    .Select(set => new
                    {
                        exercise.LoggingModeSnapshot,
                        set.Kind,
                        set.WeightKg,
                        set.Repetitions,
                        set.DurationSeconds,
                        set.CompletedAtUtc
                    })))
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(item => item.CompletedAtUtc)
            .Select(item => new StrengthSetEvidence(
                item.LoggingModeSnapshot,
                item.Kind,
                item.WeightKg,
                item.Repetitions,
                item.DurationSeconds,
                item.CompletedAtUtc))
            .ToList();
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

    private static void ReconcileSets(
        AppDbContext context,
        WorkoutSessionExercise current,
        WorkoutSessionExercise proposed)
    {
        var existingById = current.Sets.ToDictionary(item => item.Id);
        var proposedIds = proposed.Sets.Select(item => item.Id).ToHashSet();
        foreach (var removed in current.Sets.Where(item => !proposedIds.Contains(item.Id)).ToList())
        {
            context.WorkoutSets.Remove(removed);
        }

        foreach (var proposedSet in proposed.Sets)
        {
            if (existingById.TryGetValue(proposedSet.Id, out var existingSet))
            {
                CopySetValues(context.Entry(existingSet), proposedSet);
            }
            else
            {
                context.WorkoutSets.Add(proposedSet);
            }
        }
    }

    private static bool ReconcileSetOrdering(
        AppDbContext context,
        WorkoutSession existing,
        WorkoutSession proposed)
    {
        var requiresOrderPhase = false;
        var existingExercises = existing.Exercises.ToDictionary(item => item.Id);
        foreach (var proposedExercise in proposed.Exercises)
        {
            if (!existingExercises.TryGetValue(proposedExercise.Id, out var existingExercise))
            {
                requiresOrderPhase |= proposedExercise.Sets.Count > 0;
                foreach (var set in proposedExercise.Sets)
                {
                    context.Entry(set).Property(item => item.SortOrder).CurrentValue = TemporarySortOrderBase + set.SortOrder;
                }

                continue;
            }

            var proposedIds = proposedExercise.Sets.Select(item => item.Id).ToHashSet();
            requiresOrderPhase |= existingExercise.Sets.Any(item => !proposedIds.Contains(item.Id));
            requiresOrderPhase |= proposedExercise.Sets.Any(item =>
                !existingExercise.Sets.Any(existingSet => existingSet.Id == item.Id && existingSet.SortOrder == item.SortOrder));
            foreach (var set in proposedExercise.Sets)
            {
                var entry = context.Entry(set.Id == Guid.Empty
                    ? set
                    : existingExercise.Sets.SingleOrDefault(item => item.Id == set.Id) ?? set);
                entry.Property(item => item.SortOrder).CurrentValue = TemporarySortOrderBase + set.SortOrder;
            }
        }

        return requiresOrderPhase;
    }

    private static void CopySetValues(EntityEntry<WorkoutSet> entry, WorkoutSet source)
    {
        entry.Property(item => item.SortOrder).CurrentValue = source.SortOrder;
        entry.Property(item => item.Kind).CurrentValue = source.Kind;
        entry.Property(item => item.WeightKg).CurrentValue = source.WeightKg;
        entry.Property(item => item.Repetitions).CurrentValue = source.Repetitions;
        entry.Property(item => item.DurationSeconds).CurrentValue = source.DurationSeconds;
        entry.Property(item => item.CompletedAtUtc).CurrentValue = source.CompletedAtUtc;
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

    private static async Task ApplyFinalSetOrderingAsync(
        AppDbContext context,
        WorkoutSession session,
        CancellationToken cancellationToken)
    {
        var parameters = new List<NpgsqlParameter>();
        var cases = new List<string>();
        var ids = new List<string>();
        var index = 0;
        foreach (var set in session.Exercises.SelectMany(item => item.Sets))
        {
            parameters.Add(new NpgsqlParameter($"p_set_id_{index}", set.Id));
            parameters.Add(new NpgsqlParameter($"p_set_order_{index}", set.SortOrder));
            cases.Add($"WHEN @p_set_id_{index} THEN @p_set_order_{index}");
            ids.Add($"@p_set_id_{index}");
            index++;
        }

        if (ids.Count == 0)
        {
            return;
        }

        var sql = $"UPDATE \"WorkoutSets\" SET \"SortOrder\" = CASE \"Id\" {string.Join(' ', cases)} END WHERE \"Id\" IN ({string.Join(", ", ids)});";
        await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
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
