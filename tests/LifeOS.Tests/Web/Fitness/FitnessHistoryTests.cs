using Bunit;
using LifeOS.Core.DTOs.Fitness;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.Enums;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LifeOS.Tests.Web.Fitness;

public sealed class FitnessHistoryTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IWorkoutSessionService> _sessions = new();
    private readonly Mock<IExerciseService> _exercises = new();

    public FitnessHistoryTests()
    {
        _context.Services.AddSingleton(_sessions.Object);
        _context.Services.AddSingleton(_exercises.Object);
    }

    [Fact]
    public void CompletionSummary_RendersCountsFeelingAchievementsAndHistoricalSets()
    {
        var exercise = CreateExercise("Dumbbell Bench Press", ExerciseLoggingMode.WeightAndReps,
            originalName: "Barbell Bench Press", setKind: WorkoutSetKind.Working);
        var session = CreateCompletedSession([exercise], [new StrengthRecordAchievement(
            StrengthRecordType.HeaviestWeight, true, 32.5m, 8, null, null, null, null)]);
        _sessions.Setup(service => service.GetCompletedWorkoutAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var cut = _context.Render<FitnessWorkoutCompleted>(parameters => parameters.Add(item => item.SessionId, session.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Workout complete", cut.Markup);
            Assert.Contains("Good", cut.Markup);
            Assert.Contains("First record", cut.Markup);
            Assert.Contains("Dumbbell Bench Press", cut.Markup);
            Assert.Contains("Substituted for Barbell Bench Press", cut.Markup);
            Assert.Contains("Working", cut.Markup);
            Assert.DoesNotContain("Total Volume", cut.Markup);
        });
    }

    [Fact]
    public void CompletionSummary_WithNoAchievementsOmitsAchievementFailureMessage()
    {
        var session = CreateCompletedSession([CreateExercise("Push-Up", ExerciseLoggingMode.BodyweightAndReps)], []);
        _sessions.Setup(service => service.GetCompletedWorkoutAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var cut = _context.Render<FitnessWorkoutCompleted>(parameters => parameters.Add(item => item.SessionId, session.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Workout totals", cut.Markup);
            Assert.DoesNotContain("No achievements", cut.Markup);
            Assert.DoesNotContain("Strength records", cut.Markup);
        });
    }

    [Fact]
    public void WorkoutHistory_RendersCompletedItemsAndFeeling()
    {
        var item = new WorkoutSessionSummaryDto(Guid.NewGuid(), "Push Day", new DateOnly(2026, 8, 10),
            WorkoutSessionStatus.Completed, DateTimeOffset.UtcNow.AddMinutes(-45), DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(45), 2, 2, 5, SessionFeeling.Great, 4);
        _sessions.Setup(service => service.GetWorkoutHistoryAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutHistoryPageDto([item], 1, 20, 1));

        var cut = _context.Render<FitnessHistory>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Push Day", cut.Markup);
            Assert.Contains("Great", cut.Markup);
            Assert.Contains("View workout", cut.Markup);
            Assert.Contains("Page 1 of 1", cut.Markup);
        });
    }

    [Fact]
    public void WorkoutHistory_EmptyStateIsUseful()
    {
        _sessions.Setup(service => service.GetWorkoutHistoryAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutHistoryPageDto([], 1, 20, 0));

        var cut = _context.Render<FitnessHistory>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No completed workouts yet.", cut.Markup);
            Assert.Contains("Back to Fitness", cut.Markup);
        });
    }

    [Fact]
    public void WorkoutHistory_NextPageRequestsBackendPage()
    {
        var item = new WorkoutSessionSummaryDto(Guid.NewGuid(), "Push Day", new DateOnly(2026, 8, 10),
            WorkoutSessionStatus.Completed, DateTimeOffset.UtcNow.AddMinutes(-45), DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(45), 1, 1, 1, null, 1);
        _sessions.Setup(service => service.GetWorkoutHistoryAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutHistoryPageDto([item], 1, 20, 21));
        _sessions.Setup(service => service.GetWorkoutHistoryAsync(2, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutHistoryPageDto([item], 2, 20, 21));

        var cut = _context.Render<FitnessHistory>();
        cut.WaitForAssertion(() => Assert.Contains("Page 1 of 2", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Next")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Page 2 of 2", cut.Markup);
            _sessions.Verify(service => service.GetWorkoutHistoryAsync(2, 20, It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Fact]
    public void CompletedDetail_RendersSnapshotsSubstitutionSkippedAndReadOnlySets()
    {
        var performed = CreateExercise("Dumbbell Bench Press", ExerciseLoggingMode.AddedWeightAndReps,
            originalName: "Barbell Bench Press", setKind: WorkoutSetKind.WarmUp);
        var skipped = CreateExercise("Pull-Up", ExerciseLoggingMode.RepsOnly, isSkipped: true);
        var session = CreateCompletedSession([performed, skipped], []);
        _sessions.Setup(service => service.GetCompletedWorkoutAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var cut = _context.Render<FitnessHistoryDetail>(parameters => parameters.Add(item => item.SessionId, session.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Dumbbell Bench Press", cut.Markup);
            Assert.Contains("Substituted for Barbell Bench Press", cut.Markup);
            Assert.Contains("Skipped", cut.Markup);
            Assert.Contains("Warm-up", cut.Markup);
            Assert.DoesNotContain("Complete", cut.Markup);
            Assert.DoesNotContain("Remove", cut.Markup);
        });
    }

    [Fact]
    public void ExerciseHistory_RendersCurrentBestsAndOccurrences()
    {
        var exerciseId = Guid.NewGuid();
        var best = new StrengthRecordAchievement(StrengthRecordType.HeaviestWeight, false, 100m, 5, null, 95m, null, null);
        var history = new ExerciseHistoryDto(exerciseId, "Bench Press", ExerciseLoggingMode.WeightAndReps,
            [new PreviousPerformanceDto(exerciseId, "Dumbbell Bench Press", Guid.NewGuid(), "Push Day", new DateOnly(2026, 8, 1), DateTimeOffset.UtcNow, 1,
                [new WorkoutSetDto(Guid.NewGuid(), 1, WorkoutSetKind.Working, 100m, 5, null, DateTimeOffset.UtcNow)])], 1, 20, 1)
        { CurrentBests = [best] };
        _exercises.Setup(service => service.GetExerciseAsync(exerciseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExerciseDetailDto(exerciseId, "Bench Press", MuscleGroup.Chest, ExerciseEquipment.Barbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps, []));
        _sessions.Setup(service => service.GetExerciseHistoryAsync(exerciseId, 1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(history);

        var cut = _context.Render<FitnessExerciseHistory>(parameters => parameters.Add(item => item.ExerciseId, exerciseId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Current bests", cut.Markup);
            Assert.Contains("Heaviest weight", cut.Markup);
            Assert.Contains("100 kg", cut.Markup);
            Assert.Contains("Dumbbell Bench Press", cut.Markup);
            Assert.Contains("Push Day", cut.Markup);
        });
    }

    [Fact]
    public void ExerciseHistory_NoHistoryShowsNeutralState()
    {
        var exerciseId = Guid.NewGuid();
        _exercises.Setup(service => service.GetExerciseAsync(exerciseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExerciseDetailDto(exerciseId, "Plank", MuscleGroup.Core, ExerciseEquipment.Bodyweight, MovementPattern.Core, ExerciseLoggingMode.Duration, []));
        _sessions.Setup(service => service.GetExerciseHistoryAsync(exerciseId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExerciseHistoryDto(exerciseId, "Plank", ExerciseLoggingMode.Duration, [], 1, 20, 0));

        var cut = _context.Render<FitnessExerciseHistory>(parameters => parameters.Add(item => item.ExerciseId, exerciseId));

        cut.WaitForAssertion(() => Assert.Contains("No completed history for this exercise yet.", cut.Markup));
    }

    public void Dispose() => _context.Dispose();

    private static WorkoutSessionDetailDto CreateCompletedSession(
        IReadOnlyList<WorkoutSessionExerciseDto> exercises, IReadOnlyList<StrengthRecordAchievement> achievements) =>
        new(Guid.NewGuid(), null, "Push Day", new DateOnly(2026, 8, 10), DateTimeOffset.UtcNow.AddMinutes(-45),
            WorkoutSessionStatus.Completed, DateTimeOffset.UtcNow, null, SessionFeeling.Good, 5, null, null, null, exercises)
        { StrengthRecordAchievements = achievements };

    private static WorkoutSessionExerciseDto CreateExercise(
        string name, ExerciseLoggingMode mode, string? originalName = null,
        WorkoutSetKind setKind = WorkoutSetKind.Working, bool isSkipped = false)
    {
        var id = Guid.NewGuid();
        return new WorkoutSessionExerciseDto(Guid.NewGuid(), 1, Guid.NewGuid(), originalName ?? name, id, name, mode, 3, 8, 12, 90, isSkipped,
            isSkipped ? [] : [new WorkoutSetDto(Guid.NewGuid(), 1, setKind, mode is ExerciseLoggingMode.WeightAndReps or ExerciseLoggingMode.AddedWeightAndReps ? 20m : null, mode is ExerciseLoggingMode.Duration ? null : 8, mode is ExerciseLoggingMode.Duration ? 45 : null, DateTimeOffset.UtcNow)]);
    }
}
