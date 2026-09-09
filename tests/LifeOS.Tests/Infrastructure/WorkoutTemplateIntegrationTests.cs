using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.WorkoutTemplates;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.WorkoutTemplates;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Exceptions;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class WorkoutTemplateIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public WorkoutTemplateIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_ShouldPersistDuplicateExercisesAndProjectCurrentMetadata()
    {
        var userId = Guid.NewGuid();
        var exercise = ExerciseDefaults.Definitions.First();
        var service = CreateService(userId);

        var detail = await service.CreateTemplateAsync(new CreateWorkoutTemplateDto(
            "  Push Day  ",
            [CreateExercise(exercise), CreateExercise(exercise)]));

        Assert.Equal("Push Day", detail.Name);
        Assert.Equal(3, detail.Version);
        Assert.Equal([1, 2], detail.Exercises.Select(item => item.SortOrder));
        Assert.Equal([exercise.Id, exercise.Id], detail.Exercises.Select(item => item.ExerciseId));
        Assert.All(detail.Exercises, item =>
        {
            Assert.Equal(exercise.Name, item.ExerciseName);
            Assert.Equal(exercise.LoggingMode, item.LoggingMode);
        });
    }

    [Fact]
    public async Task InvalidExerciseConfiguration_ShouldNotCreatePartialTemplate()
    {
        var userId = Guid.NewGuid();
        var exercise = ExerciseDefaults.Definitions.First();
        var service = CreateService(userId);
        var invalid = CreateExercise(exercise) with { LoggingMode = OppositeMode(exercise.LoggingMode) };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateTemplateAsync(
            new CreateWorkoutTemplateDto("Invalid", [invalid])));

        Assert.Empty(await service.GetTemplatesAsync());
    }

    [Fact]
    public async Task Mutations_ShouldIncrementVersionOnceAndMaintainOrder()
    {
        var userId = Guid.NewGuid();
        var definitions = ExerciseDefaults.Definitions.Take(3).ToArray();
        var service = CreateService(userId);
        var detail = await service.CreateTemplateAsync(new CreateWorkoutTemplateDto(
            "Strength",
            definitions.Select(CreateExercise).ToArray()));

        detail = await service.RenameTemplateAsync(detail.Id, new RenameWorkoutTemplateDto("Renamed", detail.Version));
        Assert.Equal(5, detail.Version);

        detail = await service.AddExerciseAsync(detail.Id, CreateAddExercise(definitions[0], detail.Version));
        Assert.Equal(6, detail.Version);

        var child = detail.Exercises[0];
        detail = await service.UpdateExerciseAsync(
            detail.Id,
            new UpdateWorkoutTemplateExerciseDto(
                child.TemplateExerciseId,
                child.LoggingMode,
                5,
                child.TargetRepMin,
                child.TargetRepMax,
                120,
                detail.Version));
        Assert.Equal(7, detail.Version);
        Assert.Equal(5, detail.Exercises[0].TargetSetCount);
        Assert.Equal(120, detail.Exercises[0].DefaultRestSeconds);

        detail = await service.ReorderExercisesAsync(
            detail.Id,
            new ReorderWorkoutTemplateExercisesDto(
                [detail.Exercises[^1].TemplateExerciseId, detail.Exercises[0].TemplateExerciseId, detail.Exercises[1].TemplateExerciseId, detail.Exercises[2].TemplateExerciseId],
                detail.Version));
        Assert.Equal(8, detail.Version);
        Assert.Equal([1, 2, 3, 4], detail.Exercises.Select(item => item.SortOrder));

        detail = await service.RemoveExerciseAsync(
            detail.Id,
            detail.Exercises[^1].TemplateExerciseId,
            detail.Version);
        Assert.Equal(9, detail.Version);
        Assert.Equal([1, 2, 3], detail.Exercises.Select(item => item.SortOrder));

        detail = await service.RemoveExerciseAsync(
            detail.Id,
            detail.Exercises[^1].TemplateExerciseId,
            detail.Version);
        detail = await service.RemoveExerciseAsync(
            detail.Id,
            detail.Exercises[^1].TemplateExerciseId,
            detail.Version);
        await Assert.ThrowsAsync<ValidationException>(() => service.RemoveExerciseAsync(
            detail.Id,
            detail.Exercises[0].TemplateExerciseId,
            detail.Version));
    }

    [Fact]
    public async Task Reorder_ShouldWorkAgainstFilteredUniqueIndex()
    {
        var service = CreateService(Guid.NewGuid());
        var definitions = ExerciseDefaults.Definitions.Take(3).ToArray();
        var detail = await service.CreateTemplateAsync(new CreateWorkoutTemplateDto(
            "Reorder",
            definitions.Select(CreateExercise).ToArray()));

        var order = new[] { detail.Exercises[2].TemplateExerciseId, detail.Exercises[0].TemplateExerciseId, detail.Exercises[1].TemplateExerciseId };
        detail = await service.ReorderExercisesAsync(
            detail.Id,
            new ReorderWorkoutTemplateExercisesDto(order, detail.Version));

        Assert.Equal(order, detail.Exercises.Select(item => item.TemplateExerciseId));
        await using var context = _fixture.CreateDbContext();
        var persisted = await context.WorkoutTemplateExercises
            .Where(item => item.WorkoutTemplateId == detail.Id)
            .OrderBy(item => item.SortOrder)
            .Select(item => item.SortOrder)
            .ToListAsync();
        Assert.Equal([1, 2, 3], persisted);
    }

    [Fact]
    public async Task StaleMutation_ShouldReturnControlledConcurrencyConflict()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(userId);
        var definition = ExerciseDefaults.Definitions.First();
        var detail = await service.CreateTemplateAsync(new CreateWorkoutTemplateDto("Concurrent", [CreateExercise(definition)]));
        var repository = new WorkoutTemplateRepository(_fixture.CreateDbContextFactory());
        var first = await repository.GetByIdAsync(userId, detail.Id);
        var second = await repository.GetByIdAsync(userId, detail.Id);
        first!.Rename("First");
        second!.Rename("Second");

        var firstResult = await repository.UpdateAsync(userId, first, detail.Version);
        var secondResult = await repository.UpdateAsync(userId, second, detail.Version);

        Assert.Equal(WorkoutTemplateWriteStatus.Succeeded, firstResult.Status);
        Assert.Equal(WorkoutTemplateWriteStatus.ConcurrencyConflict, secondResult.Status);
        Assert.Equal("First", (await service.GetTemplateAsync(detail.Id))!.Name);
    }

    [Fact]
    public async Task OwnershipAndDelete_ShouldBeUserScopedAndSoftDeleteAggregate()
    {
        var ownerId = Guid.NewGuid();
        var otherUserService = CreateService(Guid.NewGuid());
        var ownerService = CreateService(ownerId);
        var definition = ExerciseDefaults.Definitions.First();
        var detail = await ownerService.CreateTemplateAsync(new CreateWorkoutTemplateDto("Owned", [CreateExercise(definition)]));

        Assert.Null(await otherUserService.GetTemplateAsync(detail.Id));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => otherUserService.DeleteTemplateAsync(detail.Id, detail.Version));

        await ownerService.DeleteTemplateAsync(detail.Id, detail.Version);
        Assert.Empty(await ownerService.GetTemplatesAsync());

        await using var context = _fixture.CreateDbContext();
        Assert.True(await context.WorkoutTemplates.IgnoreQueryFilters().Where(item => item.Id == detail.Id).Select(item => item.IsDeleted).SingleAsync());
        Assert.True(await context.WorkoutTemplateExercises.IgnoreQueryFilters().Where(item => item.WorkoutTemplateId == detail.Id).Select(item => item.IsDeleted).SingleAsync());
    }

    [Fact]
    public async Task UnauthenticatedUser_ShouldBeRejected()
    {
        var service = CreateService(Guid.Empty, false);
        await Assert.ThrowsAsync<CurrentUserUnavailableException>(() => service.GetTemplatesAsync());
    }

    private WorkoutTemplateService CreateService(Guid userId, bool authenticated = true) =>
        new(
            new WorkoutTemplateRepository(_fixture.CreateDbContextFactory()),
            new ExerciseRepository(_fixture.CreateDbContextFactory()),
            new TestCurrentUserService(userId, authenticated));

    private static CreateWorkoutTemplateExerciseDto CreateExercise(ExerciseDefinition definition) =>
        new(definition.Id, definition.LoggingMode, 3, IsRepBased(definition.LoggingMode) ? 6 : null, IsRepBased(definition.LoggingMode) ? 10 : null, 90);

    private static AddWorkoutTemplateExerciseDto CreateAddExercise(ExerciseDefinition definition, long version) =>
        new(definition.Id, definition.LoggingMode, 3, IsRepBased(definition.LoggingMode) ? 6 : null, IsRepBased(definition.LoggingMode) ? 10 : null, 90, version);

    private static bool IsRepBased(ExerciseLoggingMode mode) =>
        mode is ExerciseLoggingMode.WeightAndReps or ExerciseLoggingMode.BodyweightAndReps or ExerciseLoggingMode.AddedWeightAndReps or ExerciseLoggingMode.AssistedWeightAndReps or ExerciseLoggingMode.RepsOnly;

    private static ExerciseLoggingMode OppositeMode(ExerciseLoggingMode mode) =>
        mode == ExerciseLoggingMode.Duration ? ExerciseLoggingMode.RepsOnly : ExerciseLoggingMode.Duration;

    private sealed class TestCurrentUserService(Guid userId, bool authenticated) : ICurrentUserService
    {
        public Guid UserId { get; } = userId;
        public bool IsAuthenticated { get; } = authenticated;
    }
}
