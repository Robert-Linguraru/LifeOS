using LifeOS.Core.Enums;
using LifeOS.Core.Interfaces;
using LifeOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Jobs;

public class StreakBonusJob : IStreakBonusJob
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IXPService _xpService;

    public StreakBonusJob(IDbContextFactory<AppDbContext> dbFactory, IXPService xpService)
    {
        _dbFactory = dbFactory;
        _xpService = xpService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var activeStreaks = await db.StreakRecords
            .Where(s =>
                s.CurrentStreak >= 1 &&
                s.LastCompletedDate.HasValue &&
                (s.LastCompletedDate.Value == today || s.LastCompletedDate.Value == yesterday))
            .ToListAsync(cancellationToken);

        var byUser = activeStreaks.GroupBy(s => s.UserId);

        foreach (var userGroup in byUser)
        {
            string userId = userGroup.Key;
            int activeCount = 0;
            int totalXP = 0;

            foreach (var streak in userGroup)
            {
                if (activeCount >= 4) break;

                bool isMomentumStreak = await IsMomentumStreakAsync(
                    db,
                    streak.UserId,
                    streak.SourceId,
                    streak.SourceType,
                    today);

                bool isActiveStreak = (streak.LastCompletedDate.HasValue &&
                                       streak.LastCompletedDate.Value == today)
                                      || isMomentumStreak;

                if (!isActiveStreak) continue;

                totalXP += 25;
                activeCount++;
            }

            if (totalXP > 0)
            {
                await _xpService.AwardFlatXPAsync(
                    userId,
                    totalXP,
                    XPSource.StreakBonus,
                    notes: $"Streak bonus — {activeCount} active streak(s) × 25 XP");
            }
        }
    }

    private async Task<bool> IsMomentumStreakAsync(
        AppDbContext db,
        string userId,
        Guid sourceId,
        StreakSourceType sourceType,
        DateOnly today)
    {
        var last4Days = Enumerable.Range(0, 4)
            .Select(i => today.AddDays(-i))
            .ToList();

        int completionsInLast4Days;

        if (sourceType == StreakSourceType.Habit)
        {
            completionsInLast4Days = await db.HabitLogs
                .CountAsync(l => l.HabitId == sourceId &&
                                 l.UserId == userId &&
                                 last4Days.Contains(l.Date));
        }
        else
        {
            completionsInLast4Days = await db.TaskItems
                .CountAsync(t => t.Id == sourceId &&
                                 t.UserId == userId &&
                                 t.Status == Core.Enums.TaskStatus.Completed &&
                                 t.DueDate.HasValue &&
                                 last4Days.Contains(DateOnly.FromDateTime(t.DueDate.Value)));
        }

        return completionsInLast4Days >= 3;
    }
}