using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Entities;

public sealed class WorkoutTemplateExercise : UserOwnedEntity
{
    private WorkoutTemplateExercise()
    {
    }

    internal WorkoutTemplateExercise(
        Guid id,
        Guid userId,
        Guid workoutTemplateId,
        Guid exerciseId,
        int sortOrder,
        int targetSetCount,
        int? targetRepMin,
        int? targetRepMax,
        int defaultRestSeconds,
        ExerciseLoggingMode loggingMode)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Template exercise ID cannot be empty.", nameof(id));
        }

        ValidateUserId(userId);
        ValidateTemplateId(workoutTemplateId);
        ValidateExerciseId(exerciseId);
        ValidateConfiguration(sortOrder, targetSetCount, targetRepMin, targetRepMax, defaultRestSeconds, loggingMode);

        Id = id;
        UserId = userId;
        WorkoutTemplateId = workoutTemplateId;
        ExerciseId = exerciseId;
        SortOrder = sortOrder;
        TargetSetCount = targetSetCount;
        TargetRepMin = targetRepMin;
        TargetRepMax = targetRepMax;
        DefaultRestSeconds = defaultRestSeconds;
    }

    public Guid WorkoutTemplateId { get; private set; }

    public Guid ExerciseId { get; private set; }

    public int SortOrder { get; private set; }

    public int TargetSetCount { get; private set; }

    public int? TargetRepMin { get; private set; }

    public int? TargetRepMax { get; private set; }

    public int DefaultRestSeconds { get; private set; }

    internal void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        SortOrder = sortOrder;
    }

    internal void UpdateConfiguration(
        int targetSetCount,
        int? targetRepMin,
        int? targetRepMax,
        int defaultRestSeconds,
        ExerciseLoggingMode loggingMode)
    {
        ValidateConfiguration(
            SortOrder,
            targetSetCount,
            targetRepMin,
            targetRepMax,
            defaultRestSeconds,
            loggingMode);

        TargetSetCount = targetSetCount;
        TargetRepMin = targetRepMin;
        TargetRepMax = targetRepMax;
        DefaultRestSeconds = defaultRestSeconds;
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }
    }

    private static void ValidateTemplateId(Guid workoutTemplateId)
    {
        if (workoutTemplateId == Guid.Empty)
        {
            throw new ArgumentException("Workout template ID cannot be empty.", nameof(workoutTemplateId));
        }
    }

    private static void ValidateExerciseId(Guid exerciseId)
    {
        if (exerciseId == Guid.Empty)
        {
            throw new ArgumentException("Exercise ID cannot be empty.", nameof(exerciseId));
        }
    }

    private static void ValidateConfiguration(
        int sortOrder,
        int targetSetCount,
        int? targetRepMin,
        int? targetRepMax,
        int defaultRestSeconds,
        ExerciseLoggingMode loggingMode)
    {
        if (sortOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        if (targetSetCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSetCount));
        }

        if (defaultRestSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultRestSeconds));
        }

        if (!Enum.IsDefined(loggingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(loggingMode));
        }

        if (targetRepMin is null != targetRepMax is null)
        {
            throw new ArgumentException("Target rep minimum and maximum must both be present or absent.");
        }

        if (targetRepMin is not null && (targetRepMin < 1 || targetRepMax < 1 || targetRepMin > targetRepMax))
        {
            throw new ArgumentException("Target rep range is invalid.");
        }

        if ((targetRepMin is not null || targetRepMax is not null) && !IsRepBased(loggingMode))
        {
            throw new ArgumentException("Target rep ranges require a rep-based logging mode.", nameof(loggingMode));
        }
    }

    private static bool IsRepBased(ExerciseLoggingMode loggingMode) =>
        loggingMode is ExerciseLoggingMode.WeightAndReps
            or ExerciseLoggingMode.BodyweightAndReps
            or ExerciseLoggingMode.AddedWeightAndReps
            or ExerciseLoggingMode.AssistedWeightAndReps
            or ExerciseLoggingMode.RepsOnly;
}
