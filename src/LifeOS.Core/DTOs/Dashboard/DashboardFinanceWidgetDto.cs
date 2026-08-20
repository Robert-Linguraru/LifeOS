namespace LifeOS.Core.DTOs.Dashboard;

public sealed class DashboardFinanceWidgetDto
{
    public bool HasTransactions { get; init; }

    public decimal TotalIncome { get; init; }

    public decimal TotalExpenses { get; init; }

    public decimal NetCashFlow => TotalIncome - TotalExpenses;

    public string? LargestExpenseCategory { get; init; }
}
