using LifeOS.Core.DTOs.Fitness;
using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions.Fitness;

public interface IExerciseRepository
{
    Task<IReadOnlyList<Exercise>> GetActiveByIdsAsync(
        IReadOnlyCollection<Guid> exerciseIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExerciseSummaryDto>> GetAsync(
        ExerciseQuery query,
        CancellationToken cancellationToken = default);

    Task<ExerciseDetailDto?> GetByIdAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default);

    Task<ExerciseDetailDto?> GetByIdIncludingInactiveAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default);
}
