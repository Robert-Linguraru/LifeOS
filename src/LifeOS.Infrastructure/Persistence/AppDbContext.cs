using System.Reflection.Emit;
using LifeOS.Core.Abstractions;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LifeOS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{

    private readonly IDateTimeProvider _dateTimeProvider;
    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }
    public DbSet<UserSettings> UserSettings =>
    Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.Entity<UserSettings>().HasQueryFilter(settings => !settings.IsDeleted);
    }

    public override int SaveChanges()
    {
        ApplyEntityLifecycleRules();

        return base.SaveChanges();
    }
    public override Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        ApplyEntityLifecycleRules();

        return base.SaveChangesAsync(cancellationToken);
    }
    private void ApplyEntityLifecycleRules()
    {
        ChangeTracker.DetectChanges();

        var utcNow = _dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            ApplyEntityLifecycleRule(entry, utcNow);
        }
    }
    private static void ApplyEntityLifecycleRule(
    EntityEntry<BaseEntity> entry,
    DateTimeOffset utcNow)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                ApplyCreatedAuditValues(entry, utcNow);
                break;

            case EntityState.Modified:
                ApplyUpdatedAuditValues(entry, utcNow);
                break;

            case EntityState.Deleted:
                ApplySoftDeleteValues(entry, utcNow);
                break;
        }
    }
    private static void ApplyCreatedAuditValues(
    EntityEntry<BaseEntity> entry,
    DateTimeOffset utcNow)
    {
        entry.Entity.CreatedAtUtc = utcNow;
        entry.Entity.UpdatedAtUtc = utcNow;
        entry.Entity.IsDeleted = false;
        entry.Entity.DeletedAtUtc = null;
    }
    private static void ApplyUpdatedAuditValues(
    EntityEntry<BaseEntity> entry,
    DateTimeOffset utcNow)
    {
        entry.Entity.UpdatedAtUtc = utcNow;

        entry.Property(entity => entity.CreatedAtUtc)
            .IsModified = false;

        entry.Property(entity => entity.IsDeleted)
            .IsModified = false;

        entry.Property(entity => entity.DeletedAtUtc)
            .IsModified = false;
    }
    private static void ApplySoftDeleteValues(
    EntityEntry<BaseEntity> entry,
    DateTimeOffset utcNow)
    {
        entry.State = EntityState.Modified;

        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAtUtc = utcNow;
        entry.Entity.UpdatedAtUtc = utcNow;

        entry.Property(entity => entity.CreatedAtUtc)
            .IsModified = false;
    }
}