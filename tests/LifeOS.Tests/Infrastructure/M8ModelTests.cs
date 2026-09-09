using LifeOS.Core.Abstractions;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Time;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LifeOS.Tests.Infrastructure.Persistence;

public sealed class M8ModelTests
{
    [Fact]
    public void Model_ShouldConfigureGlobalExerciseCatalogAndSecondaryMuscles()
    {
        using var context = CreateContext();
        var exercise = Entity(context, typeof(Exercise));
        var secondary = Entity(context, typeof(ExerciseSecondaryMuscleGroup));

        Assert.Equal("Exercises", exercise.GetTableName());
        Assert.Null(exercise.FindProperty("UserId"));
        Assert.Equal(200, exercise.FindProperty(nameof(Exercise.Name))!.GetMaxLength());
        Assert.Contains(exercise.GetIndexes(), index => index.IsUnique && Names(index).SequenceEqual([nameof(Exercise.Name)]));
        Assert.Equal(90, exercise.GetSeedData().Count());
        Assert.Contains(exercise.GetSeedData(), row =>
            row[nameof(Exercise.Id)]!.Equals(ExerciseDefaults.Definitions.Single(item => item.Name == "Pull-Up").Id));

        Assert.Equal("ExerciseSecondaryMuscleGroups", secondary.GetTableName());
        Assert.Equal(ExerciseDefaults.Definitions.Sum(item => item.SecondaryMuscleGroups.Count), secondary.GetSeedData().Count());
        Assert.Equal(["ExerciseId", nameof(ExerciseSecondaryMuscleGroup.MuscleGroup)], secondary.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Contains(secondary.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Exercise)
            && foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_ShouldConfigureTemplateOwnershipConcurrencyAndOrdering()
    {
        using var context = CreateContext();
        var template = Entity(context, typeof(WorkoutTemplate));
        var exercise = Entity(context, typeof(WorkoutTemplateExercise));

        Assert.True(template.FindProperty(nameof(WorkoutTemplate.UserId))!.IsNullable == false);
        Assert.True(template.FindProperty(nameof(WorkoutTemplate.Version))!.IsConcurrencyToken);
        Assert.Contains(template.GetKeys(), key => Names(key).SequenceEqual([nameof(WorkoutTemplate.Id), nameof(WorkoutTemplate.UserId)]));
        Assert.Contains(exercise.GetIndexes(), index =>
            index.IsUnique
            && Names(index).SequenceEqual([nameof(WorkoutTemplateExercise.WorkoutTemplateId), nameof(WorkoutTemplateExercise.SortOrder)])
            && index.GetFilter() == "\"IsDeleted\" = false");
        Assert.DoesNotContain(exercise.GetIndexes(), index => index.IsUnique && Names(index).SequenceEqual([nameof(WorkoutTemplateExercise.WorkoutTemplateId), nameof(WorkoutTemplateExercise.ExerciseId)]));
        Assert.Contains(exercise.GetForeignKeys(), foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains("CK_WorkoutTemplateExercises_TargetRepRange", Constraints(context, typeof(WorkoutTemplateExercise)));
    }

    [Fact]
    public void Model_ShouldConfigureSessionConcurrencyActiveIndexLifecycleAndTimer()
    {
        using var context = CreateContext();
        var session = Entity(context, typeof(WorkoutSession));
        var exercise = Entity(context, typeof(WorkoutSessionExercise));

        Assert.True(session.FindProperty(nameof(WorkoutSession.Version))!.IsConcurrencyToken);
        Assert.True(session.FindProperty(nameof(WorkoutSession.OriginTemplateId))!.IsNullable);
        Assert.Contains(session.GetIndexes(), index =>
            index.IsUnique
            && Names(index).SequenceEqual([nameof(WorkoutSession.UserId)])
            && index.GetFilter() == "\"Status\" = 0 AND \"IsDeleted\" = false");
        Assert.Contains(session.GetIndexes(), index => Names(index).SequenceEqual([nameof(WorkoutSession.UserId), nameof(WorkoutSession.Status), nameof(WorkoutSession.StartedAtUtc)]));
        Assert.Contains(session.GetIndexes(), index => Names(index).SequenceEqual([nameof(WorkoutSession.UserId), nameof(WorkoutSession.WorkoutDate)]));
        Assert.Contains("CK_WorkoutSessions_LifecycleTimestamps", Constraints(context, typeof(WorkoutSession)));
        Assert.Contains("CK_WorkoutSessions_RestTimerState", Constraints(context, typeof(WorkoutSession)));
        Assert.Contains(exercise.GetIndexes(), index => index.IsUnique && Names(index).SequenceEqual([nameof(WorkoutSessionExercise.WorkoutSessionId), nameof(WorkoutSessionExercise.SortOrder)]));
        Assert.Contains(exercise.GetForeignKeys(), foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_ShouldConfigureSetPrecisionOrderingAndLocalConstraints()
    {
        using var context = CreateContext();
        var set = Entity(context, typeof(WorkoutSet));
        var weight = set.FindProperty(nameof(WorkoutSet.WeightKg))!;

        Assert.Equal(typeof(decimal?), weight.ClrType);
        Assert.Equal(18, weight.GetPrecision());
        Assert.Equal(2, weight.GetScale());
        Assert.True(weight.IsNullable);
        Assert.Contains(set.GetIndexes(), index =>
            index.IsUnique
            && Names(index).SequenceEqual([nameof(WorkoutSet.WorkoutSessionExerciseId), nameof(WorkoutSet.SortOrder)])
            && index.GetFilter() == "\"IsDeleted\" = false");
        Assert.Contains("CK_WorkoutSets_Weight_Positive", Constraints(context, typeof(WorkoutSet)));
        Assert.Contains("CK_WorkoutSets_Repetitions_Positive", Constraints(context, typeof(WorkoutSet)));
        Assert.Contains("CK_WorkoutSets_DurationSeconds_Positive", Constraints(context, typeof(WorkoutSet)));
        Assert.Contains(set.GetForeignKeys(), foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    private static IEntityType Entity(AppDbContext context, Type type) =>
        context.GetService<IDesignTimeModel>().Model.FindEntityType(type)!;

    private static IReadOnlyList<string> Constraints(AppDbContext context, Type type) =>
        Entity(context, type).GetCheckConstraints().Select(constraint => constraint.Name).ToArray();

    private static IEnumerable<string> Names(IIndex index) =>
        index.Properties.Select(property => property.Name);

    private static IEnumerable<string> Names(IKey key) =>
        key.Properties.Select(property => property.Name);

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=lifeos_model_tests;Username=test;Password=test")
            .Options;
        return new AppDbContext(options, new TestDateTimeProvider());
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public DateOnly GetCurrentDate(string timeZoneId) => DateOnly.FromDateTime(UtcNow.UtcDateTime);
        public bool IsValidTimeZone(string timeZoneId) => true;
        public LocalTimeConversionResult ConvertLocalToUtc(DateOnly localDate, TimeOnly localTime, string timeZoneId) =>
            LocalTimeConversionResult.Success(new DateTimeOffset(localDate.ToDateTime(localTime), TimeSpan.Zero));
        public DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcInstant, string timeZoneId) => utcInstant;
        public IReadOnlyList<string> GetTimeZoneIds() => [];
    }
}
