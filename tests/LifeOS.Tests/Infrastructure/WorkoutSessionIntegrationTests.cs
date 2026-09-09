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
