using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class HabitLogConfiguration : IEntityTypeConfiguration<HabitLog>
{
    public void Configure(EntityTypeBuilder<HabitLog> builder)
    {
        builder.ToTable("HabitLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.UserId)
            .IsRequired();

        builder.Property(log => log.HabitId)
            .IsRequired();

        builder.Property(log => log.CompletionDate)
            .IsRequired();

        builder.Property(log => log.CompletedAtUtc)
            .IsRequired();

        builder.Property(log => log.CreatedAtUtc)
            .IsRequired();

        builder.Property(log => log.UpdatedAtUtc)
            .IsRequired();

        builder.Property(log => log.IsDeleted)
            .IsRequired();

        builder.HasOne<Habit>()
            .WithMany()
            .HasForeignKey(log => log.HabitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(log => new
        {
            log.UserId,
            log.HabitId,
            log.CompletionDate
        })
            .IsUnique();

        builder.HasIndex(log => new
        {
            log.UserId,
            log.CompletionDate
        });

        builder.HasIndex(log => new
        {
            log.HabitId,
            log.CompletionDate
        });
    }
}
