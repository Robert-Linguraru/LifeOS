using LifeOS.Core.Abstractions.Fitness;
using LifeOS.Core.DTOs.Fitness;
using LifeOS.Core.Services;

namespace LifeOS.Infrastructure.Services;

public sealed class ExerciseService : IExerciseService
{
    private readonly IExerciseRepository _repository;

    public ExerciseService(IExerciseRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ExerciseSummaryDto>> GetExercisesAsync(
        ExerciseQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _repository.GetAsync(query with { Search = query.Search?.Trim() }, cancellationToken);
    }

    public Task<ExerciseDetailDto?> GetExerciseAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(exerciseId, cancellationToken);
}
