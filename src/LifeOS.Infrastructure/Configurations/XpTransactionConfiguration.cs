using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class XpTransactionConfiguration
    : IEntityTypeConfiguration<XpTransaction>
{
    public void Configure(EntityTypeBuilder<XpTransaction> builder)
    {
        builder.ToTable("XpTransactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.UserId)
            .IsRequired();

        builder.Property(transaction => transaction.Source)
            .IsRequired();

        builder.Property(transaction => transaction.SourceType);

        builder.Property(transaction => transaction.SourceEntityId);

        builder.Property(transaction => transaction.XpAmount)
            .IsRequired();

        builder.Property(transaction => transaction.OccurredAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(transaction => transaction.BusinessDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(transaction => transaction.IdempotencyKey)
            .HasMaxLength(200);

        builder.Property(transaction => transaction.Notes)
            .HasMaxLength(500);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .IsRequired();

        builder.Property(transaction => transaction.UpdatedAtUtc)
            .IsRequired();

        builder.Property(transaction => transaction.IsDeleted)
            .IsRequired();

        builder.HasIndex(transaction => new
        {
            transaction.UserId,
            transaction.IdempotencyKey
        })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.HasIndex(transaction => new
        {
            transaction.UserId,
            transaction.OccurredAtUtc
        });

        builder.HasIndex(transaction => new
        {
            transaction.UserId,
            transaction.BusinessDate
        });

        builder.HasIndex(transaction => new
        {
            transaction.UserId,
            transaction.Source
        });
    }
}
