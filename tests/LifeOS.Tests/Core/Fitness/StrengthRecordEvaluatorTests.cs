using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Services;

namespace LifeOS.Tests.Core.Fitness;

public sealed class StrengthRecordEvaluatorTests
{
    private static readonly DateTimeOffset CompletedAt = new(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluator_ShouldIgnoreWarmUpsAndIncompleteCandidates()
    {
        var warmUp = Evidence(WorkoutSetKind.WarmUp, ExerciseLoggingMode.WeightAndReps, 100m, 5, null);
        var incomplete = new StrengthSetEvidence(ExerciseLoggingMode.RepsOnly, WorkoutSetKind.Working, null, 10, null, null);

        Assert.Empty(StrengthRecordEvaluator.Evaluate(warmUp, []));
        Assert.Empty(StrengthRecordEvaluator.Evaluate(incomplete, []));
    }

    [Fact]
    public void WeightAndReps_ShouldReturnFirstHeaviestOnlyForANewWeightAndBestRepsAtExistingWeight()
    {
        var first = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.WeightAndReps, 100m, 5, null);
        var firstAchievements = StrengthRecordEvaluator.Evaluate(first, []);
        Assert.Collection(firstAchievements, achievement =>
        {
            Assert.Equal(StrengthRecordType.HeaviestWeight, achievement.Type);
            Assert.True(achievement.IsFirstRecord);
        });

        var newWeight = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.WeightAndReps, 110m, 3, null);
        var newWeightAchievements = StrengthRecordEvaluator.Evaluate(newWeight, [first]);
        Assert.Single(newWeightAchievements);
        Assert.Equal(StrengthRecordType.HeaviestWeight, newWeightAchievements[0].Type);
        Assert.False(newWeightAchievements[0].IsFirstRecord);

        var moreReps = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.WeightAndReps, 100m, 6, null);
        var repAchievements = StrengthRecordEvaluator.Evaluate(moreReps, [first, newWeight]);
        Assert.Single(repAchievements);
        Assert.Equal(StrengthRecordType.BestRepsAtWeight, repAchievements[0].Type);
    }

    [Fact]
    public void Evaluator_ShouldRejectTiesAndIgnoreIrrelevantModes()
    {
        var prior = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.RepsOnly, null, 10, null);
        var tie = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.RepsOnly, null, 10, null);
        var irrelevant = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.WeightAndReps, 200m, 20, null);

        Assert.Empty(StrengthRecordEvaluator.Evaluate(tie, [prior]));
        var first = StrengthRecordEvaluator.Evaluate(tie, [irrelevant]);
        Assert.Single(first);
        Assert.True(first[0].IsFirstRecord);
    }

    [Theory]
    [InlineData(ExerciseLoggingMode.BodyweightAndReps, 12, StrengthRecordType.MostReps)]
    [InlineData(ExerciseLoggingMode.RepsOnly, 12, StrengthRecordType.MostReps)]
    [InlineData(ExerciseLoggingMode.Duration, 60, StrengthRecordType.LongestDuration)]
    public void Evaluator_ShouldEvaluateRepAndDurationModes(
        ExerciseLoggingMode mode,
        int value,
        StrengthRecordType expectedType)
    {
        var candidate = mode == ExerciseLoggingMode.Duration
            ? Evidence(WorkoutSetKind.Working, mode, null, null, value)
            : Evidence(WorkoutSetKind.Working, mode, null, value, null);

        var result = StrengthRecordEvaluator.Evaluate(candidate, []);

        Assert.Single(result);
        Assert.Equal(expectedType, result[0].Type);
        Assert.True(result[0].IsFirstRecord);
    }

    [Fact]
    public void Evaluator_ShouldUseAddedWeightAndReturnNoAssistedWeightRecords()
    {
        var prior = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.AddedWeightAndReps, 10m, 5, null);
        var candidate = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.AddedWeightAndReps, 10m, 6, null);
        var result = StrengthRecordEvaluator.Evaluate(candidate, [prior]);
        Assert.Single(result);
        Assert.Equal(StrengthRecordType.BestRepsAtWeight, result[0].Type);

        var assisted = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.AssistedWeightAndReps, 20m, 10, null);
        Assert.Empty(StrengthRecordEvaluator.Evaluate(assisted, []));
    }

    [Fact]
    public void Evaluator_ShouldEvaluateWeightAndDurationAtExactWeight()
    {
        var prior = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.WeightAndDuration, 50m, null, 30);
        var candidate = Evidence(WorkoutSetKind.Working, ExerciseLoggingMode.WeightAndDuration, 50m, null, 40);

        var result = StrengthRecordEvaluator.Evaluate(candidate, [prior]);

        Assert.Single(result);
        Assert.Equal(StrengthRecordType.LongestDurationAtWeight, result[0].Type);
        Assert.Equal(30, result[0].PreviousDurationSeconds);
    }

    [Fact]
    public void EnumContracts_ShouldRemainStable()
    {
        Assert.Equal(3, Enum.GetValues<WorkoutSessionStatus>().Length);
        Assert.Equal(0, (int)WorkoutSessionStatus.InProgress);
        Assert.Equal(1, (int)WorkoutSessionStatus.Completed);
        Assert.Equal(2, (int)WorkoutSessionStatus.Discarded);

        Assert.Equal(2, Enum.GetValues<WorkoutSetKind>().Length);
        Assert.Equal(0, (int)WorkoutSetKind.WarmUp);
        Assert.Equal(1, (int)WorkoutSetKind.Working);

        Assert.Equal(4, Enum.GetValues<SessionFeeling>().Length);
        Assert.Equal(0, (int)SessionFeeling.Weak);
        Assert.Equal(1, (int)SessionFeeling.Normal);
        Assert.Equal(2, (int)SessionFeeling.Good);
        Assert.Equal(3, (int)SessionFeeling.Great);
    }

    private static StrengthSetEvidence Evidence(
        WorkoutSetKind kind,
        ExerciseLoggingMode mode,
        decimal? weight,
        int? repetitions,
        int? duration) =>
        new(mode, kind, weight, repetitions, duration, CompletedAt);
}
