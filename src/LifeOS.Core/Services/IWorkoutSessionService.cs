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

    Task<WorkoutSessionDetailDto> AddExerciseAsync(
        Guid sessionId,
        AddSessionExerciseDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> RemoveExerciseAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> SubstituteExerciseAsync(
        Guid sessionId,
        SubstituteSessionExerciseDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> SkipExerciseAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> UnskipExerciseAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> AddSetAsync(
        Guid sessionId,
        AddWorkoutSetDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> UpdateSetAsync(
        Guid sessionId,
        UpdateWorkoutSetDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> RemoveSetAsync(
        Guid sessionId,
        Guid sessionExerciseId,
        Guid setId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> CompleteSetAsync(
        Guid sessionId,
        CompleteWorkoutSetDto dto,
        CancellationToken cancellationToken = default);
}
