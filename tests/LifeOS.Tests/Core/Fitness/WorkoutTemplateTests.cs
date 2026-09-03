using LifeOS.Core.DTOs.WorkoutTemplates;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;

namespace LifeOS.Tests.Core.Fitness;

public sealed class WorkoutTemplateTests
{
    private static readonly Guid UserId = Guid.Parse("20000000-0000-4000-8000-000000000001");
    private static readonly Guid ExerciseId = Guid.Parse("30000000-0000-4000-8000-000000000001");

    [Fact]
    public void Template_ShouldRequireOwnerAndNameAndStartAtInitialVersion()
    {
        var template = new WorkoutTemplate(Guid.NewGuid(), UserId, "  Push Day  ");

        Assert.Equal(UserId, template.UserId);
        Assert.Equal("Push Day", template.Name);
        Assert.Equal(WorkoutTemplate.InitialVersion, template.Version);
        Assert.Empty(template.Exercises);
        Assert.Throws<InvalidOperationException>(() => template.ValidateForPersistence());

        template.Rename("  Pull Day  ");
        Assert.Equal("Pull Day", template.Name);
        Assert.Equal(WorkoutTemplate.InitialVersion + 1, template.Version);

        Assert.Throws<ArgumentException>(() => new WorkoutTemplate(Guid.NewGuid(), Guid.Empty, "Push Day"));
        Assert.Throws<ArgumentException>(() => new WorkoutTemplate(Guid.NewGuid(), UserId, "  "));
        Assert.Throws<ArgumentException>(() => new WorkoutTemplate(Guid.NewGuid(), UserId, new string('x', WorkoutTemplate.MaximumNameLength + 1)));
    }

    [Fact]
    public void Template_ShouldAllowDuplicateExercisesAndMaintainContiguousOrdering()
    {
        var template = CreateTemplate();
        var first = AddExercise(template, ExerciseId);
        var second = AddExercise(template, ExerciseId);
        var third = AddExercise(template, Guid.NewGuid());

        Assert.Equal([1, 2, 3], template.Exercises.Select(exercise => exercise.SortOrder));
        Assert.Equal(WorkoutTemplate.InitialVersion + 3, template.Version);

        template.ReorderExercises([third.Id, first.Id, second.Id]);
        Assert.Equal([third.Id, first.Id, second.Id], template.Exercises.Select(exercise => exercise.Id));
        Assert.Equal([1, 2, 3], template.Exercises.Select(exercise => exercise.SortOrder));
        template.ValidateForPersistence();

        template.RemoveExercise(first.Id);
        Assert.Equal([1, 2], template.Exercises.Select(exercise => exercise.SortOrder));
        Assert.Equal(2, template.Exercises.Count);
    }

    [Fact]
    public void Template_ShouldRejectInvalidReorders()
    {
        var template = CreateTemplate();
        var first = AddExercise(template, ExerciseId);
        var second = AddExercise(template, Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => template.ReorderExercises([first.Id]));
        Assert.Throws<ArgumentException>(() => template.ReorderExercises([first.Id, first.Id]));
        Assert.Throws<ArgumentException>(() => template.ReorderExercises([first.Id, Guid.NewGuid()]));
        Assert.Equal([first.Id, second.Id], template.Exercises.Select(exercise => exercise.Id));
    }

    [Theory]
    [InlineData(ExerciseLoggingMode.WeightAndReps)]
    [InlineData(ExerciseLoggingMode.BodyweightAndReps)]
    [InlineData(ExerciseLoggingMode.AddedWeightAndReps)]
    [InlineData(ExerciseLoggingMode.AssistedWeightAndReps)]
    [InlineData(ExerciseLoggingMode.RepsOnly)]
    public void TemplateExercise_ShouldAllowRepRangesForRepBasedModes(ExerciseLoggingMode loggingMode)
    {
        var template = CreateTemplate();
        var exercise = template.AddExercise(ExerciseId, loggingMode, 3, 6, 10, 90);

        Assert.Equal(3, exercise.TargetSetCount);
        Assert.Equal(6, exercise.TargetRepMin);
        Assert.Equal(10, exercise.TargetRepMax);
        Assert.Equal(90, exercise.DefaultRestSeconds);
        template.ValidateForPersistence();
    }

    [Theory]
    [InlineData(ExerciseLoggingMode.Duration)]
    [InlineData(ExerciseLoggingMode.WeightAndDuration)]
    public void TemplateExercise_ShouldRejectRepRangesForDurationModes(ExerciseLoggingMode loggingMode)
    {
        var template = CreateTemplate();

        Assert.Throws<ArgumentException>(() => template.AddExercise(ExerciseId, loggingMode, 3, 6, 10, 0));
    }

    [Fact]
    public void TemplateExercise_ShouldValidateTargetsAndRest()
    {
        var template = CreateTemplate();

        Assert.Throws<ArgumentOutOfRangeException>(() => template.AddExercise(ExerciseId, ExerciseLoggingMode.RepsOnly, 0, null, null, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => template.AddExercise(ExerciseId, ExerciseLoggingMode.RepsOnly, 1, null, null, -1));
        Assert.Throws<ArgumentException>(() => template.AddExercise(ExerciseId, ExerciseLoggingMode.RepsOnly, 1, 6, null, 0));
        Assert.Throws<ArgumentException>(() => template.AddExercise(ExerciseId, ExerciseLoggingMode.RepsOnly, 1, null, 8, 0));
        Assert.Throws<ArgumentException>(() => template.AddExercise(ExerciseId, ExerciseLoggingMode.RepsOnly, 1, 10, 6, 0));
        Assert.Throws<ArgumentException>(() => template.AddExercise(ExerciseId, ExerciseLoggingMode.RepsOnly, 1, 0, 6, 0));

        var noRest = template.AddExercise(ExerciseId, ExerciseLoggingMode.RepsOnly, 1, null, null, 0);
        Assert.Equal(0, noRest.DefaultRestSeconds);
    }

    [Fact]
    public void MutationDtos_ShouldNotAcceptUserIdAndShouldCarryExpectedVersions()
    {
        var mutationTypes = new[]
        {
            typeof(RenameWorkoutTemplateDto),
            typeof(AddWorkoutTemplateExerciseDto),
            typeof(UpdateWorkoutTemplateExerciseDto),
            typeof(ReorderWorkoutTemplateExercisesDto)
        };

        Assert.All(mutationTypes, type =>
        {
            Assert.Null(type.GetProperty("UserId"));
            Assert.NotNull(type.GetProperty("ExpectedVersion"));
        });

        Assert.NotNull(typeof(WorkoutTemplateDetailDto).GetProperty("Version"));
        Assert.DoesNotContain(typeof(WorkoutTemplateExercise), typeof(WorkoutTemplateDetailDto).GetProperties().Select(property => property.PropertyType));
    }

    private static WorkoutTemplate CreateTemplate() =>
        new(Guid.NewGuid(), UserId, "Push Day");

    private static WorkoutTemplateExercise AddExercise(WorkoutTemplate template, Guid exerciseId) =>
        template.AddExercise(exerciseId, ExerciseLoggingMode.WeightAndReps, 3, 6, 10, 90);
}
