using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.DTOs.WorkoutSessions;

public sealed record WorkoutSetDto(
    Guid Id,
    int SortOrder,
    WorkoutSetKind Kind,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    DateTimeOffset? CompletedAtUtc);

public sealed record WorkoutSessionExerciseDto(
    Guid Id,
    int SortOrder,
    Guid OriginalExerciseId,
    string OriginalExerciseNameSnapshot,
    Guid ExerciseId,
    string ExerciseNameSnapshot,
    ExerciseLoggingMode LoggingModeSnapshot,
    int TargetSetCountSnapshot,
    int? TargetRepMinSnapshot,
    int? TargetRepMaxSnapshot,
    int DefaultRestSecondsSnapshot,
    bool IsSkipped,
    IReadOnlyList<WorkoutSetDto> Sets);

public sealed record WorkoutSessionDetailDto(
    Guid Id,
    Guid? OriginTemplateId,
    string NameSnapshot,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAtUtc,
    WorkoutSessionStatus Status,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? DiscardedAtUtc,
    SessionFeeling? SessionFeeling,
    long Version,
    int? RestTimerDurationSeconds,
    DateTimeOffset? RestTimerEndsAtUtc,
    int? RestTimerPausedRemainingSeconds,
    IReadOnlyList<WorkoutSessionExerciseDto> Exercises);

public sealed record WorkoutSessionSummaryDto(
    Guid Id,
    string NameSnapshot,
    DateOnly WorkoutDate,
    WorkoutSessionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int ExerciseCount,
    long Version);

public sealed record StartTemplateWorkoutDto(
    Guid TemplateId,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAtUtc);

public sealed record StartCustomWorkoutDto(
    string NameSnapshot,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAtUtc,
    IReadOnlyList<StartWorkoutExerciseDto> Exercises);

public sealed record StartWorkoutExerciseDto(
    Guid ExerciseId,
    string ExerciseNameSnapshot,
    ExerciseLoggingMode LoggingModeSnapshot,
    int TargetSetCountSnapshot,
    int? TargetRepMinSnapshot,
    int? TargetRepMaxSnapshot,
    int DefaultRestSecondsSnapshot);

public sealed record AddSessionExerciseDto(
    Guid ExerciseId,
    string ExerciseNameSnapshot,
    ExerciseLoggingMode LoggingModeSnapshot,
    int TargetSetCountSnapshot,
    int? TargetRepMinSnapshot,
    int? TargetRepMaxSnapshot,
    int DefaultRestSecondsSnapshot,
    long ExpectedVersion);

public sealed record SubstituteSessionExerciseDto(
    Guid SessionExerciseId,
    Guid ExerciseId,
    string ExerciseNameSnapshot,
    ExerciseLoggingMode LoggingMode,
    long ExpectedVersion);

public sealed record SetSessionExerciseSkippedDto(
    Guid SessionExerciseId,
    bool IsSkipped,
    long ExpectedVersion);

public sealed record AddWorkoutSetDto(
    Guid SessionExerciseId,
    WorkoutSetKind Kind,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    long ExpectedVersion);

public sealed record UpdateWorkoutSetDto(
    Guid SessionExerciseId,
    Guid SetId,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    long ExpectedVersion);

public sealed record CompleteWorkoutSetDto(
    Guid SessionExerciseId,
    Guid SetId,
    DateTimeOffset CompletedAtUtc,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    bool StartRestTimer,
    long ExpectedVersion);

public sealed record StartRestTimerDto(
    int DurationSeconds,
    DateTimeOffset NowUtc,
    long ExpectedVersion);

public sealed record TimerTimestampDto(
    DateTimeOffset NowUtc,
    long ExpectedVersion);

public sealed record AdjustRestTimerDto(
    int DurationSeconds,
    DateTimeOffset NowUtc,
    long ExpectedVersion);

public sealed record CompleteWorkoutDto(
    DateTimeOffset CompletedAtUtc,
    SessionFeeling? SessionFeeling,
    long ExpectedVersion);

public sealed record DiscardWorkoutDto(
    DateTimeOffset DiscardedAtUtc,
    long ExpectedVersion);

public sealed record WorkoutPerformanceSummaryDto(
    Guid SessionExerciseId,
    string ExerciseNameSnapshot,
    decimal? ExternalLoadTimesRepsKg,
    int? CompletedRepetitions,
    int? CompletedDurationSeconds);

public sealed record WorkoutCompletionSummaryDto(
    Guid SessionId,
    string NameSnapshot,
    TimeSpan Duration,
    int ExerciseCount,
    int CompletedExerciseCount,
    int WorkingSetCount,
    SessionFeeling? SessionFeeling,
    IReadOnlyList<WorkoutPerformanceSummaryDto> Performance);

public sealed record PreviousPerformanceDto(
    Guid ExerciseId,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<WorkoutSetDto> WorkingSets);

public sealed record ExerciseHistoryDto(
    Guid ExerciseId,
    string ExerciseNameSnapshot,
    IReadOnlyList<PreviousPerformanceDto> Sessions);
