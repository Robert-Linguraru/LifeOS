namespace LifeOS.Core.DTOs.Finance;

public sealed class FinanceSummaryDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    public decimal TotalIncome { get; init; }

    public decimal TotalExpenses { get; init; }

    public decimal NetCashFlow => TotalIncome - TotalExpenses;

    public IReadOnlyList<FinanceCategorySummaryDto> ExpenseCategories { get; init; } =
        Array.Empty<FinanceCategorySummaryDto>();

    public IReadOnlyList<FinanceTransactionDto> Transactions { get; init; } =
        Array.Empty<FinanceTransactionDto>();
}
