using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class NotificationPersistenceIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public NotificationPersistenceIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ValidNotifications_PersistAndReload()
    {
        var userId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.AddRange(
                CreateNotification(userId),
                CreateNotification(
                    userId,
                    NotificationSourceType.Reminder,
                    sourceId));

            await context.SaveChangesAsync();
        }

        await using var reloadContext = _fixture.CreateDbContext();
        var notifications = await reloadContext.Notifications
            .Where(item => item.UserId == userId)
            .ToListAsync();

        Assert.Equal(2, notifications.Count);
        Assert.Contains(
            notifications,
            item => item.SourceType is null && item.SourceId is null);
        Assert.Contains(
            notifications,
            item => item.SourceType == NotificationSourceType.Reminder &&
                item.SourceId == sourceId);
    }

    [Fact]
    public async Task SourcePairConstraint_RejectsPartialValues()
    {
        await AssertRejectsAsync(
            CreateNotification(
                Guid.NewGuid(),
                NotificationSourceType.Reminder,
                null));
        await AssertRejectsAsync(
            CreateNotification(
                Guid.NewGuid(),
                null,
                Guid.NewGuid()));
    }

    [Fact]
    public async Task DismissedNotificationRequiresReadTimestamp()
    {
        var notification = CreateNotification(Guid.NewGuid());
        notification.DismissedAtUtc = notification.CreatedAtUtc.AddMinutes(1);

        await AssertRejectsAsync(notification);

        var valid = CreateNotification(Guid.NewGuid());
        valid.ReadAtUtc = valid.CreatedAtUtc.AddMinutes(1);
        valid.DismissedAtUtc = valid.CreatedAtUtc.AddMinutes(2);

        await using var context = _fixture.CreateDbContext();
        context.Notifications.Add(valid);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task IdempotencyKey_IsUniquePerUserOnly()
    {
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var first = CreateNotification(firstUser);
        first.IdempotencyKey = "same-key";
        var duplicate = CreateNotification(firstUser);
        duplicate.IdempotencyKey = "same-key";
        var differentUser = CreateNotification(secondUser);
        differentUser.IdempotencyKey = "same-key";

        await using var context = _fixture.CreateDbContext();
        context.Notifications.Add(first);
        context.Notifications.Add(differentUser);
        await context.SaveChangesAsync();

        context.Notifications.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SoftDeletedNotification_IsExcludedFromOrdinaryQueries()
    {
        var notification = CreateNotification(Guid.NewGuid());

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            context.Remove(notification);
            await context.SaveChangesAsync();
        }

        await using var reloadContext = _fixture.CreateDbContext();
        Assert.Null(await reloadContext.Notifications
            .SingleOrDefaultAsync(item => item.Id == notification.Id));
    }

    private async Task AssertRejectsAsync(Notification notification)
    {
        await using var context = _fixture.CreateDbContext();
        context.Notifications.Add(notification);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    private static Notification CreateNotification(
        Guid userId,
        NotificationSourceType? sourceType = null,
        Guid? sourceId = null)
    {
        return new Notification
        {
            UserId = userId,
            Type = NotificationType.ReminderDue,
            Title = "Reminder due",
            Message = "Message",
            SourceType = sourceType,
            SourceId = sourceId,
            IdempotencyKey = $"key-{Guid.NewGuid():N}"
        };
    }
}
