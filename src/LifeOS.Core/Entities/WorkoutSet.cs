using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Entities;

public sealed class WorkoutSet : UserOwnedEntity
{
    private WorkoutSet()
    {
    }

    internal WorkoutSet(
        Guid id,
        Guid userId,
        Guid workoutSessionExerciseId,
        int sortOrder,
        WorkoutSetKind kind,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Workout set ID cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (workoutSessionExerciseId == Guid.Empty)
        {
            throw new ArgumentException("Session exercise ID cannot be empty.", nameof(workoutSessionExerciseId));
        }

        if (sortOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ValidateMeasurements(weightKg, repetitions, durationSeconds);

        Id = id;
        UserId = userId;
        WorkoutSessionExerciseId = workoutSessionExerciseId;
        SortOrder = sortOrder;
        Kind = kind;
        WeightKg = weightKg;
        Repetitions = repetitions;
        DurationSeconds = durationSeconds;
    }

    public Guid WorkoutSessionExerciseId { get; private set; }

    public int SortOrder { get; private set; }

    public WorkoutSetKind Kind { get; private set; }

    public decimal? WeightKg { get; private set; }

    public int? Repetitions { get; private set; }

    public int? DurationSeconds { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public bool IsCompleted => CompletedAtUtc is not null;

    internal void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        SortOrder = sortOrder;
    }

    internal void UpdateMeasurements(
        ExerciseLoggingMode loggingMode,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        ValidateMeasurements(weightKg, repetitions, durationSeconds);
        if (IsCompleted)
        {
            ValidateCompletedMeasurements(loggingMode, weightKg, repetitions, durationSeconds);
        }

        WeightKg = weightKg;
        Repetitions = repetitions;
        DurationSeconds = durationSeconds;
    }

    internal void Complete(
        ExerciseLoggingMode loggingMode,
        DateTimeOffset completedAtUtc,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        if (completedAtUtc == default)
        {
            throw new ArgumentException("Completion time is required.", nameof(completedAtUtc));
        }

        ValidateMeasurements(weightKg, repetitions, durationSeconds);
        ValidateCompletedMeasurements(loggingMode, weightKg, repetitions, durationSeconds);

        WeightKg = weightKg;
        Repetitions = repetitions;
        DurationSeconds = durationSeconds;
        CompletedAtUtc = completedAtUtc;
    }

    private static void ValidateMeasurements(decimal? weightKg, int? repetitions, int? durationSeconds)
    {
        if (weightKg is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg));
        }

        if (repetitions is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repetitions));
        }

        if (durationSeconds is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }
    }

    private static void ValidateCompletedMeasurements(
        ExerciseLoggingMode loggingMode,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        if (!Enum.IsDefined(loggingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(loggingMode));
        }

        var requiresWeight = loggingMode is ExerciseLoggingMode.WeightAndReps
            or ExerciseLoggingMode.AddedWeightAndReps
            or ExerciseLoggingMode.AssistedWeightAndReps
            or ExerciseLoggingMode.WeightAndDuration;
        var requiresRepetitions = loggingMode is ExerciseLoggingMode.WeightAndReps
            or ExerciseLoggingMode.BodyweightAndReps
            or ExerciseLoggingMode.AddedWeightAndReps
            or ExerciseLoggingMode.AssistedWeightAndReps
            or ExerciseLoggingMode.RepsOnly;
        var requiresDuration = loggingMode is ExerciseLoggingMode.Duration
            or ExerciseLoggingMode.WeightAndDuration;

        if (requiresWeight != (weightKg is not null) || (requiresWeight && weightKg <= 0))
        {
            throw new ArgumentException("Completed weight does not match the logging mode.", nameof(weightKg));
        }

        if (requiresRepetitions != (repetitions is not null) || (requiresRepetitions && repetitions <= 0))
        {
            throw new ArgumentException("Completed repetitions do not match the logging mode.", nameof(repetitions));
        }

        if (requiresDuration != (durationSeconds is not null) || (requiresDuration && durationSeconds <= 0))
        {
            throw new ArgumentException("Completed duration does not match the logging mode.", nameof(durationSeconds));
        }
    }
}
