using Bunit;
using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Habits;
using LifeOS.Core.DTOs.Reminders;
using LifeOS.Core.DTOs.Tasks;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Core.Enums.Tasks;
using LifeOS.Core.Services;
using LifeOS.Core.Time;
using LifeOS.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LifeOS.Tests.Web.Reminders;

public sealed class ReminderEditorTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IReminderService> _reminders = new();
    private readonly Mock<ITaskService> _tasks = new();
    private readonly Mock<IHabitService> _habits = new();
    private readonly Mock<IUserSettingsService> _settings = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();

    public ReminderEditorTests()
    {
        _settings.Setup(service => service.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto
            {
                TimeZoneId = "UTC",
                TimeZoneConfiguredAtUtc = DateTimeOffset.UtcNow
            });
        _dateTime.Setup(service => service.GetTimeZoneIds()).Returns(["UTC"]);
        _dateTime.Setup(service => service.IsValidTimeZone("UTC")).Returns(true);
        _dateTime.SetupGet(service => service.UtcNow)
            .Returns(new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero));
        _dateTime.Setup(service => service.ConvertLocalToUtc(
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), "UTC"))
            .Returns((DateOnly date, TimeOnly time, string _) =>
                LocalTimeConversionResult.Success(new DateTimeOffset(
                    date.ToDateTime(time), TimeSpan.Zero)));

        _context.Services.AddSingleton(_reminders.Object);
        _context.Services.AddSingleton(_tasks.Object);
        _context.Services.AddSingleton(_habits.Object);
        _context.Services.AddSingleton(_settings.Object);
        _context.Services.AddSingleton(_dateTime.Object);
    }

    [Fact]
    public void GenericCreate_IsCustomOnly()
    {
        var cut = _context.Render<ReminderEditor>();

        Assert.Contains("Custom reminder", cut.Markup);
        Assert.DoesNotContain("Task —", cut.Markup);
        Assert.DoesNotContain("Habit —", cut.Markup);
        Assert.Empty(cut.FindAll("#reminder-source-type"));
    }

    [Fact]
    public void UnconfirmedTimezone_ShowsGateAndSettingsLink()
    {
        _settings.Setup(service => service.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { TimeZoneId = "UTC" });

        var cut = _context.Render<ReminderEditor>();

        Assert.Contains("Confirm your time zone first", cut.Markup);
        Assert.Contains("href=\"/settings\"", cut.Markup);
        Assert.Empty(cut.FindAll("#reminder-title"));
    }

    [Fact]
    public void TaskSource_PrefillsSourceTitleAndFutureDueValues()
    {
        var taskId = Guid.NewGuid();
        _tasks.Setup(service => service.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskDetailsDto
            {
                Id = taskId,
                Title = "Submit tax documents",
                Status = TaskItemStatus.Active,
                DueDate = new DateOnly(2026, 8, 15),
                DueTime = new TimeOnly(15, 0)
            });

        _context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/reminders/new?sourceType=Task&sourceId={taskId:D}");
        var cut = _context.Render<ReminderEditor>();

        Assert.Single(cut.FindAll(".reminder-editor__source"));
        Assert.Contains("Task — Submit tax documents", cut.Markup);
        Assert.Equal("Submit tax documents", cut.Find("#reminder-title").GetAttribute("value"));
        Assert.Equal("2026-08-15", cut.Find("#reminder-date").GetAttribute("value"));
        Assert.Contains("15:00", cut.Find("#reminder-time").GetAttribute("value"));
        Assert.Empty(cut.FindAll("#reminder-source-type"));
    }

    [Fact]
    public void HabitSource_PrefillsTitleWithoutInventingSchedule()
    {
        var habitId = Guid.NewGuid();
        _habits.Setup(service => service.GetHabitByIdAsync(habitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HabitDetailsDto
            {
                Id = habitId,
                Name = "Morning walk",
                IsActive = true
            });

        _context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/reminders/new?sourceType=Habit&sourceId={habitId:D}");
        var cut = _context.Render<ReminderEditor>();

        Assert.Single(cut.FindAll(".reminder-editor__source"));
        Assert.Contains("Habit — Morning walk", cut.Markup);
        Assert.Equal("Morning walk", cut.Find("#reminder-title").GetAttribute("value"));
        Assert.Empty(cut.FindAll("#reminder-source-type"));
    }

    [Fact]
    public void FiredReminder_IsReadOnly()
    {
        var reminderId = Guid.NewGuid();
        _reminders.Setup(service => service.GetDetailsAsync(reminderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReminderDetailsDto
            {
                Id = reminderId,
                Title = "Fired reminder",
                Status = ReminderStatus.Fired,
                SourceType = ReminderSourceType.Custom,
                TimeZoneId = "UTC",
                ScheduledLocalDate = new DateOnly(2026, 8, 15),
                ScheduledLocalTime = new TimeOnly(15, 0),
                Version = 1
            });

        var cut = _context.Render<ReminderEditor>(parameters => parameters
            .Add(component => component.ReminderId, reminderId));

        Assert.Contains("can no longer be edited", cut.Markup);
        Assert.Empty(cut.FindAll("button[type=\"submit\"]"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
