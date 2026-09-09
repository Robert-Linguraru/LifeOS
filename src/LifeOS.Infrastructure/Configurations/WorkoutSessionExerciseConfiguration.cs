using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class WorkoutSessionExerciseConfiguration : IEntityTypeConfiguration<WorkoutSessionExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutSessionExercise> builder)
    {
        builder.ToTable("WorkoutSessionExercises", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_WorkoutSessionExercises_SortOrder_Positive", "\"SortOrder\" > 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessionExercises_TargetSetCount_Positive", "\"TargetSetCountSnapshot\" > 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessionExercises_TargetRepRange", "(\"TargetRepMinSnapshot\" IS NULL AND \"TargetRepMaxSnapshot\" IS NULL) OR (\"TargetRepMinSnapshot\" > 0 AND \"TargetRepMaxSnapshot\" > 0 AND \"TargetRepMinSnapshot\" <= \"TargetRepMaxSnapshot\")");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessionExercises_DefaultRestSeconds_NonNegative", "\"DefaultRestSecondsSnapshot\" >= 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessionExercises_LoggingMode", "\"LoggingModeSnapshot\" BETWEEN 0 AND 6");
        });

        builder.HasKey(exercise => exercise.Id);
        builder.Property(exercise => exercise.UserId).IsRequired();
        builder.Property(exercise => exercise.WorkoutSessionId).IsRequired();
        builder.Property(exercise => exercise.SortOrder).IsRequired();
        builder.Property(exercise => exercise.OriginalExerciseId).IsRequired();
        builder.Property(exercise => exercise.OriginalExerciseNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(exercise => exercise.ExerciseId).IsRequired();
        builder.Property(exercise => exercise.ExerciseNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(exercise => exercise.LoggingModeSnapshot).IsRequired();
        builder.Property(exercise => exercise.TargetSetCountSnapshot).IsRequired();
        builder.Property(exercise => exercise.TargetRepMinSnapshot);
        builder.Property(exercise => exercise.TargetRepMaxSnapshot);
        builder.Property(exercise => exercise.DefaultRestSecondsSnapshot).IsRequired();
        builder.Property(exercise => exercise.IsSkipped).IsRequired();
        ConfigureAudit(builder);

        builder.HasAlternateKey(exercise => new { exercise.Id, exercise.UserId })
            .HasName("AK_WorkoutSessionExercises_Id_UserId");
        builder.HasIndex(exercise => new { exercise.WorkoutSessionId, exercise.SortOrder })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(exercise => new { exercise.UserId, exercise.WorkoutSessionId });
        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(exercise => exercise.OriginalExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(exercise => exercise.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(exercise => exercise.Sets)
            .WithOne()
            .HasForeignKey(set => new { set.WorkoutSessionExerciseId, set.UserId })
            .HasPrincipalKey(exercise => new { exercise.Id, exercise.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(exercise => exercise.Sets)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkoutSessionExercise> builder)
    {
        builder.Property(exercise => exercise.CreatedAtUtc).IsRequired();
        builder.Property(exercise => exercise.UpdatedAtUtc).IsRequired();
        builder.Property(exercise => exercise.IsDeleted).IsRequired();
    }
}
