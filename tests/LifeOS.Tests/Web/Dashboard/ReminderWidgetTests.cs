using Bunit;
using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs.Dashboard;
using LifeOS.Core.DTOs.Reminders;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Dashboard;
using LifeOS.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LifeOS.Tests.Web.Dashboard;

public sealed class ReminderWidgetTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IDashboardService> _dashboard = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();
    private readonly NotificationRefreshCoordinator _coordinator = new();
    private readonly List<ReminderSummaryDto> _reminders = [];

    public ReminderWidgetTests()
    {
        _dashboard.Setup(service => service.GetReminderWidgetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new DashboardReminderWidgetDto
            {
                Reminders = _reminders.ToList()
            });
        _dateTime.SetupGet(service => service.UtcNow)
            .Returns(new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero));
        _context.Services.AddSingleton(_dashboard.Object);
        _context.Services.AddSingleton(_dateTime.Object);
        _context.Services.AddSingleton(_coordinator);
    }

    [Fact]
    public void EmptyState_ShowsOpenReminders()
    {
        var cut = _context.Render<ReminderWidget>();

        Assert.Contains("No upcoming reminders.", cut.Markup);
        Assert.Contains("Open reminders", cut.Markup);
    }

    [Fact]
    public void PopulatedState_ShowsReminderProjection()
    {
        _reminders.Add(new ReminderSummaryDto
        {
            Id = Guid.NewGuid(),
            Title = "Pay bill",
            ScheduledLocalDate = new DateOnly(2026, 8, 15),
            ScheduledLocalTime = new TimeOnly(17, 0),
            TimeZoneId = "UTC",
            SourceType = ReminderSourceType.Custom,
            ScheduledForUtc = new DateTimeOffset(2026, 8, 15, 17, 0, 0, TimeSpan.Zero)
        });

        var cut = _context.Render<ReminderWidget>();

        Assert.Contains("Pay bill", cut.Markup);
        Assert.Contains("15 Aug 2026", cut.Markup);
        Assert.Contains("UTC", cut.Markup);
    }

    [Fact]
    public void CoordinatorRefresh_ReloadsAndRerendersAuthoritativeData()
    {
        _reminders.Add(new ReminderSummaryDto
        {
            Id = Guid.NewGuid(),
            Title = "Pending reminder",
            SourceType = ReminderSourceType.Custom
        });
        var cut = _context.Render<ReminderWidget>();
        Assert.Contains("Pending reminder", cut.Markup);

        _reminders.Clear();
        _coordinator.RequestRefresh();

        cut.WaitForAssertion(() => Assert.Contains("No upcoming reminders.", cut.Markup));
    }

    [Fact]
    public void ErrorState_ProvidesRetryAndRecovers()
    {
        var fail = true;
        _dashboard.Setup(service => service.GetReminderWidgetAsync(It.IsAny<CancellationToken>()))
            .Returns(() => fail
                ? Task.FromException<DashboardReminderWidgetDto>(new InvalidOperationException())
                : Task.FromResult(new DashboardReminderWidgetDto()));

        var cut = _context.Render<ReminderWidget>();
        Assert.Contains("Reminders could not be loaded", cut.Markup);

        fail = false;
        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.Contains("No upcoming reminders.", cut.Markup));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
