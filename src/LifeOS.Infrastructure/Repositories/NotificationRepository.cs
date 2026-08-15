using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public NotificationRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Notification>> GetLatestNonDismissedAsync(
        Guid userId,
        int limit = NotificationConstants.DefaultListLimit,
        CancellationToken cancellationToken = default)
    {
        var boundedLimit = Math.Clamp(
            limit,
            1,
            NotificationConstants.DefaultListLimit);

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId &&
                notification.DismissedAtUtc == null)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Notifications
            .Where(notification =>
                notification.UserId == userId &&
                notification.DismissedAtUtc == null &&
                notification.ReadAtUtc == null)
            .CountAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Notifications
            .Where(notification =>
                notification.Id == notificationId &&
                notification.UserId == userId &&
                notification.ReadAtUtc == null &&
                notification.DismissedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        notification => notification.ReadAtUtc,
                        readAtUtc)
                    .SetProperty(
                        notification => notification.UpdatedAtUtc,
                        readAtUtc),
                cancellationToken);
    }

    public async Task DismissAsync(
        Guid userId,
        Guid notificationId,
        DateTimeOffset dismissedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Notifications
            .Where(notification =>
                notification.Id == notificationId &&
                notification.UserId == userId &&
                notification.DismissedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        notification => notification.ReadAtUtc,
                        notification => notification.ReadAtUtc ?? dismissedAtUtc)
                    .SetProperty(
                        notification => notification.DismissedAtUtc,
                        dismissedAtUtc)
                    .SetProperty(
                        notification => notification.UpdatedAtUtc,
                        dismissedAtUtc),
                cancellationToken);
    }
}
