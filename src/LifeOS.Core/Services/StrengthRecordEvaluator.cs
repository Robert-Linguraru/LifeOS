using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Services;

public enum StrengthRecordType
{
    HeaviestWeight = 0,
    BestRepsAtWeight = 1,
    MostReps = 2,
    LongestDuration = 3,
    LongestDurationAtWeight = 4
}

public sealed record StrengthSetEvidence(
    ExerciseLoggingMode LoggingMode,
    WorkoutSetKind Kind,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    DateTimeOffset? CompletedAtUtc)
{
    public bool IsCompleted => CompletedAtUtc is not null;
}

public sealed record StrengthRecordAchievement(
    StrengthRecordType Type,
    bool IsFirstRecord,
    decimal? WeightKg,
    int? Repetitions,
    int? DurationSeconds,
    decimal? PreviousWeightKg,
    int? PreviousRepetitions,
    int? PreviousDurationSeconds);

public static class StrengthRecordEvaluator
{
    public static IReadOnlyList<StrengthRecordAchievement> Evaluate(
        StrengthSetEvidence candidate,
        IEnumerable<StrengthSetEvidence> priorEvidence)
    {
        ArgumentNullException.ThrowIfNull(priorEvidence);

        if (candidate.Kind != WorkoutSetKind.Working || !candidate.IsCompleted || !IsValid(candidate))
        {
            return [];
        }

        var prior = priorEvidence
            .Where(evidence => evidence.Kind == WorkoutSetKind.Working
                && evidence.IsCompleted
                && evidence.LoggingMode == candidate.LoggingMode
                && IsValid(evidence))
            .ToArray();

        return candidate.LoggingMode switch
        {
            ExerciseLoggingMode.WeightAndReps => EvaluateWeightAndReps(candidate, prior),
            ExerciseLoggingMode.BodyweightAndReps => EvaluateMostReps(candidate, prior),
            ExerciseLoggingMode.AddedWeightAndReps => EvaluateWeightAndReps(candidate, prior),
            ExerciseLoggingMode.AssistedWeightAndReps => [],
            ExerciseLoggingMode.RepsOnly => EvaluateMostReps(candidate, prior),
            ExerciseLoggingMode.Duration => EvaluateDuration(candidate, prior),
            ExerciseLoggingMode.WeightAndDuration => EvaluateWeightAndDuration(candidate, prior),
            _ => []
        };
    }

    private static IReadOnlyList<StrengthRecordAchievement> EvaluateWeightAndReps(
        StrengthSetEvidence candidate,
        IReadOnlyList<StrengthSetEvidence> prior)
    {
        var candidateWeight = candidate.WeightKg!.Value;
        decimal? heaviest = prior.Count == 0 ? null : prior.Max(evidence => evidence.WeightKg!.Value);
        var isNewWeight = heaviest is null || candidateWeight > heaviest.Value;
        var achievements = new List<StrengthRecordAchievement>();

        if (isNewWeight)
        {
            achievements.Add(new(
                StrengthRecordType.HeaviestWeight,
                heaviest is null,
                candidateWeight,
                candidate.Repetitions,
                null,
                heaviest,
                null,
                null));

            return achievements;
        }

        var atWeight = prior.Where(evidence => evidence.WeightKg == candidateWeight).ToArray();
        int? bestReps = atWeight.Length == 0 ? null : atWeight.Max(evidence => evidence.Repetitions!.Value);
        if (bestReps is null || candidate.Repetitions > bestReps.Value)
        {
            achievements.Add(new(
                StrengthRecordType.BestRepsAtWeight,
                bestReps is null,
                candidateWeight,
                candidate.Repetitions,
                null,
                candidateWeight,
                bestReps,
                null));
        }

        return achievements;
    }

    private static IReadOnlyList<StrengthRecordAchievement> EvaluateMostReps(
        StrengthSetEvidence candidate,
        IReadOnlyList<StrengthSetEvidence> prior)
    {
        int? previous = prior.Count == 0 ? null : prior.Max(evidence => evidence.Repetitions!.Value);
        if (previous is not null && candidate.Repetitions <= previous.Value)
        {
            return [];
        }

        return
        [
            new(
                StrengthRecordType.MostReps,
                previous is null,
                null,
                candidate.Repetitions,
                null,
                null,
                previous,
                null)
        ];
    }

    private static IReadOnlyList<StrengthRecordAchievement> EvaluateDuration(
        StrengthSetEvidence candidate,
        IReadOnlyList<StrengthSetEvidence> prior)
    {
        int? previous = prior.Count == 0 ? null : prior.Max(evidence => evidence.DurationSeconds!.Value);
        if (previous is not null && candidate.DurationSeconds <= previous.Value)
        {
            return [];
        }

        return
        [
            new(
                StrengthRecordType.LongestDuration,
                previous is null,
                null,
                null,
                candidate.DurationSeconds,
                null,
                null,
                previous)
        ];
    }

    private static IReadOnlyList<StrengthRecordAchievement> EvaluateWeightAndDuration(
        StrengthSetEvidence candidate,
        IReadOnlyList<StrengthSetEvidence> prior)
    {
        var candidateWeight = candidate.WeightKg!.Value;
        decimal? heaviest = prior.Count == 0 ? null : prior.Max(evidence => evidence.WeightKg!.Value);
        if (heaviest is null || candidateWeight > heaviest.Value)
        {
            return
            [
                new(
                    StrengthRecordType.HeaviestWeight,
                    heaviest is null,
                    candidateWeight,
                    null,
                    candidate.DurationSeconds,
                    heaviest,
                    null,
                    null)
            ];
        }

        var atWeight = prior.Where(evidence => evidence.WeightKg == candidateWeight).ToArray();
        int? previousDuration = atWeight.Length == 0 ? null : atWeight.Max(evidence => evidence.DurationSeconds!.Value);
        if (previousDuration is null || candidate.DurationSeconds > previousDuration.Value)
        {
            return
            [
                new(
                    StrengthRecordType.LongestDurationAtWeight,
                    previousDuration is null,
                    candidateWeight,
                    null,
                    candidate.DurationSeconds,
                    candidateWeight,
                    null,
                    previousDuration)
            ];
        }

        return [];
    }

    private static bool IsValid(StrengthSetEvidence evidence)
    {
        var hasWeight = evidence.WeightKg is > 0;
        var hasRepetitions = evidence.Repetitions is > 0;
        var hasDuration = evidence.DurationSeconds is > 0;

        return evidence.LoggingMode switch
        {
            ExerciseLoggingMode.WeightAndReps or ExerciseLoggingMode.AddedWeightAndReps or ExerciseLoggingMode.AssistedWeightAndReps
                => hasWeight && hasRepetitions && !hasDuration,
            ExerciseLoggingMode.BodyweightAndReps or ExerciseLoggingMode.RepsOnly
                => !hasWeight && hasRepetitions && !hasDuration,
            ExerciseLoggingMode.Duration
                => !hasWeight && !hasRepetitions && hasDuration,
            ExerciseLoggingMode.WeightAndDuration
                => hasWeight && !hasRepetitions && hasDuration,
            _ => false
        };
    }
}
