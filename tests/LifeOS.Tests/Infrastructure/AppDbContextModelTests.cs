using LifeOS.Core.Abstractions;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Time;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LifeOS.Tests.Infrastructure.Persistence;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void Model_ShouldMapTaskItemToTasksTable()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(TaskItem));

        Assert.NotNull(entityType);
        Assert.Equal("Tasks", entityType.GetTableName());
    }

    [Fact]
    public void Model_ShouldConfigureTaskTitleAndDescriptionLengths()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(TaskItem));

        Assert.NotNull(entityType);

        var titleProperty = entityType.FindProperty(nameof(TaskItem.Title));
        var descriptionProperty = entityType.FindProperty(nameof(TaskItem.Description));

        Assert.NotNull(titleProperty);
        Assert.NotNull(descriptionProperty);

        Assert.Equal(TaskConstants.TitleMaxLength, titleProperty.GetMaxLength());
        Assert.Equal(
            TaskConstants.DescriptionMaxLength,
            descriptionProperty.GetMaxLength());
    }

    [Fact]
    public void Model_ShouldConfigureTaskIndexes()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(TaskItem));

        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes()
            .Select(index => index.Properties
                .Select(property => property.Name)
                .ToArray())
            .ToList();

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(TaskItem.UserId), nameof(TaskItem.Status)]));

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(TaskItem.UserId), nameof(TaskItem.DueDate)]));

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(TaskItem.UserId), nameof(TaskItem.IsDeleted)]));
    }

    [Fact]
    public void Model_ShouldConfigureUserSettingsUserIdIndexAsUnique()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserSettings));

        Assert.NotNull(entityType);

        var index = entityType.GetIndexes()
            .Single(index => index.Properties
                .Select(property => property.Name)
                .SequenceEqual([nameof(UserSettings.UserId)]));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Model_ShouldMapOptionalTimeZoneConfirmationWithoutAnIndex()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserSettings));

        Assert.NotNull(entityType);

        var property = entityType.FindProperty(
            nameof(UserSettings.TimeZoneConfiguredAtUtc));

        Assert.NotNull(property);
        Assert.True(property.IsNullable);
        Assert.Equal("timestamp with time zone", property.GetColumnType());
        Assert.DoesNotContain(
            entityType.GetIndexes(),
            index => index.Properties.Any(item =>
                item.Name == nameof(UserSettings.TimeZoneConfiguredAtUtc)));
    }

    [Fact]
    public void Model_ShouldMapHabitWithDocumentedProperties()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Habit));

        Assert.NotNull(entityType);
        Assert.Equal("Habits", entityType.GetTableName());

        var name = entityType.FindProperty(nameof(Habit.Name));
        var description = entityType.FindProperty(nameof(Habit.Description));
        var targetQuantity = entityType.FindProperty(nameof(Habit.TargetQuantity));
        var targetUnit = entityType.FindProperty(nameof(Habit.TargetUnit));

        Assert.NotNull(name);
        Assert.NotNull(description);
        Assert.NotNull(targetQuantity);
        Assert.NotNull(targetUnit);

        Assert.True(name.IsNullable == false);
        Assert.Equal(HabitConstants.NameMaxLength, name.GetMaxLength());
        Assert.True(description.IsNullable);
        Assert.Equal(
            HabitConstants.DescriptionMaxLength,
            description.GetMaxLength());
        Assert.True(targetQuantity.IsNullable);
        Assert.Equal(18, targetQuantity.GetPrecision());
        Assert.Equal(2, targetQuantity.GetScale());
        Assert.True(targetUnit.IsNullable);
        Assert.Equal(
            HabitConstants.TargetUnitMaxLength,
            targetUnit.GetMaxLength());
    }

    [Fact]
    public void Model_ShouldConfigureHabitIndexesWithoutNameUniqueness()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Habit));

        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes().ToList();

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(Habit.UserId), nameof(Habit.IsActive)]));

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(Habit.UserId), nameof(Habit.IsDeleted)]));

        Assert.DoesNotContain(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Habit.UserId), nameof(Habit.Name)]));
    }

    [Fact]
    public void Model_ShouldMapHabitLogWithUniqueCompletionIndexAndSupportingIndexes()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(HabitLog));

        Assert.NotNull(entityType);
        Assert.Equal("HabitLogs", entityType.GetTableName());

        var indexes = entityType.GetIndexes().ToList();

        var uniqueCompletionIndex = indexes.Single(
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [
                        nameof(HabitLog.UserId),
                        nameof(HabitLog.HabitId),
                        nameof(HabitLog.CompletionDate)
                    ]));

        Assert.True(uniqueCompletionIndex.IsUnique);

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(HabitLog.UserId), nameof(HabitLog.CompletionDate)]));

        Assert.Contains(
            indexes,
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [nameof(HabitLog.HabitId), nameof(HabitLog.CompletionDate)]));
    }

    [Fact]
    public void Model_ShouldConfigureHabitLogForeignKeyWithoutCascadeDelete()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(HabitLog));

        Assert.NotNull(entityType);

        var foreignKey = entityType.GetForeignKeys().Single(
            key => key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(HabitLog.HabitId)]));

        Assert.Equal(typeof(Habit), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Model_ShouldApplySoftDeleteFilterToAllBaseEntities()
    {
        using var context = CreateContext();

        var baseEntityTypes = context.Model
            .GetEntityTypes()
            .Where(entityType =>
                typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            .ToList();

        Assert.NotEmpty(baseEntityTypes);

        foreach (var entityType in baseEntityTypes)
        {
            Assert.NotNull(entityType.GetQueryFilter());
        }
    }

    [Fact]
    public void Model_ShouldMapXpTransactionWithAppendOnlyLedgerContract()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(XpTransaction));

        Assert.NotNull(entityType);
        Assert.Equal("XpTransactions", entityType.GetTableName());
        Assert.Equal(
            nameof(XpTransaction.Id),
            entityType.FindPrimaryKey()!.Properties.Single().Name);

        Assert.False(entityType.FindProperty(nameof(XpTransaction.UserId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(XpTransaction.Source))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(XpTransaction.SourceType))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(XpTransaction.SourceEntityId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(XpTransaction.XpAmount))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(XpTransaction.OccurredAtUtc))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(XpTransaction.BusinessDate))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(XpTransaction.IdempotencyKey))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(XpTransaction.Notes))!.IsNullable);
        Assert.Equal(
            200,
            entityType.FindProperty(nameof(XpTransaction.IdempotencyKey))!.GetMaxLength());
        Assert.Equal(
            500,
            entityType.FindProperty(nameof(XpTransaction.Notes))!.GetMaxLength());
        Assert.Equal(
            "date",
            entityType.FindProperty(nameof(XpTransaction.BusinessDate))!.GetColumnType());
        Assert.Equal(
            "timestamp with time zone",
            entityType.FindProperty(nameof(XpTransaction.OccurredAtUtc))!.GetColumnType());

        var indexes = entityType.GetIndexes().ToList();
        var idempotencyIndex = indexes.Single(index => index.Properties
            .Select(property => property.Name)
            .SequenceEqual([
                nameof(XpTransaction.UserId),
                nameof(XpTransaction.IdempotencyKey)]));

        Assert.True(idempotencyIndex.IsUnique);
        Assert.Equal(
            "\"IdempotencyKey\" IS NOT NULL",
            idempotencyIndex.GetFilter());
        Assert.Contains(indexes, index => index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(XpTransaction.UserId), nameof(XpTransaction.OccurredAtUtc)]));
        Assert.Contains(indexes, index => index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(XpTransaction.UserId), nameof(XpTransaction.BusinessDate)]));
        Assert.Contains(indexes, index => index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(XpTransaction.UserId), nameof(XpTransaction.Source)]));

        Assert.Empty(entityType.GetForeignKeys());
    }

    [Fact]
    public void Model_ShouldMapUserProgressionWithConcurrencyAndConstraints()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserProgression));

        Assert.NotNull(entityType);
        Assert.Equal("UserProgressions", entityType.GetTableName());
        Assert.Equal(
            nameof(UserProgression.Id),
            entityType.FindPrimaryKey()!.Properties.Single().Name);

        var userIdIndex = entityType.GetIndexes().Single(index => index.Properties
            .Select(property => property.Name)
            .SequenceEqual([nameof(UserProgression.UserId)]));
        Assert.True(userIdIndex.IsUnique);

        Assert.Equal(
            typeof(long),
            entityType.FindProperty(nameof(UserProgression.TotalLifetimeXp))!.ClrType);
        Assert.Equal(
            "bigint",
            entityType.FindProperty(nameof(UserProgression.TotalLifetimeXp))!.GetColumnType());
        Assert.Equal(
            "bigint",
            entityType.FindProperty(nameof(UserProgression.Version))!.GetColumnType());
        Assert.False(entityType.FindProperty(nameof(UserProgression.Version))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(UserProgression.Version))!.IsConcurrencyToken);
        Assert.True(entityType.FindProperty(nameof(UserProgression.DailyQuestXpDate))!.IsNullable);
        Assert.Equal(
            "date",
            entityType.FindProperty(nameof(UserProgression.DailyQuestXpDate))!.GetColumnType());

        var designEntityType = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(UserProgression));

        Assert.NotNull(designEntityType);

        var constraints = designEntityType.GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToList();

        Assert.Contains("CK_UserProgressions_TotalLifetimeXp_NonNegative", constraints);
        Assert.Contains("CK_UserProgressions_CurrentLevel_AtLeastOne", constraints);
        Assert.Contains("CK_UserProgressions_DailyQuestXpToday_InRange", constraints);
        Assert.Contains("CK_UserProgressions_Version_NonNegative", constraints);
    }

    [Fact]
    public void SaveChanges_ShouldRejectModifiedXpTransactionBeforePersistence()
    {
        using var context = CreateContext();
        var transaction = new XpTransaction { UserId = Guid.NewGuid() };

        context.Attach(transaction);
        context.Entry(transaction).State = EntityState.Modified;

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.SaveChanges());

        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityState.Modified, context.Entry(transaction).State);
    }

    [Fact]
    public void SaveChanges_ShouldRejectDeletedXpTransactionBeforeSoftDeleteConversion()
    {
        using var context = CreateContext();
        var transaction = new XpTransaction { UserId = Guid.NewGuid() };

        context.Attach(transaction);
        context.Entry(transaction).State = EntityState.Deleted;

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.SaveChanges());

        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityState.Deleted, context.Entry(transaction).State);
        Assert.False(transaction.IsDeleted);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=lifeos_model_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(
            options,
            new TestDateTimeProvider());
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public DateOnly GetCurrentDate(string timeZoneId)
        {
            return DateOnly.FromDateTime(UtcNow.UtcDateTime);
        }

        public bool IsValidTimeZone(string timeZoneId)
        {
            return true;
        }

        public LocalTimeConversionResult ConvertLocalToUtc(
            DateOnly localDate,
            TimeOnly localTime,
            string timeZoneId)
        {
            return LocalTimeConversionResult.Success(
                new DateTimeOffset(
                    localDate.ToDateTime(localTime),
                    TimeSpan.Zero));
        }

        public DateTimeOffset ConvertUtcToLocal(
            DateTimeOffset utcInstant,
            string timeZoneId)
        {
            return utcInstant;
        }

        public IReadOnlyList<string> GetTimeZoneIds()
        {
            return ["UTC"];
        }
    }
}