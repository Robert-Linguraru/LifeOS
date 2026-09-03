using LifeOS.Core.Entities;

namespace LifeOS.Core.Abstractions.WorkoutTemplates;

public enum WorkoutTemplateWriteStatus
{
    Succeeded = 0,
    NotFound = 1,
    ConcurrencyConflict = 2
}

public sealed class WorkoutTemplateWriteResult
{
    public WorkoutTemplateWriteStatus Status { get; init; }

    public WorkoutTemplate? Template { get; init; }
}

public interface IWorkoutTemplateRepository
{
    Task<IReadOnlyList<WorkoutTemplate>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplate?> GetByIdAsync(
        Guid userId,
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        WorkoutTemplate template,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateWriteResult> UpdateAsync(
        Guid userId,
        WorkoutTemplate template,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<WorkoutTemplateWriteResult> DeleteAsync(
        Guid userId,
        Guid templateId,
        long expectedVersion,
        CancellationToken cancellationToken = default);
}
