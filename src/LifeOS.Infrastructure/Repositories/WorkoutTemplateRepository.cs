using LifeOS.Core.Abstractions.WorkoutTemplates;
using LifeOS.Core.Entities;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace LifeOS.Infrastructure.Repositories;

public sealed class WorkoutTemplateRepository : IWorkoutTemplateRepository
{
    private const int TemporarySortOrderBase = 1_000_000;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public WorkoutTemplateRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<WorkoutTemplate>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.WorkoutTemplates
            .AsNoTracking()
            .Include(template => template.Exercises)
            .Where(template => template.UserId == userId)
            .OrderByDescending(template => template.UpdatedAtUtc)
            .ThenBy(template => template.Name)
            .ThenBy(template => template.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkoutTemplate?> GetByIdAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        if (templateId == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.WorkoutTemplates
            .AsNoTracking()
            .Include(template => template.Exercises)
            .SingleOrDefaultAsync(
                template => template.Id == templateId && template.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        WorkoutTemplate template,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.WorkoutTemplates.Add(template);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkoutTemplateWriteResult> UpdateAsync(
        Guid userId,
        WorkoutTemplate template,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var existing = await context.WorkoutTemplates
            .Include(item => item.Exercises)
            .SingleOrDefaultAsync(
                item => item.Id == template.Id && item.UserId == userId,
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
        context.Entry(existing).Property(item => item.Name).CurrentValue = template.Name;
        context.Entry(existing).Property(item => item.Version).CurrentValue = template.Version;

        var existingById = existing.Exercises.ToDictionary(item => item.Id);
        var proposedIds = template.Exercises.Select(item => item.Id).ToHashSet();

        foreach (var removed in existing.Exercises.Where(item => !proposedIds.Contains(item.Id)).ToList())
        {
            context.WorkoutTemplateExercises.Remove(removed);
        }

        foreach (var proposed in template.Exercises)
        {
            if (existingById.TryGetValue(proposed.Id, out var current))
            {
                CopyExerciseValues(context.Entry(current), proposed);
            }
            else
            {
                context.WorkoutTemplateExercises.Add(proposed);
            }
        }

        var requiresOrderPhase = template.Exercises.Count > 0;

        if (requiresOrderPhase)
        {
            var activeProposed = template.Exercises.ToList();
            for (var index = 0; index < activeProposed.Count; index++)
            {
                var proposed = activeProposed[index];
                var entry = existingById.TryGetValue(proposed.Id, out var current)
                    ? context.Entry(current)
                    : context.Entry(proposed);
                entry.Property(item => item.SortOrder).CurrentValue = TemporarySortOrderBase + index;
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict();
            }

            try
            {
                await ApplyFinalOrderingAsync(context, activeProposed, existingById, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Succeeded(existing);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict();
            }
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Succeeded(existing);
        }

        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
    }

    private static async Task ApplyFinalOrderingAsync(
        AppDbContext context,
        IReadOnlyList<WorkoutTemplateExercise> exercises,
        IReadOnlyDictionary<Guid, WorkoutTemplateExercise> existingById,
        CancellationToken cancellationToken)
    {
        var parameters = new List<NpgsqlParameter>();
        var cases = new List<string>();
        var ids = new List<string>();

        for (var index = 0; index < exercises.Count; index++)
        {
            var idParameter = new NpgsqlParameter($"p_id_{index}", exercises[index].Id);
            var orderParameter = new NpgsqlParameter($"p_order_{index}", index + 1);
            parameters.Add(idParameter);
            parameters.Add(orderParameter);
            cases.Add($"WHEN @p_id_{index} THEN @p_order_{index}");
            ids.Add($"@p_id_{index}");
            var tracked = existingById.TryGetValue(exercises[index].Id, out var existing)
                ? existing
                : exercises[index];
            var sortOrder = context.Entry(tracked).Property(item => item.SortOrder);
            sortOrder.CurrentValue = index + 1;
            sortOrder.OriginalValue = index + 1;
            sortOrder.IsModified = false;
        }

        var sql = $"UPDATE \"WorkoutTemplateExercises\" SET \"SortOrder\" = CASE \"Id\" {string.Join(' ', cases)} END WHERE \"Id\" IN ({string.Join(", ", ids)});";
        await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);

        foreach (var exercise in exercises)
        {
            var tracked = existingById.TryGetValue(exercise.Id, out var existing)
                ? existing
                : exercise;
            context.Entry(tracked).State = EntityState.Detached;
        }
    }

    public async Task<WorkoutTemplateWriteResult> DeleteAsync(
        Guid userId,
        Guid templateId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var template = await context.WorkoutTemplates
            .Include(item => item.Exercises)
            .SingleOrDefaultAsync(
                item => item.Id == templateId && item.UserId == userId,
                cancellationToken);

        if (template is null)
        {
            return NotFound();
        }

        if (template.Version != expectedVersion)
        {
            return Conflict();
        }

        context.Entry(template).Property(item => item.Version).OriginalValue = expectedVersion;
        foreach (var exercise in template.Exercises)
        {
            context.WorkoutTemplateExercises.Remove(exercise);
        }

        context.WorkoutTemplates.Remove(template);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Succeeded(null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
    }

    private static void CopyExerciseValues(
        EntityEntry<WorkoutTemplateExercise> entry,
        WorkoutTemplateExercise source)
    {
        entry.Property(item => item.SortOrder).CurrentValue = source.SortOrder;
        entry.Property(item => item.TargetSetCount).CurrentValue = source.TargetSetCount;
        entry.Property(item => item.TargetRepMin).CurrentValue = source.TargetRepMin;
        entry.Property(item => item.TargetRepMax).CurrentValue = source.TargetRepMax;
        entry.Property(item => item.DefaultRestSeconds).CurrentValue = source.DefaultRestSeconds;
    }

    private static WorkoutTemplateWriteResult Succeeded(WorkoutTemplate? template) =>
        new() { Status = WorkoutTemplateWriteStatus.Succeeded, Template = template };

    private static WorkoutTemplateWriteResult NotFound() =>
        new() { Status = WorkoutTemplateWriteStatus.NotFound };

    private static WorkoutTemplateWriteResult Conflict() =>
        new() { Status = WorkoutTemplateWriteStatus.ConcurrencyConflict };
}
