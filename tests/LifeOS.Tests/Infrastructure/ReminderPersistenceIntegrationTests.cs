using LifeOS.Core.Entities;
using LifeOS.Core.Constants;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class ReminderPersistenceIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public ReminderPersistenceIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CustomReminder_PersistsWithPendingLifecycle()
    {
        var reminder = CreateReminder(Guid.NewGuid());

        await using var context = _fixture.CreateDbContext();
        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();

        var saved = await context.Reminders
            .SingleAsync(item => item.Id == reminder.Id);

        Assert.Equal(ReminderSourceType.Custom, saved.SourceType);
        Assert.Null(saved.SourceId);
        Assert.Null(saved.SourceTitle);
        Assert.Equal(ReminderStatus.Pending, saved.Status);
        Assert.Null(saved.FiredAtUtc);
        Assert.Null(saved.NotificationId);
    }

    [Fact]
    public async Task TaskAndHabitReminders_PersistSourceSnapshotsWithoutForeignKeys()
    {
        var taskReminder = CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Task,
            Guid.NewGuid(),
            "Task snapshot");
        var habitReminder = CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Habit,
            Guid.NewGuid(),
            "Habit snapshot");

        await using var context = _fixture.CreateDbContext();
        context.Reminders.AddRange(taskReminder, habitReminder);
        await context.SaveChangesAsync();

        Assert.Equal(
            2,
            await context.Reminders.CountAsync(item =>
                item.Id == taskReminder.Id || item.Id == habitReminder.Id));
    }

    [Fact]
    public async Task SourceConstraint_RejectsInvalidShapes()
    {
        await AssertRejectsAsync(CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Task,
            null,
            "Task snapshot"));
        await AssertRejectsAsync(CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Task,
            Guid.NewGuid(),
            null));
        await AssertRejectsAsync(CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Habit,
            null,
            "Habit snapshot"));
        await AssertRejectsAsync(CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Habit,
            Guid.NewGuid(),
            null));
        await AssertRejectsAsync(CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Custom,
            Guid.NewGuid(),
            null));
        await AssertRejectsAsync(CreateReminder(
            Guid.NewGuid(),
            ReminderSourceType.Custom,
            null,
            "Unexpected snapshot"));
    }

    [Fact]
    public async Task LifecycleConstraints_RejectInvalidStates()
    {
        var pendingWithFiredAt = CreateReminder(Guid.NewGuid());
        pendingWithFiredAt.FiredAtUtc = pendingWithFiredAt.ScheduledForUtc;
        await AssertRejectsAsync(pendingWithFiredAt);

        var pendingWithNotification = CreateReminder(Guid.NewGuid());
        pendingWithNotification.NotificationId = Guid.NewGuid();
        await AssertRejectsAsync(pendingWithNotification);

        var cancelledWithFiredAt = CreateReminder(Guid.NewGuid());
        cancelledWithFiredAt.Status = ReminderStatus.Cancelled;
        cancelledWithFiredAt.FiredAtUtc = cancelledWithFiredAt.ScheduledForUtc;
        await AssertRejectsAsync(cancelledWithFiredAt);

        var firedWithoutTimestamp = CreateReminder(Guid.NewGuid());
        firedWithoutTimestamp.Status = ReminderStatus.Fired;
        firedWithoutTimestamp.NotificationId = Guid.NewGuid();
        await AssertRejectsAsync(firedWithoutTimestamp);

        var firedWithoutNotification = CreateReminder(Guid.NewGuid());
        firedWithoutNotification.Status = ReminderStatus.Fired;
        firedWithoutNotification.FiredAtUtc = firedWithoutNotification.ScheduledForUtc;
        await AssertRejectsAsync(firedWithoutNotification);
    }

    [Fact]
    public async Task FiredReminder_AcceptsValidNotificationLink()
    {
        var userId = Guid.NewGuid();
        var notification = CreateNotification(userId);
        var reminder = CreateReminder(userId);
        reminder.Status = ReminderStatus.Fired;
        reminder.FiredAtUtc = reminder.ScheduledForUtc;
        reminder.NotificationId = notification.Id;

        await using var context = _fixture.CreateDbContext();
        context.Notifications.Add(notification);
        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task NotificationLinkAndIdempotencyConstraintsAreEnforced()
    {
        var userId = Guid.NewGuid();
        var notification = CreateNotification(userId);
        var first = CreateReminder(userId);
        first.Status = ReminderStatus.Fired;
        first.FiredAtUtc = first.ScheduledForUtc;
        first.NotificationId = notification.Id;
        var duplicateLink = CreateReminder(userId);
        duplicateLink.Status = ReminderStatus.Fired;
        duplicateLink.FiredAtUtc = duplicateLink.ScheduledForUtc;
        duplicateLink.NotificationId = notification.Id;

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.Add(notification);
            context.Reminders.Add(first);
            await context.SaveChangesAsync();
        }

        await AssertRejectsAsync(duplicateLink);

        var duplicateKey = CreateReminder(Guid.NewGuid());
        duplicateKey.IdempotencyKey = first.IdempotencyKey;
        await using var differentUserContext = _fixture.CreateDbContext();
        differentUserContext.Reminders.Add(duplicateKey);
        await differentUserContext.SaveChangesAsync();
    }

    [Fact]
    public async Task NegativeVersion_IsRejected()
    {
        var reminder = CreateReminder(Guid.NewGuid());
        reminder.Version = -1;

        await AssertRejectsAsync(reminder);
    }

    [Fact]
    public async Task SoftDeletedReminder_IsExcludedFromOrdinaryQueries()
    {
        var reminder = CreateReminder(Guid.NewGuid());

        await using (var context = _fixture.CreateDbContext())
        {
            context.Reminders.Add(reminder);
            await context.SaveChangesAsync();
            context.Remove(reminder);
            await context.SaveChangesAsync();
        }

        await using var reloadContext = _fixture.CreateDbContext();
        Assert.Null(await reloadContext.Reminders
            .SingleOrDefaultAsync(item => item.Id == reminder.Id));
    }

    [Fact]
    public async Task Version_IsAnOptimisticConcurrencyToken()
    {
        var reminder = CreateReminder(Guid.NewGuid());

        await using (var seedContext = _fixture.CreateDbContext())
        {
            seedContext.Reminders.Add(reminder);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var first = await firstContext.Reminders
            .SingleAsync(item => item.Id == reminder.Id);
        var second = await secondContext.Reminders
            .SingleAsync(item => item.Id == reminder.Id);

        first.Title = "First update";
        first.Version++;
        second.Title = "Stale update";
        second.Version++;

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task PendingQuery_ReturnsNextThreeForUserAndExcludesTerminalDeletedAndForeignRows()
    {
        var userId = Guid.NewGuid();
        var first = CreateReminder(userId);
        first.Title = "First";
        first.ScheduledForUtc = first.ScheduledForUtc.AddHours(1);
        var second = CreateReminder(userId);
        second.Title = "Second";
        second.ScheduledForUtc = second.ScheduledForUtc.AddHours(2);
        var third = CreateReminder(userId);
        third.Title = "Third";
        third.ScheduledForUtc = third.ScheduledForUtc.AddHours(3);
        var fourth = CreateReminder(userId);
        fourth.Title = "Fourth";
        fourth.ScheduledForUtc = fourth.ScheduledForUtc.AddHours(4);
        var fifth = CreateReminder(userId);
        fifth.Title = "Fifth";
        fifth.ScheduledForUtc = fifth.ScheduledForUtc.AddHours(5);
        var firedNotification = CreateNotification(userId);
        var fired = CreateReminder(userId);
        fired.Status = ReminderStatus.Fired;
        fired.FiredAtUtc = fired.ScheduledForUtc;
        fired.NotificationId = firedNotification.Id;
        var cancelled = CreateReminder(userId);
        cancelled.Status = ReminderStatus.Cancelled;
        var deleted = CreateReminder(userId);
        var foreign = CreateReminder(Guid.NewGuid());

        await using (var context = _fixture.CreateDbContext())
        {
            context.Reminders.AddRange(
                first, second, third, fourth, fifth,
                fired, cancelled, deleted, foreign);
            context.Notifications.Add(firedNotification);
            await context.SaveChangesAsync();
            context.Remove(deleted);
            await context.SaveChangesAsync();
        }

        var reminders = await new ReminderRepository(
                _fixture.CreateDbContextFactory())
            .GetPendingAsync(userId, ReminderConstants.DashboardListLimit);

        Assert.Equal(
            new[] { first.Id, second.Id, third.Id },
            reminders.Select(reminder => reminder.Id));
        Assert.DoesNotContain(reminders, reminder => reminder.Id == fourth.Id);
        Assert.DoesNotContain(reminders, reminder => reminder.Id == fifth.Id);
        Assert.DoesNotContain(reminders, reminder => reminder.Id == fired.Id);
        Assert.DoesNotContain(reminders, reminder => reminder.Id == cancelled.Id);
        Assert.DoesNotContain(reminders, reminder => reminder.Id == deleted.Id);
        Assert.DoesNotContain(reminders, reminder => reminder.Id == foreign.Id);
    }

    private async Task AssertRejectsAsync(Reminder reminder)
    {
        await using var context = _fixture.CreateDbContext();
        context.Reminders.Add(reminder);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    private static Reminder CreateReminder(
        Guid userId,
        ReminderSourceType sourceType = ReminderSourceType.Custom,
        Guid? sourceId = null,
        string? sourceTitle = null)
    {
        return new Reminder
        {
            UserId = userId,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceTitle = sourceTitle,
            Title = "Reminder",
            Message = "Message",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = "UTC",
            ScheduledForUtc = new DateTimeOffset(
                2026,
                8,
                20,
                14,
                30,
                0,
                TimeSpan.Zero),
            IdempotencyKey = $"reminder-{Guid.NewGuid():N}"
        };
    }

    private static Notification CreateNotification(Guid userId)
    {
        return new Notification
        {
            UserId = userId,
            Type = NotificationType.ReminderDue,
            Title = "Reminder due",
            Message = "Message",
            IdempotencyKey = $"notification-{Guid.NewGuid():N}"
        };
    }
}
