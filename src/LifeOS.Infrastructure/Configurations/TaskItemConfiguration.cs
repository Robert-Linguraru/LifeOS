using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.UserId)
            .IsRequired();

        builder.Property(task => task.Title)
            .HasMaxLength(TaskConstants.TitleMaxLength)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasMaxLength(TaskConstants.DescriptionMaxLength);

        builder.Property(task => task.DueDate);

        builder.Property(task => task.DueTime);

        builder.Property(task => task.Priority)
            .IsRequired();

        builder.Property(task => task.Status)
            .IsRequired();

        builder.Property(task => task.Category)
            .IsRequired();

        builder.Property(task => task.EstimatedTime)
            .IsRequired();

        builder.Property(task => task.FrictionLevel)
            .IsRequired();

        builder.Property(task => task.CompletedAtUtc);

        builder.Property(task => task.CompletedDate);

        builder.Property(task => task.CreatedAtUtc)
            .IsRequired();

        builder.Property(task => task.UpdatedAtUtc)
            .IsRequired();

        builder.Property(task => task.IsDeleted)
            .IsRequired();

        builder.Property(task => task.DeletedAtUtc);

        builder.HasIndex(task => new
        {
            task.UserId,
            task.Status
        });

        builder.HasIndex(task => new
        {
            task.UserId,
            task.DueDate
        });

        builder.HasIndex(task => new
        {
            task.UserId,
            task.IsDeleted
        });
    }
}