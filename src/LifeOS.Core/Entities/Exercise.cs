using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Entities;

public sealed class Exercise : BaseEntity
{
    private readonly List<ExerciseSecondaryMuscleGroup> _secondaryMuscleGroups = [];

    private Exercise()
    {
    }

    public Exercise(
        Guid id,
        string name,
        MuscleGroup primaryMuscleGroup,
        ExerciseEquipment equipment,
        MovementPattern movementPattern,
        ExerciseLoggingMode loggingMode,
        bool isActive,
        int sortOrder,
        IEnumerable<MuscleGroup>? secondaryMuscleGroups = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Exercise ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Exercise name is required.", nameof(name));
        }

        if (!Enum.IsDefined(primaryMuscleGroup))
        {
            throw new ArgumentOutOfRangeException(nameof(primaryMuscleGroup));
        }

        if (!Enum.IsDefined(equipment))
        {
            throw new ArgumentOutOfRangeException(nameof(equipment));
        }

        if (!Enum.IsDefined(movementPattern))
        {
            throw new ArgumentOutOfRangeException(nameof(movementPattern));
        }

        if (!Enum.IsDefined(loggingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(loggingMode));
        }

        if (sortOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        var secondary = (secondaryMuscleGroups ?? []).ToArray();
        if (secondary.Any(group => !Enum.IsDefined(group)))
        {
            throw new ArgumentOutOfRangeException(nameof(secondaryMuscleGroups));
        }

        if (secondary.Any(group => group == primaryMuscleGroup))
        {
            throw new ArgumentException("The primary muscle group cannot also be secondary.", nameof(secondaryMuscleGroups));
        }

        if (secondary.Distinct().Count() != secondary.Length)
        {
            throw new ArgumentException("Secondary muscle groups must be unique.", nameof(secondaryMuscleGroups));
        }

        Id = id;
        Name = name.Trim();
        PrimaryMuscleGroup = primaryMuscleGroup;
        Equipment = equipment;
        MovementPattern = movementPattern;
        LoggingMode = loggingMode;
        IsActive = isActive;
        SortOrder = sortOrder;
        _secondaryMuscleGroups.AddRange(secondary.Select(group => new ExerciseSecondaryMuscleGroup(group)));
    }

    public string Name { get; private set; } = string.Empty;

    public MuscleGroup PrimaryMuscleGroup { get; private set; }

    public ExerciseEquipment Equipment { get; private set; }

    public MovementPattern MovementPattern { get; private set; }

    public ExerciseLoggingMode LoggingMode { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<ExerciseSecondaryMuscleGroup> SecondaryMuscleGroups => _secondaryMuscleGroups;
}
