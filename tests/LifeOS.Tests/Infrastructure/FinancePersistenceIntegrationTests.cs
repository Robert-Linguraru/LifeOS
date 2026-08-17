using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Finance;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LifeOS.Tests.Infrastructure;

public sealed class FinancePersistenceIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public FinancePersistenceIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_ShouldSeedCoreFinanceCategories()
    {
        await using var context = _fixture.CreateDbContext();

        var categories = await context.FinanceCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ToListAsync();

        Assert.Equal(FinanceCategoryDefaults.All.Count, categories.Count);

        foreach (var definition in FinanceCategoryDefaults.All)
        {
            var category = Assert.Single(
                categories,
                item => item.Id == definition.Id);

            Assert.Equal(definition.Name, category.Name);
            Assert.Equal(definition.Type, category.Type);
            Assert.Equal(definition.SortOrder, category.SortOrder);
            Assert.True(category.IsActive);
            Assert.False(category.IsDeleted);
        }
    }

    [Fact]
    public async Task FinanceTransaction_ShouldPersistAndSoftDelete()
    {
        var transaction = new FinanceTransaction(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 15),
            FinanceTransactionType.Expense,
            42.75m,
            FinanceCategoryDefaults.FoodId,
            "Lunch");

        await using (var context = _fixture.CreateDbContext())
        {
            context.FinanceTransactions.Add(transaction);
            await context.SaveChangesAsync();
        }

        await using (var verificationContext = _fixture.CreateDbContext())
        {
            var persisted = await verificationContext.FinanceTransactions
                .SingleAsync(item => item.Id == transaction.Id);

            Assert.Equal(transaction.UserId, persisted.UserId);
            Assert.Equal(transaction.TransactionDate, persisted.TransactionDate);
            Assert.Equal(transaction.Type, persisted.Type);
            Assert.Equal(transaction.Amount, persisted.Amount);
            Assert.Equal(transaction.CategoryId, persisted.CategoryId);
            Assert.Equal(transaction.Description, persisted.Description);

            verificationContext.FinanceTransactions.Remove(persisted);
            await verificationContext.SaveChangesAsync();
        }

        await using var filteredContext = _fixture.CreateDbContext();
        Assert.Null(await filteredContext.FinanceTransactions
            .SingleOrDefaultAsync(item => item.Id == transaction.Id));

        var deleted = await filteredContext.FinanceTransactions
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == transaction.Id);

        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAtUtc);
    }

    [Fact]
    public async Task PostgreSql_ShouldRejectInvalidAmount()
    {
        var transactionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(
            2026,
            9,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);

        await using var context = _fixture.CreateDbContext(timestamp);

        await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "FinanceTransactions"
                    ("Id", "UserId", "TransactionDate", "Type", "Amount", "CategoryId",
                     "Description", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted", "DeletedAtUtc")
                VALUES
                    ({transactionId}, {userId}, {new DateOnly(2026, 9, 15)}, {0}, {-1m},
                     {FinanceCategoryDefaults.FoodId}, {null}, {timestamp}, {timestamp}, {false}, {null})
                """));
    }

    [Fact]
    public async Task PostgreSql_ShouldRejectMissingCategoryReference()
    {
        var transactionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(
            2026,
            9,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);

        await using var context = _fixture.CreateDbContext(timestamp);

        await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "FinanceTransactions"
                    ("Id", "UserId", "TransactionDate", "Type", "Amount", "CategoryId",
                     "Description", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted", "DeletedAtUtc")
                VALUES
                    ({transactionId}, {userId}, {new DateOnly(2026, 9, 15)}, {1}, {10m},
                     {Guid.NewGuid()}, {null}, {timestamp}, {timestamp}, {false}, {null})
                """));
    }

    [Fact]
    public async Task ExistingUserSettingsRow_ShouldReceiveApplicationCurrencyDefault()
    {
        var settingsId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(
            2026,
            9,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);

        await using var context = _fixture.CreateDbContext(timestamp);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UserSettings"
                ("Id", "TimeZoneId", "TimeZoneConfiguredAtUtc", "CreatedAtUtc",
                 "UpdatedAtUtc", "IsDeleted", "DeletedAtUtc", "UserId")
            VALUES
                ({settingsId}, {"UTC"}, {null}, {timestamp}, {timestamp}, {false}, {null}, {userId})
            """);

        var settings = await context.UserSettings
            .AsNoTracking()
            .SingleAsync(item => item.Id == settingsId);

        Assert.Equal(FinanceConstants.DefaultCurrency, settings.Currency);
    }
}
