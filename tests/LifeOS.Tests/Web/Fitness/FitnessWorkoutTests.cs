using Bunit;
using LifeOS.Core.DTOs.Fitness;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LifeOS.Tests.Web.Fitness;

public sealed class FitnessWorkoutTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IWorkoutSessionService> _sessions = new();
    private readonly Mock<IExerciseService> _exercises = new();

    public FitnessWorkoutTests()
    {
        _exercises.Setup(service => service.GetExercisesAsync(
                It.IsAny<ExerciseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExerciseSummaryDto>());
        _sessions.Setup(service => service.GetPreviousPerformancesAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PreviousPerformanceDto>());
        _context.Services.AddSingleton(_sessions.Object);
        _context.Services.AddSingleton(_exercises.Object);
    }

    [Fact]
    public void ActiveWorkout_RendersOrderedExercisesPreviousPerformanceAndSkipState()
    {
        var firstExercise = CreateExercise(Guid.NewGuid(), "Bench Press", 2, isSkipped: true);
        var secondExercise = CreateExercise(Guid.NewGuid(), "Pull-Up", 1);
        var session = CreateSession(Guid.NewGuid(), [firstExercise, secondExercise]);
        _sessions.Setup(service => service.GetActiveSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _sessions.Setup(service => service.GetPreviousPerformancesAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PreviousPerformanceDto(
                secondExercise.ExerciseId, "Pull-Up", Guid.NewGuid(), "Earlier workout", new DateOnly(2026, 1, 1),
                DateTimeOffset.UtcNow, 1, [new WorkoutSetDto(Guid.NewGuid(), 1, WorkoutSetKind.Working, null, 8, null, DateTimeOffset.UtcNow)])]);

        var cut = _context.Render<FitnessWorkoutPlaceholder>(parameters =>
            parameters.Add(item => item.SessionId, session.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Markup.IndexOf("Pull-Up", StringComparison.Ordinal) < cut.Markup.IndexOf("Bench Press", StringComparison.Ordinal));
            Assert.Contains("Earlier workout", cut.Markup);
            Assert.Contains("Skipped", cut.Markup);
            Assert.Contains("Unskip Exercise", cut.Markup);
        });
    }

    [Theory]
    [InlineData(ExerciseLoggingMode.WeightAndReps, "Weight (kg)", "Duration (seconds)")]
    [InlineData(ExerciseLoggingMode.BodyweightAndReps, "Reps", "Weight (kg)")]
    [InlineData(ExerciseLoggingMode.AddedWeightAndReps, "Added weight (kg)", "Duration (seconds)")]
    [InlineData(ExerciseLoggingMode.AssistedWeightAndReps, "Assistance weight (kg)", "Duration (seconds)")]
    [InlineData(ExerciseLoggingMode.RepsOnly, "Reps", "Weight (kg)")]
    [InlineData(ExerciseLoggingMode.Duration, "Duration (seconds)", "Reps")]
    [InlineData(ExerciseLoggingMode.WeightAndDuration, "Weight (kg)", "Reps")]
    public void ActiveWorkout_RendersInputsForLoggingMode(
        ExerciseLoggingMode mode, string expectedLabel, string absentLabel)
    {
        var exercise = CreateExercise(
            Guid.NewGuid(), "Test Exercise", 1, mode: mode,
            sets: [new WorkoutSetDto(Guid.NewGuid(), 1, WorkoutSetKind.Working, 5m, 8, 30, null)]);
        var session = CreateSession(Guid.NewGuid(), [exercise]);
        _sessions.Setup(service => service.GetActiveSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var cut = _context.Render<FitnessWorkoutPlaceholder>();

        cut.WaitForAssertion(() =>
        {
            var labels = cut.FindAll("label").Select(label => label.TextContent);
            Assert.Contains(labels, label => label.Contains(expectedLabel, StringComparison.Ordinal));
            Assert.DoesNotContain(labels, label => label.Contains(absentLabel, StringComparison.Ordinal));
        });
    }

    [Fact]
    public void AddSet_PersistsAndRendersAuthoritativeSet()
    {
        var exercise = CreateExercise(Guid.NewGuid(), "Bench Press", 1);
        var session = CreateSession(Guid.NewGuid(), [exercise]);
        var addedSet = new WorkoutSetDto(Guid.NewGuid(), 1, WorkoutSetKind.Working, 7.5m, 8, null, null);
        var latest = CreateSession(session.Id, [exercise with { Sets = [addedSet] }], version: 3);
        _sessions.Setup(service => service.GetActiveSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _sessions.Setup(service => service.AddSetAsync(
                session.Id, It.IsAny<AddWorkoutSetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(latest);

        var cut = _context.Render<FitnessWorkoutPlaceholder>();
        cut.WaitForAssertion(() => Assert.Contains("Add Working Set", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Add Working Set")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Set 1", cut.Markup);
            _sessions.Verify(service => service.AddSetAsync(
                session.Id,
                It.Is<AddWorkoutSetDto>(dto => dto.Kind == WorkoutSetKind.Working && dto.ExpectedVersion == session.Version),
                It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Fact]
    public void ConcurrencyConflict_ReloadsLatestWithoutRetryingStaleAction()
    {
        var exercise = CreateExercise(Guid.NewGuid(), "Bench Press", 1);
        var session = CreateSession(Guid.NewGuid(), [exercise]);
        var latest = CreateSession(session.Id, [exercise], version: 4, name: "Latest Push Day");
        _sessions.Setup(service => service.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session)
            .Callback(() => _sessions.Setup(service => service.GetActiveSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(latest));
        _sessions.Setup(service => service.AddSetAsync(
                It.IsAny<Guid>(), It.IsAny<AddWorkoutSetDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkoutSessionConcurrencyException());

        var cut = _context.Render<FitnessWorkoutPlaceholder>();
        cut.WaitForAssertion(() => Assert.Contains("Add Working Set", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Add Working Set")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("changed in another tab", cut.Markup);
            Assert.Contains("Latest Push Day", cut.Markup);
            _sessions.Verify(service => service.AddSetAsync(
                It.IsAny<Guid>(), It.IsAny<AddWorkoutSetDto>(), It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    public void Dispose() => _context.Dispose();

    private static WorkoutSessionDetailDto CreateSession(
        Guid id, IReadOnlyList<WorkoutSessionExerciseDto> exercises, long version = 1, string name = "Push Day") => new(
        id, null, name, new DateOnly(2026, 8, 9), DateTimeOffset.UtcNow,
        WorkoutSessionStatus.InProgress, null, null, null, version, null, null, null, exercises);

    private static WorkoutSessionExerciseDto CreateExercise(
        Guid id, string name, int sortOrder, ExerciseLoggingMode mode = ExerciseLoggingMode.WeightAndReps,
        bool isSkipped = false, IReadOnlyList<WorkoutSetDto>? sets = null) => new(
        Guid.NewGuid(), sortOrder, id, name, id, name, mode, 3, 8, 12, 90, isSkipped, sets ?? []);
}
