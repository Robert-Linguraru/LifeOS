using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Abstractions.Reminders;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class ReminderProcessingIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateTimeOffset Cutoff =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainerFixture _fixture;

    public ReminderProcessingIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CommitFireAsync_AtomicallyFiresFromAuthoritativeReminder()
    {
        var reminder = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        reminder.Title = "Authoritative title";
        reminder.Message = "Authoritative message";
        await SeedAsync(reminder);
        var request = CreateRequest(reminder, 0);

        var result = await CreateRepository().CommitFireAsync(request);

        Assert.Equal(ReminderFireCommitStatus.Fired, result.Status);
        await using var context = _fixture.CreateDbContext();
        var persistedReminder = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        var notification = await context.Notifications.SingleAsync(
            item => item.Id == persistedReminder.NotificationId);
        Assert.Equal(ReminderStatus.Fired, persistedReminder.Status);
        Assert.Equal(1, persistedReminder.Version);
        Assert.Equal(Cutoff, persistedReminder.FiredAtUtc);
        Assert.Equal(reminder.Id, notification.SourceId);
        Assert.Equal(NotificationSourceType.Reminder, notification.SourceType);
        Assert.Equal("Authoritative title", notification.Title);
        Assert.Equal("Authoritative message", notification.Message);
        Assert.Equal($"ReminderFired:{reminder.Id:N}", notification.IdempotencyKey);
    }

    [Fact]
    public async Task CommitFireAsync_IsIdempotentForAlreadyFiredReminder()
    {
        var reminder = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        await SeedAsync(reminder);
        var repository = CreateRepository();
        var request = CreateRequest(reminder, 0);

        var first = await repository.CommitFireAsync(request);
        var second = await repository.CommitFireAsync(request);

        Assert.Equal(ReminderFireCommitStatus.Fired, first.Status);
        Assert.Equal(ReminderFireCommitStatus.AlreadyFired, second.Status);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.Notifications.CountAsync(
            item => item.SourceId == reminder.Id));
        Assert.Equal(1, (await context.Reminders.SingleAsync(item => item.Id == reminder.Id)).Version);
    }

    [Fact]
    public async Task CommitFireAsync_ReturnsExpectedNoSideEffectOutcomes()
    {
        var cancelled = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        cancelled.Status = ReminderStatus.Cancelled;
        var notDue = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(1));
        var mismatch = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        await SeedAsync(cancelled);
        await SeedAsync(notDue);
        await SeedAsync(mismatch);
        var repository = CreateRepository();

        Assert.Equal(ReminderFireCommitStatus.Cancelled,
            (await repository.CommitFireAsync(CreateRequest(cancelled, 0))).Status);
        Assert.Equal(ReminderFireCommitStatus.NotDue,
            (await repository.CommitFireAsync(CreateRequest(notDue, 0))).Status);
        Assert.Equal(ReminderFireCommitStatus.ConcurrencyLost,
            (await repository.CommitFireAsync(CreateRequest(mismatch, 1))).Status);
        Assert.Equal(ReminderFireCommitStatus.Missing,
            (await repository.CommitFireAsync(CreateRequest(
                CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1)), 0))).Status);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(0, await context.Notifications.CountAsync(
            notification => notification.SourceId == cancelled.Id ||
                notification.SourceId == notDue.Id ||
                notification.SourceId == mismatch.Id));
    }

    [Fact]
    public async Task CommitFireAsync_StaleCandidateUsesAuthoritativeVersionAndContent()
    {
        var reminder = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        await SeedAsync(reminder);
        await using (var context = _fixture.CreateDbContext())
        {
            var pending = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
            pending.Title = "Edited title";
            pending.ScheduledForUtc = Cutoff.AddMinutes(1);
            pending.Version = 1;
            await context.SaveChangesAsync();
        }

        var result = await CreateRepository().CommitFireAsync(CreateRequest(reminder, 0));

        Assert.Equal(ReminderFireCommitStatus.NotDue, result.Status);
        await using var reload = _fixture.CreateDbContext();
        Assert.Equal(ReminderStatus.Pending,
            (await reload.Reminders.SingleAsync(item => item.Id == reminder.Id)).Status);
        Assert.Equal(0, await reload.Notifications.CountAsync(
            item => item.SourceId == reminder.Id));
    }

    [Fact]
    public async Task CommitFireAsync_UniqueKeyRollbackLeavesReminderPending()
    {
        var reminder = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        await SeedAsync(reminder);
        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.Add(new Notification
            {
                UserId = reminder.UserId,
                Type = NotificationType.ReminderDue,
                Title = "Existing",
                Message = "Existing",
                SourceType = NotificationSourceType.Reminder,
                SourceId = Guid.NewGuid(),
                IdempotencyKey = reminder.IdempotencyKey
            });
            await context.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<DbUpdateException>(
            () => CreateRepository().CommitFireAsync(CreateRequest(reminder, 0)));

        await using var reload = _fixture.CreateDbContext();
        var persisted = await reload.Reminders.SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal(ReminderStatus.Pending, persisted.Status);
        Assert.Null(persisted.NotificationId);
        Assert.Equal(1, await reload.Notifications.CountAsync(
            item => item.IdempotencyKey == reminder.IdempotencyKey));
    }

    [Fact]
    public async Task TwoWorkers_ProduceOneNotificationAndOneFiredTransition()
    {
        var reminder = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        await SeedAsync(reminder);
        var first = CreateRepository();
        var second = CreateRepository();

        var results = await Task.WhenAll(
            first.CommitFireAsync(CreateRequest(reminder, 0)),
            second.CommitFireAsync(CreateRequest(reminder, 0)));

        Assert.Contains(results, result => result.Status == ReminderFireCommitStatus.Fired);
        Assert.Contains(results, result => result.Status == ReminderFireCommitStatus.AlreadyFired);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.Notifications.CountAsync(
            item => item.SourceId == reminder.Id));
        var persisted = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal(ReminderStatus.Fired, persisted.Status);
        Assert.Equal(1, persisted.Version);
        Assert.NotNull(persisted.NotificationId);
    }

    [Fact]
    public async Task FireAndCancelRace_ProducesOneValidTerminalOutcome()
    {
        var reminder = CreateReminder(Guid.NewGuid(), Cutoff.AddMinutes(-1));
        await SeedAsync(reminder);
        var repository = CreateRepository();

        await Task.WhenAll(
            repository.CommitFireAsync(CreateRequest(reminder, 0)),
            repository.CancelPendingAsync(reminder.UserId, reminder.Id, 0));

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Reminders.SingleAsync(item => item.Id == reminder.Id);
        var notificationCount = await context.Notifications.CountAsync(
            item => item.SourceId == reminder.Id);

        Assert.Equal(1, persisted.Version);
        if (persisted.Status == ReminderStatus.Fired)
        {
            Assert.Equal(1, notificationCount);
            Assert.NotNull(persisted.NotificationId);
        }
        else
        {
            Assert.Equal(ReminderStatus.Cancelled, persisted.Status);
            Assert.Equal(0, notificationCount);
            Assert.Null(persisted.NotificationId);
        }
    }

    private ReminderRepository CreateRepository()
    {
        return new ReminderRepository(_fixture.CreateDbContextFactory());
    }

    private async Task SeedAsync(Reminder reminder)
    {
        await using var context = _fixture.CreateDbContext();
        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();
    }

    private static ReminderFireCommitRequest CreateRequest(
        Reminder reminder,
        long expectedVersion)
    {
        return new ReminderFireCommitRequest
        {
            ReminderId = reminder.Id,
            UserId = reminder.UserId,
            ExpectedVersion = expectedVersion,
            DueCutoffUtc = Cutoff,
            FiredAtUtc = Cutoff,
            Notification = new NotificationDraft
            {
                NotificationId = reminder.Id,
                UserId = reminder.UserId,
                Type = NotificationType.ReminderDue,
                Title = "Draft title",
                Message = "Draft message",
                SourceType = NotificationSourceType.Reminder,
                SourceId = reminder.Id,
                IdempotencyKey = reminder.IdempotencyKey
            }
        };
    }

    private static Reminder CreateReminder(
        Guid userId,
        DateTimeOffset scheduledForUtc)
    {
        var id = Guid.NewGuid();
        return new Reminder
        {
            Id = id,
            UserId = userId,
            Title = "Reminder title",
            Message = "Reminder message",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(12, 0),
            TimeZoneId = "UTC",
            ScheduledForUtc = scheduledForUtc,
            IdempotencyKey = $"ReminderFired:{id:N}"
        };
    }
}
