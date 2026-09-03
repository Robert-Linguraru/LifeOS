using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Tests.Core.Fitness;

public sealed class WorkoutSessionTests
{
    private static readonly Guid UserId = Guid.Parse("40000000-0000-4000-8000-000000000001");
    private static readonly Guid ExerciseId = Guid.Parse("50000000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Session_ShouldStartInProgressWithHistoricalSnapshot()
    {
        var session = CreateSession();
        var exercise = AddExercise(session);

        Assert.Equal(WorkoutSessionStatus.InProgress, session.Status);
        Assert.Equal(WorkoutSession.InitialVersion + 1, session.Version);
        Assert.Equal("Push Day", session.NameSnapshot);
        Assert.Equal(ExerciseId, exercise.OriginalExerciseId);
        Assert.Equal(exercise.OriginalExerciseId, exercise.ExerciseId);
        Assert.Equal(exercise.OriginalExerciseNameSnapshot, exercise.ExerciseNameSnapshot);
        Assert.Equal(ExerciseLoggingMode.WeightAndReps, exercise.LoggingModeSnapshot);
    }

    [Fact]
    public void Substitution_ShouldPreserveOriginalSnapshotAndRejectAnyExistingSet()
    {
        var session = CreateSession();
        var exercise = AddExercise(session);
        var replacementId = Guid.NewGuid();

        session.SubstituteExercise(exercise.Id, replacementId, "Dumbbell Bench Press", ExerciseLoggingMode.WeightAndReps);
        Assert.Equal(ExerciseId, exercise.OriginalExerciseId);
        Assert.Equal("Barbell Bench Press", exercise.OriginalExerciseNameSnapshot);
        Assert.Equal(replacementId, exercise.ExerciseId);
        Assert.Equal("Dumbbell Bench Press", exercise.ExerciseNameSnapshot);

        var set = session.AddSet(exercise.Id, WorkoutSetKind.Working, null, null, null);
        Assert.Throws<InvalidOperationException>(() => session.SubstituteExercise(
            exercise.Id,
            Guid.NewGuid(),
            "Incline Bench Press",
            ExerciseLoggingMode.WeightAndReps));
        Assert.Contains(set, exercise.Sets);
    }

    [Fact]
    public void Skip_ShouldPreserveExerciseAndRejectCompletedSets()
    {
        var session = CreateSession();
        var exercise = AddExercise(session);
        session.SkipExercise(exercise.Id);
        Assert.True(exercise.IsSkipped);
        Assert.Single(session.Exercises);

        session.UnskipExercise(exercise.Id);
        var set = session.AddSet(exercise.Id, WorkoutSetKind.Working, null, null, null);
        session.CompleteSet(exercise.Id, set.Id, Start.AddMinutes(1), 80m, 5, null);
        Assert.Throws<InvalidOperationException>(() => session.SkipExercise(exercise.Id));
    }

    [Theory]
    [InlineData(ExerciseLoggingMode.WeightAndReps, "80", 5, null)]
    [InlineData(ExerciseLoggingMode.BodyweightAndReps, null, 10, null)]
    [InlineData(ExerciseLoggingMode.AddedWeightAndReps, "20", 5, null)]
    [InlineData(ExerciseLoggingMode.AssistedWeightAndReps, "30", 5, null)]
    [InlineData(ExerciseLoggingMode.RepsOnly, null, 20, null)]
    [InlineData(ExerciseLoggingMode.Duration, null, null, 60)]
    [InlineData(ExerciseLoggingMode.WeightAndDuration, "40", null, 60)]
    public void CompleteSet_ShouldAcceptTheExactLoggingModeShape(
        ExerciseLoggingMode mode,
        string? weightKg,
        int? repetitions,
        int? durationSeconds)
    {
        var session = CreateSession();
        var exercise = AddExercise(session, mode);
        var set = session.AddSet(exercise.Id, WorkoutSetKind.Working, null, null, null);

        decimal? weight = weightKg is null ? null : decimal.Parse(weightKg);
        session.CompleteSet(exercise.Id, set.Id, Start.AddMinutes(1), weight, repetitions, durationSeconds);

        Assert.True(set.IsCompleted);
        Assert.Equal(weight, set.WeightKg);
        Assert.Equal(repetitions, set.Repetitions);
        Assert.Equal(durationSeconds, set.DurationSeconds);
    }

    [Fact]
    public void CompleteSet_ShouldRejectIncompatibleMeasurements()
    {
        var session = CreateSession();
        var exercise = AddExercise(session, ExerciseLoggingMode.Duration);
        var set = session.AddSet(exercise.Id, WorkoutSetKind.Working, null, null, null);

        Assert.Throws<ArgumentException>(() => session.CompleteSet(exercise.Id, set.Id, Start, 10m, null, 60));
        Assert.False(set.IsCompleted);

        var bodyweightExercise = AddExercise(session, ExerciseLoggingMode.BodyweightAndReps);
        var bodyweightSet = session.AddSet(bodyweightExercise.Id, WorkoutSetKind.Working, null, null, null);
        Assert.Throws<ArgumentException>(() => session.CompleteSet(bodyweightExercise.Id, bodyweightSet.Id, Start, 10m, 5, null));
    }

    [Fact]
    public void Sets_ShouldBeDraftUntilCompletedAndNormalizeAfterRemoval()
    {
        var session = CreateSession();
        var exercise = AddExercise(session);
        var first = session.AddSet(exercise.Id, WorkoutSetKind.WarmUp, 20m, 10, null);
        var second = session.AddSet(exercise.Id, WorkoutSetKind.Working, null, null, null);

        Assert.False(first.IsCompleted);
        session.RemoveSet(exercise.Id, first.Id);
        Assert.Equal(1, second.SortOrder);

        session.CompleteSet(exercise.Id, second.Id, Start, 80m, 5, null);
        session.UpdateSet(exercise.Id, second.Id, 82.5m, 4, null);
        Assert.Equal(82.5m, second.WeightKg);
        Assert.Equal(4, second.Repetitions);
        Assert.True(second.IsCompleted);
    }

    [Fact]
    public void Timer_ShouldPersistRunningPausedAndClearedStatesDeterministically()
    {
        var session = CreateSession();
        session.StartRestTimer(10, Start);
        Assert.Equal(10, session.RestTimerDurationSeconds);
        Assert.Equal(Start.AddSeconds(10), session.RestTimerEndsAtUtc);
        Assert.Null(session.RestTimerPausedRemainingSeconds);

        session.PauseRestTimer(Start.AddSeconds(3));
        Assert.Equal(7, session.RestTimerPausedRemainingSeconds);
        Assert.Null(session.RestTimerEndsAtUtc);

        session.ResumeRestTimer(Start.AddSeconds(20));
        Assert.Equal(Start.AddSeconds(27), session.RestTimerEndsAtUtc);
        session.AdjustRestTimer(5, Start.AddSeconds(21));
        Assert.Equal(Start.AddSeconds(26), session.RestTimerEndsAtUtc);
        session.ResetRestTimer(Start.AddSeconds(30));
        Assert.Equal(Start.AddSeconds(35), session.RestTimerEndsAtUtc);
        session.ClearRestTimer();
        Assert.Null(session.RestTimerDurationSeconds);
        Assert.Null(session.RestTimerEndsAtUtc);
        Assert.Null(session.RestTimerPausedRemainingSeconds);
    }

    [Fact]
    public void Timer_ShouldClearWhenPauseFindsExpiredTimer()
    {
        var session = CreateSession();
        session.StartRestTimer(10, Start);
        session.PauseRestTimer(Start.AddSeconds(11));

        Assert.Null(session.RestTimerDurationSeconds);
        Assert.Null(session.RestTimerEndsAtUtc);
        Assert.Null(session.RestTimerPausedRemainingSeconds);
    }

    [Fact]
    public void Completion_ShouldRequireEvidenceRemoveDraftsClearTimerAndBecomeTerminal()
    {
        var session = CreateSession();
        var exercise = AddExercise(session);
        var completed = session.AddSet(exercise.Id, WorkoutSetKind.Working, null, null, null);
        session.CompleteSet(exercise.Id, completed.Id, Start.AddMinutes(1), 80m, 5, null);
        session.AddSet(exercise.Id, WorkoutSetKind.WarmUp, 20m, 10, null);
        session.StartRestTimer(60, Start);

        session.Complete(Start.AddMinutes(30), SessionFeeling.Great);

        Assert.Equal(WorkoutSessionStatus.Completed, session.Status);
        Assert.Equal(Start.AddMinutes(30), session.CompletedAtUtc);
        Assert.Equal(SessionFeeling.Great, session.SessionFeeling);
        Assert.Single(exercise.Sets);
        Assert.Null(session.RestTimerDurationSeconds);
        Assert.Throws<InvalidOperationException>(() => session.AddSet(exercise.Id, WorkoutSetKind.Working, null, null, null));
        Assert.Throws<InvalidOperationException>(() => session.Discard(Start.AddMinutes(31)));
    }

    [Fact]
    public void Discard_ShouldWorkWithoutSetsAndBecomeTerminal()
    {
        var session = CreateSession();
        session.StartRestTimer(30, Start);
        session.Discard(Start.AddMinutes(5));

        Assert.Equal(WorkoutSessionStatus.Discarded, session.Status);
        Assert.Equal(Start.AddMinutes(5), session.DiscardedAtUtc);
        Assert.Null(session.CompletedAtUtc);
        Assert.Null(session.RestTimerDurationSeconds);
        Assert.Throws<InvalidOperationException>(() => session.Complete(Start.AddMinutes(6)));
    }

    private static WorkoutSession CreateSession() =>
        new(Guid.NewGuid(), UserId, "  Push Day  ", new DateOnly(2026, 1, 10), Start);

    private static WorkoutSessionExercise AddExercise(
        WorkoutSession session,
        ExerciseLoggingMode mode = ExerciseLoggingMode.WeightAndReps) =>
        session.AddExercise(
            ExerciseId,
            "Barbell Bench Press",
            mode,
            3,
            mode is ExerciseLoggingMode.Duration or ExerciseLoggingMode.WeightAndDuration ? null : 5,
            mode is ExerciseLoggingMode.Duration or ExerciseLoggingMode.WeightAndDuration ? null : 10,
            90);
}
