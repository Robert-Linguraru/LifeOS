using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Exceptions;
using LifeOS.Infrastructure.Services;
using Moq;

namespace LifeOS.Tests.Services;

public sealed class NotificationServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 14, 30, 0, TimeSpan.Zero);

    private readonly Mock<INotificationRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private NotificationService CreateService()
    {
        _currentUser.Setup(user => user.UserId).Returns(UserId);
        _currentUser.Setup(user => user.IsAuthenticated).Returns(true);
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);

        return new NotificationService(
            _currentUser.Object,
            _repository.Object,
            _dateTimeProvider.Object);
    }

    [Fact]
    public async Task GetNotificationsAsync_UsesCurrentUserAndDefaultLimitAndMapsResults()
    {
        var notification = CreateNotification();
        _repository
            .Setup(repository => repository.GetLatestNonDismissedAsync(
                UserId,
                NotificationConstants.DefaultListLimit,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([notification]);

        var result = await CreateService().GetNotificationsAsync();

        var dto = Assert.Single(result);
        Assert.Equal(notification.Id, dto.Id);
        Assert.Equal(notification.Title, dto.Title);
        _repository.VerifyAll();
    }

    [Fact]
    public async Task GetUnreadCountAsync_UsesCurrentUser()
    {
        _repository
            .Setup(repository => repository.GetUnreadCountAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await CreateService().GetUnreadCountAsync();

        Assert.Equal(3, result);
        _repository.VerifyAll();
    }

    [Fact]
    public async Task MarkAsReadAsync_ForwardsCurrentUserAndAuthoritativeTimestamp()
    {
        _repository
            .Setup(repository => repository.MarkAsReadAsync(
                UserId,
                It.IsAny<Guid>(),
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var notificationId = Guid.NewGuid();

        await CreateService().MarkAsReadAsync(notificationId);

        _repository.Verify(repository => repository.MarkAsReadAsync(
            UserId,
            notificationId,
            Now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DismissAsync_UsesOneAuthoritativeTimestamp()
    {
        _repository
            .Setup(repository => repository.DismissAsync(
                UserId,
                It.IsAny<Guid>(),
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var notificationId = Guid.NewGuid();

        await CreateService().DismissAsync(notificationId);

        _repository.Verify(repository => repository.DismissAsync(
            UserId,
            notificationId,
            Now,
            It.IsAny<CancellationToken>()), Times.Once);
        _dateTimeProvider.VerifyGet(provider => provider.UtcNow, Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_EmptyIdIsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().MarkAsReadAsync(Guid.Empty));

        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnauthenticatedUser_DoesNotAccessRepository()
    {
        var service = CreateService();
        _currentUser.Setup(user => user.IsAuthenticated).Returns(false);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(
            () => service.GetUnreadCountAsync());

        _repository.VerifyNoOtherCalls();
    }

    private static Notification CreateNotification()
    {
        return new Notification
        {
            UserId = UserId,
            Type = NotificationType.ReminderDue,
            Title = "Reminder",
            Message = "Message",
            IdempotencyKey = $"key-{Guid.NewGuid():N}"
        };
    }
}
