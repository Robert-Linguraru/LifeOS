using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Interfaces;
using LifeOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Jobs;

public class DailyScoreJob : IDailyScoreJob
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IXPService _xpService;

    public DailyScoreJob(IDbContextFactory<AppDbContext> dbFactory, IXPService xpService)
    {
        _dbFactory = dbFactory;
        _xpService = xpService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var users = await db.Users
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            bool alreadyScored = await db.DailyScores
                .AnyAsync(d => d.UserId == user.Id && d.Date == today, cancellationToken);

            if (alreadyScored) continue;

            var score = await CalculateScoreAsync(db, user.Id, today, cancellationToken);

            db.DailyScores.Add(score);
            await db.SaveChangesAsync(cancellationToken);

            int xpToAward = Math.Min(score.TotalScore * 5, 500);
            if (xpToAward > 0)
                await _xpService.AwardFlatXPAsync(
                    user.Id,
                    xpToAward,
                    XPSource.DailyScore,
                    notes: $"Daily life score — {score.TotalScore}/100");
        }
    }

    private async Task<DailyScore> CalculateScoreAsync(
        AppDbContext db, string userId, DateOnly date, CancellationToken ct)
    {
        var score = new DailyScore
        {
            UserId = userId,
            Date = date
        };

        int totalWeight = 0;
        int earnedPoints = 0;

        // --- HABITS (30 pts) ---
        var dueHabits = await db.HabitEntities
            .Where(h => h.UserId == userId && h.IsActive)
            .ToListAsync(ct);

        if (dueHabits.Any())
        {
            int completedHabits = await db.HabitLogs
                .CountAsync(l => l.UserId == userId && l.Date == date, ct);

            int habitScore = (int)Math.Round(
                (double)completedHabits / dueHabits.Count * 30);

            score.HabitScore = Math.Min(habitScore, 30);
            earnedPoints += score.HabitScore;
            totalWeight += 30;
        }

        // --- TASKS (20 pts) ---
        var dueTasks = await db.TaskItems
            .Where(t => t.UserId == userId &&
                        t.DueDate.HasValue &&
                        DateOnly.FromDateTime(t.DueDate.Value) == date)
            .ToListAsync(ct);

        if (dueTasks.Any())
        {
            int completedTasks = dueTasks.Count(t =>
                t.Status == Core.Enums.TaskStatus.Completed);

            int taskScore = (int)Math.Round(
                (double)completedTasks / dueTasks.Count * 20);

            score.TaskScore = Math.Min(taskScore, 20);
            earnedPoints += score.TaskScore;
            totalWeight += 20;
        }

        // --- SLEEP (15 pts) — module not built yet, skip ---
        // --- WORKOUT (15 pts) — module not built yet, skip ---
        // --- FINANCE (10 pts) — module not built yet, skip ---
        // --- NUTRITION (10 pts) — module not built yet, skip ---

        score.TotalScore = totalWeight > 0
            ? (int)Math.Round((double)earnedPoints / totalWeight * 100)
            : 0;

        score.XPAwarded = Math.Min(score.TotalScore * 5, 500);

        return score;
    }
}