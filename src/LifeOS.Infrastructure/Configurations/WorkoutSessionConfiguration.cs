using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Configurations;

public sealed class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("WorkoutSessions", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_WorkoutSessions_Status", "\"Status\" BETWEEN 0 AND 2");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessions_SessionFeeling", "\"SessionFeeling\" IS NULL OR \"SessionFeeling\" BETWEEN 0 AND 3");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessions_Version_Positive", "\"Version\" >= 1");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessions_LifecycleTimestamps", "(\"Status\" = 0 AND \"CompletedAtUtc\" IS NULL AND \"DiscardedAtUtc\" IS NULL) OR (\"Status\" = 1 AND \"CompletedAtUtc\" IS NOT NULL AND \"DiscardedAtUtc\" IS NULL) OR (\"Status\" = 2 AND \"CompletedAtUtc\" IS NULL AND \"DiscardedAtUtc\" IS NOT NULL)");
            tableBuilder.HasCheckConstraint("CK_WorkoutSessions_RestTimerState", "(\"RestTimerDurationSeconds\" IS NULL AND \"RestTimerEndsAtUtc\" IS NULL AND \"RestTimerPausedRemainingSeconds\" IS NULL) OR (\"RestTimerDurationSeconds\" > 0 AND ((\"RestTimerEndsAtUtc\" IS NOT NULL AND \"RestTimerPausedRemainingSeconds\" IS NULL) OR (\"RestTimerEndsAtUtc\" IS NULL AND \"RestTimerPausedRemainingSeconds\" > 0)))");
        });

        builder.HasKey(session => session.Id);
        builder.Property(session => session.UserId).IsRequired();
        builder.Property(session => session.NameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(session => session.WorkoutDate).IsRequired();
        builder.Property(session => session.StartedAtUtc).IsRequired();
        builder.Property(session => session.Status).IsRequired();
        builder.Property(session => session.SessionFeeling);
        builder.Property(session => session.Version).IsRequired().IsConcurrencyToken();
        builder.Property(session => session.RestTimerDurationSeconds);
        builder.Property(session => session.RestTimerEndsAtUtc);
        builder.Property(session => session.RestTimerPausedRemainingSeconds);
        ConfigureAudit(builder);

        builder.HasIndex(session => new { session.UserId, session.Status, session.StartedAtUtc });
        builder.HasIndex(session => new { session.UserId, session.WorkoutDate });
        builder.HasIndex(session => session.OriginTemplateId);
        builder.HasIndex(session => session.UserId)
            .IsUnique()
            .HasFilter("\"Status\" = 0 AND \"IsDeleted\" = false");
        builder.HasOne<WorkoutTemplate>()
            .WithMany()
            .HasForeignKey(session => session.OriginTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(session => session.Exercises)
            .WithOne()
            .HasForeignKey(exercise => new { exercise.WorkoutSessionId, exercise.UserId })
            .HasPrincipalKey(session => new { session.Id, session.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(session => session.Exercises)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasAlternateKey(session => new { session.Id, session.UserId })
            .HasName("AK_WorkoutSessions_Id_UserId");
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.Property(session => session.CreatedAtUtc).IsRequired();
        builder.Property(session => session.UpdatedAtUtc).IsRequired();
        builder.Property(session => session.IsDeleted).IsRequired();
    }
}
