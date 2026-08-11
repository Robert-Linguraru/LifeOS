using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.ToTable("Habits");

        builder.HasKey(habit => habit.Id);

        builder.Property(habit => habit.UserId)
            .IsRequired();

        builder.Property(habit => habit.Name)
            .HasMaxLength(HabitConstants.NameMaxLength)
            .IsRequired();

        builder.Property(habit => habit.Description)
            .HasMaxLength(HabitConstants.DescriptionMaxLength);

        builder.Property(habit => habit.Frequency)
            .IsRequired();

        builder.Property(habit => habit.TargetType)
            .IsRequired();

        builder.Property(habit => habit.TargetQuantity)
            .HasPrecision(18, 2);

        builder.Property(habit => habit.TargetUnit)
            .HasMaxLength(HabitConstants.TargetUnitMaxLength);

        builder.Property(habit => habit.IsActive)
            .IsRequired();

        builder.Property(habit => habit.EstimatedTime)
            .IsRequired();

        builder.Property(habit => habit.FrictionLevel)
            .IsRequired();

        builder.Property(habit => habit.CreatedAtUtc)
            .IsRequired();

        builder.Property(habit => habit.UpdatedAtUtc)
            .IsRequired();

        builder.Property(habit => habit.IsDeleted)
            .IsRequired();

        builder.HasIndex(habit => new
        {
            habit.UserId,
            habit.IsActive
        });

        builder.HasIndex(habit => new
        {
            habit.UserId,
            habit.IsDeleted
        });
    }
}
