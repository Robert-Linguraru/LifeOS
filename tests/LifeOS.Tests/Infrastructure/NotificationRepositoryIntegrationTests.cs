using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class NotificationRepositoryIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateTimeOffset FirstCreated =
        new(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainerFixture _fixture;

    public NotificationRepositoryIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLatestNonDismissedAsync_IsScopedFilteredOrderedAndLimited()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var oldest = CreateNotification(userId, FirstCreated);
        var newest = CreateNotification(userId, FirstCreated.AddMinutes(2));
        var dismissed = CreateNotification(userId, FirstCreated.AddMinutes(3));
        dismissed.DismissedAtUtc = FirstCreated.AddMinutes(4);
        dismissed.ReadAtUtc = FirstCreated.AddMinutes(4);
        var deleted = CreateNotification(userId, FirstCreated.AddMinutes(5));
        var otherUser = CreateNotification(otherUserId, FirstCreated.AddMinutes(6));

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.AddRange(
                oldest, newest, dismissed, deleted, otherUser);
            await context.SaveChangesAsync();

            await context.Notifications
                .Where(notification => notification.Id == oldest.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        notification => notification.CreatedAtUtc,
                        FirstCreated));
            await context.Notifications
                .Where(notification => notification.Id == newest.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        notification => notification.CreatedAtUtc,
                        FirstCreated.AddMinutes(2)));

            context.Remove(deleted);
            await context.SaveChangesAsync();
        }

        var repository = new NotificationRepository(
            _fixture.CreateDbContextFactory());
        var result = await repository.GetLatestNonDismissedAsync(userId, 1);

        var notification = Assert.Single(result);
        Assert.Equal(newest.Id, notification.Id);
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyCurrentUsersUnreadNotifications()
    {
        var userId = Guid.NewGuid();
        var unread = CreateNotification(userId, FirstCreated);
        var read = CreateNotification(userId, FirstCreated.AddMinutes(1));
        read.ReadAtUtc = FirstCreated.AddMinutes(2);
        var dismissed = CreateNotification(userId, FirstCreated.AddMinutes(3));
        dismissed.ReadAtUtc = FirstCreated.AddMinutes(4);
        dismissed.DismissedAtUtc = FirstCreated.AddMinutes(5);
        var deleted = CreateNotification(userId, FirstCreated.AddMinutes(6));
        var otherUser = CreateNotification(Guid.NewGuid(), FirstCreated.AddMinutes(7));

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.AddRange(
                unread, read, dismissed, deleted, otherUser);
            await context.SaveChangesAsync();
            context.Remove(deleted);
            await context.SaveChangesAsync();
        }

        var repository = new NotificationRepository(
            _fixture.CreateDbContextFactory());

        Assert.Equal(1, await repository.GetUnreadCountAsync(userId));
    }

    [Fact]
    public async Task MarkAsReadAsync_IsIdempotentAndDoesNotMutateDismissedOrForeignNotifications()
    {
        var userId = Guid.NewGuid();
        var readAt = FirstCreated.AddMinutes(10);
        var originalReadAt = FirstCreated.AddMinutes(1);
        var alreadyRead = CreateNotification(userId, FirstCreated);
        alreadyRead.ReadAtUtc = originalReadAt;
        var dismissed = CreateNotification(userId, FirstCreated.AddMinutes(2));
        dismissed.ReadAtUtc = originalReadAt;
        dismissed.DismissedAtUtc = FirstCreated.AddMinutes(3);
        var foreign = CreateNotification(Guid.NewGuid(), FirstCreated.AddMinutes(4));

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.AddRange(alreadyRead, dismissed, foreign);
            await context.SaveChangesAsync();
        }

        var repository = new NotificationRepository(
            _fixture.CreateDbContextFactory());
        await repository.MarkAsReadAsync(userId, alreadyRead.Id, readAt);
        await repository.MarkAsReadAsync(userId, alreadyRead.Id, readAt.AddMinutes(1));
        await repository.MarkAsReadAsync(userId, dismissed.Id, readAt);
        await repository.MarkAsReadAsync(userId, foreign.Id, readAt);
        await repository.MarkAsReadAsync(userId, Guid.NewGuid(), readAt);

        await using var reload = _fixture.CreateDbContext();
        var persistedRead = await reload.Notifications
            .IgnoreQueryFilters()
            .SingleAsync(notification => notification.Id == alreadyRead.Id);
        var persistedDismissed = await reload.Notifications
            .IgnoreQueryFilters()
            .SingleAsync(notification => notification.Id == dismissed.Id);
        var persistedForeign = await reload.Notifications
            .IgnoreQueryFilters()
            .SingleAsync(notification => notification.Id == foreign.Id);

        Assert.Equal(originalReadAt, persistedRead.ReadAtUtc);
        Assert.Equal(originalReadAt, persistedDismissed.ReadAtUtc);
        Assert.Null(persistedForeign.ReadAtUtc);
    }

    [Fact]
    public async Task DismissAsync_MakesUnreadReadAndPreservesExistingTimestamps()
    {
        var userId = Guid.NewGuid();
        var unread = CreateNotification(userId, FirstCreated);
        var readAt = FirstCreated.AddMinutes(1);
        var read = CreateNotification(userId, FirstCreated.AddMinutes(2));
        read.ReadAtUtc = readAt;
        var foreign = CreateNotification(Guid.NewGuid(), FirstCreated.AddMinutes(3));

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.AddRange(unread, read, foreign);
            await context.SaveChangesAsync();
        }

        var repository = new NotificationRepository(
            _fixture.CreateDbContextFactory());
        var dismissedAt = FirstCreated.AddMinutes(10);
        await repository.DismissAsync(userId, unread.Id, dismissedAt);
        await repository.DismissAsync(userId, unread.Id, dismissedAt.AddMinutes(1));
        await repository.DismissAsync(userId, read.Id, dismissedAt);
        await repository.DismissAsync(userId, foreign.Id, dismissedAt);
        await repository.DismissAsync(userId, Guid.NewGuid(), dismissedAt);

        await using var reload = _fixture.CreateDbContext();
        var persistedUnread = await reload.Notifications
            .SingleAsync(notification => notification.Id == unread.Id);
        var persistedRead = await reload.Notifications
            .SingleAsync(notification => notification.Id == read.Id);
        var persistedForeign = await reload.Notifications
            .SingleAsync(notification => notification.Id == foreign.Id);

        Assert.Equal(dismissedAt, persistedUnread.ReadAtUtc);
        Assert.Equal(dismissedAt, persistedUnread.DismissedAtUtc);
        Assert.Equal(readAt, persistedRead.ReadAtUtc);
        Assert.Equal(dismissedAt, persistedRead.DismissedAtUtc);
        Assert.Null(persistedForeign.DismissedAtUtc);
        Assert.Null(persistedForeign.ReadAtUtc);
    }

    [Fact]
    public async Task ConcurrentMarkAsRead_LeavesOneStableReadTimestamp()
    {
        var userId = Guid.NewGuid();
        var notification = CreateNotification(userId, FirstCreated);
        await SeedAsync(notification);
        var repository = new NotificationRepository(_fixture.CreateDbContextFactory());
        var firstTimestamp = FirstCreated.AddMinutes(10);
        var secondTimestamp = FirstCreated.AddMinutes(11);

        await Task.WhenAll(
            repository.MarkAsReadAsync(userId, notification.Id, firstTimestamp),
            repository.MarkAsReadAsync(userId, notification.Id, secondTimestamp));

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Notifications.SingleAsync(item => item.Id == notification.Id);
        Assert.NotNull(persisted.ReadAtUtc);
        Assert.Contains(persisted.ReadAtUtc.Value, new[] { firstTimestamp, secondTimestamp });
        Assert.Null(persisted.DismissedAtUtc);
    }

    [Fact]
    public async Task ConcurrentDismissals_LeaveOneStableDismissalAndReadTimestamp()
    {
        var userId = Guid.NewGuid();
        var notification = CreateNotification(userId, FirstCreated);
        await SeedAsync(notification);
        var repository = new NotificationRepository(_fixture.CreateDbContextFactory());
        var firstTimestamp = FirstCreated.AddMinutes(10);
        var secondTimestamp = FirstCreated.AddMinutes(11);

        await Task.WhenAll(
            repository.DismissAsync(userId, notification.Id, firstTimestamp),
            repository.DismissAsync(userId, notification.Id, secondTimestamp));

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Notifications.SingleAsync(item => item.Id == notification.Id);
        Assert.Equal(persisted.ReadAtUtc, persisted.DismissedAtUtc);
        Assert.Contains(persisted.DismissedAtUtc.Value, new[] { firstTimestamp, secondTimestamp });
    }

    [Fact]
    public async Task ConcurrentMarkReadAndDismiss_ConvergesToReadAndDismissed()
    {
        var userId = Guid.NewGuid();
        var notification = CreateNotification(userId, FirstCreated);
        await SeedAsync(notification);
        var repository = new NotificationRepository(_fixture.CreateDbContextFactory());
        var readTimestamp = FirstCreated.AddMinutes(10);
        var dismissTimestamp = FirstCreated.AddMinutes(11);

        await Task.WhenAll(
            repository.MarkAsReadAsync(userId, notification.Id, readTimestamp),
            repository.DismissAsync(userId, notification.Id, dismissTimestamp));

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Notifications.SingleAsync(item => item.Id == notification.Id);
        Assert.NotNull(persisted.ReadAtUtc);
        Assert.NotNull(persisted.DismissedAtUtc);
    }

    private async Task SeedAsync(Notification notification)
    {
        await using var context = _fixture.CreateDbContext();
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
    }

    private static Notification CreateNotification(
        Guid userId,
        DateTimeOffset createdAtUtc)
    {
        return new Notification
        {
            UserId = userId,
            Type = NotificationType.ReminderDue,
            Title = "Notification",
            Message = "Message",
            IdempotencyKey = $"key-{Guid.NewGuid():N}",
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };
    }
}
