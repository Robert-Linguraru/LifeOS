using LifeOS.Core.Abstractions;
using LifeOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace LifeOS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{

    private readonly IDateTimeProvider _dateTimeProvider;
    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<XpTransaction> XpTransactions => Set<XpTransaction>();
    public DbSet<UserProgression> UserProgressions => Set<UserProgression>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplySoftDeleteQueryFilters(builder);
    }

    public override int SaveChanges()
    {
        ApplyEntityLifecycleRules();

        return base.SaveChanges();
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");

            var isDeletedProperty = Expression.Property(
                parameter,
                nameof(BaseEntity.IsDeleted));

            var notDeleted = Expression.Not(isDeletedProperty);

            var filter = Expression.Lambda(notDeleted, parameter);

            entityType.SetQueryFilter(filter);
        }
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

        RejectXpTransactionMutations();

        var utcNow = _dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            ApplyEntityLifecycleRule(entry, utcNow);
        }
    }

    private void RejectXpTransactionMutations()
    {
        var mutatedTransaction = ChangeTracker
            .Entries<XpTransaction>()
            .FirstOrDefault(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted);

        if (mutatedTransaction is not null)
        {
            throw new InvalidOperationException(
                "Existing XP transactions are append-only and cannot be modified or deleted.");
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