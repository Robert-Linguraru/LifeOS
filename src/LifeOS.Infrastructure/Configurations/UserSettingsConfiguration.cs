using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class UserSettingsConfiguration
    : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(
        EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Id)
            .ValueGeneratedNever();

        builder.Property(settings => settings.UserId)
            .IsRequired();

        builder.Property(settings => settings.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(settings => settings.TimeZoneConfiguredAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(settings => settings.CreatedAtUtc)
            .IsRequired();

        builder.Property(settings => settings.UpdatedAtUtc)
            .IsRequired();

        builder.Property(settings => settings.IsDeleted)
            .IsRequired();

        builder.HasIndex(settings => settings.UserId)
            .IsUnique();
    }
}