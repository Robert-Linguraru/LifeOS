using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.Entities;
using LifeOS.Core.Services;

namespace LifeOS.Core.Abstractions.WorkoutSessions;

public enum WorkoutSessionWriteStatus
{
    Succeeded = 0,
    NotFound = 1,
    ConcurrencyConflict = 2,
    ActiveSessionAlreadyExists = 3
}

public sealed class WorkoutSessionWriteResult
{
    public WorkoutSessionWriteStatus Status { get; init; }

    public WorkoutSession? Session { get; init; }
}

public interface IWorkoutSessionRepository
{
    Task<WorkoutSession?> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WorkoutSession?> GetByIdAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionWriteResult> AddAsync(
        WorkoutSession session,
        CancellationToken cancellationToken = default);
}
