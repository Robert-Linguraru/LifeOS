using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class UserProgressionConfiguration
    : IEntityTypeConfiguration<UserProgression>
{
    public void Configure(EntityTypeBuilder<UserProgression> builder)
    {
        builder.ToTable(
            "UserProgressions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_UserProgressions_TotalLifetimeXp_NonNegative",
                    "\"TotalLifetimeXp\" >= 0");
                tableBuilder.HasCheckConstraint(
                    "CK_UserProgressions_CurrentLevel_AtLeastOne",
                    "\"CurrentLevel\" >= 1");
                tableBuilder.HasCheckConstraint(
                    "CK_UserProgressions_DailyQuestXpToday_InRange",
                    "\"DailyQuestXpToday\" >= 0 AND \"DailyQuestXpToday\" <= 500");
                tableBuilder.HasCheckConstraint(
                    "CK_UserProgressions_Version_NonNegative",
                    "\"Version\" >= 0");
            });

        builder.HasKey(progression => progression.Id);

        builder.Property(progression => progression.UserId)
            .IsRequired();

        builder.Property(progression => progression.TotalLifetimeXp)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(progression => progression.CurrentLevel)
            .IsRequired();

        builder.Property(progression => progression.CurrentEchelon)
            .IsRequired();

        builder.Property(progression => progression.DailyQuestXpToday)
            .IsRequired();

        builder.Property(progression => progression.DailyQuestXpDate)
            .HasColumnType("date");

        builder.Property(progression => progression.Version)
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(progression => progression.CreatedAtUtc)
            .IsRequired();

        builder.Property(progression => progression.UpdatedAtUtc)
            .IsRequired();

        builder.Property(progression => progression.IsDeleted)
            .IsRequired();

        builder.HasIndex(progression => progression.UserId)
            .IsUnique();
    }
}
