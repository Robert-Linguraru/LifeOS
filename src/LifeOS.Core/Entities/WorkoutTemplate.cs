using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Entities;

public sealed class WorkoutTemplate : UserOwnedEntity
{
    public const long InitialVersion = 1;
    public const int MaximumNameLength = 200;

    private readonly List<WorkoutTemplateExercise> _exercises = [];

    private WorkoutTemplate()
    {
    }

    public WorkoutTemplate(Guid id, Guid userId, string name, long version = InitialVersion)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Workout template ID cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        Id = id;
        UserId = userId;
        Name = NormalizeName(name);
        Version = version > 0 ? version : throw new ArgumentOutOfRangeException(nameof(version));
    }

    public string Name { get; private set; } = string.Empty;

    public long Version { get; private set; } = InitialVersion;

    public IReadOnlyList<WorkoutTemplateExercise> Exercises => _exercises;

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        IncrementVersion();
    }

    public WorkoutTemplateExercise AddExercise(
        Guid exerciseId,
        ExerciseLoggingMode loggingMode,
        int targetSetCount,
        int? targetRepMin,
        int? targetRepMax,
        int defaultRestSeconds)
    {
        var templateExercise = new WorkoutTemplateExercise(
            Guid.NewGuid(),
            UserId,
            Id,
            exerciseId,
            _exercises.Count + 1,
            targetSetCount,
            targetRepMin,
            targetRepMax,
            defaultRestSeconds,
            loggingMode);

        _exercises.Add(templateExercise);
        IncrementVersion();
        return templateExercise;
    }

    public void RemoveExercise(Guid templateExerciseId)
    {
        var exercise = FindExercise(templateExerciseId);
        _exercises.Remove(exercise);
        NormalizeOrdering();
        IncrementVersion();
    }

    public void UpdateExercise(
        Guid templateExerciseId,
        ExerciseLoggingMode loggingMode,
        int targetSetCount,
        int? targetRepMin,
        int? targetRepMax,
        int defaultRestSeconds)
    {
        var exercise = FindExercise(templateExerciseId);
        exercise.UpdateConfiguration(targetSetCount, targetRepMin, targetRepMax, defaultRestSeconds, loggingMode);
        IncrementVersion();
    }

    public void ReorderExercises(IEnumerable<Guid> orderedExerciseIds)
    {
        ArgumentNullException.ThrowIfNull(orderedExerciseIds);

        var orderedIds = orderedExerciseIds.ToArray();
        if (orderedIds.Length != _exercises.Count || orderedIds.Distinct().Count() != orderedIds.Length)
        {
            throw new ArgumentException("Reorder must contain every template exercise exactly once.", nameof(orderedExerciseIds));
        }

        var currentIds = _exercises.Select(exercise => exercise.Id).ToHashSet();
        if (!orderedIds.All(currentIds.Contains))
        {
            throw new ArgumentException("Reorder contains an unknown template exercise.", nameof(orderedExerciseIds));
        }

        var byId = _exercises.ToDictionary(exercise => exercise.Id);
        _exercises.Clear();
        for (var index = 0; index < orderedIds.Length; index++)
        {
            var exercise = byId[orderedIds[index]];
            exercise.SetSortOrder(index + 1);
            _exercises.Add(exercise);
        }

        IncrementVersion();
    }

    public void ValidateForPersistence()
    {
        if (_exercises.Count == 0)
        {
            throw new InvalidOperationException("A usable workout template must contain at least one exercise.");
        }

        if (_exercises.Select(exercise => exercise.SortOrder).Distinct().Count() != _exercises.Count)
        {
            throw new InvalidOperationException("Workout template exercise order must be unique.");
        }
    }

    private WorkoutTemplateExercise FindExercise(Guid templateExerciseId) =>
        _exercises.SingleOrDefault(exercise => exercise.Id == templateExerciseId)
        ?? throw new KeyNotFoundException("Template exercise was not found.");

    private void NormalizeOrdering()
    {
        for (var index = 0; index < _exercises.Count; index++)
        {
            _exercises[index].SetSortOrder(index + 1);
        }
    }

    private void IncrementVersion()
    {
        checked
        {
            Version++;
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workout template name is required.", nameof(name));
        }

        var normalized = name.Trim();
        if (normalized.Length > MaximumNameLength)
        {
            throw new ArgumentException($"Workout template name cannot exceed {MaximumNameLength} characters.", nameof(name));
        }

        return normalized;
    }
}
