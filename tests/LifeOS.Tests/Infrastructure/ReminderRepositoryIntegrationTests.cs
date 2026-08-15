using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class ReminderRepositoryIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateTimeOffset Scheduled =
        new(2026, 8, 20, 14, 30, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainerFixture _fixture;

    public ReminderRepositoryIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAndGet_IsUserScopedAndSoftDeleteAware()
    {
        var userId = Guid.NewGuid();
        var reminder = CreateReminder(userId);
        await SeedAsync(reminder);

        var repository = CreateRepository();
        Assert.Equal(reminder.Id, (await repository.GetByIdAsync(userId, reminder.Id))!.Id);
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid(), reminder.Id));

        await using (var context = _fixture.CreateDbContext())
        {
            var tracked = await context.Reminders.IgnoreQueryFilters()
                .SingleAsync(item => item.Id == reminder.Id);
            context.Remove(tracked);
            await context.SaveChangesAsync();
        }

        Assert.Null(await repository.GetByIdAsync(userId, reminder.Id));
    }

    [Fact]
    public async Task GetPending_IsFilteredOrderedAndBounded()
    {
        var userId = Guid.NewGuid();
        var first = CreateReminder(userId, Scheduled.AddHours(1));
        var second = CreateReminder(userId, Scheduled);
        var fired = CreateReminder(userId, Scheduled.AddHours(2));
        fired.Status = ReminderStatus.Fired;
        fired.FiredAtUtc = Scheduled;
        fired.NotificationId = Guid.NewGuid();
        var cancelled = CreateReminder(userId, Scheduled.AddHours(3));
        cancelled.Status = ReminderStatus.Cancelled;
        var otherUser = CreateReminder(Guid.NewGuid(), Scheduled);

        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.Add(new Notification
            {
                Id = fired.NotificationId!.Value,
                UserId = fired.UserId,
                Type = NotificationType.ReminderDue,
                Title = "Reminder notification",
                Message = "Message",
                IdempotencyKey = $"notification-{Guid.NewGuid():N}"
            });
            context.Reminders.AddRange(first, second, fired, cancelled, otherUser);
            await context.SaveChangesAsync();
        }

        var result = await CreateRepository().GetPendingAsync(userId, 1);

        var selected = Assert.Single(result);
        Assert.Equal(second.Id, selected.Id);
    }

    [Fact]
    public async Task UpdatePending_RequiresVersionAndIncrementsOnce()
    {
        var reminder = CreateReminder(Guid.NewGuid());
        await SeedAsync(reminder);
        var update = CreateReminder(reminder.UserId, Scheduled.AddHours(1));
        update.Id = reminder.Id;
        update.Title = "Updated";
        update.IdempotencyKey = "must-remain-original";
        update.UpdatedAtUtc = Scheduled.AddMinutes(1);
        var originalKey = reminder.IdempotencyKey;

        var repository = CreateRepository();
        await repository.UpdatePendingAsync(reminder.UserId, update, 0);
        await repository.UpdatePendingAsync(reminder.UserId, update, 0);

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal("Updated", persisted.Title);
        Assert.Equal(1, persisted.Version);
        Assert.Equal(originalKey, persisted.IdempotencyKey);
        Assert.Equal(ReminderStatus.Pending, persisted.Status);
        Assert.Null(persisted.FiredAtUtc);
        Assert.Null(persisted.NotificationId);
    }

    [Fact]
    public async Task UpdatePending_CannotUpdateTerminalOrForeignReminder()
    {
        var cancelled = CreateReminder(Guid.NewGuid());
        cancelled.Status = ReminderStatus.Cancelled;
        var fired = CreateReminder(Guid.NewGuid());
        fired.Status = ReminderStatus.Fired;
        fired.FiredAtUtc = Scheduled;
        fired.NotificationId = Guid.NewGuid();
        await SeedAsync(cancelled);
        await SeedAsync(fired);
        var update = CreateReminder(cancelled.UserId);
        update.Id = cancelled.Id;

        var repository = CreateRepository();
        await repository.UpdatePendingAsync(cancelled.UserId, update, 0);
        await repository.UpdatePendingAsync(fired.UserId, update, 0);
        await repository.UpdatePendingAsync(Guid.NewGuid(), update, 0);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(ReminderStatus.Cancelled,
            (await context.Reminders.SingleAsync(item => item.Id == cancelled.Id)).Status);
        Assert.Equal(ReminderStatus.Fired,
            (await context.Reminders.SingleAsync(item => item.Id == fired.Id)).Status);
    }

    [Fact]
    public async Task CancelPending_IncrementsVersionAndRepeatedCancelDoesNotChangeIt()
    {
        var reminder = CreateReminder(Guid.NewGuid());
        await SeedAsync(reminder);
        var repository = CreateRepository();

        await repository.CancelPendingAsync(reminder.UserId, reminder.Id, 0);
        await repository.CancelPendingAsync(reminder.UserId, reminder.Id, 0);

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal(ReminderStatus.Cancelled, persisted.Status);
        Assert.Equal(1, persisted.Version);
        Assert.Null(persisted.FiredAtUtc);
        Assert.Null(persisted.NotificationId);
    }

    [Fact]
    public async Task TwoUpdatesWithSameVersion_OnlyOneWins()
    {
        var reminder = CreateReminder(Guid.NewGuid());
        await SeedAsync(reminder);
        var first = CreateReminder(reminder.UserId, Scheduled.AddHours(1));
        first.Id = reminder.Id;
        first.Title = "First";
        var second = CreateReminder(reminder.UserId, Scheduled.AddHours(2));
        second.Id = reminder.Id;
        second.Title = "Second";
        var repository = CreateRepository();

        await Task.WhenAll(
            repository.UpdatePendingAsync(reminder.UserId, first, 0),
            repository.UpdatePendingAsync(reminder.UserId, second, 0));

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal(1, persisted.Version);
        Assert.Contains(persisted.Title, new[] { "First", "Second" });
    }

    [Fact]
    public async Task UpdateAndCancelRace_ProducesOneValidWinner()
    {
        var reminder = CreateReminder(Guid.NewGuid());
        await SeedAsync(reminder);
        var update = CreateReminder(reminder.UserId, Scheduled.AddHours(1));
        update.Id = reminder.Id;
        update.Title = "Updated";
        var repository = CreateRepository();

        await Task.WhenAll(
            repository.UpdatePendingAsync(reminder.UserId, update, 0),
            repository.CancelPendingAsync(reminder.UserId, reminder.Id, 0));

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal(1, persisted.Version);
        Assert.True(
            persisted.Status == ReminderStatus.Pending ||
            persisted.Status == ReminderStatus.Cancelled);
        if (persisted.Status == ReminderStatus.Pending)
        {
            Assert.Equal("Updated", persisted.Title);
        }
    }

    [Fact]
    public async Task TwoCancellations_ProduceOneTransition()
    {
        var reminder = CreateReminder(Guid.NewGuid());
        await SeedAsync(reminder);
        var repository = CreateRepository();

        await Task.WhenAll(
            repository.CancelPendingAsync(reminder.UserId, reminder.Id, 0),
            repository.CancelPendingAsync(reminder.UserId, reminder.Id, 0));

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal(ReminderStatus.Cancelled, persisted.Status);
        Assert.Equal(1, persisted.Version);
        Assert.Null(persisted.FiredAtUtc);
        Assert.Null(persisted.NotificationId);
    }

    private ReminderRepository CreateRepository()
    {
        return new ReminderRepository(_fixture.CreateDbContextFactory());
    }

    private async Task SeedAsync(Reminder reminder)
    {
        await using var context = _fixture.CreateDbContext();
        if (reminder.NotificationId is Guid notificationId)
        {
            context.Notifications.Add(new Notification
            {
                Id = notificationId,
                UserId = reminder.UserId,
                Type = NotificationType.ReminderDue,
                Title = "Reminder notification",
                Message = "Message",
                IdempotencyKey = $"notification-{Guid.NewGuid():N}"
            });
        }

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();
    }

    private static Reminder CreateReminder(
        Guid userId,
        DateTimeOffset? scheduledForUtc = null)
    {
        var id = Guid.NewGuid();
        return new Reminder
        {
            Id = id,
            UserId = userId,
            SourceType = ReminderSourceType.Custom,
            Title = "Reminder",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(14, 30),
            TimeZoneId = "UTC",
            ScheduledForUtc = scheduledForUtc ?? Scheduled,
            IdempotencyKey = $"ReminderFired:{id:N}"
        };
    }
}
