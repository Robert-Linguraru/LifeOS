namespace LifeOS.Core.DTOs.Finance;

public sealed class FinanceCategorySummaryDto
{
    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public decimal TotalExpenses { get; init; }
}
