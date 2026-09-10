using LifeOS.Core.Enums.Fitness;

using LifeOS.Core.Services;

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
    IReadOnlyList<WorkoutSessionExerciseDto> Exercises)
{
    public IReadOnlyList<StrengthRecordAchievement> StrengthRecordAchievements { get; init; } = [];
}

public sealed record WorkoutSessionSummaryDto(
    Guid Id,
    string NameSnapshot,
    DateOnly WorkoutDate,
    WorkoutSessionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    TimeSpan? Duration,
    int ExerciseCount,
    int CompletedExerciseCount,
    int WorkingSetCount,
    SessionFeeling? SessionFeeling,
    long Version);

public sealed record WorkoutHistoryPageDto(
    IReadOnlyList<WorkoutSessionSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

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
    WorkoutSetKind Kind,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    long ExpectedVersion);

public sealed record CompleteWorkoutSetDto(
    Guid SessionExerciseId,
    Guid SetId,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    bool StartRestTimer,
    long ExpectedVersion);

public sealed record StartRestTimerDto(
    int DurationSeconds,
    long ExpectedVersion);

public sealed record TimerTimestampDto(
    long ExpectedVersion);

public sealed record AdjustRestTimerDto(
    int DurationSeconds,
    long ExpectedVersion);

public sealed record CompleteWorkoutDto(
    SessionFeeling? SessionFeeling,
    long ExpectedVersion);

public sealed record DiscardWorkoutDto(
    long ExpectedVersion);

public sealed record WorkoutPerformanceSummaryDto(
    Guid SessionExerciseId,
    string ExerciseNameSnapshot,
    ExerciseLoggingMode LoggingMode,
    decimal? ExternalLoadTimesRepsKg,
    decimal? AddedWeightTimesRepsKg,
    decimal? AssistanceWeightKg,
    decimal? BestWeightKg,
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
    IReadOnlyList<WorkoutPerformanceSummaryDto> Performance)
{
    public IReadOnlyList<StrengthRecordAchievement> StrengthRecordAchievements { get; init; } = [];
}

public sealed record PreviousPerformanceDto(
    Guid ExerciseId,
    string ExerciseNameSnapshot,
    Guid SessionId,
    string SessionNameSnapshot,
    DateOnly WorkoutDate,
    DateTimeOffset CompletedAtUtc,
    int SessionExerciseSortOrder,
    IReadOnlyList<WorkoutSetDto> WorkingSets);

public sealed record ExerciseHistoryDto(
    Guid ExerciseId,
    string ExerciseNameSnapshot,
    ExerciseLoggingMode LoggingMode,
    IReadOnlyList<PreviousPerformanceDto> Sessions,
    int Page,
    int PageSize,
    int TotalCount)
{
    public IReadOnlyList<StrengthRecordAchievement> CurrentBests { get; init; } = [];
}
