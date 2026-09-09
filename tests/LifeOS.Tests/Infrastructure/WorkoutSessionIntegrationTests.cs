using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Fitness;
using LifeOS.Core.Abstractions.WorkoutSessions;
using LifeOS.Core.Abstractions.WorkoutTemplates;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.DTOs.WorkoutTemplates;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Time;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class WorkoutSessionIntegrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public WorkoutSessionIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartFromTemplate_ShouldPersistInitialVersionAndHistoricalSnapshots()
    {
        var userId = Guid.NewGuid();
        var definitions = ExerciseDefaults.Definitions.Take(2).ToArray();
        var template = await CreateTemplateAsync(userId, definitions);
        var service = CreateSessionService(userId);

        var detail = await service.StartFromTemplateAsync(
            new StartTemplateWorkoutDto(template.Id, new DateOnly(2000, 1, 1), DateTimeOffset.MinValue));

        Assert.Equal(1, detail.Version);
        Assert.Equal(template.Id, detail.OriginTemplateId);
        Assert.Equal(template.Name, detail.NameSnapshot);
        Assert.Equal(new DateOnly(2026, 8, 9), detail.WorkoutDate);
        Assert.Null(detail.RestTimerDurationSeconds);
        Assert.Null(detail.RestTimerEndsAtUtc);
        Assert.Null(detail.RestTimerPausedRemainingSeconds);
        Assert.Equal(definitions.Select(item => item.Id), detail.Exercises.Select(item => item.ExerciseId));
        Assert.All(detail.Exercises, item =>
        {
            Assert.Equal(item.OriginalExerciseId, item.ExerciseId);
            Assert.Equal(item.OriginalExerciseNameSnapshot, item.ExerciseNameSnapshot);
            Assert.Empty(item.Sets);
            Assert.False(item.IsSkipped);
        });
    }

    [Fact]
    public async Task CustomWorkout_ShouldUseAuthoritativeExerciseSnapshotsAndNoOriginTemplate()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First();
        var service = CreateSessionService(userId);

        var detail = await service.StartCustomWorkoutAsync(new StartCustomWorkoutDto(
            "Custom Day",
            new DateOnly(2000, 1, 1),
            DateTimeOffset.MinValue,
            [new StartWorkoutExerciseDto(
                definition.Id,
                "caller value",
                definition.LoggingMode,
                4,
                8,
                12,
                75)]));

        Assert.Null(detail.OriginTemplateId);
        Assert.Equal("Custom Day", detail.NameSnapshot);
        Assert.Equal(definition.Name, detail.Exercises[0].ExerciseNameSnapshot);
        Assert.Equal(definition.LoggingMode, detail.Exercises[0].LoggingModeSnapshot);
        Assert.Equal(1, detail.Version);
    }

    [Fact]
    public async Task ExistingActiveSession_ShouldBeReturnedInsteadOfStartingAnother()
    {
        var userId = Guid.NewGuid();
        var firstTemplate = await CreateTemplateAsync(userId, ExerciseDefaults.Definitions.Take(1).ToArray(), "Push");
        var secondTemplate = await CreateTemplateAsync(userId, ExerciseDefaults.Definitions.Skip(1).Take(1).ToArray(), "Pull");
        var service = CreateSessionService(userId);

        var first = await service.StartFromTemplateAsync(new StartTemplateWorkoutDto(firstTemplate.Id, default, default));
        var second = await service.StartFromTemplateAsync(new StartTemplateWorkoutDto(secondTemplate.Id, default, default));

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Push", second.NameSnapshot);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.WorkoutSessions.CountAsync(item => item.UserId == userId && item.Status == WorkoutSessionStatus.InProgress));
    }

    [Fact]
    public async Task ConcurrentStarts_ShouldResolveToOneAuthoritativeSession()
    {
        var userId = Guid.NewGuid();
        var template = await CreateTemplateAsync(userId, ExerciseDefaults.Definitions.Take(2).ToArray());
        var firstService = CreateSessionService(userId);
        var secondService = CreateSessionService(userId);
        var request = new StartTemplateWorkoutDto(template.Id, default, default);

        var results = await Task.WhenAll(
            firstService.StartFromTemplateAsync(request),
            secondService.StartFromTemplateAsync(request));

        Assert.Equal(results[0].Id, results[1].Id);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.WorkoutSessions.CountAsync(item => item.UserId == userId && item.Status == WorkoutSessionStatus.InProgress && !item.IsDeleted));
    }

    [Fact]
    public async Task DifferentUsers_ShouldEachHaveTheirOwnActiveSession()
    {
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var firstTemplate = await CreateTemplateAsync(firstUser, ExerciseDefaults.Definitions.Take(1).ToArray());
        var secondTemplate = await CreateTemplateAsync(secondUser, ExerciseDefaults.Definitions.Take(1).ToArray());

        var results = await Task.WhenAll(
            CreateSessionService(firstUser).StartFromTemplateAsync(new StartTemplateWorkoutDto(firstTemplate.Id, default, default)),
            CreateSessionService(secondUser).StartFromTemplateAsync(new StartTemplateWorkoutDto(secondTemplate.Id, default, default)));

        Assert.NotEqual(results[0].Id, results[1].Id);
    }

    [Fact]
    public async Task TemplateChangesAndDeletion_ShouldNotChangeHistoricalActiveSession()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First();
        var templateRepository = new WorkoutTemplateRepository(_fixture.CreateDbContextFactory());
        var template = await CreateTemplateAsync(userId, [definition], "Original");
        var sessionService = CreateSessionService(userId);
        var started = await sessionService.StartFromTemplateAsync(new StartTemplateWorkoutDto(template.Id, default, default));
        var before = started.Exercises[0];

        var mutableTemplate = await templateRepository.GetByIdAsync(userId, template.Id);
        mutableTemplate!.Rename("Changed");
        await templateRepository.UpdateAsync(userId, mutableTemplate, template.Version);
        await templateRepository.DeleteAsync(userId, template.Id, mutableTemplate.Version);

        var resumed = await sessionService.GetActiveSessionAsync();
        Assert.NotNull(resumed);
        Assert.Equal("Original", resumed.NameSnapshot);
        Assert.Equal(before.ExerciseNameSnapshot, resumed.Exercises[0].ExerciseNameSnapshot);
        Assert.Equal(before.TargetSetCountSnapshot, resumed.Exercises[0].TargetSetCountSnapshot);
        Assert.Equal(before.DefaultRestSecondsSnapshot, resumed.Exercises[0].DefaultRestSecondsSnapshot);
    }

    [Fact]
    public async Task UserIsolation_ShouldPreventCrossUserTemplateAndSessionAccess()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var template = await CreateTemplateAsync(owner, ExerciseDefaults.Definitions.Take(1).ToArray());
        var session = await CreateSessionService(owner).StartFromTemplateAsync(new StartTemplateWorkoutDto(template.Id, default, default));

        var otherService = CreateSessionService(other);
        Assert.Null(await otherService.GetSessionAsync(session.Id));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => otherService.StartFromTemplateAsync(new StartTemplateWorkoutDto(template.Id, default, default)));
        Assert.Null(await otherService.GetActiveSessionAsync());
    }

    [Fact]
    public async Task CustomWorkout_ShouldRespectActiveSessionInvariant()
    {
        var userId = Guid.NewGuid();
        var service = CreateSessionService(userId);
        var definition = ExerciseDefaults.Definitions.First();
        var request = new StartCustomWorkoutDto(
            "Custom",
            default,
            default,
            [new StartWorkoutExerciseDto(definition.Id, "ignored", definition.LoggingMode, 1, null, null, 0)]);

        var first = await service.StartCustomWorkoutAsync(request);
        var second = await service.StartCustomWorkoutAsync(request);

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task InactiveTemplateExercise_ShouldPreventSessionStart()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First();
        var template = await CreateTemplateAsync(userId, [definition]);
        await using (var context = _fixture.CreateDbContext())
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Exercises\" SET \"IsActive\" = false WHERE \"Id\" = {definition.Id};");
        }

        try
        {
            await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                CreateSessionService(userId).StartFromTemplateAsync(new StartTemplateWorkoutDto(template.Id, default, default)));
        }
        finally
        {
            await using var context = _fixture.CreateDbContext();
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Exercises\" SET \"IsActive\" = true WHERE \"Id\" = {definition.Id};");
        }
    }

    [Fact]
    public async Task CustomWorkout_AddRemove_ShouldAllowDuplicatesAndNormalizeOrder()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First();
        var service = CreateSessionService(userId);
        var start = await service.StartCustomWorkoutAsync(CreateCustomRequest(definition));

        var added = await service.AddExerciseAsync(
            start.Id,
            new AddSessionExerciseDto(definition.Id, "ignored", definition.LoggingMode, 2, 6, 8, 60, start.Version));
        Assert.Equal(2, added.Version);
        Assert.Equal(2, added.Exercises.Count);
        Assert.Equal([1, 2], added.Exercises.Select(item => item.SortOrder));
        Assert.Equal([definition.Id, definition.Id], added.Exercises.Select(item => item.ExerciseId));

        var removed = await service.RemoveExerciseAsync(
            added.Id,
            added.Exercises[0].Id,
            added.Version);
        Assert.Equal(3, removed.Version);
        Assert.Single(removed.Exercises);
        Assert.Equal(1, removed.Exercises[0].SortOrder);

        await Assert.ThrowsAsync<ValidationException>(() => service.RemoveExerciseAsync(
            removed.Id,
            removed.Exercises[0].Id,
            removed.Version));
    }

    [Fact]
    public async Task SubstituteSkipAndUnskip_ShouldPreserveOriginalAndTemplateState()
    {
        var userId = Guid.NewGuid();
        var original = ExerciseDefaults.Definitions.First();
        var replacement = ExerciseDefaults.Definitions.First(item => item.Id != original.Id && item.LoggingMode == original.LoggingMode);
        var template = await CreateTemplateAsync(userId, [original]);
        var templateRepository = new WorkoutTemplateRepository(_fixture.CreateDbContextFactory());
        var service = CreateSessionService(userId);
        var started = await service.StartFromTemplateAsync(new StartTemplateWorkoutDto(template.Id, default, default));

        var substituted = await service.SubstituteExerciseAsync(
            started.Id,
            new SubstituteSessionExerciseDto(
                started.Exercises[0].Id,
                replacement.Id,
                "ignored",
                replacement.LoggingMode,
                started.Version));
        Assert.Equal(original.Id, substituted.Exercises[0].OriginalExerciseId);
        Assert.Equal(original.Name, substituted.Exercises[0].OriginalExerciseNameSnapshot);
        Assert.Equal(replacement.Id, substituted.Exercises[0].ExerciseId);
        Assert.Equal(replacement.Name, substituted.Exercises[0].ExerciseNameSnapshot);

        var skipped = await service.SkipExerciseAsync(substituted.Id, substituted.Exercises[0].Id, substituted.Version);
        Assert.True(skipped.Exercises[0].IsSkipped);
        var unskipped = await service.UnskipExerciseAsync(skipped.Id, skipped.Exercises[0].Id, skipped.Version);
        Assert.False(unskipped.Exercises[0].IsSkipped);

        var unchangedTemplate = await templateRepository.GetByIdAsync(userId, template.Id);
        Assert.Equal(original.Id, unchangedTemplate!.Exercises[0].ExerciseId);
    }

    [Fact]
    public async Task IncompatibleSubstitution_ShouldLeaveSessionUnchanged()
    {
        var userId = Guid.NewGuid();
        var original = ExerciseDefaults.Definitions.First();
        var duration = ExerciseDefaults.Definitions.First(item => item.LoggingMode == ExerciseLoggingMode.Duration);
        var template = await CreateTemplateAsync(userId, [original]);
        var service = CreateSessionService(userId);
        var started = await service.StartFromTemplateAsync(new StartTemplateWorkoutDto(template.Id, default, default));

        await Assert.ThrowsAsync<ValidationException>(() => service.SubstituteExerciseAsync(
            started.Id,
            new SubstituteSessionExerciseDto(
                started.Exercises[0].Id,
                duration.Id,
                duration.Name,
                duration.LoggingMode,
                started.Version)));

        var unchanged = (await service.GetActiveSessionAsync())!;
        Assert.Equal(started.Version, unchanged.Version);
        Assert.Equal(original.Id, unchanged.Exercises[0].ExerciseId);
    }

    [Fact]
    public async Task StaleSessionMutation_ShouldReturnControlledConcurrencyConflict()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First();
        var service = CreateSessionService(userId);
        var started = await service.StartCustomWorkoutAsync(CreateCustomRequest(definition));
        var repository = new WorkoutSessionRepository(_fixture.CreateDbContextFactory());
        var first = await repository.GetByIdAsync(userId, started.Id);
        var second = await repository.GetByIdAsync(userId, started.Id);
        first!.SkipExercise(first.Exercises[0].Id);
        second!.SkipExercise(second.Exercises[0].Id);

        var firstResult = await repository.UpdateAsync(userId, first, started.Version);
        var secondResult = await repository.UpdateAsync(userId, second, started.Version);

        Assert.Equal(WorkoutSessionWriteStatus.Succeeded, firstResult.Status);
        Assert.Equal(WorkoutSessionWriteStatus.ConcurrencyConflict, secondResult.Status);
        Assert.True((await service.GetActiveSessionAsync())!.Exercises[0].IsSkipped);
    }

    [Fact]
    public async Task CrossUserMutation_ShouldBeHiddenAsNotFound()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First();
        var started = await CreateSessionService(owner).StartCustomWorkoutAsync(CreateCustomRequest(definition));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            CreateSessionService(other).SkipExerciseAsync(started.Id, started.Exercises[0].Id, started.Version));
    }

    [Fact]
    public async Task SetWorkflow_ShouldPersistDraftCompletionCorrectionAndResume()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First(item => item.LoggingMode == ExerciseLoggingMode.WeightAndReps);
        var service = CreateSessionService(userId);
        var started = await service.StartCustomWorkoutAsync(CreateCustomRequest(definition));
        var exerciseId = started.Exercises[0].Id;

        var draft = await service.AddSetAsync(
            started.Id,
            new AddWorkoutSetDto(exerciseId, WorkoutSetKind.WarmUp, 80m, null, null, started.Version));
        Assert.Equal(2, draft.Version);
        Assert.Null(draft.Exercises[0].Sets[0].CompletedAtUtc);

        var edited = await service.UpdateSetAsync(
            draft.Id,
            new UpdateWorkoutSetDto(exerciseId, draft.Exercises[0].Sets[0].Id, WorkoutSetKind.Working, 80m, 8, null, draft.Version));
        Assert.Equal(3, edited.Version);
        Assert.Equal(WorkoutSetKind.Working, edited.Exercises[0].Sets[0].Kind);

        var completed = await service.CompleteSetAsync(
            edited.Id,
            new CompleteWorkoutSetDto(exerciseId, edited.Exercises[0].Sets[0].Id, 80m, 8, null, edited.Version));
        Assert.Equal(4, completed.Version);
        Assert.Equal(new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero), completed.Exercises[0].Sets[0].CompletedAtUtc);

        var corrected = await service.UpdateSetAsync(
            completed.Id,
            new UpdateWorkoutSetDto(exerciseId, completed.Exercises[0].Sets[0].Id, WorkoutSetKind.WarmUp, 82.5m, 7, null, completed.Version));
        Assert.Equal(5, corrected.Version);
        Assert.Equal(completed.Exercises[0].Sets[0].CompletedAtUtc, corrected.Exercises[0].Sets[0].CompletedAtUtc);

        var resumed = await CreateSessionService(userId).GetActiveSessionAsync();
        var set = Assert.Single(Assert.Single(resumed!.Exercises).Sets);
        Assert.Equal(WorkoutSetKind.WarmUp, set.Kind);
        Assert.Equal(82.5m, set.WeightKg);
        Assert.Equal(7, set.Repetitions);
        Assert.Equal(corrected.Version, resumed.Version);
    }

    [Theory]
    [InlineData(ExerciseLoggingMode.WeightAndReps, "80", 8, null)]
    [InlineData(ExerciseLoggingMode.BodyweightAndReps, null, 8, null)]
    [InlineData(ExerciseLoggingMode.AddedWeightAndReps, "20", 8, null)]
    [InlineData(ExerciseLoggingMode.AssistedWeightAndReps, "20", 8, null)]
    [InlineData(ExerciseLoggingMode.RepsOnly, null, 8, null)]
    [InlineData(ExerciseLoggingMode.Duration, null, null, 60)]
    [InlineData(ExerciseLoggingMode.WeightAndDuration, "20", null, 60)]
    public async Task CompleteSet_ShouldAcceptEveryLoggingMode(
        ExerciseLoggingMode mode,
        string? weight,
        int? repetitions,
        int? duration)
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First(item => item.LoggingMode == mode);
        var service = CreateSessionService(userId);
        var started = await service.StartCustomWorkoutAsync(CreateCustomRequest(definition));
        var exerciseId = started.Exercises[0].Id;
        var draft = await service.AddSetAsync(
            started.Id,
            new AddWorkoutSetDto(exerciseId, WorkoutSetKind.Working, null, null, null, started.Version));

        var completed = await service.CompleteSetAsync(
            draft.Id,
            new CompleteWorkoutSetDto(exerciseId, draft.Exercises[0].Sets[0].Id, weight is null ? null : decimal.Parse(weight), repetitions, duration, draft.Version));

        Assert.True(completed.Exercises[0].Sets[0].CompletedAtUtc.HasValue);
    }

    [Theory]
    [InlineData(ExerciseLoggingMode.WeightAndReps, null, 8, null)]
    [InlineData(ExerciseLoggingMode.BodyweightAndReps, "80", 8, null)]
    [InlineData(ExerciseLoggingMode.AddedWeightAndReps, null, 8, null)]
    [InlineData(ExerciseLoggingMode.AssistedWeightAndReps, null, 8, null)]
    [InlineData(ExerciseLoggingMode.RepsOnly, "1", 8, null)]
    [InlineData(ExerciseLoggingMode.Duration, null, 1, 60)]
    [InlineData(ExerciseLoggingMode.WeightAndDuration, "20", 1, 60)]
    public async Task CompleteSet_ShouldRejectMissingOrExtraMeasurements(
        ExerciseLoggingMode mode,
        string? weight,
        int? repetitions,
        int? duration)
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First(item => item.LoggingMode == mode);
        var service = CreateSessionService(userId);
        var started = await service.StartCustomWorkoutAsync(CreateCustomRequest(definition));
        var exerciseId = started.Exercises[0].Id;
        var draft = await service.AddSetAsync(
            started.Id,
            new AddWorkoutSetDto(exerciseId, WorkoutSetKind.Working, null, null, null, started.Version));

        await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteSetAsync(
            draft.Id,
            new CompleteWorkoutSetDto(exerciseId, draft.Exercises[0].Sets[0].Id, weight is null ? null : decimal.Parse(weight), repetitions, duration, draft.Version)));

        var unchanged = await service.GetActiveSessionAsync();
        Assert.Equal(draft.Version, unchanged!.Version);
        Assert.Null(unchanged.Exercises[0].Sets[0].CompletedAtUtc);
    }

    [Fact]
    public async Task RemoveSet_ShouldNormalizeOrderAndSoftDeleteCompletedSet()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First(item => item.LoggingMode == ExerciseLoggingMode.RepsOnly);
        var service = CreateSessionService(userId);
        var started = await service.StartCustomWorkoutAsync(CreateCustomRequest(definition));
        var exerciseId = started.Exercises[0].Id;
        var first = await service.AddSetAsync(started.Id, new AddWorkoutSetDto(exerciseId, WorkoutSetKind.Working, null, null, null, started.Version));
        var second = await service.AddSetAsync(first.Id, new AddWorkoutSetDto(exerciseId, WorkoutSetKind.WarmUp, null, null, null, first.Version));
        var third = await service.AddSetAsync(second.Id, new AddWorkoutSetDto(exerciseId, WorkoutSetKind.Working, null, null, null, second.Version));

        var removed = await service.RemoveSetAsync(third.Id, exerciseId, third.Exercises[0].Sets[1].Id, third.Version);
        Assert.Equal(5, removed.Version);
        Assert.Equal([1, 2], removed.Exercises[0].Sets.Select(item => item.SortOrder));

        var completed = await service.CompleteSetAsync(
            removed.Id,
            new CompleteWorkoutSetDto(exerciseId, removed.Exercises[0].Sets[0].Id, null, 5, null, removed.Version));
        var removedCompleted = await service.RemoveSetAsync(
            completed.Id,
            exerciseId,
            completed.Exercises[0].Sets[0].Id,
            completed.Version);
        var remaining = Assert.Single(removedCompleted.Exercises[0].Sets);
        Assert.Equal(1, remaining.SortOrder);

        await using var context = _fixture.CreateDbContext();
        Assert.True(await context.WorkoutSets.IgnoreQueryFilters().AnyAsync(item => item.Id == third.Exercises[0].Sets[1].Id && item.IsDeleted));
        Assert.True(await context.WorkoutSets.IgnoreQueryFilters().AnyAsync(item => item.Id == completed.Exercises[0].Sets[0].Id && item.IsDeleted));
    }

    [Fact]
    public async Task StaleSetMutation_ShouldReturnConcurrencyConflictAndPreserveAcceptedState()
    {
        var userId = Guid.NewGuid();
        var definition = ExerciseDefaults.Definitions.First(item => item.LoggingMode == ExerciseLoggingMode.RepsOnly);
        var service = CreateSessionService(userId);
        var started = await service.StartCustomWorkoutAsync(CreateCustomRequest(definition));
        var exerciseId = started.Exercises[0].Id;
        var draft = await service.AddSetAsync(started.Id, new AddWorkoutSetDto(exerciseId, WorkoutSetKind.Working, null, null, null, started.Version));
        var first = CreateSessionService(userId);
        var second = CreateSessionService(userId);

        var accepted = await first.UpdateSetAsync(draft.Id, new UpdateWorkoutSetDto(exerciseId, draft.Exercises[0].Sets[0].Id, WorkoutSetKind.WarmUp, null, 8, null, draft.Version));
        await Assert.ThrowsAsync<WorkoutSessionConcurrencyException>(() => second.UpdateSetAsync(
            draft.Id,
            new UpdateWorkoutSetDto(exerciseId, draft.Exercises[0].Sets[0].Id, WorkoutSetKind.Working, null, 10, null, draft.Version)));

        var final = await service.GetActiveSessionAsync();
        Assert.Equal(accepted.Version, final!.Version);
        Assert.Equal(8, final.Exercises[0].Sets[0].Repetitions);
    }

    private async Task<WorkoutTemplate> CreateTemplateAsync(
        Guid userId,
        IReadOnlyList<ExerciseDefinition> definitions,
        string name = "Push Day")
    {
        var template = new WorkoutTemplate(Guid.NewGuid(), userId, name);
        foreach (var definition in definitions)
        {
            template.AddExercise(
                definition.Id,
                definition.LoggingMode,
                3,
                IsRepBased(definition.LoggingMode) ? 6 : null,
                IsRepBased(definition.LoggingMode) ? 10 : null,
                90);
        }

        await new WorkoutTemplateRepository(_fixture.CreateDbContextFactory()).AddAsync(template);
        return template;
    }

    private static StartCustomWorkoutDto CreateCustomRequest(ExerciseDefinition definition) =>
        new(
            "Custom Day",
            default,
            default,
            [new StartWorkoutExerciseDto(
                definition.Id,
                "ignored",
                definition.LoggingMode,
                3,
                IsRepBased(definition.LoggingMode) ? 6 : null,
                IsRepBased(definition.LoggingMode) ? 10 : null,
                90)]);

    private WorkoutSessionService CreateSessionService(Guid userId) =>
        new(
            new WorkoutSessionRepository(_fixture.CreateDbContextFactory()),
            new WorkoutTemplateRepository(_fixture.CreateDbContextFactory()),
            new ExerciseRepository(_fixture.CreateDbContextFactory()),
            new UserSettingsRepository(_fixture.CreateDbContextFactory()),
            new TestCurrentUserService(userId),
            new TestDateTimeProvider());

    private static bool IsRepBased(ExerciseLoggingMode mode) =>
        mode is ExerciseLoggingMode.WeightAndReps or ExerciseLoggingMode.BodyweightAndReps or ExerciseLoggingMode.AddedWeightAndReps or ExerciseLoggingMode.AssistedWeightAndReps or ExerciseLoggingMode.RepsOnly;

    private sealed class TestCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid UserId { get; } = userId;
        public bool IsAuthenticated => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
        public bool IsValidTimeZone(string timeZoneId) => timeZoneId == "UTC";
        public DateOnly GetCurrentDate(string timeZoneId) => new(2026, 8, 9);
        public LocalTimeConversionResult ConvertLocalToUtc(DateOnly localDate, TimeOnly localTime, string timeZoneId) =>
            LocalTimeConversionResult.Success(new DateTimeOffset(localDate.ToDateTime(localTime), TimeSpan.Zero));
        public DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcInstant, string timeZoneId) => utcInstant;
        public IReadOnlyList<string> GetTimeZoneIds() => ["UTC"];
    }
}
