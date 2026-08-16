namespace LifeOS.Core.DTOs.Finance;

public sealed class FinanceYearSummaryDto
{
    public int Year { get; init; }

    public decimal TotalIncome { get; init; }

    public decimal TotalExpenses { get; init; }

    public decimal NetCashFlow => TotalIncome - TotalExpenses;
}
