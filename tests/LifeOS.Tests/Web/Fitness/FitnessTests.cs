using Bunit;
using LifeOS.Core.DTOs.Fitness;
using LifeOS.Core.DTOs.WorkoutSessions;
using LifeOS.Core.DTOs.WorkoutTemplates;
using LifeOS.Core.Enums.Fitness;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FitnessPage = LifeOS.Web.Components.Pages.Fitness;
using FitnessExercisesPage = LifeOS.Web.Components.Pages.FitnessExercises;
using FitnessTemplateEditorPage = LifeOS.Web.Components.Pages.FitnessTemplateEditor;

namespace LifeOS.Tests.Web.Fitness;

public sealed class FitnessTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IWorkoutSessionService> _sessions = new();
    private readonly Mock<IWorkoutTemplateService> _templates = new();

    public FitnessTests()
    {
        _sessions.Setup(service => service.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkoutSessionDetailDto?)null);
        _templates.Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WorkoutTemplateSummaryDto>());
        var exercises = new Mock<IExerciseService>();
        exercises.Setup(service => service.GetExercisesAsync(It.IsAny<ExerciseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExerciseSummaryDto>());
        _context.Services.AddSingleton(_sessions.Object);
        _context.Services.AddSingleton(_templates.Object);
        _context.Services.AddSingleton(exercises.Object);
    }

    [Fact]
    public void FitnessPage_ShowsNoActiveWorkoutAndTemplateEmptyState()
    {
        var cut = _context.Render<FitnessPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ready to train?", cut.Markup);
            Assert.Contains("No workout templates yet.", cut.Markup);
            Assert.DoesNotContain("Resume workout", cut.Markup);
        });
    }

    [Fact]
    public void FitnessPage_ShowsResumeActionWhenWorkoutIsActive()
    {
        var sessionId = Guid.NewGuid();
        _sessions.Setup(service => service.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession(sessionId));

        var cut = _context.Render<FitnessPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Workout in progress", cut.Markup);
            Assert.Contains("Resume workout", cut.Markup);
        });
        cut.FindAll("button").Single(button => button.TextContent.Contains("Resume workout")).Click();
        Assert.EndsWith($"/fitness/workout/{sessionId}", _context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void ExistingTemplate_ConcurrencyConflictShowsFeedbackAndReloadsLatestVersion()
    {
        var templateId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        var latest = new WorkoutTemplateDetailDto(
            templateId,
            "Latest Push Day",
            4,
            [new WorkoutTemplateExerciseDto(Guid.NewGuid(), exerciseId, "Pull-Up", ExerciseLoggingMode.RepsOnly, 1, 3, 8, 12, 0)],
            DateTimeOffset.UtcNow);
        _templates.Setup(service => service.GetTemplateAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(latest);
        _templates.Setup(service => service.RenameTemplateAsync(
                templateId, It.IsAny<RenameWorkoutTemplateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkoutTemplateConcurrencyException());

        var cut = _context.Render<FitnessTemplateEditorPage>(parameters => parameters.Add(item => item.TemplateId, templateId));
        cut.WaitForAssertion(() => Assert.Contains("Latest Push Day", cut.Markup));
        cut.Find("#template-name").Change("Stale name");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("changed in another tab", cut.Markup);
            Assert.Contains("Version 4", cut.Markup);
            _templates.Verify(service => service.GetTemplateAsync(templateId, It.IsAny<CancellationToken>()), Times.AtLeast(2));
        });
    }

    [Fact]
    public void FitnessPage_StartTemplateNavigatesToActiveWorkout()
    {
        var templateId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        _templates.Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new WorkoutTemplateSummaryDto(templateId, "Push Day", 2, 3, DateTimeOffset.UtcNow)]);
        _sessions.Setup(service => service.StartFromTemplateAsync(
                It.IsAny<StartTemplateWorkoutDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession(sessionId));

        var cut = _context.Render<FitnessPage>();
        cut.WaitForAssertion(() => Assert.Contains("Push Day", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Start workout")).Click();

        cut.WaitForAssertion(() =>
            Assert.EndsWith($"/fitness/workout/{sessionId}", _context.Services.GetRequiredService<NavigationManager>().Uri));
    }

    public void Dispose() => _context.Dispose();

    private static WorkoutSessionDetailDto CreateSession(Guid id) => new(
        id,
        null,
        "Push Day",
        new DateOnly(2026, 8, 9),
        DateTimeOffset.UtcNow,
        WorkoutSessionStatus.InProgress,
        null,
        null,
        null,
        2,
        null,
        null,
        null,
        []);
}

public sealed class FitnessExercisesTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IExerciseService> _exercises = new();

    public FitnessExercisesTests()
    {
        _exercises.Setup(service => service.GetExercisesAsync(
                It.IsAny<ExerciseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseQuery query, CancellationToken _) =>
                string.Equals(query.Search, "bench", StringComparison.OrdinalIgnoreCase)
                    ? [new ExerciseSummaryDto(Guid.NewGuid(), "Barbell Bench Press", MuscleGroup.Chest, ExerciseEquipment.Barbell, MovementPattern.HorizontalPush, ExerciseLoggingMode.WeightAndReps)]
                    : Array.Empty<ExerciseSummaryDto>());
        _context.Services.AddSingleton(_exercises.Object);
    }

    [Fact]
    public void ExerciseLibrary_RendersMetadataAndUsesServerSearch()
    {
        var cut = _context.Render<FitnessExercisesPage>();
        cut.WaitForAssertion(() => Assert.Contains("No matching Exercises", cut.Markup));

        cut.Find("#exercise-search").Change(" bench ");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Barbell Bench Press", cut.Markup);
            Assert.Contains("Weight + Reps", cut.Markup);
            _exercises.Verify(service => service.GetExercisesAsync(
                It.Is<ExerciseQuery>(query => query.Search == "bench"), It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    public void Dispose() => _context.Dispose();
}

public sealed class FitnessTemplateEditorTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IExerciseService> _exercises = new();
    private readonly Mock<IWorkoutTemplateService> _templates = new();

    public FitnessTemplateEditorTests()
    {
        _exercises.Setup(service => service.GetExercisesAsync(
                It.IsAny<ExerciseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ExerciseSummaryDto(Guid.NewGuid(), "Pull-Up", MuscleGroup.BackLats, ExerciseEquipment.PullUpBar, MovementPattern.VerticalPull, ExerciseLoggingMode.RepsOnly)
            ]);
        _context.Services.AddSingleton(_exercises.Object);
        _context.Services.AddSingleton(_templates.Object);
    }

    [Fact]
    public void NewTemplate_AllowsDuplicateExerciseSelectionAndShowsRepTargets()
    {
        var cut = _context.Render<FitnessTemplateEditorPage>();
        cut.WaitForAssertion(() => Assert.Contains("Choose an Exercise", cut.Markup));

        var picker = cut.Find("#exercise-picker");
        var optionValue = picker.QuerySelectorAll("option")[1].GetAttribute("value");
        picker.Change(optionValue);
        cut.FindAll("button").Single(button => button.TextContent.Contains("Add Exercise")).Click();
        cut.Find("#exercise-picker").Change(optionValue);
        cut.FindAll("button").Single(button => button.TextContent.Contains("Add Exercise")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll("h3").Count(node => node.TextContent.Contains("Pull-Up")));
            Assert.Contains("Minimum reps", cut.Markup);
            Assert.Contains("Maximum reps", cut.Markup);
        });
    }

    public void Dispose() => _context.Dispose();
}
