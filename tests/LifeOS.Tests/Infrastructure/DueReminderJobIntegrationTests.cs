using LifeOS.Core.Abstractions;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Enums.Reminders;
using LifeOS.Core.Time;
using LifeOS.Infrastructure.Jobs;
using LifeOS.Infrastructure.Options;
using LifeOS.Infrastructure.Repositories;
using LifeOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LifeOS.Tests.Infrastructure;

public sealed class DueReminderJobIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainerFixture _fixture;

    public DueReminderJobIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesDueReminderThroughRealPostgreSqlWorkflow()
    {
        var reminder = CreateReminder();
        await using (var seedContext = _fixture.CreateDbContext(Now))
        {
            seedContext.Reminders.Add(reminder);
            await seedContext.SaveChangesAsync();
        }

        var repository = new ReminderRepository(_fixture.CreateDbContextFactory());
        var processing = new ReminderProcessingService(
            repository,
            new FixedDateTimeProvider(Now),
            NullLogger<ReminderProcessingService>.Instance);
        var job = new DueReminderJob(
            processing,
            Options.Create(new ReminderProcessingOptions { BatchSize = 100 }));

        await job.ExecuteAsync();
        await job.ExecuteAsync();

        await using var context = _fixture.CreateDbContext(Now);
        var persistedReminder = await context.Reminders
            .SingleAsync(item => item.Id == reminder.Id);
        Assert.Equal(ReminderStatus.Fired, persistedReminder.Status);
        Assert.Equal(1, persistedReminder.Version);
        Assert.Equal(1, await context.Notifications.CountAsync(
            item => item.SourceId == reminder.Id));
    }

    private static Reminder CreateReminder()
    {
        var id = Guid.NewGuid();
        return new Reminder
        {
            Id = id,
            UserId = Guid.NewGuid(),
            SourceType = ReminderSourceType.Custom,
            Title = "Job reminder",
            Message = "Job message",
            ScheduledLocalDate = new DateOnly(2026, 8, 20),
            ScheduledLocalTime = new TimeOnly(11, 59),
            TimeZoneId = "UTC",
            ScheduledForUtc = Now.AddMinutes(-1),
            IdempotencyKey = $"ReminderFired:{id:N}"
        };
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }

        public bool IsValidTimeZone(string timeZoneId) => timeZoneId == "UTC";

        public DateOnly GetCurrentDate(string timeZoneId) => DateOnly.FromDateTime(UtcNow.UtcDateTime);

        public LocalTimeConversionResult ConvertLocalToUtc(
            DateOnly localDate,
            TimeOnly localTime,
            string timeZoneId) =>
            LocalTimeConversionResult.Success(
                new DateTimeOffset(localDate.ToDateTime(localTime), TimeSpan.Zero));

        public DateTimeOffset ConvertUtcToLocal(
            DateTimeOffset utcInstant,
            string timeZoneId) => utcInstant;

        public IReadOnlyList<string> GetTimeZoneIds() => ["UTC"];
    }
}
