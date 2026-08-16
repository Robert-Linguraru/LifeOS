using LifeOS.Core.DTOs.Finance;

namespace LifeOS.Tests.Core.Finance;

public sealed class FinanceProjectionTests
{
    [Fact]
    public void MonthlySummary_NetCashFlow_ShouldBeIncomeMinusExpenses()
    {
        var summary = new FinanceSummaryDto
        {
            Year = 2026,
            Month = 9,
            TotalIncome = 1500m,
            TotalExpenses = 425.75m
        };

        Assert.Equal(1074.25m, summary.NetCashFlow);
    }

    [Fact]
    public void YearSummary_NetCashFlow_ShouldBeIncomeMinusExpenses()
    {
        var summary = new FinanceYearSummaryDto
        {
            Year = 2026,
            TotalIncome = 12000m,
            TotalExpenses = 8750m
        };

        Assert.Equal(3250m, summary.NetCashFlow);
    }
}
