using LifeOS.Core.DTOs.Fitness;

namespace LifeOS.Core.Abstractions.Fitness;

public interface IExerciseRepository
{
    Task<IReadOnlyList<ExerciseSummaryDto>> GetAsync(
        ExerciseQuery query,
        CancellationToken cancellationToken = default);

    Task<ExerciseDetailDto?> GetByIdAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default);
}
