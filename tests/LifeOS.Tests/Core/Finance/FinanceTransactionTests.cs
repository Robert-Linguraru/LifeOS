using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Finance;

namespace LifeOS.Tests.Core.Finance;

public sealed class FinanceTransactionTests
{
    private static readonly Guid UserId =
        Guid.Parse("2ddf7f7b-8ed9-4db9-a4dc-27e2a4efc501");

    [Fact]
    public void IncomeTransaction_ShouldStoreApprovedFields()
    {
        var date = new DateOnly(2026, 9, 15);
        var transaction = new FinanceTransaction(
            UserId,
            date,
            FinanceTransactionType.Income,
            1250.50m,
            FinanceCategoryDefaults.SalaryId,
            "September salary");

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(UserId, transaction.UserId);
        Assert.Equal(date, transaction.TransactionDate);
        Assert.Equal(FinanceTransactionType.Income, transaction.Type);
        Assert.Equal(1250.50m, transaction.Amount);
        Assert.Equal(FinanceCategoryDefaults.SalaryId, transaction.CategoryId);
        Assert.Equal("September salary", transaction.Description);
        Assert.IsAssignableFrom<UserOwnedEntity>(transaction);
    }

    [Fact]
    public void ExpenseTransaction_ShouldNormalizeOptionalDescription()
    {
        var transaction = new FinanceTransaction(
            UserId,
            new DateOnly(2026, 9, 16),
            FinanceTransactionType.Expense,
            25m,
            FinanceCategoryDefaults.FoodId,
            "  Lunch  ");

        Assert.Equal("Lunch", transaction.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Transaction_ShouldRejectNonPositiveAmount(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinanceTransaction(
            UserId,
            new DateOnly(2026, 9, 15),
            FinanceTransactionType.Expense,
            amount,
            FinanceCategoryDefaults.FoodId,
            null));
    }

    [Fact]
    public void Transaction_ShouldRejectInvalidType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinanceTransaction(
            UserId,
            new DateOnly(2026, 9, 15),
            (FinanceTransactionType)99,
            25m,
            FinanceCategoryDefaults.FoodId,
            null));
    }

    [Fact]
    public void Transaction_ShouldRejectUnboundedDescription()
    {
        var description = new string('x', FinanceConstants.DescriptionMaxLength + 1);

        Assert.Throws<ArgumentException>(() => new FinanceTransaction(
            UserId,
            new DateOnly(2026, 9, 15),
            FinanceTransactionType.Expense,
            25m,
            FinanceCategoryDefaults.FoodId,
            description));
    }

    [Fact]
    public void Transaction_Update_ShouldReplaceFinancialFields()
    {
        var transaction = new FinanceTransaction(
            UserId,
            new DateOnly(2026, 9, 15),
            FinanceTransactionType.Expense,
            25m,
            FinanceCategoryDefaults.FoodId,
            null);

        transaction.Update(
            new DateOnly(2026, 9, 20),
            FinanceTransactionType.Income,
            75m,
            FinanceCategoryDefaults.OtherIncomeId,
            "Refund");

        Assert.Equal(new DateOnly(2026, 9, 20), transaction.TransactionDate);
        Assert.Equal(FinanceTransactionType.Income, transaction.Type);
        Assert.Equal(75m, transaction.Amount);
        Assert.Equal(FinanceCategoryDefaults.OtherIncomeId, transaction.CategoryId);
        Assert.Equal("Refund", transaction.Description);
    }
}
