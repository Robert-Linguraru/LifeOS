using LifeOS.Core.Abstractions.Fitness;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Fitness;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class ExerciseLibraryIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public ExerciseLibraryIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrationChain_ShouldEndAtM8StrengthTraining()
    {
        await using var context = _fixture.CreateDbContext();

        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

        Assert.Equal("20260909195028_AddStrengthTraining", applied[^1]);
        Assert.Equal(9, applied.Length);
    }

    [Fact]
    public async Task SeededExercises_ShouldContainStableCatalogAndSecondaryMuscles()
    {
        var repository = CreateRepository();
        var exercises = await repository.GetAsync(new ExerciseQuery());

        Assert.Equal(90, exercises.Count);
        Assert.Equal(
            ExerciseDefaults.Definitions.OrderBy(item => item.SortOrder).Select(item => item.Id),
            exercises.Select(item => item.Id));

        var bench = await repository.GetByIdAsync(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        var pullUp = await repository.GetByIdAsync(Guid.Parse("10000000-0000-4000-8000-000000000015"));

        Assert.Equal("Barbell Bench Press", bench!.Name);
        Assert.Equal("Pull-Up", pullUp!.Name);
        Assert.Contains(MuscleGroup.Biceps, pullUp.SecondaryMuscleGroups);
        Assert.Contains(MuscleGroup.Forearms, pullUp.SecondaryMuscleGroups);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(119, await context.ExerciseSecondaryMuscleGroups.CountAsync());
        Assert.DoesNotContain(
            context.Model.FindEntityType(typeof(ExerciseSecondaryMuscleGroup))!.GetProperties(),
            property => property.Name == "ExerciseId1");
    }

    [Fact]
    public async Task Search_ShouldBeCaseInsensitiveAndWhitespaceTolerant()
    {
        var repository = CreateRepository();

        var results = await repository.GetAsync(new ExerciseQuery("  pReSs  "));

        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.Contains("press", result.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Filters_ShouldComposeAcrossApprovedMetadata()
    {
        var repository = CreateRepository();

        var results = await repository.GetAsync(new ExerciseQuery(
            Search: "press",
            PrimaryMuscleGroup: MuscleGroup.Chest,
            Equipment: ExerciseEquipment.Dumbbell,
            MovementPattern: MovementPattern.HorizontalPush,
            LoggingMode: ExerciseLoggingMode.WeightAndReps));

        Assert.NotEmpty(results);
        Assert.All(results, result =>
        {
            Assert.Contains("press", result.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(MuscleGroup.Chest, result.PrimaryMuscleGroup);
            Assert.Equal(ExerciseEquipment.Dumbbell, result.Equipment);
            Assert.Equal(MovementPattern.HorizontalPush, result.MovementPattern);
            Assert.Equal(ExerciseLoggingMode.WeightAndReps, result.LoggingMode);
        });
    }

    [Fact]
    public async Task Reads_ShouldReturnDeterministicActiveOrderAndExpectedDetail()
    {
        var repository = CreateRepository();
        var results = await repository.GetAsync(new ExerciseQuery());

        Assert.Equal(
            ExerciseDefaults.Definitions.OrderBy(item => item.SortOrder).Select(item => item.Name),
            results.Select(item => item.Name));

        var detail = await repository.GetByIdAsync(ExerciseDefaults.Definitions[0].Id);
        Assert.NotNull(detail);
        Assert.NotEmpty(detail.SecondaryMuscleGroups);
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
        Assert.Null(await repository.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task ActiveSessionIndex_ShouldRejectTwoInProgressSessionsForOneUser()
    {
        var userId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);

        await using (var context = _fixture.CreateDbContext())
        {
            context.WorkoutSessions.Add(new WorkoutSession(Guid.NewGuid(), userId, "First", new DateOnly(2026, 9, 10), start));
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            context.WorkoutSessions.Add(new WorkoutSession(Guid.NewGuid(), userId, "Second", new DateOnly(2026, 9, 10), start.AddMinutes(1)));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = _fixture.CreateDbContext())
        {
            var existing = await context.WorkoutSessions.SingleAsync(session => session.UserId == userId);
            context.WorkoutSessions.Remove(existing);
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateDbContext())
        {
            context.WorkoutSessions.Add(new WorkoutSession(Guid.NewGuid(), userId, "Third", new DateOnly(2026, 9, 10), start.AddMinutes(2)));
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Cancellation_ShouldBePropagatedByExerciseReads()
    {
        var repository = CreateRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetAsync(new ExerciseQuery(), cancellation.Token));
    }

    private IExerciseRepository CreateRepository() =>
        new ExerciseRepository(_fixture.CreateDbContextFactory());
}
