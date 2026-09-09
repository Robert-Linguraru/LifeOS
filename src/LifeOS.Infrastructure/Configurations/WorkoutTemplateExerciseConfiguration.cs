using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class WorkoutTemplateExerciseConfiguration : IEntityTypeConfiguration<WorkoutTemplateExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutTemplateExercise> builder)
    {
        builder.ToTable("WorkoutTemplateExercises", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_WorkoutTemplateExercises_SortOrder_Positive", "\"SortOrder\" > 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutTemplateExercises_TargetSetCount_Positive", "\"TargetSetCount\" > 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutTemplateExercises_TargetRepRange", "(\"TargetRepMin\" IS NULL AND \"TargetRepMax\" IS NULL) OR (\"TargetRepMin\" > 0 AND \"TargetRepMax\" > 0 AND \"TargetRepMin\" <= \"TargetRepMax\")");
            tableBuilder.HasCheckConstraint("CK_WorkoutTemplateExercises_DefaultRestSeconds_NonNegative", "\"DefaultRestSeconds\" >= 0");
        });

        builder.HasKey(exercise => exercise.Id);
        builder.Property(exercise => exercise.UserId).IsRequired();
        builder.Property(exercise => exercise.WorkoutTemplateId).IsRequired();
        builder.Property(exercise => exercise.ExerciseId).IsRequired();
        builder.Property(exercise => exercise.SortOrder).IsRequired();
        builder.Property(exercise => exercise.TargetSetCount).IsRequired();
        builder.Property(exercise => exercise.TargetRepMin);
        builder.Property(exercise => exercise.TargetRepMax);
        builder.Property(exercise => exercise.DefaultRestSeconds).IsRequired();
        ConfigureAudit(builder);

        builder.HasIndex(exercise => new { exercise.WorkoutTemplateId, exercise.SortOrder })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(exercise => new { exercise.UserId, exercise.WorkoutTemplateId });
        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(exercise => exercise.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkoutTemplateExercise> builder)
    {
        builder.Property(exercise => exercise.CreatedAtUtc).IsRequired();
        builder.Property(exercise => exercise.UpdatedAtUtc).IsRequired();
        builder.Property(exercise => exercise.IsDeleted).IsRequired();
    }
}
