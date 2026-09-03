using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Entities;

public sealed class WorkoutSession : UserOwnedEntity
{
    public const long InitialVersion = 1;

    private readonly List<WorkoutSessionExercise> _exercises = [];

    private WorkoutSession()
    {
    }

    public WorkoutSession(
        Guid id,
        Guid userId,
        string nameSnapshot,
        DateOnly workoutDate,
        DateTimeOffset startedAtUtc,
        Guid? originTemplateId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Workout session ID cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(nameSnapshot))
        {
            throw new ArgumentException("Workout session name snapshot is required.", nameof(nameSnapshot));
        }

        if (startedAtUtc == default)
        {
            throw new ArgumentException("Workout start time is required.", nameof(startedAtUtc));
        }

        if (originTemplateId == Guid.Empty)
        {
            throw new ArgumentException("Origin template ID cannot be empty.", nameof(originTemplateId));
        }

        Id = id;
        UserId = userId;
        NameSnapshot = nameSnapshot.Trim();
        WorkoutDate = workoutDate;
        StartedAtUtc = startedAtUtc;
        OriginTemplateId = originTemplateId;
        Status = WorkoutSessionStatus.InProgress;
        Version = InitialVersion;
    }

    public Guid? OriginTemplateId { get; private set; }

    public string NameSnapshot { get; private set; } = string.Empty;

    public DateOnly WorkoutDate { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public WorkoutSessionStatus Status { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset? DiscardedAtUtc { get; private set; }

    public SessionFeeling? SessionFeeling { get; private set; }

    public long Version { get; private set; }

    public int? RestTimerDurationSeconds { get; private set; }

    public DateTimeOffset? RestTimerEndsAtUtc { get; private set; }

    public int? RestTimerPausedRemainingSeconds { get; private set; }

    public IReadOnlyList<WorkoutSessionExercise> Exercises => _exercises;

    public WorkoutSessionExercise AddExercise(
        Guid exerciseId,
        string exerciseNameSnapshot,
        ExerciseLoggingMode loggingModeSnapshot,
        int targetSetCountSnapshot,
        int? targetRepMinSnapshot,
        int? targetRepMaxSnapshot,
        int defaultRestSecondsSnapshot)
    {
        EnsureInProgress();
        var exercise = new WorkoutSessionExercise(
            Guid.NewGuid(),
            UserId,
            Id,
            _exercises.Count + 1,
            exerciseId,
            exerciseNameSnapshot,
            loggingModeSnapshot,
            targetSetCountSnapshot,
            targetRepMinSnapshot,
            targetRepMaxSnapshot,
            defaultRestSecondsSnapshot);
        _exercises.Add(exercise);
        IncrementVersion();
        return exercise;
    }

    public void RemoveExercise(Guid sessionExerciseId)
    {
        EnsureInProgress();
        _exercises.Remove(FindExercise(sessionExerciseId));
        NormalizeExerciseOrdering();
        IncrementVersion();
    }

    public void SubstituteExercise(
        Guid sessionExerciseId,
        Guid exerciseId,
        string exerciseNameSnapshot,
        ExerciseLoggingMode loggingMode)
    {
        EnsureInProgress();
        FindExercise(sessionExerciseId).Substitute(exerciseId, exerciseNameSnapshot, loggingMode);
        IncrementVersion();
    }

    public void SkipExercise(Guid sessionExerciseId)
    {
        EnsureInProgress();
        FindExercise(sessionExerciseId).Skip();
        IncrementVersion();
    }

    public void UnskipExercise(Guid sessionExerciseId)
    {
        EnsureInProgress();
        FindExercise(sessionExerciseId).Unskip();
        IncrementVersion();
    }

    public WorkoutSet AddSet(
        Guid sessionExerciseId,
        WorkoutSetKind kind,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        EnsureInProgress();
        var set = FindExercise(sessionExerciseId).AddSet(kind, weightKg, repetitions, durationSeconds);
        IncrementVersion();
        return set;
    }

    public void UpdateSet(
        Guid sessionExerciseId,
        Guid setId,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        EnsureInProgress();
        FindExercise(sessionExerciseId).UpdateSet(setId, weightKg, repetitions, durationSeconds);
        IncrementVersion();
    }

    public void RemoveSet(Guid sessionExerciseId, Guid setId)
    {
        EnsureInProgress();
        FindExercise(sessionExerciseId).RemoveSet(setId);
        IncrementVersion();
    }

    public void CompleteSet(
        Guid sessionExerciseId,
        Guid setId,
        DateTimeOffset completedAtUtc,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        EnsureInProgress();
        FindExercise(sessionExerciseId).CompleteSet(setId, completedAtUtc, weightKg, repetitions, durationSeconds);
        IncrementVersion();
    }

    public void CompleteSetAndMaybeStartRest(
        Guid sessionExerciseId,
        Guid setId,
        DateTimeOffset completedAtUtc,
        decimal? weightKg,
        int? repetitions,
        int? durationSeconds,
        bool startRestTimer)
    {
        EnsureInProgress();
        var exercise = FindExercise(sessionExerciseId);
        exercise.CompleteSet(setId, completedAtUtc, weightKg, repetitions, durationSeconds);
        if (startRestTimer && exercise.DefaultRestSecondsSnapshot > 0)
        {
            SetRunningTimer(exercise.DefaultRestSecondsSnapshot, completedAtUtc);
        }

        IncrementVersion();
    }

    public void StartRestTimer(int durationSeconds, DateTimeOffset nowUtc)
    {
        EnsureInProgress();
        SetRunningTimer(durationSeconds, nowUtc);
        IncrementVersion();
    }

    public void PauseRestTimer(DateTimeOffset nowUtc)
    {
        EnsureInProgress();
        if (RestTimerDurationSeconds is null || RestTimerEndsAtUtc is null)
        {
            throw new InvalidOperationException("A running rest timer is required.");
        }

        var remaining = RemainingSeconds(RestTimerEndsAtUtc.Value, nowUtc);
        if (remaining == 0)
        {
            ClearTimer();
        }
        else
        {
            RestTimerPausedRemainingSeconds = remaining;
            RestTimerEndsAtUtc = null;
        }

        IncrementVersion();
    }

    public void ResumeRestTimer(DateTimeOffset nowUtc)
    {
        EnsureInProgress();
        if (RestTimerDurationSeconds is null || RestTimerPausedRemainingSeconds is null)
        {
            throw new InvalidOperationException("A paused rest timer is required.");
        }

        RestTimerEndsAtUtc = RequireTime(nowUtc).AddSeconds(RestTimerPausedRemainingSeconds.Value);
        RestTimerPausedRemainingSeconds = null;
        IncrementVersion();
    }

    public void ResetRestTimer(DateTimeOffset nowUtc)
    {
        EnsureInProgress();
        if (RestTimerDurationSeconds is null)
        {
            throw new InvalidOperationException("A configured rest timer is required.");
        }

        RestTimerEndsAtUtc = RequireTime(nowUtc).AddSeconds(RestTimerDurationSeconds.Value);
        RestTimerPausedRemainingSeconds = null;
        IncrementVersion();
    }

    public void AdjustRestTimer(int durationSeconds, DateTimeOffset nowUtc)
    {
        EnsureInProgress();
        if (durationSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        if (RestTimerEndsAtUtc is not null)
        {
            RestTimerEndsAtUtc = RequireTime(nowUtc).AddSeconds(durationSeconds);
            RestTimerPausedRemainingSeconds = null;
        }
        else if (RestTimerPausedRemainingSeconds is not null)
        {
            RestTimerPausedRemainingSeconds = durationSeconds;
        }
        else
        {
            throw new InvalidOperationException("An active rest timer is required.");
        }

        RestTimerDurationSeconds = durationSeconds;
        IncrementVersion();
    }

    public void ClearRestTimer()
    {
        EnsureInProgress();
        if (RestTimerDurationSeconds is null && RestTimerEndsAtUtc is null && RestTimerPausedRemainingSeconds is null)
        {
            throw new InvalidOperationException("An active rest timer is required.");
        }

        ClearTimer();
        IncrementVersion();
    }

    public void Complete(DateTimeOffset completedAtUtc, SessionFeeling? feeling = null)
    {
        EnsureInProgress();
        var completedSets = _exercises.SelectMany(exercise => exercise.Sets).Where(set => set.IsCompleted).ToArray();
        if (completedSets.Length == 0)
        {
            throw new InvalidOperationException("A workout requires at least one completed set.");
        }

        foreach (var exercise in _exercises)
        {
            exercise.RemoveDraftSets();
            if (!exercise.Sets.Any(set => set.IsCompleted))
            {
                exercise.Skip();
            }
        }

        CompletedAtUtc = RequireTime(completedAtUtc);
        DiscardedAtUtc = null;
        SessionFeeling = feeling;
        Status = WorkoutSessionStatus.Completed;
        ClearTimer();
        IncrementVersion();
    }

    public void Discard(DateTimeOffset discardedAtUtc)
    {
        EnsureInProgress();
        DiscardedAtUtc = RequireTime(discardedAtUtc);
        CompletedAtUtc = null;
        Status = WorkoutSessionStatus.Discarded;
        ClearTimer();
        IncrementVersion();
    }

    private void SetRunningTimer(int durationSeconds, DateTimeOffset nowUtc)
    {
        if (durationSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        RestTimerDurationSeconds = durationSeconds;
        RestTimerEndsAtUtc = RequireTime(nowUtc).AddSeconds(durationSeconds);
        RestTimerPausedRemainingSeconds = null;
    }

    private void ClearTimer()
    {
        RestTimerDurationSeconds = null;
        RestTimerEndsAtUtc = null;
        RestTimerPausedRemainingSeconds = null;
    }

    private WorkoutSessionExercise FindExercise(Guid sessionExerciseId) =>
        _exercises.SingleOrDefault(exercise => exercise.Id == sessionExerciseId)
        ?? throw new KeyNotFoundException("Session exercise was not found.");

    private void NormalizeExerciseOrdering()
    {
        for (var index = 0; index < _exercises.Count; index++)
        {
            _exercises[index].SetSortOrder(index + 1);
        }
    }

    private void EnsureInProgress()
    {
        if (Status != WorkoutSessionStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress workout can be changed.");
        }
    }

    private void IncrementVersion()
    {
        checked
        {
            Version++;
        }
    }

    private static DateTimeOffset RequireTime(DateTimeOffset value)
    {
        if (value == default)
        {
            throw new ArgumentException("A UTC timestamp is required.", nameof(value));
        }

        return value;
    }

    private static int RemainingSeconds(DateTimeOffset endsAtUtc, DateTimeOffset nowUtc)
    {
        var remaining = (endsAtUtc - RequireTime(nowUtc)).TotalSeconds;
        return remaining <= 0 ? 0 : (int)Math.Ceiling(remaining);
    }
}
