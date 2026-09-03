using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Entities;

public sealed class WorkoutSessionExercise : UserOwnedEntity
{
    private readonly List<WorkoutSet> _sets = [];

    private WorkoutSessionExercise()
    {
    }

    internal WorkoutSessionExercise(
        Guid id,
        Guid userId,
        Guid workoutSessionId,
        int sortOrder,
        Guid originalExerciseId,
        string originalExerciseNameSnapshot,
        ExerciseLoggingMode loggingModeSnapshot,
        int targetSetCountSnapshot,
        int? targetRepMinSnapshot,
        int? targetRepMaxSnapshot,
        int defaultRestSecondsSnapshot)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Session exercise ID cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (workoutSessionId == Guid.Empty)
        {
            throw new ArgumentException("Workout session ID cannot be empty.", nameof(workoutSessionId));
        }

        if (originalExerciseId == Guid.Empty)
        {
            throw new ArgumentException("Original exercise ID cannot be empty.", nameof(originalExerciseId));
        }

        if (string.IsNullOrWhiteSpace(originalExerciseNameSnapshot))
        {
            throw new ArgumentException("Original exercise name is required.", nameof(originalExerciseNameSnapshot));
        }

        ValidateSnapshotConfiguration(sortOrder, targetSetCountSnapshot, targetRepMinSnapshot, targetRepMaxSnapshot, defaultRestSecondsSnapshot, loggingModeSnapshot);

        Id = id;
        UserId = userId;
        WorkoutSessionId = workoutSessionId;
        SortOrder = sortOrder;
        OriginalExerciseId = originalExerciseId;
        OriginalExerciseNameSnapshot = originalExerciseNameSnapshot.Trim();
        ExerciseId = originalExerciseId;
        ExerciseNameSnapshot = OriginalExerciseNameSnapshot;
        LoggingModeSnapshot = loggingModeSnapshot;
        TargetSetCountSnapshot = targetSetCountSnapshot;
        TargetRepMinSnapshot = targetRepMinSnapshot;
        TargetRepMaxSnapshot = targetRepMaxSnapshot;
        DefaultRestSecondsSnapshot = defaultRestSecondsSnapshot;
    }

    public Guid WorkoutSessionId { get; private set; }

    public int SortOrder { get; private set; }

    public Guid OriginalExerciseId { get; private set; }

    public string OriginalExerciseNameSnapshot { get; private set; } = string.Empty;

    public Guid ExerciseId { get; private set; }

    public string ExerciseNameSnapshot { get; private set; } = string.Empty;

    public ExerciseLoggingMode LoggingModeSnapshot { get; private set; }

    public int TargetSetCountSnapshot { get; private set; }

    public int? TargetRepMinSnapshot { get; private set; }

    public int? TargetRepMaxSnapshot { get; private set; }

    public int DefaultRestSecondsSnapshot { get; private set; }

    public bool IsSkipped { get; private set; }

    public IReadOnlyList<WorkoutSet> Sets => _sets;

    internal void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        SortOrder = sortOrder;
    }

    internal void Substitute(Guid exerciseId, string exerciseNameSnapshot, ExerciseLoggingMode loggingMode)
    {
        if (_sets.Count > 0)
        {
            throw new InvalidOperationException("An exercise with sets cannot be substituted.");
        }

        if (exerciseId == Guid.Empty)
        {
            throw new ArgumentException("Exercise ID cannot be empty.", nameof(exerciseId));
        }

        if (string.IsNullOrWhiteSpace(exerciseNameSnapshot))
        {
            throw new ArgumentException("Exercise name is required.", nameof(exerciseNameSnapshot));
        }

        if (!Enum.IsDefined(loggingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(loggingMode));
        }

        ExerciseId = exerciseId;
        ExerciseNameSnapshot = exerciseNameSnapshot.Trim();
        LoggingModeSnapshot = loggingMode;
    }

    internal void Skip()
    {
        if (_sets.Any(set => set.IsCompleted))
        {
            throw new InvalidOperationException("An exercise with completed sets cannot be skipped.");
        }

        if (_sets.Count > 0)
        {
            throw new InvalidOperationException("Remove draft sets before skipping an exercise.");
        }

        IsSkipped = true;
    }

    internal void Unskip() => IsSkipped = false;

    internal WorkoutSet AddSet(
        WorkoutSetKind kind,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        if (IsSkipped)
        {
            throw new InvalidOperationException("An exercise must be unskipped before adding a set.");
        }

        var set = new WorkoutSet(
            Guid.NewGuid(),
            UserId,
            Id,
            _sets.Count + 1,
            kind,
            weightKg,
            repetitions,
            durationSeconds);
        _sets.Add(set);
        return set;
    }

    internal void UpdateSet(Guid setId, decimal? weightKg, int? repetitions, int? durationSeconds)
    {
        var set = FindSet(setId);
        set.UpdateMeasurements(LoggingModeSnapshot, weightKg, repetitions, durationSeconds);
    }

    internal void CompleteSet(Guid setId, DateTimeOffset completedAtUtc, decimal? weightKg, int? repetitions, int? durationSeconds)
    {
        var set = FindSet(setId);
        set.Complete(LoggingModeSnapshot, completedAtUtc, weightKg, repetitions, durationSeconds);
    }

    internal void RemoveSet(Guid setId)
    {
        _sets.Remove(FindSet(setId));
        NormalizeSetOrdering();
    }

    internal void RemoveDraftSets()
    {
        _sets.RemoveAll(set => !set.IsCompleted);
        NormalizeSetOrdering();
    }

    private WorkoutSet FindSet(Guid setId) =>
        _sets.SingleOrDefault(set => set.Id == setId)
        ?? throw new KeyNotFoundException("Workout set was not found.");

    private void NormalizeSetOrdering()
    {
        for (var index = 0; index < _sets.Count; index++)
        {
            _sets[index].SetSortOrder(index + 1);
        }
    }

    private static void ValidateSnapshotConfiguration(
        int sortOrder,
        int targetSetCount,
        int? targetRepMin,
        int? targetRepMax,
        int defaultRestSeconds,
        ExerciseLoggingMode loggingMode)
    {
        if (sortOrder < 1 || targetSetCount < 1 || defaultRestSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSetCount));
        }

        if (!Enum.IsDefined(loggingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(loggingMode));
        }

        if ((targetRepMin is null) != (targetRepMax is null)
            || (targetRepMin is not null && (targetRepMin < 1 || targetRepMax < 1 || targetRepMin > targetRepMax)))
        {
            throw new ArgumentException("Target rep snapshot is invalid.");
        }

        var repBased = loggingMode is ExerciseLoggingMode.WeightAndReps
            or ExerciseLoggingMode.BodyweightAndReps
            or ExerciseLoggingMode.AddedWeightAndReps
            or ExerciseLoggingMode.AssistedWeightAndReps
            or ExerciseLoggingMode.RepsOnly;
        if ((targetRepMin is not null || targetRepMax is not null) && !repBased)
        {
            throw new ArgumentException("Target rep snapshots require a rep-based logging mode.");
        }
    }
}
