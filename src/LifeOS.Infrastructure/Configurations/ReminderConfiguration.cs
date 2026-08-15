using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Reminders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class ReminderConfiguration
    : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable(
            "Reminders",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Reminders_SourceShape",
                    "((\"SourceType\" IN (0, 1) AND \"SourceId\" IS NOT NULL AND \"SourceTitle\" IS NOT NULL) OR (\"SourceType\" = 2 AND \"SourceId\" IS NULL AND \"SourceTitle\" IS NULL))");
                tableBuilder.HasCheckConstraint(
                    "CK_Reminders_Lifecycle",
                    "((\"Status\" = 0 AND \"FiredAtUtc\" IS NULL AND \"NotificationId\" IS NULL) OR (\"Status\" = 1 AND \"FiredAtUtc\" IS NOT NULL AND \"NotificationId\" IS NOT NULL) OR (\"Status\" = 2 AND \"FiredAtUtc\" IS NULL AND \"NotificationId\" IS NULL))");
                tableBuilder.HasCheckConstraint(
                    "CK_Reminders_Version_NonNegative",
                    "\"Version\" >= 0");
            });

        builder.HasKey(reminder => reminder.Id);

        builder.Property(reminder => reminder.UserId)
            .IsRequired();

        builder.Property(reminder => reminder.SourceType)
            .IsRequired();

        builder.Property(reminder => reminder.SourceId);

        builder.Property(reminder => reminder.SourceTitle)
            .HasMaxLength(ReminderConstants.SourceTitleMaxLength);

        builder.Property(reminder => reminder.Title)
            .HasMaxLength(ReminderConstants.TitleMaxLength)
            .IsRequired();

        builder.Property(reminder => reminder.Message)
            .HasMaxLength(ReminderConstants.MessageMaxLength);

        builder.Property(reminder => reminder.ScheduledLocalDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(reminder => reminder.ScheduledLocalTime)
            .HasColumnType("time without time zone")
            .IsRequired();

        builder.Property(reminder => reminder.TimeZoneId)
            .HasMaxLength(ReminderConstants.TimeZoneIdMaxLength)
            .IsRequired();

        builder.Property(reminder => reminder.ScheduledForUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(reminder => reminder.Status)
            .HasDefaultValue(ReminderStatus.Pending)
            .IsRequired();

        builder.Property(reminder => reminder.FiredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(reminder => reminder.NotificationId);

        builder.Property(reminder => reminder.IdempotencyKey)
            .HasMaxLength(ReminderConstants.IdempotencyKeyMaxLength)
            .IsRequired();

        builder.Property(reminder => reminder.Version)
            .HasColumnType("bigint")
            .HasDefaultValue(0L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(reminder => reminder.CreatedAtUtc)
            .IsRequired();

        builder.Property(reminder => reminder.UpdatedAtUtc)
            .IsRequired();

        builder.Property(reminder => reminder.IsDeleted)
            .IsRequired();

        builder.HasIndex(reminder => new
        {
            reminder.UserId,
            reminder.IdempotencyKey
        })
            .IsUnique();

        builder.HasIndex(reminder => new
        {
            reminder.UserId,
            reminder.Status,
            reminder.ScheduledForUtc
        });

        builder.HasIndex(reminder => new
        {
            reminder.Status,
            reminder.ScheduledForUtc
        });

        builder.HasIndex(reminder => reminder.NotificationId)
            .IsUnique()
            .HasFilter("\"NotificationId\" IS NOT NULL");

        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(reminder => reminder.NotificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
