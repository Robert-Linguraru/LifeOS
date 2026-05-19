using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using LifeOS.Core.Interfaces;
using LifeOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Services
{
    public class XPService : IXPService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private const int DailyQuestXPCap = 500;

        public XPService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public int CalculateQuestXP(EstimatedTime time, FrictionLevel friction)
        {
            int timeBase = time switch
            {
                EstimatedTime.Under15m => 50,
                EstimatedTime.From15To30m => 100,
                EstimatedTime.From30To60m => 150,
                EstimatedTime.Over60m => 200,
                _ => 50
            };

            double multiplier = friction switch
            {
                FrictionLevel.Low => 1.0,
                FrictionLevel.Medium => 1.5,
                FrictionLevel.High => 2.0,
                _ => 1.0
            };

            return (int)(timeBase * multiplier);
        }

        public async Task<int> AwardQuestXPAsync(string userId, Guid sourceEntityId,
            EstimatedTime time, FrictionLevel friction, XPSource source)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var progression = await db.UserProgressions.FindAsync(userId);
            if (progression is null) return 0;

            if (progression.DailyQuestXPDate < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                progression.DailyQuestXPToday = 0;
                progression.DailyQuestXPDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            int calculated = CalculateQuestXP(time, friction);
            int remaining = DailyQuestXPCap - progression.DailyQuestXPToday;
            int awarded = Math.Min(calculated, remaining);

            if (awarded <= 0) return 0;

            progression.DailyQuestXPToday += awarded;
            progression.TotalLifetimeXP += awarded;
            progression.CurrentLevel = ComputeLevel(progression.TotalLifetimeXP);
            progression.CurrentEchelon = ComputeEchelon(progression.CurrentLevel);

            db.XPTransactions.Add(new XPTransaction
            {
                UserId = userId,
                Source = source,
                XPAmount = awarded,
                SourceEntityId = sourceEntityId,
                Notes = $"{time} / {friction} friction"
            });

            await db.SaveChangesAsync();
            return awarded;
        }

        public async Task AwardFlatXPAsync(string userId, int amount, XPSource source, Guid? sourceEntityId = null, string? notes = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var progression = await db.Set<UserProgression>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (progression == null) return;

            progression.TotalLifetimeXP += amount;
            progression.CurrentLevel = ComputeLevel(progression.TotalLifetimeXP);
            progression.CurrentEchelon = ComputeEchelon(progression.CurrentLevel);

            db.Set<XPTransaction>().Add(new XPTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Source = source,
                XPAmount = amount,
                SourceEntityId = sourceEntityId,
                Timestamp = DateTime.UtcNow,
                Notes = notes
            });

            await db.SaveChangesAsync();
        }

        public async Task<UserProgression?> GetProgressionAsync(string userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.UserProgressions.FindAsync(userId);
        }

        public async Task UpdateStreakAsync(string userId, Guid sourceId, StreakSourceType sourceType)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var streak = await db.StreakRecords
                .FirstOrDefaultAsync(s => s.UserId == userId
                    && s.SourceId == sourceId
                    && s.SourceType == sourceType);

            if (streak is null)
            {
                streak = new StreakRecord
                {
                    UserId = userId,
                    SourceId = sourceId,
                    SourceType = sourceType,
                    CurrentStreak = 1,
                    LongestStreak = 1,
                    LastCompletedDate = today
                };
                db.StreakRecords.Add(streak);
            }
            else
            {
                var yesterday = today.AddDays(-1);
                if (streak.LastCompletedDate == yesterday || streak.LastCompletedDate == today)
                {
                    if (streak.LastCompletedDate != today)
                    {
                        streak.CurrentStreak++;
                        streak.LongestStreak = Math.Max(streak.LongestStreak, streak.CurrentStreak);
                    }
                }
                else
                {
                    streak.CurrentStreak = 1;
                }
                streak.LastCompletedDate = today;
            }

            await db.SaveChangesAsync();
        }

        private static int ComputeLevel(long totalXP)
        {
            int level = 1;
            long cumulative = 0;
            while (true)
            {
                long needed = level * 30 + 150;
                if (cumulative + needed > totalXP) break;
                cumulative += needed;
                level++;
            }
            return level;
        }

        private static Echelon ComputeEchelon(int level) => level switch
        {
            >= 200 => Echelon.Ascendant,
            >= 175 => Echelon.Abyssal,
            >= 150 => Echelon.Immortal,
            >= 125 => Echelon.Celestial,
            >= 100 => Echelon.Apex,
            >= 75 => Echelon.Radiant,
            >= 50 => Echelon.Onyx,
            >= 40 => Echelon.Platinum,
            >= 30 => Echelon.Gold,
            >= 20 => Echelon.Silver,
            >= 10 => Echelon.Bronze,
            _ => Echelon.Iron
        };
    }
}