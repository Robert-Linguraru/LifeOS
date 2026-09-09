using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.DTOs.WorkoutSessions;

namespace LifeOS.Core.Services;

public interface IWorkoutSessionService
{
    Task<WorkoutSessionDetailDto?> GetActiveSessionAsync(
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> StartFromTemplateAsync(
        StartTemplateWorkoutDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> StartCustomWorkoutAsync(
        StartCustomWorkoutDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
