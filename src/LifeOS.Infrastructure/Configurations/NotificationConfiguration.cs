using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable(
            "Notifications",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Notifications_SourcePair",
                    "(\"SourceType\" IS NULL AND \"SourceId\" IS NULL) OR (\"SourceType\" IS NOT NULL AND \"SourceId\" IS NOT NULL)");
                tableBuilder.HasCheckConstraint(
                    "CK_Notifications_DismissedRequiresRead",
                    "\"DismissedAtUtc\" IS NULL OR \"ReadAtUtc\" IS NOT NULL");
            });

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.UserId)
            .IsRequired();

        builder.Property(notification => notification.Type)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasMaxLength(NotificationConstants.TitleMaxLength)
            .IsRequired();

        builder.Property(notification => notification.Message)
            .HasMaxLength(NotificationConstants.MessageMaxLength)
            .IsRequired();

        builder.Property(notification => notification.SourceType);
        builder.Property(notification => notification.SourceId);

        builder.Property(notification => notification.ReadAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(notification => notification.DismissedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(notification => notification.IdempotencyKey)
            .HasMaxLength(NotificationConstants.IdempotencyKeyMaxLength)
            .IsRequired();

        builder.Property(notification => notification.CreatedAtUtc)
            .IsRequired();

        builder.Property(notification => notification.UpdatedAtUtc)
            .IsRequired();

        builder.Property(notification => notification.IsDeleted)
            .IsRequired();

        builder.HasIndex(notification => new
        {
            notification.UserId,
            notification.IdempotencyKey
        })
            .IsUnique();

        builder.HasIndex(notification => new
        {
            notification.UserId,
            notification.DismissedAtUtc,
            notification.CreatedAtUtc
        });

        builder.HasIndex(notification => new
        {
            notification.UserId,
            notification.DismissedAtUtc,
            notification.ReadAtUtc
        });
    }
}
