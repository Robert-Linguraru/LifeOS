using LifeOS.Core.Constants;
using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Tests.Core.Fitness;

public sealed class ExerciseCatalogTests
{
    [Fact]
    public void Defaults_ShouldSatisfyCatalogInvariants()
    {
        var exercises = ExerciseDefaults.All;

        Assert.InRange(exercises.Count, 80, 120);
        Assert.All(exercises, exercise =>
        {
            Assert.NotEqual(Guid.Empty, exercise.Id);
            Assert.False(string.IsNullOrWhiteSpace(exercise.Name));
            Assert.True(exercise.IsActive);
            Assert.True(Enum.IsDefined(exercise.PrimaryMuscleGroup));
            Assert.True(Enum.IsDefined(exercise.Equipment));
            Assert.True(Enum.IsDefined(exercise.MovementPattern));
            Assert.True(Enum.IsDefined(exercise.LoggingMode));
            var secondary = exercise.SecondaryMuscleGroups.Select(group => group.MuscleGroup).ToArray();
            Assert.Equal(secondary.Length, secondary.Distinct().Count());
            Assert.DoesNotContain(exercise.PrimaryMuscleGroup, secondary);
        });

        Assert.Equal(exercises.Count, exercises.Select(exercise => exercise.Id).Distinct().Count());
        Assert.Equal(exercises.Count, exercises.Select(exercise => exercise.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(exercises.Count, exercises.Select(exercise => exercise.SortOrder).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, exercises.Count), exercises.OrderBy(exercise => exercise.SortOrder).Select(exercise => exercise.SortOrder));
        Assert.Equal(7, exercises.Select(exercise => exercise.LoggingMode).Distinct().Count());
    }

    [Fact]
    public void Defaults_ShouldContainRepresentativeLoggingModesAndTrainingStyles()
    {
        var exercises = ExerciseDefaults.All.ToDictionary(exercise => exercise.Name, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(ExerciseLoggingMode.WeightAndReps, exercises["Barbell Bench Press"].LoggingMode);
        Assert.Equal(ExerciseLoggingMode.BodyweightAndReps, exercises["Pull-Up"].LoggingMode);
        Assert.Equal(ExerciseLoggingMode.AddedWeightAndReps, exercises["Weighted Pull-Up"].LoggingMode);
        Assert.Equal(ExerciseLoggingMode.AssistedWeightAndReps, exercises["Assisted Pull-Up"].LoggingMode);
        Assert.Equal(ExerciseLoggingMode.Duration, exercises["Plank"].LoggingMode);
        Assert.Contains(ExerciseLoggingMode.WeightAndDuration, exercises.Values.Select(exercise => exercise.LoggingMode));
        Assert.Contains(exercises.Values, exercise => exercise.Equipment == ExerciseEquipment.Barbell);
        Assert.Contains(exercises.Values, exercise => exercise.Equipment == ExerciseEquipment.Bodyweight);
        Assert.Contains(exercises.Values, exercise => exercise.MovementPattern == MovementPattern.Skill);
        Assert.Contains(exercises.Values, exercise => exercise.Name == "L-Sit");
    }

    [Fact]
    public void FitnessEnums_ShouldKeepPersistedNumericContract()
    {
        Assert.Equal(7, Enum.GetValues<ExerciseLoggingMode>().Length);

        Assert.Equal(0, (int)MuscleGroup.Chest);
        Assert.Equal(1, (int)MuscleGroup.BackLats);
        Assert.Equal(11, (int)MuscleGroup.FullBody);

        Assert.Equal(0, (int)ExerciseEquipment.Barbell);
        Assert.Equal(10, (int)ExerciseEquipment.Other);

        Assert.Equal(0, (int)MovementPattern.HorizontalPush);
        Assert.Equal(11, (int)MovementPattern.Skill);

        Assert.Equal(0, (int)ExerciseLoggingMode.WeightAndReps);
        Assert.Equal(1, (int)ExerciseLoggingMode.BodyweightAndReps);
        Assert.Equal(2, (int)ExerciseLoggingMode.AddedWeightAndReps);
        Assert.Equal(3, (int)ExerciseLoggingMode.AssistedWeightAndReps);
        Assert.Equal(4, (int)ExerciseLoggingMode.RepsOnly);
        Assert.Equal(5, (int)ExerciseLoggingMode.Duration);
        Assert.Equal(6, (int)ExerciseLoggingMode.WeightAndDuration);
    }
}
