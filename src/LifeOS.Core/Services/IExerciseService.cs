using LifeOS.Core.DTOs.Fitness;

namespace LifeOS.Core.Services;

public interface IExerciseService
{
    Task<IReadOnlyList<ExerciseSummaryDto>> GetExercisesAsync(
        ExerciseQuery query,
        CancellationToken cancellationToken = default);

    Task<ExerciseDetailDto?> GetExerciseAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default);
}
