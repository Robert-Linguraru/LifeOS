using Bunit;
using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Dashboard;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Layout;
using LifeOS.Web.Components.Pages;
using LifeOS.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DashboardPage = LifeOS.Web.Components.Pages.Dashboard;

namespace LifeOS.Tests.Web.Dashboard;

public sealed class DashboardFinanceCompositionTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<IDashboardService> _dashboard = new();
    private readonly Mock<IUserSettingsService> _settings = new();

    public DashboardFinanceCompositionTests()
    {
        _dashboard.Setup(service => service.GetTaskWidgetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardTaskWidgetDto());
        _dashboard.Setup(service => service.GetHabitWidgetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardHabitWidgetDto());
        _dashboard.Setup(service => service.GetXpWidgetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardXpWidgetDto());
        _dashboard.Setup(service => service.GetReminderWidgetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardReminderWidgetDto());
        _dashboard.Setup(service => service.GetFinanceWidgetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardFinanceWidgetDto());
        _settings.Setup(service => service.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { Currency = "USD", TimeZoneId = "UTC" });

        _context.Services.AddSingleton(_dashboard.Object);
        _context.Services.AddSingleton(_settings.Object);
        _context.Services.AddSingleton(new Mock<ITaskService>().Object);
        _context.Services.AddSingleton(new Mock<IHabitService>().Object);
        _context.Services.AddSingleton(new Mock<IDateTimeProvider>().Object);
        _context.Services.AddSingleton(new NotificationRefreshCoordinator());
    }

    [Fact]
    public void Dashboard_ComposesFinanceAlongsideExistingWidgets()
    {
        var cut = _context.Render<DashboardPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Finance", cut.Markup);
            Assert.Contains("Tasks", cut.Markup);
            Assert.Contains("Habits", cut.Markup);
            Assert.Contains("XP Progress", cut.Markup);
            Assert.Contains("Reminders", cut.Markup);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
