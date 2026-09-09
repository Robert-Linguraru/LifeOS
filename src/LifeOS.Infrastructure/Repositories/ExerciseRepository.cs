using LifeOS.Core.Abstractions.Fitness;
using LifeOS.Core.DTOs.Fitness;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Repositories;

public sealed class ExerciseRepository : IExerciseRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ExerciseRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Exercise>> GetActiveByIdsAsync(
        IReadOnlyCollection<Guid> exerciseIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exerciseIds);

        if (exerciseIds.Count == 0)
        {
            return [];
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Exercises
            .AsNoTracking()
            .Where(exercise => exerciseIds.Contains(exercise.Id) && exercise.IsActive)
            .OrderBy(exercise => exercise.SortOrder)
            .ThenBy(exercise => exercise.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseSummaryDto>> GetAsync(
        ExerciseQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var exercises = context.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.IsActive);

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            exercises = exercises.Where(exercise =>
                EF.Functions.ILike(exercise.Name, pattern, "\\"));
        }

        if (query.PrimaryMuscleGroup is { } primaryMuscleGroup)
        {
            exercises = exercises.Where(exercise => exercise.PrimaryMuscleGroup == primaryMuscleGroup);
        }

        if (query.Equipment is { } equipment)
        {
            exercises = exercises.Where(exercise => exercise.Equipment == equipment);
        }

        if (query.MovementPattern is { } movementPattern)
        {
            exercises = exercises.Where(exercise => exercise.MovementPattern == movementPattern);
        }

        if (query.LoggingMode is { } loggingMode)
        {
            exercises = exercises.Where(exercise => exercise.LoggingMode == loggingMode);
        }

        return await exercises
            .OrderBy(exercise => exercise.SortOrder)
            .ThenBy(exercise => exercise.Name)
            .ThenBy(exercise => exercise.Id)
            .Select(exercise => new ExerciseSummaryDto(
                exercise.Id,
                exercise.Name,
                exercise.PrimaryMuscleGroup,
                exercise.Equipment,
                exercise.MovementPattern,
                exercise.LoggingMode))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExerciseDetailDto?> GetByIdAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default)
    {
        if (exerciseId == Guid.Empty)
        {
            return null;
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.Id == exerciseId && exercise.IsActive)
            .Select(exercise => new ExerciseDetailDto(
                exercise.Id,
                exercise.Name,
                exercise.PrimaryMuscleGroup,
                exercise.Equipment,
                exercise.MovementPattern,
                exercise.LoggingMode,
                exercise.SecondaryMuscleGroups
                    .OrderBy(secondary => secondary.MuscleGroup)
                    .Select(secondary => secondary.MuscleGroup)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ExerciseDetailDto?> GetByIdIncludingInactiveAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default)
    {
        if (exerciseId == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.Id == exerciseId)
            .Select(exercise => new ExerciseDetailDto(
                exercise.Id,
                exercise.Name,
                exercise.PrimaryMuscleGroup,
                exercise.Equipment,
                exercise.MovementPattern,
                exercise.LoggingMode,
                exercise.SecondaryMuscleGroups
                    .OrderBy(secondary => secondary.MuscleGroup)
                    .Select(secondary => secondary.MuscleGroup)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
