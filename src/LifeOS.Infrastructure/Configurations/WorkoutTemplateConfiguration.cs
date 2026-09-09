using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class WorkoutTemplateConfiguration : IEntityTypeConfiguration<WorkoutTemplate>
{
    public void Configure(EntityTypeBuilder<WorkoutTemplate> builder)
    {
        builder.ToTable("WorkoutTemplates", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_WorkoutTemplates_Version_Positive", "\"Version\" >= 1");
        });

        builder.HasKey(template => template.Id);
        builder.Property(template => template.UserId).IsRequired();
        builder.Property(template => template.Name).HasMaxLength(WorkoutTemplate.MaximumNameLength).IsRequired();
        builder.Property(template => template.Version).IsRequired().IsConcurrencyToken();
        ConfigureAudit(builder);

        builder.HasAlternateKey(template => new { template.Id, template.UserId })
            .HasName("AK_WorkoutTemplates_Id_UserId");
        builder.HasIndex(template => new { template.UserId, template.IsDeleted });
        builder.HasMany(template => template.Exercises)
            .WithOne()
            .HasForeignKey(exercise => new { exercise.WorkoutTemplateId, exercise.UserId })
            .HasPrincipalKey(template => new { template.Id, template.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(template => template.Exercises)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkoutTemplate> builder)
    {
        builder.Property(template => template.CreatedAtUtc).IsRequired();
        builder.Property(template => template.UpdatedAtUtc).IsRequired();
        builder.Property(template => template.IsDeleted).IsRequired();
    }
}
