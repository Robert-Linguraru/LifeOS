using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class FinanceCategoryConfiguration
    : IEntityTypeConfiguration<FinanceCategory>
{
    public void Configure(EntityTypeBuilder<FinanceCategory> builder)
    {
        builder.ToTable(
            "FinanceCategories",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_FinanceCategories_Type",
                    "\"Type\" IN (0, 1, 2)");
                tableBuilder.HasCheckConstraint(
                    "CK_FinanceCategories_SortOrder_NonNegative",
                    "\"SortOrder\" >= 0");
            });

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(FinanceConstants.CategoryNameMaxLength)
            .IsRequired();

        builder.Property(category => category.Type)
            .IsRequired();

        builder.Property(category => category.SortOrder)
            .IsRequired();

        builder.Property(category => category.IsActive)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .IsRequired();

        builder.Property(category => category.UpdatedAtUtc)
            .IsRequired();

        builder.Property(category => category.IsDeleted)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.HasIndex(category => new
        {
            category.IsActive,
            category.SortOrder
        });

        var seedTimestamp = new DateTimeOffset(
            2026,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);

        builder.HasData(
            FinanceCategoryDefaults.All.Select(category => new
            {
                category.Id,
                category.Name,
                category.Type,
                category.SortOrder,
                IsActive = true,
                CreatedAtUtc = seedTimestamp,
                UpdatedAtUtc = seedTimestamp,
                IsDeleted = false,
                DeletedAtUtc = (DateTimeOffset?)null
            }));
    }
}
