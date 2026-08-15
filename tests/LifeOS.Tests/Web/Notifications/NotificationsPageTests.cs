using Bunit;
using LifeOS.Core.Abstractions;
using LifeOS.Core.DTOs;
using LifeOS.Core.DTOs.Notifications;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Services;
using LifeOS.Web.Components.Pages;
using LifeOS.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationsPage = LifeOS.Web.Components.Pages.Notifications;

namespace LifeOS.Tests.Web.Notifications;

public sealed class NotificationsPageTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IUserSettingsService> _settings = new();
    private readonly Mock<IDateTimeProvider> _dateTime = new();
    private readonly NotificationRefreshCoordinator _coordinator = new();
    private readonly List<NotificationDto> _items = [];

    public NotificationsPageTests()
    {
        _settings.Setup(service => service.GetCurrentUserSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto { TimeZoneId = "UTC" });
        _dateTime.Setup(service => service.IsValidTimeZone("UTC")).Returns(true);
        _dateTime.Setup(service => service.ConvertUtcToLocal(It.IsAny<DateTimeOffset>(), "UTC"))
            .Returns((DateTimeOffset value, string _) => value);
        _notifications.Setup(service => service.GetNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _items.ToList());
        _context.Services.AddSingleton(_notifications.Object);
        _context.Services.AddSingleton(_settings.Object);
        _context.Services.AddSingleton(_dateTime.Object);
        _context.Services.AddSingleton(_coordinator);
    }

    [Fact]
    public void EmptyState_IsShownAfterLoading()
    {
        var cut = _context.Render<NotificationsPage>();

        Assert.Contains("You're all caught up", cut.Markup);
    }

    [Fact]
    public void PopulatedState_ShowsUnreadAndReadPresentation()
    {
        _items.Add(new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = "Level up!",
            Message = "You reached level 3.",
            Type = NotificationType.LevelUp,
            CreatedAtUtc = new DateTimeOffset(2026, 8, 15, 18, 31, 0, TimeSpan.Zero)
        });
        _items.Add(new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = "Older",
            Message = "Already read",
            Type = NotificationType.EchelonChanged,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReadAtUtc = DateTimeOffset.UtcNow
        });

        var cut = _context.Render<NotificationsPage>();

        Assert.Contains("Level up!", cut.Markup);
        Assert.Contains("Unread", cut.Markup);
        Assert.Contains("Older", cut.Markup);
        Assert.Contains("Read", cut.Markup);
        Assert.Contains("15 Aug 2026 18:31", cut.Markup);
    }

    [Fact]
    public void MarkRead_ReloadsAndPublishesRefresh()
    {
        var item = new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = "Read me",
            Message = "Message",
            Type = NotificationType.LevelUp
        };
        _items.Add(item);
        var refreshes = 0;
        _coordinator.RefreshRequested += (_, _) => refreshes++;
        _notifications.Setup(service => service.MarkAsReadAsync(item.Id, It.IsAny<CancellationToken>()))
            .Callback(() => _items.Clear())
            .Returns(Task.CompletedTask);

        var cut = _context.Render<NotificationsPage>();
        cut.Find("button.secondary-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("You're all caught up", cut.Markup);
            Assert.Equal(1, refreshes);
        });
        _notifications.Verify(service => service.MarkAsReadAsync(item.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dismiss_RemovesItemAfterAuthoritativeReload()
    {
        var item = new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = "Dismiss me",
            Message = "Message",
            Type = NotificationType.ReminderDue
        };
        _items.Add(item);
        _notifications.Setup(service => service.DismissAsync(item.Id, It.IsAny<CancellationToken>()))
            .Callback(() => _items.Clear())
            .Returns(Task.CompletedTask);

        var cut = _context.Render<NotificationsPage>();
        cut.Find("button.danger-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("You're all caught up", cut.Markup));
        _notifications.Verify(service => service.DismissAsync(item.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ReminderSource_RendersOpenReminderAction()
    {
        var reminderId = Guid.NewGuid();
        _items.Add(new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = "Reminder due",
            Message = "Do it",
            Type = NotificationType.ReminderDue,
            SourceType = NotificationSourceType.Reminder,
            SourceId = reminderId
        });

        var cut = _context.Render<NotificationsPage>();

        Assert.Contains("Open reminder", cut.Markup);
        cut.Find("button.notification-list-item__source").Click();
        Assert.EndsWith(
            $"/reminders/{reminderId:D}/edit",
            _context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
