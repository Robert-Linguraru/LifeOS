using LifeOS.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)   {}
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Habit> HabitEntities { get; set; }
        public DbSet<HabitLog> HabitLogs { get; set; }
        public DbSet<UserProgression> UserProgressions { get; set; }
        public DbSet<XPTransaction> XPTransactions { get; set; }
        public DbSet<StreakRecord> StreakRecords { get; set; }
        public DbSet<DailyScore> DailyScores { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>().ToTable("Users");

            builder.Entity<UserProgression>(entity =>
            {
                // UserId is the primary key
                entity.HasKey(u => u.UserId);

                // UserId is also the foreign key to ApplicationUser
                entity.HasOne(u => u.User)
                      .WithOne()
                      .HasForeignKey<UserProgression>(u => u.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TaskItem>().ToTable("Tasks");
            builder.Entity<Habit>().ToTable("Habits");

            builder.Entity<HabitLog>().HasIndex(h => new { h.HabitId, h.Date }).IsUnique();

            builder.Entity<DailyScore>().HasIndex(d => new { d.UserId, d.Date }).IsUnique();

            builder.Entity<StreakRecord>().HasIndex(s => new { s.UserId, s.SourceId, s.SourceType }).IsUnique();
        }
    }
}