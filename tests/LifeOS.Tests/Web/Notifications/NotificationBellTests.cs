using Bunit;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Layout;
using LifeOS.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LifeOS.Tests.Web.Notifications;

public sealed class NotificationBellTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly NotificationRefreshCoordinator _coordinator = new();

    public NotificationBellTests()
    {
        _context.Services.AddSingleton(_notifications.Object);
        _context.Services.AddSingleton(_coordinator);
    }

    [Fact]
    public void InitialLoad_DisplaysUnreadCount()
    {
        _notifications.Setup(service => service.GetUnreadCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var cut = _context.Render<NotificationBell>();

        Assert.Contains("3", cut.Markup);
        Assert.Contains("Notifications, 3 unread", cut.Markup);
    }

    [Fact]
    public void LargeCount_DisplaysCappedBadgeAndRealAccessibleCount()
    {
        _notifications.Setup(service => service.GetUnreadCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(127);

        var cut = _context.Render<NotificationBell>();

        Assert.Contains(">99+<", cut.Markup);
        Assert.Contains("Notifications, 127 unread", cut.Markup);
    }

    [Fact]
    public void ZeroCount_DoesNotDisplayNumericBadge()
    {
        _notifications.Setup(service => service.GetUnreadCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var cut = _context.Render<NotificationBell>();

        Assert.DoesNotContain("notification-bell__badge", cut.Markup);
    }

    [Fact]
    public void CoordinatorRefresh_ReloadsAuthoritativeCount()
    {
        var count = 1;
        _notifications.Setup(service => service.GetUnreadCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => count);

        var cut = _context.Render<NotificationBell>();
        count = 4;
        _coordinator.RequestRefresh();

        cut.WaitForAssertion(() => Assert.Contains(">4<", cut.Markup));
        _notifications.Verify(service => service.GetUnreadCountAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public void Failure_DoesNotPreventLaterRecovery()
    {
        var shouldFail = true;
        _notifications.Setup(service => service.GetUnreadCountAsync(It.IsAny<CancellationToken>()))
            .Returns(() => shouldFail
                ? Task.FromException<int>(new InvalidOperationException())
                : Task.FromResult(2));

        var cut = _context.Render<NotificationBell>();
        shouldFail = false;
        _coordinator.RequestRefresh();

        cut.WaitForAssertion(() => Assert.Contains(">2<", cut.Markup));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
