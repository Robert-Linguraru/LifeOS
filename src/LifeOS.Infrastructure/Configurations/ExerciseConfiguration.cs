using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Exercises_SortOrder_Positive", "\"SortOrder\" > 0");
            tableBuilder.HasCheckConstraint("CK_Exercises_MuscleGroup", "\"PrimaryMuscleGroup\" BETWEEN 0 AND 11");
            tableBuilder.HasCheckConstraint("CK_Exercises_Equipment", "\"Equipment\" BETWEEN 0 AND 10");
            tableBuilder.HasCheckConstraint("CK_Exercises_MovementPattern", "\"MovementPattern\" BETWEEN 0 AND 11");
            tableBuilder.HasCheckConstraint("CK_Exercises_LoggingMode", "\"LoggingMode\" BETWEEN 0 AND 6");
        });

        builder.HasKey(exercise => exercise.Id);
        builder.Property(exercise => exercise.Name).HasMaxLength(200).IsRequired();
        builder.Property(exercise => exercise.PrimaryMuscleGroup).IsRequired();
        builder.Property(exercise => exercise.Equipment).IsRequired();
        builder.Property(exercise => exercise.MovementPattern).IsRequired();
        builder.Property(exercise => exercise.LoggingMode).IsRequired();
        builder.Property(exercise => exercise.IsActive).IsRequired();
        builder.Property(exercise => exercise.SortOrder).IsRequired();
        ConfigureAudit(builder);

        builder.HasIndex(exercise => exercise.Name).IsUnique();
        builder.HasIndex(exercise => new { exercise.IsActive, exercise.SortOrder });

        var seedTimestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(ExerciseDefaults.Definitions.Select(exercise => new
        {
            exercise.Id,
            exercise.Name,
            exercise.PrimaryMuscleGroup,
            exercise.Equipment,
            exercise.MovementPattern,
            exercise.LoggingMode,
            exercise.IsActive,
            exercise.SortOrder,
            CreatedAtUtc = seedTimestamp,
            UpdatedAtUtc = seedTimestamp,
            IsDeleted = false,
            DeletedAtUtc = (DateTimeOffset?)null
        }));
    }

    private static void ConfigureAudit(EntityTypeBuilder<Exercise> builder)
    {
        builder.Property(exercise => exercise.CreatedAtUtc).IsRequired();
        builder.Property(exercise => exercise.UpdatedAtUtc).IsRequired();
        builder.Property(exercise => exercise.IsDeleted).IsRequired();
    }
}
