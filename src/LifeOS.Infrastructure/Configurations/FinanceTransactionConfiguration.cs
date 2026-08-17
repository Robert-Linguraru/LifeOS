using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class FinanceTransactionConfiguration
    : IEntityTypeConfiguration<FinanceTransaction>
{
    public void Configure(EntityTypeBuilder<FinanceTransaction> builder)
    {
        builder.ToTable(
            "FinanceTransactions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_FinanceTransactions_Type",
                    "\"Type\" IN (0, 1)");
                tableBuilder.HasCheckConstraint(
                    "CK_FinanceTransactions_Amount_Positive",
                    "\"Amount\" > 0");
            });

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.UserId)
            .IsRequired();

        builder.Property(transaction => transaction.TransactionDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.CategoryId)
            .IsRequired();

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(FinanceConstants.DescriptionMaxLength);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .IsRequired();

        builder.Property(transaction => transaction.UpdatedAtUtc)
            .IsRequired();

        builder.Property(transaction => transaction.IsDeleted)
            .IsRequired();

        builder.HasOne<FinanceCategory>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transaction => new
        {
            transaction.UserId,
            transaction.TransactionDate
        });

        builder.HasIndex(transaction => new
        {
            transaction.UserId,
            transaction.CategoryId,
            transaction.TransactionDate
        });

        builder.HasIndex(transaction => new
        {
            transaction.UserId,
            transaction.Type,
            transaction.TransactionDate
        });
    }
}
