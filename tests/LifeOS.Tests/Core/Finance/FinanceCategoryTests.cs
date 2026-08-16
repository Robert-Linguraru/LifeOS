using LifeOS.Core.Constants;
using LifeOS.Core.Entities;
using LifeOS.Core.Enums.Finance;

namespace LifeOS.Tests.Core.Finance;

public sealed class FinanceCategoryTests
{
    [Fact]
    public void DefaultCategories_ShouldHaveStableIdentitiesAndExpectedNames()
    {
        var definitions = FinanceCategoryDefaults.All;

        Assert.Equal(12, definitions.Count);
        Assert.Equal(
            FinanceCategoryDefaults.SalaryId,
            definitions.Single(item => item.Name == "Salary").Id);
        Assert.Equal(
            FinanceCategoryType.Expense,
            definitions.Single(item => item.Name == "Food").Type);
        Assert.Equal(
            FinanceCategoryType.Income,
            definitions.Single(item => item.Name == "Other Income").Type);
    }

    [Fact]
    public void Category_ShouldValidateTransactionTypeCompatibility()
    {
        var incomeCategory = new FinanceCategory(
            FinanceCategoryDefaults.SalaryId,
            "Salary",
            FinanceCategoryType.Income,
            1);

        Assert.True(incomeCategory.IsCompatibleWith(FinanceTransactionType.Income));
        Assert.False(incomeCategory.IsCompatibleWith(FinanceTransactionType.Expense));
    }

    [Fact]
    public void Category_ShouldRejectInvalidDefinition()
    {
        Assert.Throws<ArgumentException>(() => new FinanceCategory(
            Guid.Empty,
            "Salary",
            FinanceCategoryType.Income,
            1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new FinanceCategory(
            FinanceCategoryDefaults.SalaryId,
            "Salary",
            (FinanceCategoryType)99,
            1));
    }
}
