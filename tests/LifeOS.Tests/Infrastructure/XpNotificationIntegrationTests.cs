using LifeOS.Core.Abstractions;
using LifeOS.Core.Abstractions.Notifications;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Notifications;
using LifeOS.Core.Enums.Xp;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class XpNotificationIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private static readonly DateOnly BusinessDate = new(2026, 8, 20);
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainerFixture _fixture;

    public XpNotificationIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NoTransition_CommitsXpWithoutNotifications()
    {
        var userId = await SeedProgressionAsync(totalXp: 0, level: 1, echelon: Echelon.Iron);
        var request = CreateRequest(userId, 10, 0, 10, 1, Echelon.Iron);

        var result = await CreateRepository().CommitAwardAsync(request);

        Assert.Equal(XpAwardCommitStatus.Committed, result.Status);
        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.XpTransactions.CountAsync(item => item.UserId == userId));
        Assert.Empty(await context.Notifications.Where(item => item.UserId == userId).ToListAsync());
    }

    [Fact]
    public async Task LevelTransition_CommitsOneLevelNotification()
    {
        var userId = await SeedProgressionAsync(totalXp: 170, level: 1, echelon: Echelon.Iron);
        var transactionId = Guid.NewGuid();
        var request = CreateRequest(userId, 100, 0, 270, 2, Echelon.Iron, transactionId,
        [
            CreateDraft(userId, transactionId, NotificationType.LevelUp,
                $"XpLevelUp:{transactionId:N}", "You reached level 2.")
        ]);

        await CreateRepository().CommitAwardAsync(request);

        await using var context = _fixture.CreateDbContext();
        var notification = await context.Notifications.SingleAsync(item => item.UserId == userId);
        Assert.Equal(NotificationType.LevelUp, notification.Type);
        Assert.Equal(NotificationSourceType.XpTransaction, notification.SourceType);
        Assert.Equal(transactionId, notification.SourceId);
        Assert.Null(notification.ReadAtUtc);
        Assert.Null(notification.DismissedAtUtc);
    }

    [Fact]
    public async Task BothTransitions_CommitsTwoDistinctNotifications()
    {
        var userId = await SeedProgressionAsync(totalXp: 2500, level: 9, echelon: Echelon.Iron);
        var transactionId = Guid.NewGuid();
        var request = CreateRequest(userId, 200, 0, 2700, 10, Echelon.Bronze, transactionId,
        [
            CreateDraft(userId, transactionId, NotificationType.LevelUp,
                $"XpLevelUp:{transactionId:N}", "You reached level 10."),
            CreateDraft(userId, transactionId, NotificationType.EchelonChanged,
                $"XpEchelonChanged:{transactionId:N}", "You reached the Bronze echelon.")
        ]);

        await CreateRepository().CommitAwardAsync(request);

        await using var context = _fixture.CreateDbContext();
        var notifications = await context.Notifications
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Type)
            .ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.Equal(2, notifications.Select(item => item.Id).Distinct().Count());
        Assert.Equal(2, notifications.Select(item => item.IdempotencyKey).Distinct().Count());
    }

    [Fact]
    public async Task DuplicateAward_DoesNotDuplicateNotifications()
    {
        var userId = await SeedProgressionAsync(totalXp: 170, level: 1, echelon: Echelon.Iron);
        var transactionId = Guid.NewGuid();
        var request = CreateRequest(userId, 100, 0, 270, 2, Echelon.Iron, transactionId,
        [CreateDraft(userId, transactionId, NotificationType.LevelUp,
            $"XpLevelUp:{transactionId:N}", "You reached level 2.")]);
        var repository = CreateRepository();

        Assert.Equal(XpAwardCommitStatus.Committed,
            (await repository.CommitAwardAsync(request)).Status);
        Assert.Equal(XpAwardCommitStatus.Duplicate,
            (await repository.CommitAwardAsync(request)).Status);

        await using var context = _fixture.CreateDbContext();
        Assert.Equal(1, await context.XpTransactions.CountAsync(item => item.UserId == userId));
        Assert.Equal(1, await context.Notifications.CountAsync(item => item.UserId == userId));
    }

    [Fact]
    public async Task NotificationFailure_RollsBackXpAndProgression()
    {
        var userId = await SeedProgressionAsync(totalXp: 170, level: 1, echelon: Echelon.Iron);
        var transactionId = Guid.NewGuid();
        var request = CreateRequest(userId, 100, 0, 270, 2, Echelon.Iron, transactionId,
        [CreateDraft(userId, transactionId, NotificationType.LevelUp,
            $"XpLevelUp:{transactionId:N}", "You reached level 2.")]);
        await using (var context = _fixture.CreateDbContext())
        {
            context.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.LevelUp,
                Title = "Existing",
                Message = "Existing",
                IdempotencyKey = request.Notifications[0].IdempotencyKey
            });
            await context.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<DbUpdateException>(
            () => CreateRepository().CommitAwardAsync(request));

        await using var reload = _fixture.CreateDbContext();
        var progression = await reload.UserProgressions.SingleAsync(item => item.UserId == userId);
        Assert.Equal(170, progression.TotalLifetimeXp);
        Assert.Equal(0, await reload.XpTransactions.CountAsync(item => item.UserId == userId));
    }

    private XpRepository CreateRepository()
    {
        return new XpRepository(_fixture.CreateDbContextFactory());
    }

    private async Task<Guid> SeedProgressionAsync(long totalXp, int level, Echelon echelon)
    {
        var userId = Guid.NewGuid();
        await using var context = _fixture.CreateDbContext();
        context.UserProgressions.Add(new UserProgression
        {
            UserId = userId,
            TotalLifetimeXp = totalXp,
            CurrentLevel = level,
            CurrentEchelon = echelon,
            Version = 0
        });
        await context.SaveChangesAsync();
        return userId;
    }

    private static XpAwardCommitRequest CreateRequest(
        Guid userId,
        int amount,
        long expectedVersion,
        long resultingTotal,
        int resultingLevel,
        Echelon resultingEchelon,
        Guid? transactionId = null,
        IReadOnlyList<NotificationDraft>? notifications = null)
    {
        var id = transactionId ?? Guid.NewGuid();
        return new XpAwardCommitRequest
        {
            XpTransactionId = id,
            UserId = userId,
            Source = XpSource.QuestCompletion,
            SourceType = XpSourceType.Task,
            SourceEntityId = Guid.NewGuid(),
            XpAmount = amount,
            OccurredAtUtc = OccurredAt,
            BusinessDate = BusinessDate,
            IdempotencyKey = $"xp-{id:N}",
            ExpectedVersion = expectedVersion,
            ResultingTotalLifetimeXp = resultingTotal,
            ResultingCurrentLevel = resultingLevel,
            ResultingCurrentEchelon = resultingEchelon,
            ResultingDailyQuestXpToday = amount,
            ResultingDailyQuestXpDate = BusinessDate,
            ResultingVersion = expectedVersion + 1,
            Notifications = notifications ?? []
        };
    }

    private static NotificationDraft CreateDraft(
        Guid userId,
        Guid transactionId,
        NotificationType type,
        string key,
        string message)
    {
        return new NotificationDraft
        {
            NotificationId = type == NotificationType.LevelUp
                ? transactionId
                : Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = type == NotificationType.LevelUp
                ? "Level up!"
                : "New echelon reached!",
            Message = message,
            SourceType = NotificationSourceType.XpTransaction,
            SourceId = transactionId,
            IdempotencyKey = key
        };
    }
}
