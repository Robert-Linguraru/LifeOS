using LifeOS.Core.Constants;
using LifeOS.Core.Abstractions;
using LifeOS.Core.Entities;
using LifeOS.Core.Time;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LifeOS.Tests.Infrastructure;

public sealed class FinancePersistenceModelTests
{
    [Fact]
    public void Model_ShouldMapFinanceCategoryReferenceData()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FinanceCategory));

        Assert.NotNull(entityType);
        Assert.Equal("FinanceCategories", entityType.GetTableName());
        Assert.Null(entityType.FindProperty("UserId"));

        Assert.Equal(
            FinanceConstants.CategoryNameMaxLength,
            entityType.FindProperty(nameof(FinanceCategory.Name))!.GetMaxLength());
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(FinanceCategory.Name)]));

        var designEntityType = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(FinanceCategory));

        Assert.NotNull(designEntityType);

        var constraints = designEntityType.GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToList();

        Assert.Contains("CK_FinanceCategories_Type", constraints);
        Assert.Contains("CK_FinanceCategories_SortOrder_NonNegative", constraints);
    }

    [Fact]
    public void Model_ShouldMapFinanceTransactionPersistenceContract()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FinanceTransaction));

        Assert.NotNull(entityType);
        Assert.Equal("FinanceTransactions", entityType.GetTableName());
        Assert.Equal("date", entityType.FindProperty(nameof(FinanceTransaction.TransactionDate))!.GetColumnType());
        Assert.Equal(18, entityType.FindProperty(nameof(FinanceTransaction.Amount))!.GetPrecision());
        Assert.Equal(2, entityType.FindProperty(nameof(FinanceTransaction.Amount))!.GetScale());
        Assert.Equal(
            FinanceConstants.DescriptionMaxLength,
            entityType.FindProperty(nameof(FinanceTransaction.Description))!.GetMaxLength());

        var designEntityType = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(FinanceTransaction));

        Assert.NotNull(designEntityType);

        var constraints = designEntityType.GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToList();

        Assert.Contains("CK_FinanceTransactions_Type", constraints);
        Assert.Contains("CK_FinanceTransactions_Amount_Positive", constraints);

        var foreignKey = Assert.Single(entityType.GetForeignKeys());
        Assert.Equal(typeof(FinanceCategory), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);

        var indexes = entityType.GetIndexes()
            .Select(index => index.Properties.Select(property => property.Name).ToArray())
            .ToList();

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(FinanceTransaction.UserId), nameof(FinanceTransaction.TransactionDate)]));
        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [nameof(FinanceTransaction.UserId), nameof(FinanceTransaction.CategoryId), nameof(FinanceTransaction.TransactionDate)]));
    }

    [Fact]
    public void Model_ShouldMapCurrencyPreferenceWithoutTransactionCurrency()
    {
        using var context = CreateContext();
        var settingsType = context.Model.FindEntityType(typeof(UserSettings));
        var transactionType = context.Model.FindEntityType(typeof(FinanceTransaction));

        Assert.NotNull(settingsType);
        Assert.NotNull(transactionType);
        Assert.Equal(
            FinanceConstants.CurrencyCodeMaxLength,
            settingsType.FindProperty(nameof(UserSettings.Currency))!.GetMaxLength());
        Assert.False(settingsType.FindProperty(nameof(UserSettings.Currency))!.IsNullable);
        Assert.Equal(
            FinanceConstants.DefaultCurrency,
            settingsType.FindProperty(nameof(UserSettings.Currency))!.GetDefaultValue());
        Assert.Null(transactionType.FindProperty("Currency"));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=lifeos_model_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(options, new TestDateTimeProvider());
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public DateOnly GetCurrentDate(string timeZoneId) =>
            DateOnly.FromDateTime(UtcNow.UtcDateTime);

        public bool IsValidTimeZone(string timeZoneId) => true;

        public LocalTimeConversionResult ConvertLocalToUtc(
            DateOnly localDate,
            TimeOnly localTime,
            string timeZoneId) =>
            LocalTimeConversionResult.Success(
                new DateTimeOffset(localDate.ToDateTime(localTime), TimeSpan.Zero));

        public DateTimeOffset ConvertUtcToLocal(
            DateTimeOffset utcInstant,
            string timeZoneId) => utcInstant;

        public IReadOnlyList<string> GetTimeZoneIds() => ["UTC"];
    }
}
