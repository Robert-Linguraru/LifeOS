using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Constants;
using LifeOS.Core.DTOs.Notifications;
using LifeOS.Core.Exceptions;
using LifeOS.Core.Mappings;
using LifeOS.Core.Services;

namespace LifeOS.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationService(
        ICurrentUserService currentUser,
        INotificationRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUser = currentUser;
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetLatestNonDismissedAsync(
            GetCurrentUserId(),
            NotificationConstants.DefaultListLimit,
            cancellationToken);

        return notifications
            .Select(notification => notification.ToDto())
            .ToList();
    }

    public Task<int> GetUnreadCountAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetUnreadCountAsync(
            GetCurrentUserId(),
            cancellationToken);
    }

    public Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Notification ID cannot be empty.",
                nameof(notificationId));
        }

        return _repository.MarkAsReadAsync(
            GetCurrentUserId(),
            notificationId,
            _dateTimeProvider.UtcNow,
            cancellationToken);
    }

    public Task DismissAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Notification ID cannot be empty.",
                nameof(notificationId));
        }

        return _repository.DismissAsync(
            GetCurrentUserId(),
            notificationId,
            _dateTimeProvider.UtcNow,
            cancellationToken);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated ||
            _currentUser.UserId == Guid.Empty)
        {
            throw new CurrentUserUnavailableException();
        }

        return _currentUser.UserId;
    }
}
