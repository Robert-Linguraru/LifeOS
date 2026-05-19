using Hangfire;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Interfaces;
using LifeOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IBackgroundJobClient _jobClient;

    public NotificationService(IDbContextFactory<AppDbContext> dbFactory, IBackgroundJobClient jobClient)
    {
        _dbFactory = dbFactory;
        _jobClient = jobClient;
    }

    public Task<string> ScheduleReminderAsync(
        string userId,
        string title,
        string body,
        DateTime fireAt,
        NotificationSourceType sourceType,
        Guid sourceEntityId)
    {
        var delay = fireAt.ToUniversalTime() - DateTime.UtcNow;
        if (delay <= TimeSpan.Zero) delay = TimeSpan.FromSeconds(5);

        string jobId = _jobClient.Schedule<INotificationService>(
            svc => svc.DeliverNotificationAsync(
                userId, title, body, sourceType, sourceEntityId),
            delay);

        return Task.FromResult(jobId);
    }

    public async Task DeliverNotificationAsync(
        string userId,
        string title,
        string body,
        NotificationSourceType sourceType,
        Guid? sourceEntityId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            SourceType = sourceType,
            SourceEntityId = sourceEntityId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
    }

    public Task CancelReminderAsync(string hangfireJobId)
    {
        if (!string.IsNullOrEmpty(hangfireJobId))
            _jobClient.Delete(hangfireJobId);

        return Task.CompletedTask;
    }

    public async Task<List<Notification>> GetUnreadAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkReadAsync(Guid notificationId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var notification = await db.Notifications.FindAsync(notificationId);
        if (notification == null) return;

        notification.IsRead = true;
        await db.SaveChangesAsync();
    }

    public async Task SnoozeAsync(Guid notificationId, int minutes)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var notification = await db.Notifications.FindAsync(notificationId);
        if (notification == null) return;

        notification.SnoozedUntil = DateTime.UtcNow.AddMinutes(minutes);
        notification.IsRead = false;

        if (!string.IsNullOrEmpty(notification.HangfireJobId))
            _jobClient.Delete(notification.HangfireJobId);

        string newJobId = _jobClient.Schedule<INotificationService>(
            svc => svc.DeliverNotificationAsync(
                notification.UserId,
                notification.Title,
                notification.Body,
                notification.SourceType,
                notification.SourceEntityId),
            TimeSpan.FromMinutes(minutes));

        notification.HangfireJobId = newJobId;
        await db.SaveChangesAsync();
    }
}