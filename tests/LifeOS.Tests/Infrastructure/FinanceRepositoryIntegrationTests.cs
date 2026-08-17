using LifeOS.Core.Abstractions.Finance;
using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Finance;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Infrastructure;

public sealed class FinanceRepositoryIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public FinanceRepositoryIntegrationTests(
        PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Categories_ShouldReturnActiveSeededCategoriesInDeterministicOrder()
    {
        var repository = CreateRepositoryAsync();

        var categories = await repository.GetCategoriesAsync();

        Assert.Equal(
            FinanceCategoryDefaults.All.OrderBy(category => category.SortOrder)
                .Select(category => category.Id),
            categories.Select(category => category.Id));
        Assert.All(categories, category => Assert.True(category.IsActive));

        var category = await repository.GetCategoryByIdAsync(
            FinanceCategoryDefaults.FoodId);

        Assert.NotNull(category);
        Assert.Equal("Food", category.Name);
    }

    [Fact]
    public async Task TransactionLookupAndPeriodQueries_ShouldBeUserScopedAndBounded()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepositoryAsync();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var previousMonth = CreateTransaction(
            userId, new DateOnly(2026, 8, 31), 1);
        var firstMonth = CreateTransaction(
            userId, new DateOnly(2026, 9, 1), 2);
        var lastMonth = CreateTransaction(
            userId, new DateOnly(2026, 9, 30), 3);
        var nextMonth = CreateTransaction(
            userId, new DateOnly(2026, 10, 1), 4);
        var otherUser = CreateTransaction(
            otherUserId, new DateOnly(2026, 9, 15), 5);
        var priorYear = CreateTransaction(
            userId, new DateOnly(2025, 12, 31), 6);
        var newYear = CreateTransaction(
            userId, new DateOnly(2027, 1, 1), 7);

        foreach (var transaction in new[]
        {
            previousMonth, firstMonth, lastMonth, nextMonth, otherUser,
            priorYear, newYear
        })
        {
            await repository.AddTransactionAsync(transaction);
        }

        var month = await repository.GetTransactionsByMonthAsync(
            userId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 1));
        var year = await repository.GetTransactionsByYearAsync(
            userId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1));

        Assert.Equal(
            new[] { lastMonth.Id, firstMonth.Id },
            month.Select(transaction => transaction.Id));
        Assert.Equal(
            new[] { nextMonth.Id, lastMonth.Id, firstMonth.Id, previousMonth.Id },
            year.Select(transaction => transaction.Id));
        Assert.Null(await repository.GetTransactionByIdAsync(userId, otherUser.Id));
    }

    [Fact]
    public async Task Update_ShouldOnlyMutateTheOwningUsersTransaction()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepositoryAsync();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var transaction = CreateTransaction(
            ownerId, new DateOnly(2026, 9, 10), 10);

        await repository.AddTransactionAsync(transaction);

        transaction.Update(
            new DateOnly(2026, 9, 11),
            FinanceTransactionType.Income,
            25,
            FinanceCategoryDefaults.SalaryId,
            "Updated");
        await repository.UpdateTransactionAsync(transaction);

        var updated = await repository.GetTransactionByIdAsync(ownerId, transaction.Id);
        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(2026, 9, 11), updated.TransactionDate);
        Assert.Equal(25, updated.Amount);
        Assert.Equal("Updated", updated.Description);

        var originalDescription = updated.Description;
        var crossUser = new FinanceTransaction(
            otherUserId,
            new DateOnly(2026, 9, 12),
            FinanceTransactionType.Income,
            99,
            FinanceCategoryDefaults.SalaryId,
            "Must not update");
        crossUser.Id = transaction.Id;

        await repository.UpdateTransactionAsync(crossUser);

        var unchanged = await repository.GetTransactionByIdAsync(ownerId, transaction.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(originalDescription, unchanged.Description);
        Assert.Equal(ownerId, unchanged.UserId);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteAndExcludeTransactionFromAllNormalQueries()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepositoryAsync();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var transaction = CreateTransaction(
            ownerId, new DateOnly(2026, 9, 20), 10);
        var otherTransaction = CreateTransaction(
            otherUserId, new DateOnly(2026, 9, 20), 20);

        await repository.AddTransactionAsync(transaction);
        await repository.AddTransactionAsync(otherTransaction);

        await repository.DeleteTransactionAsync(otherUserId, transaction.Id);

        Assert.NotNull(await repository.GetTransactionByIdAsync(ownerId, transaction.Id));
        Assert.Contains(
            transaction.Id,
            (await repository.GetTransactionsByMonthAsync(
                ownerId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)))
            .Select(item => item.Id));

        await repository.DeleteTransactionAsync(ownerId, transaction.Id);

        Assert.Null(await repository.GetTransactionByIdAsync(ownerId, transaction.Id));
        Assert.DoesNotContain(
            transaction.Id,
            (await repository.GetTransactionsByMonthAsync(
                ownerId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)))
            .Select(item => item.Id));
        Assert.DoesNotContain(
            transaction.Id,
            (await repository.GetTransactionsByYearAsync(
                ownerId, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1)))
            .Select(item => item.Id));

        await using var context = _fixture.CreateDbContext();
        var deleted = await context.FinanceTransactions
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == transaction.Id);
        Assert.True(deleted.IsDeleted);
    }

    private IFinanceRepository CreateRepositoryAsync()
    {
        return new FinanceRepository(_fixture.CreateDbContextFactory());
    }

    private static FinanceTransaction CreateTransaction(
        Guid userId,
        DateOnly date,
        decimal amount)
    {
        return new FinanceTransaction(
            userId,
            date,
            FinanceTransactionType.Expense,
            amount,
            FinanceCategoryDefaults.FoodId,
            $"Transaction {amount}");
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await context.FinanceTransactions
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
    }
}
