using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Core.Entities;

public sealed class ExerciseSecondaryMuscleGroup
{
    private ExerciseSecondaryMuscleGroup()
    {
    }

    public ExerciseSecondaryMuscleGroup(MuscleGroup muscleGroup)
    {
        if (!Enum.IsDefined(muscleGroup))
        {
            throw new ArgumentOutOfRangeException(nameof(muscleGroup));
        }

        MuscleGroup = muscleGroup;
    }

    public MuscleGroup MuscleGroup { get; private set; }
}
