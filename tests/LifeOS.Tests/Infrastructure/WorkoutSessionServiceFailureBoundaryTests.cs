using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Fitness;
using LifeOS.Core.Abstractions.WorkoutSessions;
using LifeOS.Core.Abstractions.WorkoutTemplates;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Services;
using LifeOS.Core.Time;
using LifeOS.Infrastructure.Services;
using Moq;

namespace LifeOS.Tests.Infrastructure;

public sealed class WorkoutSessionServiceFailureBoundaryTests
{
    [Fact]
    public async Task CompleteSet_WhenDerivedEvidenceFailsAfterPersistence_ReturnsAcceptedMutationWithoutAchievements()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
        var session = new WorkoutSession(sessionId, userId, "Strength", new DateOnly(2026, 8, 9), now.AddMinutes(-5));
        var sessionExercise = session.AddInitialExercise(exerciseId, "Pull-Up", ExerciseLoggingMode.RepsOnly, 1, null, null, 60);
        var set = session.AddSet(sessionExercise.Id, WorkoutSetKind.Working, null, null, null);
        var expectedVersion = session.Version;

        var repository = new Mock<IWorkoutSessionRepository>();
        var sequence = new MockSequence();
        repository.InSequence(sequence)
            .Setup(item => item.GetByIdAsync(userId, sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        repository.InSequence(sequence)
            .Setup(item => item.UpdateAsync(userId, session, expectedVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new WorkoutSessionWriteResult
            {
                Status = WorkoutSessionWriteStatus.Succeeded,
                Session = session
            });
        repository.InSequence(sequence)
            .Setup(item => item.GetByIdAsync(userId, sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        repository.InSequence(sequence)
            .Setup(item => item.GetStrengthRecordEvidenceAsync(
                userId,
                exerciseId,
                sessionId,
                set.Id,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("derived evidence unavailable"));

        var service = new WorkoutSessionService(
            repository.Object,
            new Mock<IWorkoutTemplateRepository>().Object,
            new Mock<IExerciseRepository>().Object,
            new Mock<IUserSettingsRepository>().Object,
            new TestCurrentUserService(userId),
            new TestDateTimeProvider(now));

        var result = await service.CompleteSetAsync(
            sessionId,
            new CompleteWorkoutSetDto(
                sessionExercise.Id,
                set.Id,
                null,
                8,
                null,
                true,
                expectedVersion));

        Assert.Equal(expectedVersion + 1, result.Version);
        var returnedSet = Assert.Single(result.Exercises[0].Sets);
        Assert.NotNull(returnedSet.CompletedAtUtc);
        Assert.Equal(8, returnedSet.Repetitions);
        Assert.NotNull(result.RestTimerEndsAtUtc);
        Assert.Empty(result.StrengthRecordAchievements);
        repository.Verify(item => item.UpdateAsync(userId, session, expectedVersion, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.GetStrengthRecordEvidenceAsync(
            userId,
            exerciseId,
            sessionId,
            set.Id,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class TestCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid UserId { get; } = userId;
        public bool IsAuthenticated => true;
    }

    private sealed class TestDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = now;
        public bool IsValidTimeZone(string timeZoneId) => timeZoneId == "UTC";
        public DateOnly GetCurrentDate(string timeZoneId) => DateOnly.FromDateTime(UtcNow.UtcDateTime);
        public LocalTimeConversionResult ConvertLocalToUtc(DateOnly localDate, TimeOnly localTime, string timeZoneId) =>
            LocalTimeConversionResult.Success(new DateTimeOffset(localDate.ToDateTime(localTime), TimeSpan.Zero));
        public DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcInstant, string timeZoneId) => utcInstant;
        public IReadOnlyList<string> GetTimeZoneIds() => ["UTC"];
    }
}
