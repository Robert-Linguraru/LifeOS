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

    Task<IReadOnlyList<WorkoutSessionSummaryDto>> GetHistoryAsync(
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

    Task<WorkoutSessionDetailDto> SetExerciseSkippedAsync(
        Guid sessionId,
        SetSessionExerciseSkippedDto dto,
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

    Task<WorkoutSessionDetailDto> StartRestTimerAsync(
        Guid sessionId,
        StartRestTimerDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> PauseRestTimerAsync(
        Guid sessionId,
        TimerTimestampDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> ResumeRestTimerAsync(
        Guid sessionId,
        TimerTimestampDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> ResetRestTimerAsync(
        Guid sessionId,
        TimerTimestampDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> AdjustRestTimerAsync(
        Guid sessionId,
        AdjustRestTimerDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> ClearRestTimerAsync(
        Guid sessionId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<WorkoutCompletionSummaryDto> CompleteWorkoutAsync(
        Guid sessionId,
        CompleteWorkoutDto dto,
        CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetailDto> DiscardWorkoutAsync(
        Guid sessionId,
        DiscardWorkoutDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExerciseHistoryDto>> GetExerciseHistoryAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default);
}
