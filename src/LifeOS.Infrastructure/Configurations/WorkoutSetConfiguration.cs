using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class WorkoutSetConfiguration : IEntityTypeConfiguration<WorkoutSet>
{
    public void Configure(EntityTypeBuilder<WorkoutSet> builder)
    {
        builder.ToTable("WorkoutSets", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_WorkoutSets_SortOrder_Positive", "\"SortOrder\" > 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutSets_Kind", "\"Kind\" BETWEEN 0 AND 1");
            tableBuilder.HasCheckConstraint("CK_WorkoutSets_Weight_Positive", "\"WeightKg\" IS NULL OR \"WeightKg\" > 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutSets_Repetitions_Positive", "\"Repetitions\" IS NULL OR \"Repetitions\" > 0");
            tableBuilder.HasCheckConstraint("CK_WorkoutSets_DurationSeconds_Positive", "\"DurationSeconds\" IS NULL OR \"DurationSeconds\" > 0");
        });

        builder.HasKey(set => set.Id);
        builder.Property(set => set.UserId).IsRequired();
        builder.Property(set => set.WorkoutSessionExerciseId).IsRequired();
        builder.Property(set => set.SortOrder).IsRequired();
        builder.Property(set => set.Kind).IsRequired();
        builder.Property(set => set.WeightKg).HasPrecision(18, 2);
        builder.Property(set => set.Repetitions);
        builder.Property(set => set.DurationSeconds);
        builder.Property(set => set.CompletedAtUtc);
        builder.Ignore(set => set.IsCompleted);
        ConfigureAudit(builder);

        builder.HasIndex(set => new { set.WorkoutSessionExerciseId, set.SortOrder })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(set => new { set.UserId, set.WorkoutSessionExerciseId });
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkoutSet> builder)
    {
        builder.Property(set => set.CreatedAtUtc).IsRequired();
        builder.Property(set => set.UpdatedAtUtc).IsRequired();
        builder.Property(set => set.IsDeleted).IsRequired();
    }
}
