using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class ExerciseSecondaryMuscleGroupConfiguration
    : IEntityTypeConfiguration<ExerciseSecondaryMuscleGroup>
{
    public void Configure(EntityTypeBuilder<ExerciseSecondaryMuscleGroup> builder)
    {
        builder.ToTable("ExerciseSecondaryMuscleGroups", tableBuilder =>
            tableBuilder.HasCheckConstraint("CK_ExerciseSecondaryMuscleGroups_MuscleGroup", "\"MuscleGroup\" BETWEEN 0 AND 11"));

        builder.Property<Guid>("ExerciseId").IsRequired();
        builder.HasKey("ExerciseId", nameof(ExerciseSecondaryMuscleGroup.MuscleGroup));
        builder.Property(group => group.MuscleGroup).IsRequired();
        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey("ExerciseId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(ExerciseDefaults.Definitions
            .SelectMany(exercise => exercise.SecondaryMuscleGroups.Select(muscleGroup => new
            {
                ExerciseId = exercise.Id,
                MuscleGroup = muscleGroup
            })));
    }
}
